package PortalCalendar::Integration::iCal;

use Mojo::Base qw/PortalCalendar::Integration/;

use Mojo::Base -base;
use Mojo::File;
use PortalCalendar::DatabaseCache;

use iCal::Parser;
use DDP;
use DateTime;
use Try::Tiny;
use Time::Seconds;

has 'ics_url';

sub raw_details_from_web {
    my $self = shift;

    my $response = $self->caching_ua->get($self->ics_url);
    die $response->status_line . "\n" . $response->content
        unless $response->is_success;

    return $response->decoded_content;
}

sub transform_details {
    my $self     = shift;
    my $raw_text = shift;

    my $ical = iCal::Parser->new(
        no_todos     => 1,
        tz           => $self->config->get('timezone'),        # database config, editable in UI
        timezone_map => $self->app->config->{timezone_map},    # different type of config: .conf file
    );

    $self->app->log->debug("parsing calendar data...");
    my $events = {};
    try {
        $events = $ical->parse_strings($raw_text);
    } catch {
        $self->app->log->error("Error parsing calendar data: $_");
    };

    return $events;
}

sub get_all {
    my $self  = shift;
    my $cache = $self->db_cache;
    $cache->max_age(2 * ONE_HOUR);

    my $cal_data = $cache->get_or_set(
        sub {
            my $raw       = $self->raw_details_from_web();
            my $processed = $self->transform_details($raw);
            return $processed;
        },
        { url => $self->ics_url }
    );

    return $cal_data;
}

sub get_events_only {
    my $self = shift;

    return $self->get_all()->{events};
}

sub get_today_events {
    my $self = shift;
    my $date = shift // DateTime->now();

    my $all_events = $self->get_events_only();
    my $events     = $all_events->{ $date->year }->{ $date->month }->{ $date->day } || {};

    my @events = values %{$events};

    $date   = $date->clone()->set_time_zone($self->config->get('timezone'));
    @events = grep { $_->{DTSTART} > $date || $_->{allday} } @events;

    map { $_->{SUMMARY} =~ s/\\,/,/g } @events;    # fix "AA\,BB" situation

    return @events;
}

1;