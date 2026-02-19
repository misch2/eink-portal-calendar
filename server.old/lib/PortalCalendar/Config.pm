package PortalCalendar::Config;

use Mojo::Base -base;

use DDP;
use List::Util qw(any);

has 'app';
has 'display';

has 'parameters' => sub {
    my $self = shift;
    return [ keys %{ $self->defaults } ];
};

sub get_from_schema_defaults_only {
    my $self   = shift;
    my $schema = shift;
    my $name   = shift;

    my $display = $schema->resultset('Display')->default_display;
    my $item    = $schema->resultset('Config')->find({ name => $name, display_id => $display->id });

    return $item->value if $item;
    return undef;
}

sub get_from_schema_without_defaults {
    my $self   = shift;
    my $schema = shift;
    my $name   = shift;

    my $item = $schema->resultset('Config')->find({ name => $name, display_id => $self->display->id });

    return $item->value if $item;
    return undef;
}

sub get_from_schema {
    my $self   = shift;
    my $schema = shift;
    my $name   = shift;

    my $value = $self->get_from_schema_without_defaults($schema, $name);

    # 1. real value (empty string usually means "unset" in HTML form)
    if (defined $value && $value ne '') {
        return $value;
    }

    # 2. default value (modifiable)
    unless ($self->display->is_default) {
        my $value = $self->get_from_schema_defaults_only($schema, $name);
        if (defined $value && $value ne '') {
            return $value;
        }
    }

    return undef;
}

sub set_from_schema {
    my $self   = shift;
    my $schema = shift;
    my $name   = shift;
    my $value  = shift;

    if (my $item = $schema->resultset('Config')->find({ name => $name, display_id => $self->display->id })) {
        $item->update({ value => $value });
    } else {
        $schema->resultset('Config')->create(
            {
                name       => $name,
                value      => $value,
                display_id => $self->display->id,
            }
        );
    }

    return;
}

1;