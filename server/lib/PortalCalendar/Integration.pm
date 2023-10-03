package PortalCalendar::Integration;

use Mojo::Base -base;
use Mojo::File;
use Mojo::JSON qw(decode_json encode_json);

use PortalCalendar::DatabaseCache;

use DDP;
use Try::Tiny;

has 'app';
has 'db_cache_id';

has 'cache_dir' => sub {
    my $self = shift;

    return $self->app->app->home->child("cache/lwp");
};

has 'max_cache_age' => 60 * 60 * 1;    # 1 hour

has 'caching_ua' => sub {
    my $self = shift;
    return LWP::UserAgent::Cached->new(
        cache_dir => $self->cache_dir,

        nocache_if => sub {
            my $response = shift;
            return $response->code != 200;    # do not cache any bad response
        },

        recache_if => sub {
            my ($response, $path, $request) = @_;
            my $stat    = Mojo::File->new($path)->lstat;
            my $age     = time - $stat->mtime;
            my $recache = ($age > $self->max_cache_age) ? 1 : 0;    # recache anything older than max age
            $self->app->log->debug("Age($path)=$age secs => recache=$recache");
            return $recache;

        },
    );
};


1;
