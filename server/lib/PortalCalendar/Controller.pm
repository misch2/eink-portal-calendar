package PortalCalendar::Controller;
use Mojo::Base 'Mojolicious::Controller';

use DateTime;
use DateTime::Format::Strptime;
use DateTime::Format::ISO8601;
use DDP;
use Time::HiRes;
use List::Util qw(min max);
use Mojo::Log;

use PortalCalendar::Config;

has display => sub { die "override in subclass" };

has config_obj => sub { # FIXME remove, use display instead
    my $self = shift;
    return PortalCalendar::Config->new(app => $self->app, display => $self->display);
};

sub get_config {
    my $self = shift;
    my $name = shift;
    return $self->config_obj->get($name);
}

sub set_config {
    my $self  = shift;
    my $name  = shift;
    my $value = shift;
    return $self->config_obj->set($name, $value);
}

1;