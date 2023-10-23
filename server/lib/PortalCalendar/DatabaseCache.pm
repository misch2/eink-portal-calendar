package PortalCalendar::DatabaseCache;

use Mojo::Base -base;

use DDP;
use Mojo::Util qw(b64_decode b64_encode);
use Storable;
use DateTime;
use Time::Seconds;

has 'app'        => sub { die "app not set" };
has 'creator'    => sub { die "creator not set" };
has 'display_id' => sub { die "display_id not set" };

has 'max_age' => sub { 5 * ONE_MINUTE };

sub get_or_set {
    my $self         = shift;
    my $callback     = shift;
    my $db_cache_key = shift;

    my $log_prefix = "[" . $self->creator . "][" . $self->display_id . "][key=" . $db_cache_key . "] ";
    my $now        = DateTime->now(time_zone => 'UTC');
    my $dtf        = $self->app->schema->storage->datetime_parser;

    if (
        my $row = $self->app->schema->resultset('Cache')->search(
            {
                creator    => $self->creator,
                display_id => $self->display_id,
                key        => $db_cache_key,
                expires_at => { '>' => $dtf->format_datetime($now) }
            }
        )->single
    ) {
        my $data = Storable::thaw(b64_decode($row->data));
        $self->app->log->debug("${log_prefix}returning parsed data from cache (expires in " . $row->expires_at->clone->subtract_datetime_absolute($now)->in_units('seconds') . " seconds, at " . $row->expires_at . ")");
        return $data;
    }

    $self->app->log->info("${log_prefix}recalculating fresh data");
    my $data = $callback->();

    $self->app->log->debug("${log_prefix}storing serialized data into the DB");
    my $serialized_data = b64_encode(Storable::freeze $data);
    $self->app->schema->resultset('Cache')->update_or_create(
        {
            # filter keys
            creator    => $self->creator,
            display_id => $self->display_id,
            key        => $db_cache_key,

            # stored data
            data       => $serialized_data,
            created_at => $now,
            expires_at => $now->clone->add(seconds => $self->max_age),
        },
        {
            key => 'creator_key_display_id_unique',
        }
    );

    return $data;
}

sub clear {
    my $self = shift;

    my %search_args = (creator => $self->creator);
    if ($self->display_id) {
        $search_args{display_id} = $self->display_id;
    }

    $self->app->log->info("clearing cached data for " . $self->creator);
    $self->app->schema->resultset('Cache')->search(\%search_args)->delete;

    return;
}

1;