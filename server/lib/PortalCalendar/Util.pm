package PortalCalendar::Util;

use Mojo::Base -base;

use DDP;
use DateTime;
use DateTime::Format::Strptime;
use DateTime::Format::ISO8601;
use DDP;
use Digest;
use Imager;
use List::Util;
use Readonly;
use Text::Unidecode;
use Time::HiRes;
use Try::Tiny;

use PortalCalendar;
use PortalCalendar::Config;
use PortalCalendar::Minion;
use PortalCalendar::Schema;
use PortalCalendar::Integration::iCal;
use PortalCalendar::Integration::OpenWeather;
use PortalCalendar::Integration::MQTT;
use PortalCalendar::Integration::Google::Fit;
use PortalCalendar::Integration::SvatkyAPI;

has 'app';
has 'display';

Readonly my $WIDTH  => 480;
Readonly my $HEIGHT => 800;

Readonly my @PORTAL_ICONS => qw(a1 a2 a3 a4 a5 a6 a7 a8 a9 a10 a11 a12 a13 a14 a15 b11 b14 c3 c4 c7 d2 d3 d4 d5 e1 e2 e4);
Readonly my %ICON_NAME_TO_FILENAME => (
    CUBE_DISPENSER    => 'a1',
    CUBE_HAZARD       => 'a2',
    PELLET_HAZARD     => 'a3',
    PELLET_CATCHER    => 'a4',
    FLING_ENTER       => 'a5',
    FLING_EXIT        => 'a6',
    TURRET_HAZARD     => 'a7',
    DIRTY_WATER       => 'a8',
    WATER_HAZARD      => 'a9',
    CAKE              => 'a10',
    LASER_REDIRECTION => 'c3',
    CUBE_BUTTON       => 'd5',
    BLADES_HAZARD     => 'e4',
    BRIDGE_SHIELD     => 'd2',
    FAITH_PLATE       => 'e1',
    LASER_HAZARD      => 'd3',
    LASER_SENSOR      => 'c4',
    LIGHT_BRIDGE      => 'e2',
    PLAYER_BUTTON     => 'd4',
);

