#!/usr/bin/env perl
use Mojo::Base -strict;
use Mojo::File qw(curfile);

use Test::Mojo;
use Test::More;

use DDP;
use PortalCalendar::Integration::Google::Fit;

# Load application script relative to the "t" directory
my $appfile = curfile->dirname->sibling('app');
my $t = Test::Mojo->new($appfile);
#  => {
#     # mocked config
#     _googlefit_access_token => '12345',
#     _googlefit_refresh_token => 'efgh',
#     _googlefit_token_json => '{"a":"b"}',
# });

my $api = PortalCalendar::Integration::Google::Fit->new(app => $t->app);
my $x = $api->fetch_from_web(1);
#p $x;

my $y = $api->get_weight_series();
p $y;

p $api->get_last_known_weight;
