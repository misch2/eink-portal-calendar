package PortalCalendar::Integration::Weather::MetNo;

use Mojo::Base qw/PortalCalendar::Integration/;

use Mojo::JSON qw(decode_json encode_json);
use Mojo::URL;

use DDP;
use Time::Seconds;
use DateTime::Format::ISO8601;
use DateTime::Event::Sunrise;
use Storable;
use List::Util qw(min max sum uniq);

use PortalCalendar::Integration::Weather::MetNo::IconsMapping;

has 'lat'      => sub { die "lat is not defined" };
has 'lon'      => sub { die "lon is not defined" };
has 'altitude' => sub { die "altitude is not defined" };

has 'lwp_max_cache_age' => 1 * ONE_DAY;    # Just a safe maximum, real HTTP headers based caching is used in the integration module

has 'dt_format' => sub { DateTime::Format::ISO8601->new };

has 'symbol_mapper' => sub {
    my $self = shift;
    return PortalCalendar::Integration::Weather::MetNo::IconsMapping->new(app => $self->app);
};

has 'sunrise' => sub {
    my $self = shift;
    return DateTime::Event::Sunrise->new(longitude => $self->lon, latitude => $self->lat);
};

has 'url' => sub {
    my $self = shift;

    my $url = Mojo::URL->new("https://api.met.no/weatherapi/locationforecast/2.0/complete");

    # "When using requests with latitude/longitude, truncate all coordinates to max 4 decimals. There is no need to ask for weather forecasts with nanometer precision! For new products, requests with 5+ decimals will return a 403 Forbidden."
    $url->query->merge(
        lat      => sprintf("%.3f", $self->lat),
        lon      => sprintf("%.3f", $self->lon),
        altitude => $self->altitude,
    );

    return $url;
};

sub raw_text_data_from_web {
    my $self = shift;

    my $response = $self->caching_ua->get($self->url->to_unsafe_string);

    die $response->status_line . "\n" . $response->content
        unless $response->is_success;

    return $response->decoded_content;
}

sub raw_json_from_web {
    my $self = shift;

    my $cache = $self->db_cache;
    $cache->max_age(15 * ONE_MINUTE);

    return $cache->get_or_set(
        sub {
            my $text = $self->raw_text_data_from_web;
            return decode_json($text);
        },
        { url => $self->url->to_unsafe_string },
    );
}

sub _extract_data {
    my $self    = shift;
    my $json    = shift;
    my $current = shift;

    return unless exists $current->{data}->{next_1_hours}->{summary};    # skip forecasts without hourly data (too far in the future)

    my $time_start = $self->dt_format->parse_datetime($current->{time})->set_formatter($self->dt_format);
    my $time_end   = $time_start->clone->add(hours => 1);

    return {
        provider              => 'met.no',
        temperature           => $current->{data}->{instant}->{details}->{air_temperature},
        pressure_at_sea_level => $current->{data}->{instant}->{details}->{air_pressure_at_sea_level},
        humidity              => $current->{data}->{instant}->{details}->{relative_humidity},
        cloud_percent         => $current->{data}->{instant}->{details}->{cloud_area_fraction},

        # cloud_percent_at_high_alt   => $current->{data}->{instant}->{details}->{cloud_area_fraction_high},
        # cloud_percent_at_medium_alt => $current->{data}->{instant}->{details}->{cloud_area_fraction_medium},
        # cloud_percent_at_low_alt    => $current->{data}->{instant}->{details}->{cloud_area_fraction_low},
        fog_percent => $current->{data}->{instant}->{details}->{fog_area_fraction},
        wind_speed  => $current->{data}->{instant}->{details}->{wind_speed},
        wind_from   => $current->{data}->{instant}->{details}->{wind_from_direction},

        precipitation        => $current->{data}->{next_1_hours}->{details}->{precipitation_amount},
        provider_symbol_code => $current->{data}->{next_1_hours}->{summary}->{symbol_code},
        wi_symbol_code       => $self->symbol_mapper->map_symbol($current->{data}->{next_1_hours}->{summary}->{symbol_code}),
        description          => $self->symbol_mapper->map_description($current->{data}->{next_1_hours}->{summary}->{symbol_code}),

        time_start => $time_start,
        time_end   => $time_end,

        # from global properties
        updated_at => $self->dt_format->parse_datetime($json->{properties}->{meta}->{updated_at})->set_formatter($self->dt_format),
    };
}

