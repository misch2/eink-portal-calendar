package PortalCalendar::Config;

use Mojo::Base -base;

use DDP;
use List::Util qw(any);

has 'app';

has 'defaults' => sub {
    my $self = shift;
    return {
        sleep_time              => 3600,
        critical_voltage        => 1.1,
        timezone                => 'UTC',
        broken_glass            => 0,
        web_calendar1           => 0,
        web_calendar2           => 0,
        web_calendar3           => 0,
        web_calendar_ics_url1   => '',
        web_calendar_ics_url2   => '',
        web_calendar_ics_url3   => '',
        totally_random_icon     => 0,
        min_random_icons        => 4,
        max_random_icons        => 10,
        max_icons_with_calendar => 5,
        theme                   => 'portal_with_icons',
        openweather             => 0,
        openweather_api_key     => '',
        openweather_lang        => 'en',
        lat                     => '',
        lon                     => '',
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
            openweather
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

    my $item = $self->app->schema->resultset('Config')->find({ name => $name });

    return $item->value if $item;
    return $self->defaults->{$name};
}

sub set {
    my $self  = shift;
    my $name  = shift;
    my $value = shift;

    $value //= 0 if $self->is_boolean_parameter($name);

    $self->app->schema->resultset('Config')->update_or_create(
        {
            name  => $name,
            value => $value,
        },
        {
            key => 'name_unique',
        }
    );
    return;
}

1;