#!/usr/bin/env perl
use Mojo::Base -strict;
use Mojo::File qw(curfile);

use Test::Mojo;
use Test::More;

use DDP;
use PortalCalendar::Config;
use PortalCalendar::Integration::Google::Fit;

my $t = Test::Mojo->new('PortalCalendar');
#  => {
#     # mocked config
#     _googlefit_access_token => '12345',
#     _googlefit_refresh_token => 'efgh',
#     _googlefit_token_json => '{"a":"b"}',
# });

my $config = PortalCalendar::Config->new(app => $t->app, display => undef);
my $api = PortalCalendar::Integration::Google::Fit->new(app => $t->app, config => $config);
my $x = $api->fetch_from_web(1);
#p $x;

my $y = $api->get_weight_series();
p $y;

p $api->get_last_known_weight;
