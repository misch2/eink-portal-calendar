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

    $self->set_config('_last_visit', DateTime->now()->iso8601);

    $self->set_config('_last_voltage_raw', $self->req->param('adc') // $self->req->param('voltage_raw') // '');    # value has NOT NULL restriction

    my $util = PortalCalendar::Util->new(app => $self, display => $display);
    $util->update_mqtt('voltage',         $display->voltage + 0.001);                                              # to force grafana to store changed values
    $util->update_mqtt('battery_percent', $display->battery_percent() + 0.001);                                    # to force grafana to store changed values
    $util->update_mqtt('voltage_raw',     $self->get_config('_last_voltage_raw') + 0.001);                         # to force grafana to store changed values
    $util->update_mqtt('firmware',        $display->firmware);
    $util->update_mqtt('last_visit',      DateTime->now()->rfc3339);

    $util->update_mqtt('voltage',         $display->voltage);
    $util->update_mqtt('battery_percent', $display->battery_percent());
    $util->update_mqtt('voltage_raw',     $self->get_config('_last_voltage_raw'));

    my $ret = {

        # {// undef} to force scalars
        sleep               => $self->get_config('sleep_time') // undef,
        is_critical_voltage => (($display->voltage < $self->get_config('alert_voltage')) ? \1 : \0),    # JSON true/false
        battery_percent     => $display->battery_percent() // undef,
        ota_mode            => ($self->get_config('ota_mode') ? \1 : \0),                               # JSON true/false
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
    my $colors        = $self->req->param('colors')        // 256;
    my $gamma         = $self->req->param('gamma')         // 1.0;
    my $format        = $self->req->param('format')        // 'png';
    my $colormap_name = $self->req->param('colormap_name') // 'webmap';

    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);
    return $util->generate_bitmap(
        {
            rotate        => $rotate,
            flip          => $flip,
            numcolors     => $colors,
            gamma         => $gamma,
            format        => $format,
            colormap_name => $colormap_name,
        }
    );
}

# Return bitmap to client (ePaper display, special format of bitmnap):
sub bitmap_epaper {
    my $self = shift;

    $self->set_config('_last_visit', DateTime->now()->iso8601);

    my $numcolors;
    my $colormap_name;                                # see Imager::ImageTypes
    my $colormap_colors = [];                         # only for the 'none' colormap_name
    my $format          = 'epaper_native';
    my $rotate          = $self->display->rotation;

    if ($self->display->colortype eq 'BW') {
        $numcolors       = 2;
        $colormap_name   = 'none';
        $colormap_colors = [ '#000000', '#ffffff' ];
    } elsif ($self->display->colortype eq '4G') {
        $numcolors       = 4;
        $colormap_name   = 'none';
        $colormap_colors = [ '#000000', '#555555', '#aaaaaa', '#ffffff' ];
    } elsif ($self->display->colortype eq '16G') {
        $numcolors     = 16;
        $colormap_name = 'gray16';
    } elsif ($self->display->colortype eq '3C') {
        $numcolors       = 3;
        $colormap_name   = 'none';
        $colormap_colors = [ '#000000', '#ffffff', '#ff0000', '#ffff00' ];
    } else {
        die "unknown display type: " . $self->display->colortype;
    }

    if ($self->req->param('ui_preview')) {
        $format = 'png';
        $rotate = 0;
    }

    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);
    return $util->generate_bitmap(
        {
            rotate          => $rotate,
            gamma           => $self->display->gamma,
            numcolors       => $numcolors,
            colormap_name   => $colormap_name,
            colormap_colors => $colormap_colors,
            format          => $format,
            display_type    => $self->display->colortype,
        }
    );
}

1;