use Mojo::Base -strict;
use Mojo::File;
use Mojo::Util qw(sha1_sum);

use Test::More;
use Test::Mojo;
use Test::MockModule;
use Imager;
use Data::HexDump;

my $t = Test::Mojo->new('PortalCalendar');

#my $db_cache = PortalCalendar::DatabaseCache->new(app => $t->app, creator => 'mock tests');

my $display = $t->app->schema->resultset('Display')->new(
    {
        width     => 800,
        height    => 480,
        rotation  => 3,
        colortype => '3C',
        gamma     => 1.8,
    }
);

my $mock_data = Test::MockModule->new('PortalCalendar::Controller::Data');
$mock_data->mock(
    display => sub {
        return $display;
    }
);

my $mock_util = Test::MockModule->new('PortalCalendar::Util');
$mock_util->mock(
    image_name => sub {
        return 't/PortalCalendar/Util/bitmap_3C.png';
    }
);

my $tt = $t->get_ok('/calendar/bitmap')->status_is(200)->content_type_is('image/png');
is(sha1_sum($tt->tx->res->body), 'cf1eb72a4fcd4b7ae8a79cd47e6e5ecc4835c7e0', 'sha1 of bitmap data');

my $image = Imager->new(data => $tt->tx->res->body);
is($image->getwidth,   480, 'width of bitmap');
is($image->getheight,  800, 'height of bitmap');
is($image->colorcount, 4,   'colorcount of bitmap');

$tt = $t->get_ok('/calendar/bitmap/epaper')->status_is(200)->content_type_is('application/octet-stream');
my $data = $tt->tx->res->body;
is(sha1_sum($data),     'a59b92cb1e3949b930893138eaf7a87489c47c66',  'sha1 of bitmap data');
is(length($data),       $display->width * $display->height / 4 + 44, 'length of bitmap data');           # 44 bytes header
is(substr($data, 0, 3), "MM\n",                                      'magic number in bitmap header');

done_testing();
