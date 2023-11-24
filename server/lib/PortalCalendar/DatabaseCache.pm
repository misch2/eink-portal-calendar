package PortalCalendar::DatabaseCache;

use Mojo::Base -base;

use Mojo::Util qw(b64_decode b64_encode sha1_sum);
use Mojo::JSON qw(encode_json);

use DDP;
use Storable;
use DateTime;
use Time::Seconds;

has 'app'     => sub { die "app not set" };
has 'creator' => sub { die "creator not set" };

has 'minimal_cache_expiry' => 0;

has 'max_age' => sub { 5 * ONE_MINUTE };

sub get_or_set {
    my $self         = shift;
    my $callback     = shift;
    my $db_cache_key = shift;    # hash with all the parameters on which the cache depends, e.g. {lat => 1.234, lon => 5.678}

    my $cache_key_as_string = encode_json($db_cache_key);
    my $cache_key_as_digext = sha1_sum($cache_key_as_string);

    my $log_prefix = "[" . $self->creator . "][key_json=" . $cache_key_as_string . "] ";
    my $now        = DateTime->now(time_zone => 'UTC');
    my $dtf        = $self->app->schema->storage->datetime_parser;

    if (
        my $row = $self->app->schema->resultset('Cache')->search(
            {
                creator    => $self->creator,
                key        => $cache_key_as_digext,
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
    my $record          = $self->app->schema->resultset('Cache')->update_or_create(
        {
            # filter keys
            creator => $self->creator,
            key     => $cache_key_as_digext,

            # stored data
            data       => $serialized_data,
            created_at => $now,
            expires_at => $now->clone->add(seconds => $self->max_age),
        },
        {
            key => 'creator_key_unique',
        }
    );

    if ($self->minimal_cache_expiry) {
        $self->app->log->debug("${log_prefix}enforcing cache expiry at least in " . $self->minimal_cache_expiry . " seconds");
        my $future_expiry = $now->clone->add(seconds => $self->minimal_cache_expiry);
        if ($record->expires_at < $future_expiry) {
            $self->app->log->debug("${log_prefix}updating cache expiry from " . $record->expires_at . " to " . $future_expiry);
            $record->update(
                {
                    expires_at => $now->clone->add(seconds => $self->minimal_cache_expiry),
                }
            );
        }
    }

    return $data;
}

sub clear {
    my $self = shift;

    $self->app->log->info("clearing cached data for " . $self->creator);
    $self->app->schema->resultset('Cache')->search({ creator => $self->creator })->delete;

    return;
}

1;