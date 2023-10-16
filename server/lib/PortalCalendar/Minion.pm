package PortalCalendar::Minion;

use Mojo::Base 'Mojolicious';
use PortalCalendar::Config;
use PortalCalendar::Web2Png;
use PortalCalendar::Integration::iCal;
use PortalCalendar::Integration::OpenWeather;
use PortalCalendar::Integration::Google::Fit;
use DDP;

sub regenerate_image {
    my $job  = shift;
    my @args = @_;

    my $start = time;

    $job->app->log->info("Regenerating calendar image");

    # if (0) {
    #     return $job->fail("error message");
    # }

    my $converter = PortalCalendar::Web2Png->new(pageres_command => $job->app->home->child("node_modules/.bin/pageres"));
    foreach my $display ($job->app->schema->resultset('Display')->all_displays) {
        $job->app->log->info("Processing display #" . $display->id);

        $converter->convert_url($job->app->config->{url_start} . '/calendar/' . $display->id . '/html', $display->width, $display->height, $job->app->home->child("generated_images/current_calendar_" . $display->id . ".png"));
    }
    $job->app->log->info("Finished processing");

    my $elapsed = time - $start;
    return $job->finish($elapsed);
}

sub reload_calendars {
    my $job  = shift;
    my @args = @_;

    $job->app->log->info("Refreshing calendar data");

    foreach my $display ($job->app->schema->resultset('Display')->all_displays) {
        $job->app->log->info("Processing display #" . $display->id);
        my $config = PortalCalendar::Config->new(app => $job->app, display => $display);

        foreach my $calendar_no (1 .. 3) {
            $job->app->log->info("  Processing calendar #" . $calendar_no);
            next unless $config->get("web_calendar${calendar_no}");

            my $url = $config->get("web_calendar_ics_url${calendar_no}");
            next unless $url;

            my $calendar = PortalCalendar::Integration::iCal->new(ics_url => $url, db_cache_id => $display->id . '/' . $calendar_no, app => $job->app, config => $config);
            $calendar->get_events(1);    # forced parse, then store to database
        }
    }
    $job->app->log->info("Finished processing");

    return;
}

sub reload_weather {
    my $job  = shift;
    my @args = @_;

    $job->app->log->info("Refreshing weather data");

    foreach my $display ($job->app->schema->resultset('Display')->all_displays) {
        $job->app->log->info("Processing display #" . $display->id);
        my $config = PortalCalendar::Config->new(app => $job->app, display => $display);

        next unless $config->get("openweather");

        my $api = PortalCalendar::Integration::OpenWeather->new(app => $job->app, db_cache_id => $display->id, config => $config);

        # forced parse, then store to database
        $api->fetch_current_from_web(1);
        $api->fetch_forecast_from_web(1);
    }
    $job->app->log->info("Finished processing");

    return;
}

sub reload_googlefit {
    my $job  = shift;
    my @args = @_;

    $job->app->log->info("Refreshing Google Fit data");

    foreach my $display ($job->app->schema->resultset('Display')->all_displays) {
        $job->app->log->info("Processing display #" . $display->id);
        my $config = PortalCalendar::Config->new(app => $job->app, display => $display);

        my $api = PortalCalendar::Integration::Google::Fit->new(app => $job->app, db_cache_id => $display->id, config => $config);

        # forced parse, then store to database
        $api->fetch_from_web(1);
    }
    $job->app->log->info("Finished processing");

    return;
}

1;