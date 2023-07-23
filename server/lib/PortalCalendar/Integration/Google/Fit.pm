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

has 'cache_dir';
has 'db_cache_id';

has data_url => 'https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate';

has 'ua' => sub {
    my $self = shift;
    return LWP::UserAgent::Cached->new(
        cache_dir => $self->cache_dir,

        # nocache_if => sub {
        #     my $response = shift;
        #     return $response->code != 200;    # do not cache any bad response
        # },
        recache_if => sub {
            my ($response, $path, $request) = @_;
            my $stat    = Mojo::File->new($path)->lstat;
            my $age     = time - $stat->mtime;
            my $recache = ($age > 60 * 60 * 4) ? 1 : 0;    # recache anything older than 4 hours
            $self->app->log->debug("Age($path)=$age secs => recache=$recache");
            return $recache;

        },
    );
};

sub _perform_authenticated_request {
    my $self = shift;
    my $req  = shift;

    my $response = $self->ua->request($req);

    # p $response;

    if (!$response->is_success) {
        $self->get_new_access_token_from_refresh_token();
        $response = $self->ua->request($req);

        # p $response;
    }

    return $response;
}

sub fetch_from_web {
    my $self   = shift;
    my $forced = shift;

    my $cache = PortalCalendar::DatabaseCache->new(app => $self->app);
    return $cache->get_or_set(
        sub {
            my $access_token = $self->app->get_config('_googlefit_access_token');

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
            $req->header('Authorization' => "Bearer " . $access_token);
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
