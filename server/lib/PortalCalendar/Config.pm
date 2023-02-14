package PortalCalendar::Config;

use Mojo::Base -base;

use DDP;

has 'app';

has 'defaults' => sub {
    my $self = shift;
    return {
        sleep_time              => 3600,
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
    };
};

has 'parameters' => sub {
    my $self = shift;
    return [ keys %{ $self->defaults } ];
};

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