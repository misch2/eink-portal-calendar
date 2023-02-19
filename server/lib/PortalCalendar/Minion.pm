package PortalCalendar::Minion;

use Mojolicious::Lite;
use PortalCalendar::Web2Png;
use PortalCalendar::Integration::iCal;
use DDP;

sub regenerate_image {
    my $job  = shift;
    my @args = @_;

    my $start = time;

    $job->app->log->info("Regenerating calendar image");

    # if (0) {
    #     return $job->fail("error message");
    # }

    my $converter = PortalCalendar::Web2Png->new(pageres_command => app->home->child("node_modules/.bin/pageres"));
    $converter->convert_url($job->app->config->url_start . '/calendar/html', 480, 800, app->home->child("generated_images/current_calendar.png"));

    my $elapsed = time - $start;
    return $job->finish($elapsed);
}

sub reload_calendars {
    my $job  = shift;
    my @args = @_;

    $job->app->log->info("Refreshing calendar data");

    foreach my $calendar_no (1 .. 3) {
        next unless $job->app->get_config("web_calendar${calendar_no}");

        my $url = $job->app->get_config("web_calendar_ics_url${calendar_no}");
        next unless $url;

        my $calendar = PortalCalendar::Integration::iCal->new(ics_url => $url, cache_dir => $job->app->app->home->child("cache/lwp"), db_cache_id => $calendar_no, app => $job->app);
        $calendar->get_events(1);   # forced parse, then store to database
    }

    return;
}

1;