use Mojo::Base -strict;

use Test::More;
use Test::Mojo;

my $t = Test::Mojo->new('PortalCalendar');
$t->get_ok('/')->status_is(200)->content_like(qr#<h3>Select your display</h3>#i);

done_testing();
