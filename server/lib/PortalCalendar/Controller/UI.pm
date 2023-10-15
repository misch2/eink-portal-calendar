# Controller
package PortalCalendar::Controller::UI;
use Mojo::Base 'PortalCalendar::Controller';

use Mojo::Util qw(url_escape b64_decode b64_encode);
use Mojo::JSON qw(decode_json encode_json);
use Try::Tiny;

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
        nav_link => 'index',

        display    => $self->display,
        config_obj => $self->config_obj,

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

        last_voltage     => ($self->get_calculated_voltage          // '(unknown)'),
        last_voltage_raw => ($self->get_config('_last_voltage_raw') // '(unknown)'),
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

    $self->app->enqueue_task_only_once('parse_calendars');
    $self->app->enqueue_task_only_once('parse_weather');
    $self->app->enqueue_task_only_once('parse_googlefit');
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
    my $goauth = PortalCalendar::Integration::Google->new(config => $self->config_obj);
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

# OAuth 2 callback from google
# Very specific work with config here! Can't use display accessors or config methods because it doesn't get the display ID in the URL
sub googlefit_callback {
    my $self = shift;

    my $display;
    try {
        my $json = decode_json(b64_decode($self->req->param('state')));
        $display = $self->get_display_by_id($json->{display_number});
    } catch {
        $self->log->error("Error decoding state: $_");
    };

    my $config_obj = PortalCalendar::Config->new(app => $self->app, display => $display);

    $self->log->info("in callback, received this (for display #" . $display->id . "):");
    $self->log->info("code: " . $self->req->param('code'));
    $self->log->info("scope: " . $self->req->param('scope'));

    $self->log->info("converting code to a token");

    #Get tokens from auth code
    my $goauth = PortalCalendar::Integration::Google->new(config => $config_obj);
    my $res    = $self->app->ua->post(
        $goauth->google_oauth2_token_url,
        'form',
        {
            code          => $self->req->param('code'),
            client_id     => $config_obj->get('googlefit_client_id'),
            client_secret => $config_obj->get('googlefit_client_secret'),
            redirect_uri  => $config_obj->get('googlefit_auth_callback'),
            grant_type    => 'authorization_code',

            #scope         => googlefit_oauth2_scope,
        }
    )->result;

    $self->log->info("response: " . $res->to_string);

    if (!$res->is_success) {
        return $self->render(
            template => 'auth_error',
            format   => 'html',
            nav_link => 'config_ui',

            display => undef,

            # page-specific variables
            error => decode_json($res->body),
        );
    }

    # Save both tokens
    #$self->log->info(DDP::np($res->json));
    $self->log->info("JSON content: " . DDP::np($res->json));
    $config_obj->set('_googlefit_refresh_token', $res->json->{refresh_token});
    $config_obj->set('_googlefit_access_token',  $res->json->{access_token});

    # $self->set_config('_googlefit_token_json',    encode_json($res->json));

    $self->redirect_to('/auth/googlefit/success/' . $display->id);
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