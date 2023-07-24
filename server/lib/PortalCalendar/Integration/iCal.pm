package PortalCalendar::Integration::iCal;

use base qw/PortalCalendar::Integration/;

use Mojo::Base -base;
use Mojo::File;
use PortalCalendar::DatabaseCache;

use LWP::UserAgent::Cached;
use iCal::Parser;
use DDP;
use DateTime;
use Try::Tiny;

has 'ics_url';

has 'max_cache_age' => 60 * 60 * 4;    # 4 hours

sub fetch_from_web {
    my $self = shift;

    my $response = $self->caching_ua->get($self->ics_url);
    die $response->status_line
        unless $response->is_success;

    return $response->decoded_content;
}

sub get_events {
    my $self   = shift;
    my $forced = shift;

    my $cache    = PortalCalendar::DatabaseCache->new(app => $self->app);
    my $cal_data = $cache->get_or_set(
        sub {
            my $ical = iCal::Parser->new(
                no_todos     => 1,
                tz           => $self->app->get_config('timezone'),    # database config, editable in UI
                timezone_map => $self->app->config->{timezone_map},    # different type of config: .conf file
            );

            my $data = $self->fetch_from_web;
            $self->app->log->debug("parsing calendar data...");
            my $events = {};
            try {
                $events = $ical->parse_strings($data);
            } catch {
                $self->app->log->error("Error parsing calendar data: $_");
            };
            return $events;

        },
        $self->db_cache_id,
        $forced
    );
    return $cal_data->{events};
}

sub get_today_events {
    my $self = shift;
    my $date = shift // DateTime->now();

    my $all_events = $self->get_events();
    my $events     = $all_events->{ $date->year }->{ $date->month }->{ $date->day } || {};

    my @events = values %{$events};

    $date   = $date->clone()->set_time_zone($self->app->get_config('timezone'));
    @events = grep { $_->{DTSTART} > $date || $_->{allday} } @events;

    map { $_->{SUMMARY} =~ s/\\,/,/g } @events;    # fix "AA\,BB" situation

    return @events;
}

1;