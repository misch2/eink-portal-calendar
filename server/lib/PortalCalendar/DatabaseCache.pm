package PortalCalendar::DatabaseCache;

use Mojo::Base -base;

use DDP;
use Mojo::Util qw(b64_decode b64_encode);
use Storable;
use DateTime;
use Time::Seconds;

has 'app';
has 'max_cache_age' => sub { 5 * ONE_MINUTE };

sub get_or_set {
    my $self          = shift;
    my $callback      = shift;
    my $db_cache_id   = shift;
    my $force_refresh = shift;

    my $now_epoch = DateTime->now(time_zone => 'UTC')->epoch;
    if (!$force_refresh) {
        if (my $row = $self->app->schema->resultset('Cache')->find($db_cache_id)) {
            my $age = $now_epoch - $row->created_utc;
            if ($age < $self->max_cache_age) {
                my $data = Storable::thaw(b64_decode($row->data));
                $self->app->log->debug("returning parsed data from cache (id [" . $db_cache_id . "], age $age seconds < limit " . $self->max_cache_age . " seconds)");
                return $data;
            } else {
                $self->app->log->info("ignoring cache id [" . $db_cache_id . "], age $age seconds >= limit " . $self->max_cache_age . " seconds");
            }
        }
    }

    $self->app->log->info("recalculating cached data for id [" . $db_cache_id . "]");
    my $data = $callback->();

    $self->app->log->debug("storing serialized data into the DB");
    my $serialized_data = b64_encode(Storable::freeze $data);
    $self->app->schema->resultset('Cache')->update_or_create(
        {
            id          => $db_cache_id,
            data        => $serialized_data,
            created_utc => $now_epoch,
        },
        {
            key => 'primary',
        }
    );

    return $data;
}

1;