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

1;