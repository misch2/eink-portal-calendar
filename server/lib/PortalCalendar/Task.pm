package PortalCalendar::Task;

use Mojo::Base 'Mojolicious';

use DateTime;
use DateTime::Format::ISO8601;
use DDP;
use Time::Seconds;
use Try::Tiny;

use PortalCalendar::Config;
use PortalCalendar::Web2Png;
use PortalCalendar::Util;

sub regenerate_all_images {
    my $job = shift;

    foreach my $display ($job->app->schema->resultset('Display')->all_displays) {
        $job->app->minion->enqueue('regenerate_image', [ $display->id ]);
    }

    return $job->finish();
}

sub regenerate_image {
    my $job = shift;

    # my @args = @_;
    my $display_id = shift;

    my $start = time;

    my $display = $job->app->get_display_by_id($display_id);
    $job->app->log->info("Regenerating calendar image for display #" . $display->id);

    # FIXME don't just request "/calendar/id/html" via web, because that will trigger a refresh of the calendar IN THE WEBSERVER (UI thread) and not in this worker thread. And it causes errors like "Worker 1837132 has no heartbeat (50 seconds), restarting" when the UI thread is busy with getting calendar data etc..
    # It would be much better to do all the calculations here and only use the "/html" route to get the HTML code using cached-only data.
    # I.e. something like:
    #   - minion job:
    #      - fetch all the data from web and do all the calculations (calendars, weather, etc.)
    #      - store the data in the cache (ideally with something like "force cache expiry to be at least now + 5 minutes" to prevent race condition)
    #      - call the "/html" route to get the HTML code
    #   - UI thread:
    #      - just return the HTML code from the cache (i.e. with a "forced cache hit" option)

    # For now we just do the calculations here in the worker thread and we hope the result will be cached for the next 5 minutes (see FIXME above)
    $job->app->log->debug("prefetching cached data");
    my $util = PortalCalendar::Util->new(app => $job->app, display => $display);
    $util->html_for_date(DateTime->now(time_zone => $display->get_config('timezone')));    # FIXME use "forced cache expiry + N " option

    $job->app->log->debug("generating img from cached data");
    my $converter = PortalCalendar::Web2Png->new(pageres_command => $job->app->home->child("node_modules/.bin/pageres"));
    $converter->convert_url($job->app->config->{url_start} . '/calendar/' . $display->id . '/html', $display->virtual_width, $display->virtual_height, $job->app->home->child("generated_images/current_calendar_" . $display->id . ".png"));

    $job->app->log->info("Finished processing");

    my $elapsed = time - $start;
    return $job->finish($elapsed);
}

sub check_missed_connects {
    my $job  = shift;
    my @args = @_;

    my $start = time;

    $job->app->log->debug("Checking missed connections (frozen or empty battery displays)");

    my $now = DateTime->now(time_zone => 'UTC');    # the same timezone as _last_visit

    foreach my $display ($job->app->schema->resultset('Display')->all_displays) {
        try {
            my $last_visit = $display->last_visit;
            if ($last_visit) {
                my ($next, undef, undef) = $display->next_wakeup_time($last_visit);

                if ($next < $now) {
                    my $diff_seconds = $now->epoch - $last_visit->epoch;

                    $last_visit->set_time_zone('local');
                    $next->set_time_zone('local');
                    if (!$display->missed_connects) {    # first time
                        $job->app->log->warn("Display #" . $display->id . " is frozen for " . $diff_seconds . " seconds, last contact was at $last_visit, should have connected at $next");

                        # FIXME send alert
                    }
                    $display->set_missed_connects(1 + $display->missed_connects);
                }
            }
        } catch {
            $job->app->log->warn("Error while checking missed connections for display #" . $display->id . ": $_");
        };
    }

    my $elapsed = time - $start;
    return $job->finish($elapsed);
}

1;