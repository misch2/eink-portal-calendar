package PortalCalendar::Minion;

use Mojo::Base 'Mojolicious';
use PortalCalendar::Config;
use PortalCalendar::Web2Png;

use DateTime::Format::ISO8601;
use DDP;
use Time::Seconds;
use Try::Tiny;

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