package PortalCalendar::Integration::OpenWeather;

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

has 'api_key' => sub {
    my $self = shift;
    return $self->config->get('openweather_api_key');
};

has 'max_cache_age' => 60 * 5;    # 5 minutes

sub fetch_current_from_web {
    my $self   = shift;
    my $forced = shift;

    my $cache = PortalCalendar::DatabaseCache->new(app => $self->app);
    return $cache->get_or_set(
        sub {

            my $url = Mojo::URL->new('https://api.openweathermap.org/data/2.5/weather')->query(
                lat   => $self->config->get('lat'),
                lon   => $self->config->get('lon'),
                units => 'metric',
                appid => $self->api_key,
                lang  => $self->config->get('openweather_lang'),
            )->to_unsafe_string;

            $self->app->log->debug($url);
            my $response = $self->caching_ua->get($url);

            die $response->status_line . "\n" . $response->content
                unless $response->is_success;

            return decode_json($response->decoded_content);
        },
        $self->db_cache_id . '/weather_current',
        $forced
    );
}

sub fetch_forecast_from_web {
    my $self   = shift;
    my $forced = shift;

    my $cache = PortalCalendar::DatabaseCache->new(app => $self->app);
    my $data = $cache->get_or_set(
        sub {
            my $url = Mojo::URL->new('https://api.openweathermap.org/data/2.5/forecast')->query(
                lat   => $self->config->get('lat'),
                lon   => $self->config->get('lon'),
                units => 'metric',
                appid => $self->api_key,
                lang  => $self->config->get('openweather_lang'),
            )->to_unsafe_string;

            $self->app->log->debug($url);
            my $response = $self->caching_ua->get($url);

            die $response->status_line . "\n" . $response->content
                unless $response->is_success;

            return decode_json($response->decoded_content);
        },
        $self->db_cache_id . '/weather_forecast',
        $forced
    );

    return $data;
}

1;