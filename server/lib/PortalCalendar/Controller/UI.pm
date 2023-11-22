# Controller
package PortalCalendar::Controller::UI;
use Mojo::Base 'PortalCalendar::Controller';

use Mojo::Util qw(url_escape b64_decode b64_encode);
use Mojo::JSON qw(decode_json encode_json);
use Try::Tiny;

use PortalCalendar::Integration::iCal;
use PortalCalendar::Integration::Weather::OpenWeather;
use PortalCalendar::Integration::Weather::MetNo;
use PortalCalendar::Integration::Google::Fit;

has display => sub {
    my $self = shift;
    return $self->get_display_by_id($self->stash('display_number'));
};

sub select_display {
    my $self = shift;

    my $displays = $self->app->schema->resultset('Display')->search({}, { -order_by => ['id'] });

    return $self->render(
        template => 'display_list',
        format   => 'html',
        nav_link => 'index',

        display    => undef,
        config_obj => undef,
        displays   => [ $displays->all ],
    );
}

sub home {
    my $self = shift;

    return $self->render(
        template => 'index',
        format   => 'html',
        nav_link => 'home',

        display    => $self->display,
        config_obj => $self->config_obj,
    );
}

# side by side comparison
sub test {
    my $self = shift;
    return $self->render(
        template => 'test',
        format   => 'html',
        nav_link => 'compare',

        display    => $self->display,
        config_obj => $self->config_obj,
    );
}

sub config_ui_show {
    my $self = shift;

    my $values = {};
    foreach my $name (@{ $self->config_obj->parameters }) {
        my $value = $self->get_config($name);
        $values->{$name} = $value;
    }

    my @template_files = $self->app->home->child('templates/calendar_themes')->list->map(sub { $_->basename })->each;

    return $self->render(
        template => 'config_ui',
        format   => 'html',
        values   => $values,

        template_names => [ map { s/\.html\.ep$//; $_ } @template_files ],
        current_theme  => $self->display->get_config('theme'),

        last_voltage     => $self->display->voltage,
        last_voltage_raw => $self->get_config('_last_voltage_raw'),
        nav_link         => 'config_ui',

        display    => $self->display,
        config_obj => $self->config_obj,
    );
}

sub config_ui_save {
    my $self = shift;

    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);

    # Generic config parameters
    foreach my $name (@{ $self->config_obj->parameters }) {
        my $value = $self->req->param($name);
        $self->set_config($name, $value // '');
    }

    # Parameters stored in the 'displays' table
    $self->display->name($self->req->param('display_name'));
    $self->display->rotation($self->req->param('display_rotation'));
    $self->display->gamma($self->req->param('display_gamma'));
    $self->display->border_top($self->req->param('display_border_top'));
    $self->display->border_right($self->req->param('display_border_right'));
    $self->display->border_bottom($self->req->param('display_border_bottom'));
    $self->display->border_left($self->req->param('display_border_left'));
    $self->display->update;

    $self->flash(message => "Parameters saved.");

    # $self->app->log->debug("Clearing database cache");
    # PortalCalendar::Integration::iCal->new(app => $self->app)->clear_db_cache;
    # PortalCalendar::Integration::Weather::OpenWeather->new(app => $self->app)->clear_db_cache;
    # PortalCalendar::Integration::Weather::MetNo->new(app => $self->app)->clear_db_cache;
    # PortalCalendar::Integration::Google::Fit->new(app => $self->app)->clear_db_cache;

    $self->app->enqueue_task_only_once('generate_image');
    $self->redirect_to('/config_ui/' . $self->display->id);
}

sub config_ui_theme_show {
    my $self = shift;

    my $theme = $self->req->param('theme');
    $theme =~ s/[^a-zA-Z0-9_\-\ ]//g;

    my $values = {};
    foreach my $name (@{ $self->config_obj->parameters }) {
        my $value = $self->get_config($name);
        $values->{$name} = $value;
    }

    my $result;
    if ($theme eq '') {
        $result = $self->render(text => '');
    } else {
        try {
            $result = $self->render(
                template => "calendar_themes/configs/$theme",
                format   => 'html',
                values   => $values,

                display    => $self->display,
                config_obj => $self->config_obj,
            );
        } catch {
            $self->app->log->error("Error rendering theme [$theme]: $_");
            $result = $self->render(text => '');
        };
    }

    return $result;
}

# main HTML page with calendar (either for current or for specific date)
sub calendar_html_default_date {
    my $self = shift;
    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);
    return $util->html_for_date(
        DateTime->now(time_zone => $self->get_config('timezone')),
        {
            preview_colors => ($self->req->param('preview_colors') // 0)
        }
    );
}

sub calendar_html_specific_date {
    my $self = shift;
    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);
    my $dt   = DateTime::Format::Strptime->new(pattern => '%Y-%m-%d')->parse_datetime($self->stash('date'))->set_time_zone($self->get_config('timezone'));
    return $util->html_for_date(
        $dt,
        {
            preview_colors => ($self->req->param('preview_colors') // 0)
        }
    );
}

sub googlefit_redirect {
    my $self = shift;

    # see https://developers.google.com/identity/protocols/oauth2/web-server#httprest_1
    my $goauth = PortalCalendar::Integration::Google->new(app => $self->app);
    my $url    = $goauth->google_oauth2_auth_url .
        #
        "?client_id=" . $self->get_config('googlefit_client_id') .
        #
        "&access_type=offline&response_type=code&scope=" . $goauth->googlefit_oauth2_scope .
        #
        "&state=" . url_escape(b64_encode(encode_json({ display_number => $self->display->id }))) .
        #
        "&include_granted_scopes=true&redirect_uri=" . url_escape($self->get_config('googlefit_auth_callback'));

    $self->log->info("Redirecting to [$url]");
    $self->redirect_to($url);
}

sub googlefit_success {
    my $self = shift;

    # Read access token from session
    my $a_token = $self->get_config('_googlefit_access_token') or die "No access token!";

    # my $token_json = decode_json($self->get_config('_googlefit_token_json'));
    return $self->render(
        template => 'auth_success',
        format   => 'html',
        nav_link => 'config_ui',

        display    => $self->display,
        config_obj => $self->config_obj,

        # page-specific variables
        a_token    => $self->get_config('_googlefit_access_token'),
        r_token    => $self->get_config('_googlefit_refresh_token'),
        token_json => '?',                                             # DDP::np($token_json),
    );
}

1;