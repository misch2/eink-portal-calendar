package PortalCalendar::Web2Png;

use Mojo::Base -base;

use DDP;
use File::pushd;
use File::Copy;

has 'app';

sub convert_url {
    my $self      = shift;
    my $image_url = shift;
    my $width     = shift;
    my $height    = shift;

    my $ua = Mojo::UserAgent->new;

    my $service_url = Mojo::URL->new("http://localhost:" . $self->app->config->{screenshot_service_port} . '/screenshot');
    $service_url->query->merge(
        url => $image_url,
        w   => $width,
        h   => $height,
    );

    $self->app->log->info("Converting $service_url to PNG");
    my $tx = $ua->get($service_url);

    if (!$tx->res->is_success) {
        $self->app->log->error("Error: " . $tx->res->to_string);
        die $tx->res->to_string;
    }

    return $tx->res->body;
}

1;