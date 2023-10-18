package PortalCalendar::Integration::SvatkyAPI;

use base qw/PortalCalendar::Integration/;

use Mojo::Base -base;
use Mojo::JSON qw(decode_json);
use Mojo::URL;
use PortalCalendar::DatabaseCache;

use DateTime;
use Time::Seconds;

has 'lwp_max_cache_age' => 8 * ONE_HOUR;

sub get_today_details {
    my $self = shift;
    my $date = shift // DateTime->now();

    my $url      = Mojo::URL->new('https://svatkyapi.cz/api/day/' . $date->ymd('-'))->to_unsafe_string;
    my $response = $self->caching_ua->get($url);

    die $response->status_line . "\n" . $response->content
        unless $response->is_success;

    return decode_json($response->decoded_content);
}

1;