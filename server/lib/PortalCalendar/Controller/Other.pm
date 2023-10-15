# Controller
package PortalCalendar::Controller::Other;
use Mojo::Base 'PortalCalendar::Controller';

use Mojo::Util qw(url_escape b64_decode b64_encode);
use Mojo::JSON qw(decode_json encode_json);
use Try::Tiny;

has display    => sub { die "not available here" };
has config_obj => sub { die "not available here" };

# Very specific work with config here! Can't use display accessors or config methods because it doesn't get the display ID in the URL.
# OAuth 2 callback from google
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

1;
