package PortalCalendar::Minion;

use Mojolicious::Lite;
use PortalCalendar::Web2Png;
use DDP;

sub regenerate_image {
    my $job  = shift;
    my @args = @_;

    my $start = time;

    #$job->app->log->info("Regenerating calendar image");
    # if (0) {
    #     return $job->fail("error message");
    # }

    my $converter = PortalCalendar::Web2Png->new(pageres_command => app->home->child("node_modules/.bin/pageres"));
    $converter->convert_url($job->app->config->url_start . '/calendar/html', 480, 800, app->home->child("generated_images/current_calendar.png"));

    my $elapsed = time - $start;
    return $job->finish($elapsed);
}

1;