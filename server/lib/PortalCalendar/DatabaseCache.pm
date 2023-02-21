package PortalCalendar::DatabaseCache;

use Mojo::Base -base;

use DDP;
use Mojo::Util qw(b64_decode b64_encode);
use Storable;

has 'app';

sub get_or_set {
    my $self          = shift;
    my $callback      = shift;
    my $db_cache_id   = shift;
    my $force_refresh = shift;

    if (!$force_refresh) {
        if (my $row = $self->app->schema->resultset('Cache')->find($db_cache_id)) {
            my $data = Storable::thaw(b64_decode($row->data));
            $self->app->log->debug("returning parsed calendar data from cache #" . $db_cache_id);
            return $data;
        }
    }

    my $data = $callback->();

    $self->app->log->debug("storing serialized data into the DB");
    my $serialized_data = b64_encode(Storable::freeze $data);
    $self->app->schema->resultset('Cache')->update_or_create(
        {
            id   => $db_cache_id,
            data => $serialized_data,
        },
        {
            key => 'primary',
        }
    );

    return $data;
}

1;