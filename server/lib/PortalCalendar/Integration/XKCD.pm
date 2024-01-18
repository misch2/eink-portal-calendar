package PortalCalendar::Integration::XKCD;

use Mojo::Base qw/PortalCalendar::Integration/;

use Mojo::JSON qw(decode_json);
use Mojo::URL;

use DateTime;
use Time::Seconds;

has 'lwp_max_cache_age' => 4 * ONE_HOUR;

# See documentation at https://xkcd.com/json.html
has raw_json_from_web => sub {
    my $self = shift;

    my $url      = Mojo::URL->new('https://xkcd.com/info.0.json');
    my $response = $self->caching_ua->get($url->to_string);

    die $response->status_line . "\n" . $response->content
        unless $response->is_success;

    return $response->decoded_content;
};

has json => sub {
    my $self = shift;
    my $text = $self->raw_json_from_web();
    return decode_json($text);
};

1;