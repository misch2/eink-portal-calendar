package PortalCalendar::Integration::Google::Fit;

use base qw/PortalCalendar::Integration::Google/;

use Mojo::Base -base;
use Mojo::File;
use Mojo::JSON qw(decode_json encode_json);

use PortalCalendar::DatabaseCache;

use DDP;
use Try::Tiny;
use HTTP::Request;
use DateTime;

has data_url => 'https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate';

sub is_available {
    my $self = shift;
    return $self->app->get_config('_googlefit_access_token') && $self->app->get_config('_googlefit_refresh_token');
};

sub _perform_authenticated_request {
    my $self = shift;
    my $req  = shift;

    return unless $self->is_available;

    my $access_token = $self->app->get_config('_googlefit_access_token');
    $req->header('Authorization' => "Bearer " . $access_token);
    my $response = $self->caching_ua->request($req);

    # p $response;

    if (!$response->is_success) {
        my $new_access_token = $self->get_new_access_token_from_refresh_token();
        if ($new_access_token && $new_access_token ne $access_token) {
        $req->header('Authorization' => "Bearer " . $new_access_token);

        $response = $self->caching_ua->request($req);

        # p $response;
        }
    }

    return $response;
}

sub fetch_from_web {
    my $self   = shift;
    my $forced = shift;

    return unless $self->is_available;

    my $cache = PortalCalendar::DatabaseCache->new(app => $self->app);
    return $cache->get_or_set(
        sub {
            my $dt_start = DateTime->now()->subtract(days => 30)->truncate(to => 'day');
            my $dt_end   = DateTime->now();

            my $json_body = {
                aggregateBy => [
                    {
                        "dataSourceId" => "derived:com.google.weight:com.google.android.gms:merge_weight",
                    }
                ],
                "bucketByTime" => {
                    "period" => {
                        "type"       => "day",
                        "value"      => 1,
                        "timeZoneId" => "Europe/Prague",
                    }
                },
                "startTimeMillis" => 1000 * $dt_start->epoch,
                "endTimeMillis"   => 1000 * $dt_end->epoch,
            };

            my $req = HTTP::Request->new('POST', $self->data_url);
            $req->header('Content-Type'  => 'application/json');
            $req->content(encode_json($json_body));

            # p $req;

            my $response = $self->_perform_authenticated_request($req);
            die $response->status_line . "\n" . $response->content
                unless $response->is_success;

            return decode_json($response->decoded_content);
        },
        'googlefit_weight_aggregated',
        $forced
    );
}

sub get_weight_series {
    my $self   = shift;
    my $forced = shift;

    return unless $self->is_available;

    my $data = $self->fetch_from_web($forced);
    $self->app->log->debug("parsing Google Fit weight data...");

    my @ret = ();

    foreach my $bucket (@{ $data->{bucket} }) {
        my $start = DateTime->from_epoch(epoch => $bucket->{startTimeMillis} / 1000);
        my $end   = DateTime->from_epoch(epoch => $bucket->{endTimeMillis} / 1000);

        my $weight = $bucket->{dataset}->[0]->{point}->[0]->{value}->[0]->{fpVal};

        push @ret,
            {
            date   => $start->clone->truncate(to => 'day'),
            weight => $weight,
            };
    }

    return \@ret;
}

1;
