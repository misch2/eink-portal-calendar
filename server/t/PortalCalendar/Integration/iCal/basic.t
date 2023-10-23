#!/usr/bin/env perl
use Mojo::Base -strict;
use Mojo::File qw(curfile);

use Test::Mojo;
use Test::More;
use Test::MockModule;
use Test::MockObject::Extends;

use DDP;
use Encode;
use PortalCalendar::Config;
use PortalCalendar::Integration::iCal;

my $t      = Test::Mojo->new('PortalCalendar');
my $config = PortalCalendar::Config->new(app => $t->app, display => undef);

my $mock_dbcache_used = 0;
my $mock_dbcache      = Test::MockModule->new('PortalCalendar::DatabaseCache');
$mock_dbcache->mock('get_or_set' => sub { $mock_dbcache_used = 1; return $_[1]->() });    # run the callback directly, don't cache

my $mock_ua_used = 0;
my $mock_ua      = Test::MockModule->new('LWP::UserAgent::Cached');
$mock_ua->mock(
    'get' => sub {
        $mock_ua_used = 1;
        my $data = sample_data();
        return HTTP::Response->new(
            200, 'OK',
            [
                'Content-Type'   => 'text/calendar; charset=utf-8',
                'Content-Length' => length($data),
            ],
            $data
        );
    }
);

my $api = PortalCalendar::Integration::iCal->new(app => $t->app, config => $config);

my $x = $api->raw_details_from_web(1);
is($mock_ua_used, 1, "UA mock OK");
like($x, qr/^BEGIN:VCALENDAR/, "Raw data looks like a calendar");

my $y = $api->get_all();
is($mock_dbcache_used,   1, "Cache mock OK");
is(exists($y->{cals}),   1);
is(exists($y->{events}), 1);
is(exists($y->{todos}),  1);

my @events = $api->get_today_events();
is_deeply(\@events, [], "No events today");

@events = $api->get_today_events(DateTime->new(year => 2023, month => 6, day => 29));
is($events[0]->{SUMMARY}, 'Summary 2', "Event name OK");

done_testing();

exit;

##########################################################

sub sample_data {
    my $data = <<'EOT';
BEGIN:VCALENDAR
METHOD:PUBLISH
PRODID:Microsoft Exchange Server 2010
VERSION:2.0
X-WR-CALNAME:Kalendář
BEGIN:VTIMEZONE
TZID:Central Europe Standard Time
BEGIN:STANDARD
DTSTART:16010101T030000
TZOFFSETFROM:+0200
TZOFFSETTO:+0100
RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=10
END:STANDARD
BEGIN:DAYLIGHT
DTSTART:16010101T020000
TZOFFSETFROM:+0100
TZOFFSETTO:+0200
RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=3
END:DAYLIGHT
END:VTIMEZONE
BEGIN:VTIMEZONE
TZID:GMT Standard Time
BEGIN:STANDARD
DTSTART:16010101T020000
TZOFFSETFROM:+0100
TZOFFSETTO:+0000
RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=10
END:STANDARD
BEGIN:DAYLIGHT
DTSTART:16010101T010000
TZOFFSETFROM:+0000
TZOFFSETTO:+0100
RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=3
END:DAYLIGHT
END:VTIMEZONE
BEGIN:VEVENT
DESCRIPTION:Event description 1
RRULE:FREQ=WEEKLY;UNTIL=20230601T084500Z;INTERVAL=1;BYDAY=TU,TH;WKST=MO
UID:040000008200E00074C5B7101A82E00800000000A08DC62F5920D901000000000000000
 010000000DF80837F1CADC2468C341269316F114B
SUMMARY:Event name 1
DTSTART;TZID=Central Europe Standard Time:20230530T104500
DTEND;TZID=Central Europe Standard Time:20230530T110000
CLASS:PUBLIC
PRIORITY:5
DTSTAMP:20230629T090034Z
TRANSP:OPAQUE
STATUS:CONFIRMED
SEQUENCE:30
LOCATION:Schůzka Microsoft Teams
X-MICROSOFT-CDO-APPT-SEQUENCE:30
X-MICROSOFT-CDO-BUSYSTATUS:BUSY
X-MICROSOFT-CDO-INTENDEDSTATUS:BUSY
X-MICROSOFT-CDO-ALLDAYEVENT:FALSE
X-MICROSOFT-CDO-IMPORTANCE:1
X-MICROSOFT-CDO-INSTTYPE:1
X-MICROSOFT-DONOTFORWARDMEETING:FALSE
X-MICROSOFT-DISALLOW-COUNTER:FALSE
END:VEVENT
BEGIN:VEVENT
DESCRIPTION:Event description 2
UID:040000008200E00074C5B7101A82E00800000000B69ADE360BA5D901000000000000000
 0100000002C1BC6C5CCEF1E4A9220EDC0F3B0570D
SUMMARY:Summary 2
DTSTART;TZID=GMT Standard Time:20230629T110000
DTEND;TZID=GMT Standard Time:20230629T120000
CLASS:PUBLIC
PRIORITY:5
DTSTAMP:20230629T090034Z
TRANSP:OPAQUE
STATUS:CONFIRMED
SEQUENCE:0
LOCATION:Microsoft Teams Meeting
X-MICROSOFT-CDO-APPT-SEQUENCE:0
X-MICROSOFT-CDO-BUSYSTATUS:BUSY
X-MICROSOFT-CDO-INTENDEDSTATUS:BUSY
X-MICROSOFT-CDO-ALLDAYEVENT:FALSE
X-MICROSOFT-CDO-IMPORTANCE:1
X-MICROSOFT-CDO-INSTTYPE:0
X-MICROSOFT-DONOTFORWARDMEETING:FALSE
X-MICROSOFT-DISALLOW-COUNTER:FALSE
END:VEVENT
END:VCALENDAR
EOT
    return encode_utf8($data);
}