sub current {
    my $self = shift;

    my $json    = $self->raw_json_from_web;
    my $current = $json->{properties}->{timeseries}->[0];

    return $self->_extract_data($json, $current);
}

sub forecast {
    my $self = shift;

    my $json = $self->raw_json_from_web;

    my @ret;
    foreach my $current (@{ $json->{properties}->{timeseries} }) {
        my $time = $self->dt_format->parse_datetime($current->{time});

        # next if $time < DateTime->now;    # forecasts only

        push @ret, $self->_extract_data($json, $current);
    }

    return \@ret;
}

sub aggregate {
    my $self     = shift;
    my $forecast = shift;
    my $dt       = shift;
    my $hours    = shift;

    my $dt_start = $dt->clone;
    my $dt_end   = $dt->clone->add(hours => $hours);

    my @usable = ();
    foreach my $f (@$forecast) {
        if (   $f->{time_end} > $dt_start
            && $f->{time_start} < $dt_end) {
            push @usable, $f;
        }
    }
    return unless @usable;

    my $cnt        = scalar(@usable);
    my $aggregated = {
        provider       => $usable[0]->{provider},
        aggregated_cnt => $cnt,

        temperature_min => min(map { $_->{temperature} } @usable),
        temperature_max => max(map { $_->{temperature} } @usable),
        temperature_avg => sum(map { $_->{temperature} } @usable) / $cnt,

        pressure_min => min(map { $_->{pressure_at_sea_level} } @usable),
        pressure_max => max(map { $_->{pressure_at_sea_level} } @usable),
        pressure_avg => sum(map { $_->{pressure_at_sea_level} } @usable) / $cnt,

        humidity_min => min(map { $_->{humidity} } @usable),
        humidity_max => max(map { $_->{humidity} } @usable),
        humidity_avg => sum(map { $_->{humidity} } @usable) / $cnt,

        cloud_percent_min => max(map { $_->{cloud_percent} } @usable),
        cloud_percent_max => min(map { $_->{cloud_percent} } @usable),
        cloud_percent_avg => sum(map { $_->{cloud_percent} } @usable) / $cnt,

        fog_percent_min => max(map { $_->{fog_percent} } @usable),
        fog_percent_max => min(map { $_->{fog_percent} } @usable),
        fog_percent_avg => sum(map { $_->{fog_percent} } @usable) / $cnt,

        wind_speed_min => max(map { $_->{wind_speed} } @usable),
        wind_speed_max => min(map { $_->{wind_speed} } @usable),
        wind_speed_avg => sum(map { $_->{wind_speed} } @usable) / $cnt,
        wind_from      => sum(map { $_->{wind_from} } @usable) / $cnt,

        precipitation_min => min(map { $_->{precipitation} } @usable),
        precipitation_max => max(map { $_->{precipitation} } @usable),
        precipitation_avg => sum(map { $_->{precipitation} } @usable) / $cnt,
        precipitation_sum => sum(map { $_->{precipitation} } @usable),

        provider_symbol_codes => [ uniq(map { $_->{provider_symbol_code} } @usable) ],
        wi_symbol_codes       => [ uniq(map { $_->{wi_symbol_code} } @usable) ],
        descriptions          => [ uniq(map { $_->{description} } @usable) ],

        time_start => min(map { $_->{time_start} } @usable),
        time_end   => max(map { $_->{time_end} } @usable),
        updated_at => $usable[0]->{updated_at},
    };

    my $start_is_day = $self->sunrise->sunrise_sunset_span($aggregated->{time_start})->contains($aggregated->{time_start}) ? 1 : 0;
    my $end_is_day   = $self->sunrise->sunrise_sunset_span($aggregated->{time_end})->contains($aggregated->{time_end})     ? 1 : 0;
    $aggregated->{time_is_day} = $start_is_day || $end_is_day;    # prefer day over night for longer periods

    return $aggregated;
}

1;