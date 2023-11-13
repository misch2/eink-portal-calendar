# Controller
package PortalCalendar::Controller::Data;
use Mojo::Base 'PortalCalendar::Controller';

use DateTime;

has mac => sub {
    my $self = shift;
    my $mac  = $self->req->param('mac');
    return undef unless $mac;
    return lc($mac);
};

has display => sub {
    my $self = shift;
    return $self->get_display_by_mac($self->mac);
};

sub ping {
    my $self = shift;
    return $self->render(json => { status => 'ok' });
}

# Return configuration data to client (ePaper display):
sub config {
    my $self = shift;

    my $display = $self->display;
    if ($display) {
        $display->firmware($self->req->param('fw') // '');
        $display->update;
    } else {
        $display = $self->schema->resultset('Display')->create(
            {
                # Physical properties, not to be changed by the user:
                mac       => $self->mac,
                name      => "New display with MAC " . uc($self->mac) . " added on " . DateTime->now->stringify,
                width     => $self->req->param('w'),
                height    => $self->req->param('h'),
                colortype => $self->req->param('c'),
                firmware  => $self->req->param('fw') // '',

                # Logical properties, updatable by the user:
                rotation      => 0,
                gamma         => 2.2,
                border_top    => 0,
                border_right  => 0,
                border_bottom => 0,
                border_left   => 0,
            }
        );
    }

    $self->set_config('_last_visit', DateTime->now()->iso8601);    # UTC
    $display->set_missed_connects(0);

    $self->set_config('_last_voltage_raw', $self->req->param('adc') // $self->req->param('voltage_raw') // '');    # value has NOT NULL restriction
    $self->set_config('_last_voltage',     $self->req->param('v'));
    $self->set_config('_min_voltage',      $self->req->param('vmin'));
    $self->set_config('_max_voltage',      $self->req->param('vmax'));

    my $util = PortalCalendar::Util->new(app => $self, display => $display);
    my ($next_wakeup, $sleep_in_seconds, $schedule) = $display->next_wakeup_time();
    $self->app->log->info("Next wakeup at $next_wakeup (in $sleep_in_seconds seconds) according to crontab schedule '$schedule'");

    $util->update_mqtt_with_forced_jitter('voltage',         $display->voltage,                      0.001);
    $util->update_mqtt_with_forced_jitter('battery_percent', $display->battery_percent(),            0.001);
    $util->update_mqtt_with_forced_jitter('voltage_raw',     $self->get_config('_last_voltage_raw'), 0.001);
    $util->update_mqtt_with_forced_jitter('min_voltage',     $self->get_config('_min_voltage'),      0.001);
    $util->update_mqtt_with_forced_jitter('max_voltage',     $self->get_config('_max_voltage'),      0.001);
    $util->update_mqtt('firmware',   $display->firmware);
    $util->update_mqtt('last_visit', DateTime->now()->rfc3339);
    $util->update_mqtt_with_forced_jitter('sleep_time', $sleep_in_seconds, 1);

    my $ret = {

        # {// undef} to force scalars
        sleep           => $sleep_in_seconds,
        battery_percent => $display->battery_percent() // undef,
        ota_mode        => ($self->get_config('ota_mode') ? \1 : \0),    # JSON true/false
    };

    $self->render(json => $ret);
}

# /calendar/bitmap
# /calendar/bitmap?rotate=1&format=png
# /calendar/bitmap?rotate=2&flip=x
# /calendar/bitmap?rotate=3&flip=xy&format=raw8bpp
sub bitmap {
    my $self = shift;

    my $rotate        = $self->req->param('rotate')        // 0;
    my $flip          = $self->req->param('flip')          // '';
    my $gamma         = $self->req->param('gamma')         // $self->display->gamma;
    my $numcolors     = $self->req->param('colors')        // $self->display->num_colors;
    my $colormap_name = $self->req->param('colormap_name') // 'none';
    my $color_palette = $self->display->color_palette($self->req->param('preview_colors'));
    my $format        = $self->req->param('format') // 'png';

    $colormap_name = 'webmap' if scalar(@$color_palette) == 0;

    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);
    return $util->generate_bitmap(
        {
            rotate          => $rotate,
            flip            => $flip,
            gamma           => $gamma,
            numcolors       => $numcolors,
            colormap_name   => $colormap_name,
            colormap_colors => $color_palette,
            format          => $format,
            display_type    => $self->display->colortype,
        }
    );
}

# Return bitmap to client (ePaper display, special format of bitmnap) and update last_visit timestamp:
sub bitmap_epaper {
    my $self = shift;

    # No, don't update last_visit here, because this is called by the UI itself too, and it would reset the missed_connects counter:when looking at a preview image
    #$self->set_config('_last_visit', DateTime->now()->iso8601);

    my $rotate    = $self->display->rotation;
    my $numcolors = $self->display->num_colors;
    my $format    = 'epaper_native';

    my $color_palette = $self->display->color_palette($self->req->param('preview_colors'));
    my $colormap_name = (scalar(@$color_palette) > 0 ? 'none' : 'webmap');

    if ($self->req->param('web_format')) {
        $format = 'png';
        $rotate = 0;
    }

    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);
    return $util->generate_bitmap(
        {
            rotate          => $rotate,
            flip            => 0,
            gamma           => $self->display->gamma,
            numcolors       => $numcolors,
            colormap_name   => $colormap_name,
            colormap_colors => $color_palette,
            format          => $format,
            display_type    => $self->display->colortype,
        }
    );
}

1;