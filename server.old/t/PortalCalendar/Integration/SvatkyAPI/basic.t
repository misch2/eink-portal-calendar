use Mojo::Base -strict;

use Test::More;
use Test::Mojo;
use Test::MockModule;

use PortalCalendar::Integration::SvatkyAPI;
use DateTime;

my $t        = Test::Mojo->new('PortalCalendar');
my $db_cache = PortalCalendar::DatabaseCache->new(app => $t->app, creator => 'mock tests');

my $api  = PortalCalendar::Integration::SvatkyAPI->new(app => $t->app, db_cache => $db_cache);
my $data = $api->get_today_details(DateTime->new(year => 2023, month => 1, day => 1));
is_deeply(
    $data,
    {
        as_bool => {
            holiday => 1,
        },
        as_number => {
            day   => 1,
            month => 1,
            year  => 2023,
        },
        as_text => {
            day_of_week => 'neděle',
            month       => {
                nominative => "leden",
                genitive   => "ledna",

            },
            holiday => 'Nový rok / Den obnovy samostatného českého státu',
            name    => 'Nový rok',
        },
        date => "2023-01-01",
    },
    "New year 2023"
);

$data = $api->get_today_details(DateTime->new(year => 2023, month => 10, day => 18));
is_deeply(
    $data,
    {
        as_bool => {
            holiday => 0,
        },
        as_number => {
            day   => 18,
            month => 10,
            year  => 2023,
        },
        as_text => {
            day_of_week => 'středa',
            month       => {
                nominative => "říjen",
                genitive   => "října",

            },
            holiday => undef,
            name    => 'Lukáš',
        },
        date => "2023-10-18",
    },
    "18th October 2023"
);

done_testing();
