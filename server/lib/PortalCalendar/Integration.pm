package PortalCalendar::Integration;

use Mojo::Base -base;
use Mojo::File;
use Mojo::JSON qw(decode_json encode_json);

use DDP;
use Try::Tiny;
use Time::Seconds;

has 'app';
has 'config';
has 'db_cache_id';

has 'lwp_cache_dir' => sub {
    my $self = shift;

    return $self->app->app->home->child("cache/lwp");
};

# Only to prevent contacting the server too often. It is not intended to be a long term or content-dependent cache, that's a task for DatabaseCache.
has 'lwp_max_cache_age' => 10 * ONE_MINUTE;

has 'caching_ua' => sub {
    my $self = shift;
    return LWP::UserAgent::Cached->new(
        lwp_cache_dir => $self->lwp_cache_dir,

        nocache_if => sub {
            my $response = shift;
            return $response->code != 200;    # do not cache any bad response
        },

        recache_if => sub {
            my ($response, $path, $request) = @_;
            my $stat    = Mojo::File->new($path)->lstat;
            my $age     = time - $stat->mtime;
            my $recache = ($age > $self->lwp_max_cache_age) ? 1 : 0;    # recache anything older than max age
            $self->app->log->debug("Age($path)=$age secs => recache=$recache");
            return $recache;
        },
    );
};

1;
