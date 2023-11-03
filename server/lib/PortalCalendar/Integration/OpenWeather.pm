package PortalCalendar::Integration::OpenWeather;

use Mojo::Base qw/PortalCalendar::Integration/;

use Mojo::Base -base;
use Mojo::JSON qw(decode_json encode_json);
use Mojo::URL;

use PortalCalendar::DatabaseCache;

use DDP;
use Time::Seconds;

has 'api_key' => sub {
    my $self = shift;
    return $self->config->get('openweather_api_key');
};

sub fetch_current_from_web {
    my $self = shift;

    my $cache = $self->db_cache;
    $cache->max_age(30 * ONE_MINUTE);

    my $url = Mojo::URL->new('https://api.openweathermap.org/data/2.5/weather')->query(
        lat   => sprintf("%.3f", $self->config->get('lat')),
        lon   => sprintf("%.3f", $self->config->get('lon')),
        units => 'metric',
        appid => $self->api_key,
        lang  => $self->config->get('openweather_lang'),
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
        lat   => $self->config->get('lat'),
        lon   => $self->config->get('lon'),
        units => 'metric',
        appid => $self->api_key,
        lang  => $self->config->get('openweather_lang'),
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