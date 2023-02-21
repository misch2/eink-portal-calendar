package PortalCalendar::Integration::OpenWeather;

use Mojo::Base -base;
use Mojo::JSON qw(decode_json encode_json);
use Mojo::URL;

use LWP::UserAgent::Cached;
use iCal::Parser;
use DDP;
use DateTime;

has 'app';
has 'cache_dir';

has 'api_key' => sub {
    my $self = shift;
    return $self->app->get_config('openweather_api_key');
};

has 'ua' => sub {
    my $self = shift;
    return LWP::UserAgent::Cached->new(
        cache_dir => $self->cache_dir,

        # nocache_if => sub {
        #     my $response = shift;
        #     return $response->code != 200;    # do not cache any bad response
        # },
        recache_if => sub {
            my ($response, $path, $request) = @_;
            return -M $path > (1 / 24);    #  recache any response older than 1 hour (1/24 of day)
        },
    );
};

sub fetch_current_from_web {
    my $self   = shift;
    my $forced = shift;

    my $cache = PortalCalendar::DatabaseCache->new(app => $self->app);
    return $cache->get_or_set(
        sub {

            my $url = Mojo::URL->new('https://api.openweathermap.org/data/2.5/weather')->query(
                lat   => $self->app->get_config('lat'),
                lon   => $self->app->get_config('lon'),
                units => 'metric',
                appid => $self->api_key,
            )->to_unsafe_string;

            $self->app->log->debug($url);
            my $response = $self->ua->get($url);

            die $response->status_line . "\n" . $response->content
                unless $response->is_success;

            return decode_json($response->decoded_content);
        },
        'weather_current',
        $forced
    );
}

sub fetch_forecast_from_web {
    my $self   = shift;
    my $forced = shift;

    my $cache = PortalCalendar::DatabaseCache->new(app => $self->app);
    return $cache->get_or_set(
        sub {
            my $url = Mojo::URL->new('https://api.openweathermap.org/data/2.5/forecast')->query(
                lat   => $self->app->get_config('lat'),
                lon   => $self->app->get_config('lon'),
                units => 'metric',
                appid => $self->api_key,
            )->to_unsafe_string;

            $self->app->log->debug($url);
            my $response = $self->ua->get($url);

            die $response->status_line . "\n" . $response->content
                unless $response->is_success;

            return decode_json($response->decoded_content);
        },
        'weather_forecast',
        $forced
    );
}

1;