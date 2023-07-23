package PortalCalendar::Integration::Google;

use Mojo::Base -base;
use Mojo::JSON qw(decode_json encode_json);

use DDP;
use Try::Tiny;
use HTTP::Request;

has 'googlefit_oauth2_auth_url'  => 'https://accounts.google.com/o/oauth2/v2/auth';
has 'googlefit_oauth2_token_url' => 'https://oauth2.googleapis.com/token';
has 'googlefit_oauth2_scope'     => 'https://www.googleapis.com/auth/fitness.body.read';    # space separated scopes

has 'app';

sub get_new_access_token_from_refresh_token {
    my $self = shift;
    
    $self->app->log->info("Getting new access token");

    #Get tokens from auth code
    my $res = $self->app->app->ua->post(
        $self->googlefit_oauth2_token_url,
        'form',
        {
            client_id     => $self->app->get_config('googlefit_client_id'),
            client_secret => $self->app->get_config('googlefit_client_secret'),
            redirect_uri  => $self->app->get_config('googlefit_auth_callback'),
            grant_type    => 'refresh_token',
            refresh_token => $self->app->get_config('_googlefit_refresh_token'),
        }
    )->res;

    if (!$res->is_success) {
        $self->logger->error(DDP::np($res));
        return;
    }

    # Save the new access token
    #$self->log->info(DDP::np($res->json));
    $self->app->set_config('_googlefit_access_token',  $res->json->{access_token});
    $self->app->log->info("Access token refreshed: " . $self->app->get_config('_googlefit_access_token'));

    return $self->app->get_config('_googlefit_access_token');
};

1;