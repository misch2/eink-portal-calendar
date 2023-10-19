package PortalCalendar::Integration::Google::Fit;

use Mojo::Base qw/PortalCalendar::Integration::Google/;

use Mojo::Base -base;
use Mojo::File;
use Mojo::JSON qw(decode_json encode_json);

use PortalCalendar::DatabaseCache;

use DDP;
use Try::Tiny;
use HTTP::Request;
use DateTime;
use Time::Seconds;

has data_url                      => 'https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate';
has fetch_days                    => 90;                                                                   # any length
has fetch_days_during_single_call => 30;                                                                   # <2 months, otherwise it returns "aggregate duration too large" error

sub is_available {
    my $self = shift;
    return $self->config->get('_googlefit_access_token') && $self->config->get('_googlefit_refresh_token');
}

sub _perform_authenticated_request {
    my $self = shift;
    my $req  = shift;

    return unless $self->is_available;

    my $access_token = $self->config->get('_googlefit_access_token');
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

    return unless $self->is_available;

    my $cache = PortalCalendar::DatabaseCache->new(app => $self->app, max_cache_age => 1 * ONE_HOUR);
    return $cache->get_or_set(
        sub {
            my $global_dt_start = DateTime->now()->subtract(days => ($self->fetch_days - 1))->truncate(to => 'day');
            my $global_dt_end   = DateTime->now();

            $self->app->log->debug("requesting globally $global_dt_start - $global_dt_end for " . $self->fetch_days . " days");

            my $dt_start = $global_dt_start->clone;
            my $dt_end   = $dt_start->clone->add(days => $self->fetch_days_during_single_call)->subtract(seconds => 1);
            $self->app->log->trace(" local $dt_start - $dt_end");
            if (DateTime->compare($dt_end, $global_dt_end) > 0) {
                $dt_end = $global_dt_end->clone;
                $self->app->log->trace(" - truncated dt_end to $dt_end");
            }

            my $global_json = {};
            while (DateTime->compare($dt_start, $global_dt_end) < 0) {
                $self->app->log->trace("dt_start vs global_dt_end = $dt_start vs $global_dt_end");
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
                $req->header('Content-Type' => 'application/json');
                $req->content(encode_json($json_body));

                # p $req;

                my $response = $self->_perform_authenticated_request($req);
                die $response->status_line . "\n" . $response->content
                    unless $response->is_success;

                my $json = decode_json($response->decoded_content);

                $global_json->{bucket} = [] unless $global_json->{bucket};
                push @{ $global_json->{bucket} }, @{ $json->{bucket} };

                # move the date window forward
                $dt_start->add(days => $self->fetch_days_during_single_call);
                $dt_end->add(days => $self->fetch_days_during_single_call);
                $self->app->log->trace(" new local $dt_start - $dt_end");
                if (DateTime->compare($dt_end, $global_dt_end) > 0) {
                    $dt_end = $global_dt_end->clone;
                    $self->app->log->trace(" - truncated dt_end to $dt_end");
                }
            }

            return $global_json;
        },
        __PACKAGE__ . '/' . $self->db_cache_id . '/weight_aggregated'
    );
}

sub get_weight_series {
    my $self   = shift;

    return unless $self->is_available;

    my $data = $self->fetch_from_web;
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

sub get_last_known_weight {
    my $self = shift;

    my $weight;
    my $series = $self->get_weight_series;
    for (my $i = $#{$series}; $i >= 0; $i--) {
        $weight = $series->[$i]->{weight};
        last if defined $weight;
    }

    return $weight;
}

1;
