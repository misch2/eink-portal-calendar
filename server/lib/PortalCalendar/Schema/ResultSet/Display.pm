package PortalCalendar::Schema::ResultSet::Display;

use base 'DBIx::Class::ResultSet';

sub all_displays {
    my $self = shift;

    return $self->search({}, { -order_by => ['id'] })->all;
}

1;
