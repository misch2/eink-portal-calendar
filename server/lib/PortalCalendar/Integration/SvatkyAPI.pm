package PortalCalendar::Integration::SvatkyAPI;

use base qw/PortalCalendar::Integration/;

use Mojo::Base -base;
use Mojo::JSON qw(decode_json encode_json);
use Mojo::URL;
use Mojo::File;
use PortalCalendar::DatabaseCache;

use LWP::UserAgent::Cached;
use iCal::Parser;
use DDP;
use DateTime;

has 'max_cache_age' => 60 * 60 * 8;    # 8 hours

sub get_today_details {
    my $self   = shift;
    my $date   = shift // DateTime->now();
    my $forced = shift;

    my $cache = PortalCalendar::DatabaseCache->new(app => $self->app);
    my $data  = $cache->get_or_set(
        sub {
            my $url = Mojo::URL->new('https://svatkyapi.cz/api/day/' . $date->ymd('-'))->to_unsafe_string;

            $self->app->log->debug($url);
            my $response = $self->caching_ua->get($url);

            die $response->status_line . "\n" . $response->content
                unless $response->is_success;

            return decode_json($response->decoded_content);
        },
        $self->db_cache_id . "/" . $date->ymd,
        $forced
    );

    # {
    #     date          "2023-10-15" (dualvar: 2023),
    #     dayInWeek     "neděle",
    #     dayNumber     15,
    #     holidayName   undef,
    #     isHoliday     0 (JSON::PP::Boolean) (read-only),
    #     month         {
    #         genitive     "října",
    #         nominative   "říjen"
    #     },
    #     monthNumber   10,
    #     name          "Tereza",
    #     year          2023
    # }
    return $data;
}

1;