package PortalCalendar::Config;

use Mojo::Base -base;
use DDP;

has 'app';

sub get {
    my $self = shift;
    my $name = shift;

    my $item = $self->app->schema->resultset('Config')->search({ name => $name })->first;
    return $item && $item->value;
}

sub set {
    my $self  = shift;
    my $name  = shift;
    my $value = shift;

    my $orig = $self->app->schema->resultset('Config')->search({ name => $name })->first;
    $orig->value($value);
    $orig->update();

    return;
}

1;