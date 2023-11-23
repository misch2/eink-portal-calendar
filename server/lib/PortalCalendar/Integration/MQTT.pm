package PortalCalendar::Integration::MQTT;

use Mojo::Base qw/PortalCalendar::Integration/;

use Mojo::JSON qw(encode_json);

use Net::MQTT::Simple;
use DDP;
use DateTime;

sub publish_retained {
    my $self      = shift;
    my $key       = shift;
    my $value     = shift;
    my $ha_detail = shift;

    my $server   = $self->display->get_config('mqtt_server');
    my $username = $self->display->get_config('mqtt_username');
    my $password = $self->display->get_config('mqtt_password');
    my $topic    = $self->display->get_config('mqtt_topic');      # unique device identifier

    local $ENV{MQTT_SIMPLE_ALLOW_INSECURE_LOGIN} = 1;
    my $mqtt = Net::MQTT::Simple->new($server);
    $mqtt->login($username, $password);

    my $bytes = encode_json(
        {
            device => {
                manufacturer => 'Michal',
                model        => 'Portal calendar',
                identifiers  => [$topic],
                name         => $topic,
            },
            enabled_by_default => \1,

            entity_category     => $ha_detail->{entity_category},
            device_class        => $ha_detail->{device_class},
            state_class         => $ha_detail->{state_class},
            unit_of_measurement => $ha_detail->{unit_of_measurement},
            icon                => $ha_detail->{icon},

            name => "${topic} ${key}",    # space is useful for HA, it produces nice names ($topic is removed automatically before displaying)

            state_topic => "${topic}/state/$key",

            unique_id => "${topic}_${key}",

            # Only for JSON, not used here:
            #json_attributes_topic => "${topic}/state/$key",
            #value_template => "{{ value_json.battery }}"
        }
    );
    $mqtt->retain("homeassistant/$ha_detail->{component}/$topic/$key/config", $bytes);

    # Direct value storage, not JSON
    $mqtt->retain("$topic/state/$key", $value);

    return;
}

1;