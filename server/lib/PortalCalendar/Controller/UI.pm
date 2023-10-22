# Controller
package PortalCalendar::Controller::UI;
use Mojo::Base 'PortalCalendar::Controller';

use Mojo::Util qw(url_escape b64_decode b64_encode);
use Mojo::JSON qw(decode_json encode_json);
use Try::Tiny;

use PortalCalendar::Integration::iCal;
use PortalCalendar::Integration::OpenWeather;
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

        display  => undef,
        displays => [ $displays->all ],
    );
}

sub home {
    my $self = shift;

    my $last_contact_ago;
    my $last_visit_dt;

    if (my $last_visit_raw = $self->get_config('_last_visit')) {
        $last_visit_dt = DateTime::Format::ISO8601->parse_datetime($last_visit_raw);

        $last_contact_ago = DateTime->now()->subtract_datetime($last_visit_dt);
    }

    return $self->render(
        template => 'index',
        format   => 'html',
        nav_link => 'home',

        display    => $self->display,
        config_obj => $self->config_obj,

        waiting_tasks    => $self->app->minion->jobs({ states => [ 'inactive', 'active' ] })->total,
        last_contact_ago => $last_contact_ago,
        last_visit_dt    => $last_visit_dt,
        last_voltage     => $self->get_calculated_voltage          // undef,
        battery_percent  => $self->calculate_battery_percent       // undef,
        last_voltage_raw => $self->get_config('_last_voltage_raw') // undef,
    );
}

# side by side comparison
sub test {
    my $self = shift;
    return $self->render(
        template => 'test',
        format   => 'html',
        nav_link => 'compare',

        display => $self->display,
    );
}

sub config_ui_show {
    my $self = shift;

    my $values = {};
    foreach my $name (@{ $self->config_obj->parameters }) {
        my $value = $self->get_config($name);
        $values->{$name} = $value;
    }

    return $self->render(
        template => 'config_ui',
        format   => 'html',
        values   => $values,

        last_voltage     => $self->get_calculated_voltage,
        last_voltage_raw => $self->get_config('_last_voltage_raw'),
        nav_link         => 'config_ui',

        display => $self->display,
    );
}

sub config_ui_save {
    my $self = shift;

    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);

    foreach my $name (@{ $self->config_obj->parameters }) {
        my $value = $self->req->param($name);
        $self->set_config($name, $value);
    }

    $self->flash(message => "Parameters saved.");

    $util->update_mqtt('min_voltage',           $self->get_config('min_voltage'));
    $util->update_mqtt('max_voltage',           $self->get_config('max_voltage'));
    $util->update_mqtt('alert_voltage',         $self->get_config('alert_voltage'));
    $util->update_mqtt('voltage_divider_ratio', $self->get_config('voltage_divider_ratio'));
    $util->update_mqtt('sleep_time',            $self->get_config('sleep_time'));

    $self->app->log->debug("Clearing database cache");
    PortalCalendar::Integration::iCal->new(app => $self->app)->clear_db_cache;
    PortalCalendar::Integration::OpenWeather->new(app => $self->app)->clear_db_cache;
    PortalCalendar::Integration::Google::Fit->new(app => $self->app)->clear_db_cache;

    $self->app->enqueue_task_only_once('generate_image');
    $self->redirect_to('/config_ui/' . $self->display->id);
}

# main HTML page with calendar (either for current or for specific date)
sub calendar_html_default_date {
    my $self = shift;
    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);
    return $util->html_for_date(DateTime->now());
}

sub calendar_html_specific_date {
    my $self = shift;
    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);
    my $dt   = DateTime::Format::Strptime->new(pattern => '%Y-%m-%d')->parse_datetime($self->stash('date'));
    return $util->html_for_date($dt);
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

        display => $self->display,

        # page-specific variables
        a_token    => $self->get_config('_googlefit_access_token'),
        r_token    => $self->get_config('_googlefit_refresh_token'),
        token_json => '?',                                             # DDP::np($token_json),
    );
}

1;