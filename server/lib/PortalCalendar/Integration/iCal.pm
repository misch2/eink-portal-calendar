package PortalCalendar::Integration::iCal;

use Mojo::Base -base;

use LWP::UserAgent::Cached;
use iCal::Parser;
use DDP;
use DateTime;
use Mojo::Util qw(b64_decode b64_encode);
use Storable;

has 'app';
has 'ics_url';
has 'cache_dir';
has 'db_cache_id';

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
            return -M $path > 0.5;    # recache any response older than 0.5 day
        },
    );
};

sub fetch_from_web {
    my $self = shift;

    my $response = $self->ua->get($self->ics_url);
    die $response->status_line
        unless $response->is_success;

    return $response->decoded_content;
}

sub get_events {
    my $self   = shift;
    my $forced = shift;

    if (!$forced && $self->db_cache_id) {

        # try to load data from database
        if (my $row = $self->app->schema->resultset('CalendarEventsRaw')->find($self->db_cache_id)) {
            my $events = Storable::thaw(b64_decode($row->events_raw));
            $self->app->log->debug("returning parsed calendar data from cache #" . $self->db_cache_id);
            return $events;
        }
    }

    my $ical = iCal::Parser->new(no_todos => 1);

    my $data   = $self->fetch_from_web;
    $self->app->log->debug("parsing calendar data...");
    my $events = $ical->parse_strings($data);

    if ($self->db_cache_id) {
        $self->app->log->debug("storing serialized data into the DB");
        my $serialized = b64_encode(Storable::freeze $events);
        $self->app->schema->resultset('CalendarEventsRaw')->update_or_create(
            {
                calendar_id => $self->db_cache_id,
                events_raw  => $serialized,
            },
            {
                key => 'primary',
            }
        );
    }

    return $events;
}

sub get_today_events {
    my $self = shift;
    my $date = shift // DateTime->now();

    my $events = $self->get_events()->{ $date->year }->{ $date->month }->{ $date->day } || {};

    my @events = values %{$events};
    @events = sort { $a->{DTSTART}->hms cmp $b->{DTSTART}->hms } @events;
    map { $_->{SUMMARY} =~ s/\\,/,/g } @events;    # fix "AA\,BB" situation

    return @events;
}

1;