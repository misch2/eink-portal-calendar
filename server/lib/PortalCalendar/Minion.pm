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

1;