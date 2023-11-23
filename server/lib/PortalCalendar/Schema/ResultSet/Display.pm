package PortalCalendar::Schema::ResultSet::Display;

use base 'DBIx::Class::ResultSet';

sub all_displays {
    my $self = shift;

    return $self->search({}, { -order_by => ['id'] })->all;
}

sub default_display {
    my $self = shift;
    return $self->find({ id => 0 });
}

1;
