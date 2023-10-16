# Controller
package PortalCalendar::Controller::Data;
use Mojo::Base 'PortalCalendar::Controller';

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
    if (!$display) {
        $display = $self->schema->resultset('Display')->create(
            {
                mac       => $self->mac,
                name      => "MAC " . uc($self->mac),
                width     => $self->req->param('w') // -1,
                height    => $self->req->param('h') // -1,
                rotation  => $self->req->param('r') // 0,
                colortype => $self->req->param('c') // '?',
            }
        );
    }

    $self->set_config('_last_visit', DateTime->now()->iso8601);

    $self->set_config('_last_voltage_raw', $self->req->param('adc') // $self->req->param('voltage_raw') // '');    # value has NOT NULL restriction

    my $util = PortalCalendar::Util->new(app => $self, display => $display);
    $util->update_mqtt('voltage',         $self->get_calculated_voltage + 0.001);                                  # to force grafana to store changed values
    $util->update_mqtt('voltage_raw',     $self->get_config('_last_voltage_raw') + 0.001);                         # to force grafana to store changed values
    $util->update_mqtt('battery_percent', $self->calculate_battery_percent() + 0.001);                             # to force grafana to store changed values
    $util->update_mqtt('last_visit',      DateTime->now()->rfc3339);
    $util->update_mqtt('firmware',        $self->req->param('fw'));

    $util->update_mqtt('voltage',         $self->get_calculated_voltage);
    $util->update_mqtt('voltage_raw',     $self->get_config('_last_voltage_raw'));
    $util->update_mqtt('battery_percent', $self->calculate_battery_percent());

    my $ret = {

        # {// undef} to force scalars
        sleep               => $self->get_config('sleep_time') // undef,
        is_critical_voltage => (($self->get_calculated_voltage < $self->get_config('alert_voltage')) ? \1 : \0),    # JSON true/false
        battery_percent     => $self->calculate_battery_percent() // undef,
        ota_mode            => ($self->get_config('ota_mode') ? \1 : \0),                                           # JSON true/false
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

    my $numcolors       = 256;
    my $format          = 'raw8bpp';
    my $colormap_name   = 'gray';      # see Imager::ImageTypes
    my $colormap_colors = [];          # only for the 'none' colormap_name
    if ($self->display->colortype eq 'BW') {
        $numcolors     = 2;
        $colormap_name = 'mono';
        $format        = 'raw1bpp';
    } elsif ($self->display->colortype eq '4G') {
        $numcolors     = 4;
        $colormap_name = 'gray4';
        $format        = 'raw2bpp';
    } elsif ($self->display->colortype eq '16G') {
        $numcolors     = 16;
        $colormap_name = 'gray16';
        $format        = 'raw4bpp';
    } elsif ($self->display->colortype eq '3C') {    # FIXME maybe rename to 3C_RED ?
        $numcolors     = 3;
        $colormap_name = 'webmap';

        # $colormap_name   = 'none';
        # $colormap_colors = [ '#000000', '#ffffff', '#ff0000' ];
        $format = 'raw1bpp3c';
    }

    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);
    return $util->generate_bitmap(
        {
            rotate          => $self->display->rotation,
            gamma           => $self->display->gamma,
            numcolors       => $numcolors,
            colormap_name   => $colormap_name,
            colormap_colors => $colormap_colors,
            format          => $format,
        }
    );
}

# FIXME obsoleted, deprecated, to be removed in client code.
sub bitmap_epaper_mono {
    my $self = shift;

    $self->set_config('_last_visit', DateTime->now()->iso8601);
    my $util = PortalCalendar::Util->new(app => $self, display => $self->display);
    return $util->generate_bitmap(
        {
            rotate        => 3,
            numcolors     => 2,
            gamma         => 1.8,
            format        => 'raw1bpp',
            colormap_name => 'mono',
        }
    );
}

1;