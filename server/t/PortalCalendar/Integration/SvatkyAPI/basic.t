use Mojo::Base -strict;

use Test::More;
use Test::Mojo;

use PortalCalendar::Integration::SvatkyAPI;
use DateTime;

my $t = Test::Mojo->new('PortalCalendar');
my $api = PortalCalendar::Integration::SvatkyAPI->new(app => $t->app);

my $data = $api->get_today_details(DateTime->new(year => 2023, month => 1, day => 1));
is_deeply ($data, {
    date          => "2023-01-01",
    dayInWeek     => "neděle",
    dayNumber     => 1,
    holidayName   => "Nový rok / Den obnovy samostatného českého státu",
    isHoliday     => 1,
    month         => {
        genitive    => "ledna",
        nominative  => "leden"
    },
    monthNumber  => 1,
    name         => "Nový rok",
    year         => 2023
}, "New year 2023");

$data = $api->get_today_details(DateTime->new(year => 2023, month => 10, day => 18));
is_deeply ($data, {
    date          => "2023-10-18",
    dayInWeek     => "středa",
    dayNumber     => 18,
    holidayName   => undef,
    isHoliday     => 0,
    month         => {
        genitive    => "října",
        nominative  => "říjen"
    },
    monthNumber  => 10,
    name         => "Lukáš",
    year         => 2023
}, "New year 2023");

done_testing();
