package PortalCalendar::Controller;
use Mojo::Base 'Mojolicious::Controller';

use DateTime;
use DateTime::Format::Strptime;
use DateTime::Format::ISO8601;
use DDP;
use Time::HiRes;

# FIXME for oauth:
use Mojo::Util qw(url_escape);
use Mojo::JSON qw(decode_json encode_json);
use Mojo::Log;

use PortalCalendar;
use PortalCalendar::Config;
use PortalCalendar::Minion;
use PortalCalendar::Schema;
use PortalCalendar::Util;

has display => sub { die "override in subclass" };

has config_obj => sub {
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
};

sub get_calculated_voltage {
    my $self = shift;

    my $raw_adc_reading       = $self->get_config('_last_voltage_raw');
    my $voltage_divider_ratio = $self->get_config('voltage_divider_ratio');
    return unless $raw_adc_reading && $voltage_divider_ratio;

    my $adc_reference_voltage = 3.3;
    my $adc_resolution        = 4095;

    my $voltage = $raw_adc_reading * $adc_reference_voltage / $adc_resolution * $voltage_divider_ratio;

    return $voltage;
}

sub calculate_battery_percent {
    my $self = shift;
    my $min  = $self->get_config('min_voltage');
    my $max  = $self->get_config('max_voltage');

    my $cur = $self->get_calculated_voltage;
    return unless $min && $max && $cur;
    return 100 * ($cur - $min) / ($max - $min);
}

1;