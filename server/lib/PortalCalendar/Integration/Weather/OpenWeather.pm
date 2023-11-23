package PortalCalendar::Integration::Weather::OpenWeather;

use Mojo::Base qw/PortalCalendar::Integration/;

use Mojo::JSON qw(decode_json encode_json);
use Mojo::URL;

use DDP;
use Time::Seconds;

use PortalCalendar::DatabaseCache;

has 'api_key' => sub {
    my $self = shift;
    return $self->display->get_config('openweather_api_key');
};

sub fetch_current_from_web {
    my $self = shift;

    my $cache = $self->db_cache;
    $cache->max_age(30 * ONE_MINUTE);

    my $url = Mojo::URL->new('https://api.openweathermap.org/data/2.5/weather')->query(
        lat   => sprintf("%.3f", $self->display->get_config('lat')),
        lon   => sprintf("%.3f", $self->display->get_config('lon')),
        units => 'metric',
        appid => $self->api_key,
        lang  => $self->display->get_config('openweather_lang'),
    )->to_unsafe_string;

    return $cache->get_or_set(
        sub {

            $self->app->log->debug("GET $url");
            my $response = $self->caching_ua->get($url);

            die $response->status_line . "\n" . $response->content
                unless $response->is_success;

            return decode_json($response->decoded_content);
        },
        { url => $url }
    );
}

sub fetch_forecast_from_web {
    my $self = shift;

    my $cache = $self->db_cache;
    $cache->max_age(30 * ONE_MINUTE);

    my $url = Mojo::URL->new('https://api.openweathermap.org/data/2.5/forecast')->query(
        lat   => $self->display->get_config('lat'),
        lon   => $self->display->get_config('lon'),
        units => 'metric',
        appid => $self->api_key,
        lang  => $self->display->get_config('openweather_lang'),
    )->to_unsafe_string;

    my $data = $cache->get_or_set(
        sub {

            $self->app->log->debug("GET $url");
            my $response = $self->caching_ua->get($url);

            die $response->status_line . "\n" . $response->content
                unless $response->is_success;

            return decode_json($response->decoded_content);
        },
        { url => $url }
    );

    return $data;
}

1;