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
    my $start    = shift;
    my $end      = shift;

    my $ical = iCal::Parser->new(
        no_todos     => 1,
        tz           => $self->config->get('timezone'),        # database config, editable in UI
        timezone_map => $self->app->config->{timezone_map},    # different type of config: .conf file
        start        => $start,
        end          => $end,
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

sub get_all_items {
    my $self  = shift;
    my $start = shift;
    my $end   = shift;

    my $cache = $self->db_cache;
    $cache->max_age(2 * ONE_HOUR);

    my $cal_data = $cache->get_or_set(
        sub {
            my $raw       = $self->raw_details_from_web();
            my $processed = $self->transform_details($raw, $start, $end);
            return $processed;
        },
        {
            url   => $self->ics_url,
            start => $start,
            end   => $end
        }
    );

    return $cal_data;
}

sub get_events_only {
    my $self  = shift;
    my $start = shift;
    my $end   = shift;

    return $self->get_all_items($start, $end)->{events};
}

sub get_events_between {
    my $self  = shift;
    my $start = shift;
    my $end   = shift;

    # All-day events are not filterable here using iCal::Parser if the $start time is not 00:00:00. So we need to filter them manually later in the loop.
    my $all_events = $self->get_events_only($start->clone->truncate(to => 'day'), $end);

    my @ret = ();
    foreach my $year (keys %{$all_events}) {
        foreach my $month (keys %{ $all_events->{$year} }) {
            foreach my $day (keys %{ $all_events->{$year}->{$month} }) {
                my @events = values %{ $all_events->{$year}->{$month}->{$day} };

                @events = grep { $_->{DTSTART} > $start || $_->{allday} } @events;
                map { $_->{SUMMARY} =~ s/\\,/,/g } @events;    # fix "AA\,BB" situation

                push @ret, @events;
            }
        }
    }

    return @ret;
}

1;