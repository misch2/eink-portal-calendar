package PortalCalendar::Integration::iCal;

use Mojo::Base -base;

use LWP::UserAgent::Cached;
use iCal::Parser;
use DDP;
use DateTime;
use Digest;

has 'ics_url';
has 'cache_dir';

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

our %cache_by_contenthash = ();

sub get_events {
    my $self = shift;

    my $ical = iCal::Parser->new(no_todos => 1);

    my $data = $self->fetch_from_web;
    my $digest = Digest->new("SHA-256")->add(Encode::encode('utf-8', $data))->hexdigest;

    if (!exists $cache_by_contenthash{$digest}) {
        my $data = $ical->parse_strings($data);
        $cache_by_contenthash{$digest} = $data->{events};
    }

    return $cache_by_contenthash{$digest};

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