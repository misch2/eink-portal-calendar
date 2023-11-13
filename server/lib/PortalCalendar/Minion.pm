package PortalCalendar::Minion;

use Mojo::Base 'Mojolicious';
use PortalCalendar::Config;
use PortalCalendar::Web2Png;

use DateTime::Format::ISO8601;
use DDP;
use Time::Seconds;
use Try::Tiny;

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

        $converter->convert_url($job->app->config->{url_start} . '/calendar/' . $display->id . '/html', $display->virtual_width, $display->virtual_height, $job->app->home->child("generated_images/current_calendar_" . $display->id . ".png"));
    }
    $job->app->log->info("Finished processing");

    my $elapsed = time - $start;
    return $job->finish($elapsed);
}

sub check_missed_connects {
    my $job  = shift;
    my @args = @_;

    my $start = time;

    $job->app->log->info("Checking missed connections (frozen or empty battery displays)");

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
                    $job->app->log->warn("Display #" . $display->id . " is frozen for " . $diff_seconds . " seconds, last contact was at $last_visit, should have connected at $next");
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