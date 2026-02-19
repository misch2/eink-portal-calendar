package PortalCalendar::Integration;

use Mojo::Base -base;
use Mojo::File;
use Mojo::JSON qw(decode_json encode_json);

use HTTP::Caching::DeprecationWarning ':hide';
use DDP;
use Try::Tiny;
use Time::Seconds;
use LWP::UserAgent::Caching;
use CHI;

# use LWP::ConsoleLogger::Everywhere;    # uncomment to debug LWP to STDOUT

use PortalCalendar::DatabaseCache;

has 'app' => sub { die "app is not defined " };
has 'display';    # optional, only for some of the methods
has 'minimal_cache_expiry' => 0;

has 'lwp_cache_dir' => sub {
    my $self = shift;

    return $self->app->app->home->child("cache/lwp");
};

# Only to prevent contacting the server too often. It is not intended to be a long term or content-dependent cache, that's a task for DatabaseCache.
has 'lwp_max_cache_age' => 10 * ONE_MINUTE;

has 'db_cache' => sub {
    my $self = shift;
    return PortalCalendar::DatabaseCache->new(app => $self->app, creator => ref($self), minimal_cache_expiry => $self->minimal_cache_expiry);
};

has chi_cache => sub {
    my $self = shift;

    CHI->new(
        driver         => 'File',
        root_dir       => $self->lwp_cache_dir->to_string,
        file_extension => '.cache',
        l1_cache       => {
            driver   => 'Memory',
            global   => 1,
            max_size => 1024 * 1024
        }
    );
};

has caching_ua => sub {
    my $self = shift;

    return LWP::UserAgent::Caching->new(
        agent => "PortalCalendar/1.0 github.com/misch2/eink-portal-calendar",    # Non-generic agent name required, see https://api.met.no/doc/TermsOfService

        http_caching => {
            cache => $self->chi_cache,
            type  => 'private',

            # not over due within the next minute
            request_directives => ("max-age=" . $self->lwp_max_cache_age . ', min-fresh=60'),
        },
    );
};

sub clear_db_cache {
    my $self = shift;
    $self->db_cache->clear;
}

1;
