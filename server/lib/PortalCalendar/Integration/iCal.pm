package PortalCalendar::Integration::iCal;

use Moo;
use LWP::UserAgent::Cached;
use iCal::Parser;
use DDP;
use DateTime;

has ics_url   => (is => 'ro', required => 1);
has cache_dir => (is => 'ro', required => 1);

has ua => (is => 'lazy');

sub _build_ua {
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

}

sub fetch_from_web {
    my $self = shift;

    my $response = $self->ua->get($self->ics_url);
    die $response->status_line
        unless $response->is_success;

    return $response->decoded_content;
}

sub get_events {
    my $self = shift;

    my $ical = iCal::Parser->new(no_todos => 1);
    my $data = $ical->parse_strings($self->fetch_from_web);

    return $data->{events};
}

sub get_today_events {
    my $self = shift;
    my $date = shift // DateTime->now();

    my $events = $self->get_events()->{ $date->year }->{ $date->month }->{ $date->day } || {};

    my @events = values %{$events};
    @events = sort { $a->{DTSTART}->hms cmp $b->{DTSTART}->hms } @events;

    return @events;
}

1;