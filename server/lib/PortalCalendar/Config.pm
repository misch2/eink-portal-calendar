package PortalCalendar::Config;

use Mojo::Base -base;

use DDP;
use List::Util qw(any);

has 'app';
has 'display';

has 'defaults' => sub {
    my $self = shift;
    return {
        wakeup_schedule                => '0 * * * *',                                     # every hour
        timezone                       => 'UTC',
        web_calendar1                  => 0,
        web_calendar2                  => 0,
        web_calendar3                  => 0,
        web_calendar_ics_url1          => '',
        web_calendar_ics_url2          => '',
        web_calendar_ics_url3          => '',
        totally_random_icon            => 0,
        min_random_icons               => 4,
        max_random_icons               => 10,
        max_icons_with_calendar        => 5,
        theme                          => 'portal_with_icons',
        googlefit                      => 0,
        metnoweather                   => 0,
        metnoweather_granularity_hours => 2,
        openweather                    => 0,
        openweather_api_key            => '',
        openweather_lang               => 'en',
        telegram                       => 0,
        telegram_api_key               => '',
        telegram_chat_id               => '',
        mqtt                           => 0,
        mqtt_server                    => 'localhost',
        mqtt_username                  => '',
        mqtt_password                  => '',
        mqtt_topic                     => 'portal_calendar01',
        lat                            => '',
        lon                            => '',
        alt                            => '',
        ota_mode                       => 0,
        googlefit_client_id            => '',
        googlefit_client_secret        => '',
        googlefit_auth_callback        => 'https://local-server-name/auth/googlefit/cb',
        _googlefit_access_token        => '',
        _googlefit_refresh_token       => '',
    };
};

has boolean_parameters => sub {
    return [
        qw/
            totally_random_icon
            web_calendar1
            web_calendar2
            web_calendar3
            mqtt
            googlefit
            metnoweather
            openweather
            telegram
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

sub get_from_schema {
    my $self   = shift;
    my $schema = shift;
    my $name   = shift;

    my $item = $schema->resultset('Config')->find({ name => $name, display_id => ($self->display ? $self->display->id : undef) });

    return $item->value if $item;
    return $self->defaults->{$name} // undef;
}

sub set_from_schema {
    my $self   = shift;
    my $schema = shift;
    my $name   = shift;
    my $value  = shift;

    $value //= 0 if $self->is_boolean_parameter($name);

    if (my $item = $schema->resultset('Config')->find({ name => $name, display_id => ($self->display ? $self->display->id : undef) })) {
        $item->update({ value => $value });
    } else {
        $schema->resultset('Config')->create(
            {
                name       => $name,
                value      => $value,
                display_id => ($self->display ? $self->display->id : undef)
            }
        );
    }

    return;
}

sub get {
    my $self = shift;
    my $name = shift;

    return $self->get_from_schema($self->app->schema, $name);
}

sub set {
    my $self  = shift;
    my $name  = shift;
    my $value = shift;

    return $self->set_from_schema($self->app->schema, $name, $value);
}

1;