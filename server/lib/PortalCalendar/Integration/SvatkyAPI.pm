package PortalCalendar::Integration::SvatkyAPI;

use Mojo::Base qw/PortalCalendar::Integration/;

use Mojo::JSON qw(decode_json);
use Mojo::URL;

use DateTime;
use Time::Seconds;

has 'lwp_max_cache_age' => 4 * ONE_HOUR;

sub raw_details_from_web {
    my $self = shift;
    my $date = shift;

    my $url      = Mojo::URL->new('https://svatkyapi.cz/api/day/' . $date->ymd('-'))->to_unsafe_string;
    my $response = $self->caching_ua->get($url);

    die $response->status_line . "\n" . $response->content
        unless $response->is_success;

    return $response->decoded_content;
}

sub transform_details {
    my $self     = shift;
    my $raw_text = shift;

    my $raw = decode_json($raw_text);

    my $ret = {
        date    => $raw->{date},
        as_bool => {
            holiday => $raw->{isHoliday},
        },
        as_number => {
            day   => $raw->{dayNumber},
            month => $raw->{monthNumber},
            year  => $raw->{year},
        },
        as_text => {
            day_of_week => $raw->{dayInWeek},
            month       => {
                nominative => $raw->{month}->{nominative},
                genitive   => $raw->{month}->{genitive},
            },
            name    => $raw->{name},
            holiday => $raw->{holidayName},
        }
    };

    return $ret;
}

sub get_today_details {
    my $self = shift;
    my $date = shift // DateTime->now();

    my $cache = $self->db_cache;
    $cache->max_age(1 * ONE_DAY);

    return $cache->get_or_set(
        sub {
            my $raw       = $self->raw_details_from_web($date);
            my $processed = $self->transform_details($raw);
            return $processed;
        },
        { date => $date->ymd('-') }    # date only, ignore the time
    );
}

1;