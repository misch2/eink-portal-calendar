use Mojo::Base -strict;

use Test::More;
use Test::Mojo;

my $t        = Test::Mojo->new('PortalCalendar');
my $db_cache = PortalCalendar::DatabaseCache->new(app => $t->app, creator => 'mock tests');

$t->get_ok('/')->status_is(200)->content_like(qr#<h3>Displays</h3>#i);

done_testing();
