package PortalCalendar::Integration::Weather::MetNo::IconsMapping;

use Mojo::Base qw/PortalCalendar::Integration/;

# Map icons from https://github.com/metno/weathericons/tree/main/weather to the https://erikflowers.github.io/weather-icons/api-list.html OpenWeatherMap numeric ID

has _symbol_map_as_array => sub {
    return [
        { code => 'clearsky',                     description => { en => 'Clear sky',                       cz => 'Jasno' },                           openweather_id => 800 },
        { code => 'fair',                         description => { en => 'Fair',                            cz => 'Polojasno' },                       openweather_id => 801 },
        { code => 'partlycloudy',                 description => { en => 'Partly cloudy',                   cz => 'Polojasno' },                       openweather_id => 801 },
        { code => 'cloudy',                       description => { en => 'Cloudy',                          cz => 'Zataženo' },                        openweather_id => 804 },
        { code => 'lightrainshowers',             description => { en => 'Light rain showers',              cz => 'Slabé přeháňky' },                  openweather_id => 500 },
        { code => 'rainshowers',                  description => { en => 'Rain showers',                    cz => 'Přeháňky' },                        openweather_id => 501 },
        { code => 'heavyrainshowers',             description => { en => 'Heavy rain showers',              cz => 'Silné přeháňky' },                  openweather_id => 502 },
        { code => 'lightrainshowersandthunder',   description => { en => 'Light rain showers and thunder',  cz => 'Slabé přeháňky a bouřky' },         openweather_id => 200 },
        { code => 'rainshowersandthunder',        description => { en => 'Rain showers and thunder',        cz => 'Přeháňky a bouřky' },               openweather_id => 201 },
        { code => 'heavyrainshowersandthunder',   description => { en => 'Heavy rain showers and thunder',  cz => 'Silné přeháňky a bouřky' },         openweather_id => 202 },
        { code => 'lightsleetshowers',            description => { en => 'Light sleet showers',             cz => 'Slabé déšť se sněhem' },            openweather_id => 611 },
        { code => 'sleetshowers',                 description => { en => 'Sleet showers',                   cz => 'Déšť se sněhem' },                  openweather_id => 612 },
        { code => 'heavysleetshowers',            description => { en => 'Heavy sleet showers',             cz => 'Silný déšť se sněhem' },            openweather_id => 615 },
        { code => 'lightssleetshowersandthunder', description => { en => 'Light sleet showers and thunder', cz => 'Slabé déšť se sněhem a bouřky' },   openweather_id => 210 },
        { code => 'sleetshowersandthunder',       description => { en => 'Sleet showers and thunder',       cz => 'Déšť se sněhem a bouřky' },         openweather_id => 211 },
        { code => 'heavysleetshowersandthunder',  description => { en => 'Heavy sleet showers and thunder', cz => 'Silný déšť se sněhem a bouřky' },   openweather_id => 212 },
        { code => 'lightsnowshowers',             description => { en => 'Light snow showers',              cz => 'Slabé sněhové přeháňky' },          openweather_id => 600 },
        { code => 'snowshowers',                  description => { en => 'Snow showers',                    cz => 'Sněhové přeháňky' },                openweather_id => 601 },
        { code => 'heavysnowshowers',             description => { en => 'Heavy snow showers',              cz => 'Silné sněhové přeháňky' },          openweather_id => 602 },
        { code => 'lightssnowshowersandthunder',  description => { en => 'Light snow showers and thunder',  cz => 'Slabé sněhové přeháňky a bouřky' }, openweather_id => 230 },
        { code => 'snowshowersandthunder',        description => { en => 'Snow showers and thunder',        cz => 'Sněhové přeháňky a bouřky' },       openweather_id => 231 },
        { code => 'heavysnowshowersandthunder',   description => { en => 'Heavy snow showers and thunder',  cz => 'Silné sněhové přeháňky a bouřky' }, openweather_id => 232 },
        { code => 'lightrain',                    description => { en => 'Light rain',                      cz => 'Slabý déšť' },                      openweather_id => 300 },
        { code => 'rain',                         description => { en => 'Rain',                            cz => 'Déšť' },                            openweather_id => 301 },
        { code => 'heavyrain',                    description => { en => 'Heavy rain',                      cz => 'Silný déšť' },                      openweather_id => 302 },
        { code => 'lightrainandthunder',          description => { en => 'Light rain and thunder',          cz => 'Slabý déšť a bouřky' },             openweather_id => 210 },
        { code => 'rainandthunder',               description => { en => 'Rain and thunder',                cz => 'Déšť a bouřky' },                   openweather_id => 211 },
        { code => 'heavyrainandthunder',          description => { en => 'Heavy rain and thunder',          cz => 'Silný déšť a bouřky' },             openweather_id => 212 },
        { code => 'lightsleet',                   description => { en => 'Light sleet',                     cz => 'Slabý déšť se sněhem' },            openweather_id => 611 },
        { code => 'sleet',                        description => { en => 'Sleet',                           cz => 'Déšť se sněhem' },                  openweather_id => 612 },
        { code => 'heavysleet',                   description => { en => 'Heavy sleet',                     cz => 'Silný déšť se sněhem' },            openweather_id => 615 },
        { code => 'lightsleetandthunder',         description => { en => 'Light sleet and thunder',         cz => 'Slabý déšť se sněhem a bouřky' },   openweather_id => 221 },
        { code => 'sleetandthunder',              description => { en => 'Sleet and thunder',               cz => 'Déšť se sněhem a bouřky' },         openweather_id => 221 },
        { code => 'heavysleetandthunder',         description => { en => 'Heavy sleet and thunder',         cz => 'Silný déšť se sněhem a bouřky' },   openweather_id => 221 },
        { code => 'lightsnow',                    description => { en => 'Light snow',                      cz => 'Slabý sníh' },                      openweather_id => 600 },
        { code => 'snow',                         description => { en => 'Snow',                            cz => 'Sníh' },                            openweather_id => 601 },
        { code => 'heavysnow',                    description => { en => 'Heavy snow',                      cz => 'Silný sníh' },                      openweather_id => 602 },
        { code => 'lightsnowandthunder',          description => { en => 'Light snow and thunder',          cz => 'Slabý sníh a bouřky' },             openweather_id => 230 },
        { code => 'snowandthunder',               description => { en => 'Snow and thunder',                cz => 'Sníh a bouřky' },                   openweather_id => 231 },
        { code => 'heavysnowandthunder',          description => { en => 'Heavy snow and thunder',          cz => 'Silný sníh a bouřky' },             openweather_id => 232 },
        { code => 'fog',                          description => { en => 'Fog',                             cz => 'Mlha' },                            openweather_id => 741 },
    ];
};

has _symbol_map => sub {
    my $self = shift;

    my $ret = {};
    foreach my $symbol (@{ $self->_symbol_map_as_array }) {
        $ret->{ $symbol->{code} } = $symbol;
    }

    return $ret;
};

sub symbol_details {
    my $self      = shift;
    my $orig_code = shift;

    return unless defined $orig_code;

    $orig_code =~ s/_(day|night|polartwilight)$//;
    return $self->_symbol_map->{$orig_code};
}

sub map_symbol {
    my $self   = shift;
    my $symbol = shift;

    return unless defined $symbol;

    my $d = $self->symbol_details($symbol);
    if (!defined $d) {
        $self->app->log->warn("Unknown symbol: $symbol");
        return;
    }

    return $d->{openweather_id};
}

sub map_description {
    my $self   = shift;
    my $symbol = shift;

    return unless defined $symbol;

    my $d = $self->symbol_details($symbol);
    if (!defined $d) {
        $self->app->log->warn("Unknown symbol: $symbol");
        return;
    }

    return $d->{description}->{cz} // "($symbol)";
}

1;
