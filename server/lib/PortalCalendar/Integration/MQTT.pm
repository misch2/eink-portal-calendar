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

    my $component    = $ha_detail->{component} // 'sensor';
    my $config_topic = "homeassistant/$component/$topic/$key/config";
    my $state_topic  = "portal_calendar/$topic/state/$key";

    my $bytes = encode_json(
        {
            state_topic => $state_topic,
            device      => {
                manufacturer => 'Michal',
                model        => 'Portal calendar',
                identifiers  => [$topic],
                name         => $topic,
            },
            enabled_by_default  => \1,
            entity_category     => $ha_detail->{entity_category},
            device_class        => $ha_detail->{device_class},
            state_class         => $ha_detail->{state_class},
            unit_of_measurement => $ha_detail->{unit_of_measurement},
            icon                => $ha_detail->{icon},
            name                => "${topic} ${key}",                   # space is useful for HA, it produces nice names ($topic is removed automatically before displaying)
            unique_id           => "${topic}_${key}",
        }
    );

    $mqtt->retain($config_topic, $bytes);
    $mqtt->retain($state_topic,  $value);

    return;
}

1;