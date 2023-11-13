package PortalCalendar::Config;

use Mojo::Base -base;

use DDP;
use List::Util qw(any);

has 'app';
has 'display';

has 'defaults' => sub {
    my $self = shift;
    return {
        wakeup_schedule          => '0 * * * *',                                     # every hour
        min_voltage              => 3.6,    # FIXME remove
        max_voltage              => 6.0,    # FIXME remove
        alert_voltage            => 4.0,    # FIXME remove
        voltage_divider_ratio    => 2.0,    # FIXME remove
        timezone                 => 'UTC',
        broken_glass             => 0,
        web_calendar1            => 0,
        web_calendar2            => 0,
        web_calendar3            => 0,
        web_calendar_ics_url1    => '',
        web_calendar_ics_url2    => '',
        web_calendar_ics_url3    => '',
        totally_random_icon      => 0,
        min_random_icons         => 4,
        max_random_icons         => 10,
        max_icons_with_calendar  => 5,
        theme                    => 'portal_with_icons',
        openweather              => 0,
        openweather_api_key      => '',
        openweather_lang         => 'en',
        mqtt                     => 0,
        mqtt_server              => 'localhost',
        mqtt_username            => '',
        mqtt_password            => '',
        mqtt_topic               => 'portal_calendar01',
        lat                      => '',
        lon                      => '',
        ota_mode                 => 0,
        googlefit_client_id      => '',
        googlefit_client_secret  => '',
        googlefit_auth_callback  => 'https://local-server-name/auth/googlefit/cb',
        _googlefit_access_token  => '',
        _googlefit_refresh_token => '',
    };
};

has boolean_parameters => sub {
    return [
        qw/
            totally_random_icon
            broken_glass
            web_calendar1
            web_calendar2
            web_calendar3
            mqtt
            openweather
            ota_mode
            /
    ];
};

has 'parameters' => sub {
    my $self = shift;
    return [ keys %{ $self->defaults } ];
};

sub is_boolean_parameter {
    my $self = shift;
    my $name = shift;

    return 1 if any { $_ eq $name } @{ $self->boolean_parameters };
    return 0;
}

sub get {
    my $self = shift;
    my $name = shift;

    my $item = $self->app->schema->resultset('Config')->find({ name => $name, display_id => ($self->display ? $self->display->id : undef) });

    return $item->value if $item;
    return $self->defaults->{$name} // undef;
}

sub set {
    my $self  = shift;
    my $name  = shift;
    my $value = shift;

    $value //= 0 if $self->is_boolean_parameter($name);

    if (my $item = $self->app->schema->resultset('Config')->find({ name => $name, display_id => ($self->display ? $self->display->id : undef) })) {
        $item->update({ value => $value });
    } else {
        $self->app->schema->resultset('Config')->create(
            {
                name       => $name,
                value      => $value,
                display_id => ($self->display ? $self->display->id : undef)
            }
        );
    }

    return;
}

1;