Readonly my @CHAMBER_ICONS_BY_DAY_NUMBER => (
    [
        # P1 Chamber 1
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_HAZARD' => 0 ], [ 'PELLET_HAZARD' => 0 ], [ 'PELLET_CATCHER' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 0 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 4
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_HAZARD' => 1 ], [ 'PELLET_HAZARD' => 0 ], [ 'PELLET_CATCHER' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 0 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 5
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_HAZARD' => 1 ], [ 'PELLET_HAZARD' => 0 ], [ 'PELLET_CATCHER' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 0 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 6
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_HAZARD' => 0 ], [ 'PELLET_HAZARD' => 1 ], [ 'PELLET_CATCHER' => 1 ], [ 'WATER_HAZARD' => 0 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 0 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 8
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_HAZARD' => 0 ], [ 'PELLET_HAZARD' => 1 ], [ 'PELLET_CATCHER' => 1 ], [ 'WATER_HAZARD' => 1 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 1 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 9
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_HAZARD' => 1 ], [ 'PELLET_HAZARD' => 0 ], [ 'PELLET_CATCHER' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 0 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 10
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_HAZARD' => 0 ], [ 'PELLET_HAZARD' => 0 ], [ 'PELLET_CATCHER' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'FLING_ENTER'    => 1 ], [ 'FLING_EXIT'  => 1 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 0 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 11
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_HAZARD' => 0 ], [ 'PELLET_HAZARD' => 1 ], [ 'PELLET_CATCHER' => 1 ], [ 'WATER_HAZARD' => 1 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 1 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 12
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_HAZARD' => 1 ], [ 'PELLET_HAZARD' => 0 ], [ 'PELLET_CATCHER' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'FLING_ENTER'    => 1 ], [ 'FLING_EXIT'  => 1 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 0 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 13
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_HAZARD' => 1 ], [ 'PELLET_HAZARD' => 1 ], [ 'PELLET_CATCHER' => 1 ], [ 'WATER_HAZARD' => 0 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 0 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 14
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_HAZARD' => 1 ], [ 'PELLET_HAZARD' => 1 ], [ 'PELLET_CATCHER' => 1 ], [ 'WATER_HAZARD' => 1 ],
        [ 'FLING_ENTER'    => 1 ], [ 'FLING_EXIT'  => 1 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 1 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 15
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_HAZARD' => 0 ], [ 'PELLET_HAZARD' => 1 ], [ 'PELLET_CATCHER' => 1 ], [ 'WATER_HAZARD' => 1 ],
        [ 'FLING_ENTER'    => 1 ], [ 'FLING_EXIT'  => 1 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 1 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 16
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_HAZARD' => 1 ], [ 'PELLET_HAZARD' => 0 ], [ 'PELLET_CATCHER' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 1 ], [ 'DIRTY_WATER'    => 0 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 17
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_HAZARD' => 1 ], [ 'PELLET_HAZARD' => 1 ], [ 'PELLET_CATCHER' => 1 ], [ 'WATER_HAZARD' => 0 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 0 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 18
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_HAZARD' => 1 ], [ 'PELLET_HAZARD' => 1 ], [ 'PELLET_CATCHER' => 1 ], [ 'WATER_HAZARD' => 1 ],
        [ 'FLING_ENTER'    => 1 ], [ 'FLING_EXIT'  => 1 ], [ 'TURRET_HAZARD' => 1 ], [ 'DIRTY_WATER'    => 1 ], [ 'CAKE'         => 0 ],
    ],
    [
        # P1 Chamber 19
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_HAZARD' => 0 ], [ 'PELLET_HAZARD' => 1 ], [ 'PELLET_CATCHER' => 1 ], [ 'WATER_HAZARD' => 1 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'    => 1 ], [ 'CAKE'         => 1 ],
    ],
    [
        # P2 The Cold Boot Chamber 1
        [ 'LASER_SENSOR'  => 1 ], [ 'LASER_REDIRECTION' => 0 ], [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_BUTTON'  => 0 ], [ 'CUBE_HAZARD' => 0 ],
        [ 'PLAYER_BUTTON' => 0 ], [ 'WATER_HAZARD'      => 0 ], [ 'TURRET_HAZARD'  => 0 ], [ 'LASER_HAZARD' => 0 ], [ 'DIRTY_WATER' => 0 ],
    ],
    [
        # P2 The Cold Boot Chamber 2
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_BUTTON'       => 1 ], [ 'CUBE_HAZARD'   => 1 ], [ 'PLAYER_BUTTON' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'LASER_SENSOR'   => 1 ], [ 'LASER_REDIRECTION' => 1 ], [ 'TURRET_HAZARD' => 0 ], [ 'LASER_HAZARD'  => 0 ], [ 'DIRTY_WATER'  => 0 ],
    ],
    [
        # P2 The Cold Boot Chamber 3
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_BUTTON'       => 0 ], [ 'CUBE_HAZARD'   => 0 ], [ 'PLAYER_BUTTON' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'LASER_SENSOR'   => 1 ], [ 'LASER_REDIRECTION' => 1 ], [ 'TURRET_HAZARD' => 0 ], [ 'LASER_HAZARD'  => 0 ], [ 'DIRTY_WATER'  => 0 ],
    ],
    [
        # P2 The Cold Boot Chamber 4
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_BUTTON'       => 1 ], [ 'CUBE_HAZARD'   => 1 ], [ 'PLAYER_BUTTON' => 0 ], [ 'WATER_HAZARD' => 1 ],
        [ 'LASER_SENSOR'   => 1 ], [ 'LASER_REDIRECTION' => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'LASER_HAZARD'  => 0 ], [ 'DIRTY_WATER'  => 0 ],
    ],
    [
        # P2 The Cold Boot Chamber 5
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_BUTTON' => 1 ], [ 'CUBE_HAZARD' => 1 ], [ 'PLAYER_BUTTON' => 0 ], [ 'WATER_HAZARD' => 1 ],
        [ 'FLING_ENTER'    => 0 ], [ 'FLING_EXIT'  => 0 ], [ 'FAITH_PLATE' => 1 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'  => 0 ],
    ],
    [
        # P2 The Cold Boot Chamber 7
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_BUTTON'  => 1 ], [ 'CUBE_HAZARD'       => 1 ], [ 'WATER_HAZARD'  => 0 ], [ 'FLING_ENTER' => 1 ],
        [ 'FLING_EXIT'     => 1 ], [ 'LASER_SENSOR' => 1 ], [ 'LASER_REDIRECTION' => 0 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER' => 0 ],
    ],
    [
        # P2 The Cold Boot Chamber 8
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_BUTTON'  => 1 ], [ 'CUBE_HAZARD'       => 1 ], [ 'WATER_HAZARD'  => 0 ], [ 'FLING_ENTER' => 0 ],
        [ 'FLING_EXIT'     => 0 ], [ 'LASER_SENSOR' => 1 ], [ 'LASER_REDIRECTION' => 1 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER' => 0 ],
    ],
    [
        # P2 The Return Chamber 9
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_BUTTON'       => 0 ], [ 'CUBE_HAZARD' => 1 ], [ 'PLAYER_BUTTON' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'LASER_SENSOR'   => 1 ], [ 'LASER_REDIRECTION' => 1 ], [ 'FAITH_PLATE' => 1 ], [ 'TURRET_HAZARD' => 0 ], [ 'DIRTY_WATER'  => 0 ],
    ],
    [
        # P2 The Return Chamber 10
        [ 'CUBE_DISPENSER'    => 1 ], [ 'CUBE_BUTTON' => 1 ], [ 'CUBE_HAZARD' => 1 ], [ 'WATER_HAZARD' => 0 ], [ 'LASER_SENSOR' => 1 ],
        [ 'LASER_REDIRECTION' => 1 ], [ 'FAITH_PLATE' => 1 ], [ 'FLING_ENTER' => 1 ], [ 'FLING_EXIT'   => 1 ], [ 'DIRTY_WATER'  => 0 ],
    ],
    [
        # P2 The Return Chamber 11
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_BUTTON'   => 1 ], [ 'CUBE_HAZARD'   => 1 ], [ 'PLAYER_BUTTON'     => 0 ], [ 'WATER_HAZARD' => 1 ],
        [ 'LIGHT_BRIDGE'   => 1 ], [ 'TURRET_HAZARD' => 0 ], [ 'BRIDGE_SHIELD' => 0 ], [ 'LASER_REDIRECTION' => 0 ], [ 'DIRTY_WATER'  => 0 ],
    ],
    [
        # P2 The Return Chamber 13
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_BUTTON'   => 1 ], [ 'CUBE_HAZARD'   => 1 ], [ 'PLAYER_BUTTON' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'LIGHT_BRIDGE'   => 0 ], [ 'TURRET_HAZARD' => 1 ], [ 'BRIDGE_SHIELD' => 0 ], [ 'LASER_HAZARD'  => 0 ], [ 'DIRTY_WATER'  => 0 ],
    ],
    [
        # P2 The Return Chamber 15
        [ 'CUBE_DISPENSER' => 0 ], [ 'CUBE_BUTTON'   => 1 ], [ 'CUBE_HAZARD'   => 0 ], [ 'PLAYER_BUTTON' => 0 ], [ 'WATER_HAZARD' => 0 ],
        [ 'LIGHT_BRIDGE'   => 1 ], [ 'TURRET_HAZARD' => 1 ], [ 'BRIDGE_SHIELD' => 1 ], [ 'FAITH_PLATE'   => 1 ], [ 'DIRTY_WATER'  => 0 ],
    ],
    [
        # P2 The Return Chamber 16
        [ 'CUBE_DISPENSER'    => 0 ], [ 'CUBE_BUTTON'  => 1 ], [ 'CUBE_HAZARD'   => 0 ], [ 'PLAYER_BUTTON' => 1 ], [ 'WATER_HAZARD' => 0 ],
        [ 'LASER_REDIRECTION' => 1 ], [ 'LASER_SENSOR' => 1 ], [ 'TURRET_HAZARD' => 1 ], [ 'LASER_HAZARD'  => 1 ], [ 'DIRTY_WATER'  => 0 ],
    ],
    [
        # P2 The Surprise Chamber 18
        [ 'CUBE_DISPENSER' => 1 ], [ 'CUBE_BUTTON'       => 0 ], [ 'CUBE_HAZARD'   => 1 ], [ 'WATER_HAZARD'  => 1 ], [ 'LIGHT_BRIDGE' => 1 ],
        [ 'LASER_SENSOR'   => 1 ], [ 'LASER_REDIRECTION' => 1 ], [ 'TURRET_HAZARD' => 1 ], [ 'BRIDGE_SHIELD' => 1 ], [ 'LASER_HAZARD' => 1 ],
    ],
    [
        # P2 The Surprise Chamber 19
        [ 'CUBE_DISPENSER'    => 0 ], [ 'CUBE_BUTTON' => 0 ], [ 'CUBE_HAZARD'   => 0 ], [ 'PLAYER_BUTTON' => 0 ], [ 'LASER_SENSOR' => 1 ],
        [ 'LASER_REDIRECTION' => 1 ], [ 'FAITH_PLATE' => 1 ], [ 'TURRET_HAZARD' => 1 ], [ 'LASER_HAZARD'  => 1 ], [ 'DIRTY_WATER'  => 0 ],
    ]
);

sub html_for_date {
    my $self = shift;
    my $dt   = shift;

    # keep the calendar random, but consistent for any given day
    srand($dt->ymd(''));

    my @today_events;
    foreach my $calendar_no (1 .. 3) {
        next unless $self->app->get_config("web_calendar${calendar_no}");

        my $url = $self->app->get_config("web_calendar_ics_url${calendar_no}");

        next unless $url;

        my $calendar = PortalCalendar::Integration::iCal->new(ics_url => $url, db_cache_id => $self->display->id . '/' . $calendar_no, app => $self->app, config => $self->app->config_obj);
        try {
            push @today_events, $calendar->get_today_events($dt);    # cached if possible
                                                                     #p @today_events;
        } catch {
            warn "Error: $_";
        };
    }
    @today_events = sort { $a->{DTSTART} cmp $b->{DTSTART} } @today_events;

    my $has_calendar_entries = (scalar @today_events ? 1 : 0);

    my @icons;
    if (!$self->app->get_config('totally_random_icon')) {

        # true icon sets like wuspy has it
        my $set = $CHAMBER_ICONS_BY_DAY_NUMBER[ $dt->day - 1 ];
        die "no icon set for " . $dt if !$set;

        foreach (@{$set}) {
            my $name    = $_->[0];
            my $enabled = $_->[1];

            push @icons,
                {
                name   => $ICON_NAME_TO_FILENAME{$name},
                grayed => ($enabled ? 0 : 1)
                };
        }

        if ($has_calendar_entries) {
            pop @icons while (scalar @icons > $self->app->get_config('max_icons_with_calendar'));
        }
    } else {

        # random icon set
        my $gray_probability = 0.25;
        foreach my $name (List::Util::shuffle @PORTAL_ICONS) {
            push @icons,
                {
                name   => $name,
                grayed => (rand() < $gray_probability ? 1 : 0)
                };
        }
        while (scalar @icons < 16) {    # produce enough icons even with small number of source images
            push @icons, @icons;
        }

        my $min_icons_count = $self->app->get_config('min_random_icons');
        my $max_icons_count = $has_calendar_entries ? $self->app->get_config('max_icons_with_calendar') : $self->app->get_config('max_random_icons');
        $min_icons_count = $max_icons_count if $min_icons_count > $max_icons_count;

        my $icons_count = $min_icons_count + int(rand($max_icons_count - $min_icons_count + 1));
        @icons = @icons[ 0 .. $icons_count - 1 ];

    }

    my $current_weather;
    my $forecast;
    my @forecast_5_days = ();
    if ($self->app->get_config("openweather")) {
        my $api = PortalCalendar::Integration::OpenWeather->new(app => $self->app, db_cache_id => $self->display->id, config => $self->app->config_obj);
        $current_weather = $api->fetch_current_from_web;
        $forecast        = $api->fetch_forecast_from_web;

        # p $forecast, as => 'forecast';
        # p $current_weather, as => 'current';

        foreach my $days_add (0 .. 4) {
            my $dt = DateTime->now->truncate(to => 'day')->add(days => $days_add);

            my %daily_details = ();
            foreach my $hours_add (8, 12, 16) {
                my $dt2         = $dt->clone->add(hours => $hours_add);
                my $time_of_day = 'morning';
                $time_of_day = 'noon'      if $hours_add == 12;
                $time_of_day = 'afternoon' if $hours_add == 16;

                if (my $f = $self->find_nearest_forecast($forecast->{list}, $dt2)) {
                    $daily_details{$time_of_day} = {
                        required_dt => $dt2,
                        forecast    => $f
                    };
                }
            }

            push @forecast_5_days,
                {
                date    => $dt,
                details => \%daily_details,
                };
        }

        # p @forecast_5_days;
    }

    my $weight_series;
    my $last_weight;
    if ($self->app->get_config("googlefit_client_id")) {
        my $api = PortalCalendar::Integration::Google::Fit->new(app => $self->app, db_cache_id => $self->display->id, config => $self->app->config_obj);
        $weight_series = $api->get_weight_series;
        $last_weight   = $api->get_last_known_weight;
    }

    my $svatky_api = PortalCalendar::Integration::SvatkyAPI->new(app => $self->app, db_cache_id => $self->display->id, config => $self->app->config_obj);

    return $self->app->render(
        template => 'calendar_themes/' . $self->app->get_config('theme'),
        format   => 'html',
        app      => $self->app,

        # other variables
        date                 => $dt,
        icons                => \@icons,
        calendar_events      => \@today_events,
        has_calendar_entries => $has_calendar_entries,

        # name day:
        name_day_details => $svatky_api->get_today_details,

        # raw weather values:
        current_weather => $current_weather,
        forecast        => $forecast,
        # processed weather values:
        forecast_5_days => \@forecast_5_days,

        # googlefit data:
        last_weight   => $last_weight,
        weight_series => $weight_series,

    );
}

# Finds a forecast with datetime nearest to the required one.
# Returns nothing when there is no match.
sub find_nearest_forecast {
    my $self        = shift;
    my $list        = shift;
    my $dt_required = shift;

    my $ret;
    my $prev_weather;
    my $dt_prev;

    # individual forecasts
    foreach my $current_weather (@{$list}) {
        my $dt_current = DateTime->from_epoch(epoch => $current_weather->{dt})->set_time_zone($self->app->get_config('timezone'));

        if (DateTime->compare($dt_current, $dt_required) == 0) {

            # lucky guess, exact match
            $ret = $current_weather;
            last;
        } elsif (DateTime->compare($dt_current, $dt_required) < 0) {

            # still searching
            $prev_weather = $current_weather;
            $dt_prev      = $dt_current->clone;
            next;
        } else {

            # what was closer?
            if ($prev_weather) {
                my $prev_diff = $dt_required->clone->subtract_datetime($dt_prev);
                my $next_diff = $dt_current->clone->subtract_datetime($dt_required);
                if (DateTime::Duration->compare($prev_diff, $next_diff) > 0) {

                    # first interval is longer
                    $ret = $current_weather;
                    last;
                } else {

                    # 2nd interval is longer
                    $ret = $prev_weather;
                    last;
                }
            }
        }
    }

    return $ret;
}

sub generate_bitmap {
    my $self = shift;
    my $args = shift;

    # This source image needs to be generated by 'server/scripts/generate_img_from_web' set up to run periodically from a cron job.
    my $img = Imager->new(file => $self->app->app->home->child("generated_images/current_calendar_" . $self->app->display->id . ".png")) or die Imager->errstr;

    # If the generated image is larger (probably due to invalid CSS), crop it so that it display at least something:
    if ($img->getheight > $HEIGHT) {
        my $tmp = $img->crop(left => 0, top => 0, width => $WIDTH, height => $HEIGHT);
        die $img->errstr unless $tmp;
        $img = $tmp;
    }

    if ($args->{rotate} && $args->{rotate} != 0) {
        my $tmp;
        if ($args->{rotate} == 1) {
            $tmp = $img->rotate(right => 90);
        } elsif ($args->{rotate} == 2) {
            $tmp = $img->rotate(right => 180);
        } elsif ($args->{rotate} == 3) {
            $tmp = $img->rotate(right => 270);
        } else {
            die "unknown 'rotate' value: $args->{rotate}";
        }
        die $img->errstr unless $tmp;
        $img = $tmp;
    }

    if ($args->{flip} && $args->{flip} ne '') {
        my $tmp;
        if ($args->{flip} eq 'x') {
            $tmp = $img->flip(dir => 'h');
        } elsif ($args->{flip} eq 'y') {
            $tmp = $img->flip(dir => 'v');
        } elsif ($args->{flip} eq 'xy') {
            $tmp = $img->flip(dir => 'vh');
        } else {
            die "unknown 'flip' value: $args->{flip}";
        }
        die $img->errstr unless $tmp;
        $img = $tmp;
    }

    if ($args->{gamma} && $args->{gamma} != 1) {
        my @map = map { int(0.5 + 255 * ($_ / 255)**$args->{gamma}) } 0 .. 255;    # inplace conversion, no need to use $tmp here
        $img->map(all => \@map);
    }

    if ($args->{numcolors} && $args->{numcolors} < 256) {
        my $tmp = $img->to_paletted(
            {
                make_colors => {
                    2   => 'mono',
                    4   => 'gray4',
                    16  => 'gray16',
                    256 => 'gray',
                }->{ $args->{numcolors} },
                translate => 'closest',    # closest, errdiff

                # errdiff     => 'jarvis',      # floyd, jarvis, stucki, ...
            }
        );
        die $img->errstr unless $tmp;
        $img = $tmp;
    }

    if ($args->{format} eq 'png') {
        my $out;
        $img->write(data => \$out, type => 'png') or die;
        return $self->app->render(data => $out, format => 'png');
    } elsif ($args->{format} eq 'png_gray') {

        # Convert to 1 gray channel only
        my $tmp = $img->convert(preset => 'grey');
        die $img->errstr unless $tmp;
        $img = $tmp;
        $tmp = $img->convert(preset => 'noalpha');
        die $img->errstr unless $tmp;
        $img = $tmp;

        my $out;
        $img->write(data => \$out, type => 'png') or die;
        return $self->app->render(data => $out, format => 'png');
    } elsif ($args->{format} =~ /^raw/) {
        my $bitmap = '';
        if ($args->{format} eq 'raw8bpp') {
            foreach my $y (0 .. $img->getheight - 1) {
                foreach my $gray ($img->getsamples(y => $y, format => '8bit', channels => [0])) {
                    $bitmap .= chr($gray);
                }
            }
        } elsif ($args->{format} eq 'raw2bpp') {
            foreach my $y (0 .. $img->getheight - 1) {
                my $byte   = 0;
                my $bitcnt = 0;
                foreach my $gray ($img->getsamples(y => $y, format => '8bit', channels => [0])) {
                    my $bits = $gray >> 6;    # 0-3 range
                    $byte = $byte << 2 | $bits;
                    $bitcnt += 2;
                    if ($bitcnt == 8) {
                        $bitmap .= chr($byte);
                        $byte   = 0;
                        $bitcnt = 0;
                    }
                }
            }
        } elsif ($args->{format} eq 'raw1bpp') {
            foreach my $y (0 .. $img->getheight - 1) {
                my $byte   = 0;
                my $bitcnt = 0;
                foreach my $gray ($img->getsamples(y => $y, format => '8bit', channels => [0])) {
                    my $bit = $gray ? 1 : 0;
                    $byte = $byte << 1 | $bit;
                    $bitcnt++;
                    if ($bitcnt == 8) {
                        $bitmap .= chr($byte);
                        $byte   = 0;
                        $bitcnt = 0;
                    }
                }
            }
        } else {
            die "Unknown format requested: " . $args->{format};
        }

        # output format:
        #    "MM"
        #   checksum
        #    <sequence of raw values directly usable for uploading into eink display>
        my $out = "MM\n";

        # sha-1: 40 chars
        # sha-256: 64 chars
        $out .= Digest->new("SHA-1")->add($bitmap)->hexdigest . "\n";
        $out .= $bitmap;

        $self->app->res->headers->content_type('application/octet-stream');
        $self->app->res->headers->header('Content-Transfer-Encoding' => 'binary');
        return $self->app->render(data => $out);
    } else {
        die "Unknown format requested: " . $args->{format};
    }
}

sub update_mqtt {
    my $self = shift;

    my $key   = shift;
    my $value = shift;

    return unless $self->app->get_config('mqtt');

    my $api        = PortalCalendar::Integration::MQTT->new(app => $self->app, db_cache_id => $self->display->id, config => $self->app->config_obj);
    my %ha_details = (
        voltage => {
            component           => 'sensor',
            entity_category     => "diagnostic",
            device_class        => "voltage",       # see https://www.home-assistant.io/integrations/sensor/#device-class
            state_class         => 'measurement',
            unit_of_measurement => 'V',
            icon                => 'mdi:battery',
        },
        alert_voltage => {
            component           => 'sensor',
            entity_category     => "diagnostic",
            device_class        => "voltage",             # see https://www.home-assistant.io/integrations/sensor/#device-class
            state_class         => 'measurement',
            unit_of_measurement => 'V',
            icon                => 'mdi:battery-alert',
        },
        min_voltage => {
            component           => 'sensor',
            entity_category     => "diagnostic",
            device_class        => "voltage",               # see https://www.home-assistant.io/integrations/sensor/#device-class
            state_class         => 'measurement',
            unit_of_measurement => 'V',
            icon                => 'mdi:battery-outline',
        },
        max_voltage => {
            component           => 'sensor',
            entity_category     => "diagnostic",
            device_class        => "voltage",               # see https://www.home-assistant.io/integrations/sensor/#device-class
            state_class         => 'measurement',
            unit_of_measurement => 'V',
            icon                => 'mdi:battery',
        },
        battery_percent => {
            component           => 'sensor',
            entity_category     => "diagnostic",
            device_class        => "battery",               # see https://www.home-assistant.io/integrations/sensor/#device-class
            state_class         => 'measurement',
            unit_of_measurement => '%',
            icon                => 'mdi:battery-unknown',
        },
        sleep_time => {
            component           => 'sensor',
            entity_category     => "diagnostic",
            device_class        => "duration",              # see https://www.home-assistant.io/integrations/sensor/#device-class
            state_class         => 'measurement',
            unit_of_measurement => 's',
            icon                => 'mdi:sleep',
        },
        last_visit => {
            component           => 'sensor',
            entity_category     => "diagnostic",
            device_class        => "timestamp",             # see https://www.home-assistant.io/integrations/sensor/#device-class
            state_class         => '',
            unit_of_measurement => '',
            icon                => 'mdi:clock-time-four',
        }
    );

    my $ha_detail = $ha_details{$key};
    $api->publish_retained($key, $value, $ha_detail);

    return;
}

1;
