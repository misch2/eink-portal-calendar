package PortalCalendar::Integration;

use Mojo::Base -base;
use Mojo::File;
use Mojo::JSON qw(decode_json encode_json);

use DDP;
use Try::Tiny;
use Time::Seconds;
use LWP::UserAgent::Cached;

use PortalCalendar::DatabaseCache;

has 'app' => sub { die "app is not defined " };
has 'display';               # not needed in some of the methods
has 'db_cache_key' => '';    # optional, needed only when the integration uses multiple cache rows for different parts

has 'lwp_cache_dir' => sub {
    my $self = shift;

    return $self->app->app->home->child("cache/lwp");
};

# Only to prevent contacting the server too often. It is not intended to be a long term or content-dependent cache, that's a task for DatabaseCache.
has 'lwp_max_cache_age' => 10 * ONE_MINUTE;

has 'config' => sub {
    my $self = shift;
    return $self->app->config_obj;
};

has 'db_cache' => sub {
    my $self = shift;
    return PortalCalendar::DatabaseCache->new(app => $self->app, creator => ref($self), display_id => ($self->display && $self->display->id));
};

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
            if ($recache) {
                $self->app->log->info("Age($path)=$age secs => too old, will reload from the source");
            } else {
                $self->app->log->debug("Age($path)=$age secs => still OK");
            }
            return $recache;
        },
    );
};

sub clear_db_cache {
    my $self = shift;
    $self->db_cache->clear;
}

1;
