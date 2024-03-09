package PortalCalendar::Integration::MQTT;

use Mojo::Base qw/PortalCalendar::Integration/;

use Mojo::JSON qw(encode_json);

use Net::MQTT::Simple;
use DDP;
use DateTime;

has mqtt => sub {
    my $self = shift;
    local $ENV{MQTT_SIMPLE_ALLOW_INSECURE_LOGIN} = 1;

    my $server   = $self->display->get_config('mqtt_server');
    my $username = $self->display->get_config('mqtt_username');
    my $password = $self->display->get_config('mqtt_password');

    my $mqtt = Net::MQTT::Simple->new($server);
    $mqtt->login($username, $password);

    return $mqtt;
};

# has _config_topics_published => sub { return {} };

# sub was_config_topic_published {
#     my $self  = shift;
#     my $topic = shift;

#     return $self->_config_topics_published->{$topic} ? 1 : 0;
# }

# sub mark_config_topic_as_published {
#     my $self  = shift;
#     my $topic = shift;

#     $self->_config_topics_published->{$topic} = 1;

#     return;
# }

sub publish_sensor {
    my $self      = shift;
    my $key       = shift;
    my $value     = shift;
    my $ha_detail = shift;

    my $topic = $self->display->get_config('mqtt_topic');    # unique device identifier

    my $component    = $ha_detail->{component} // 'sensor';
    my $config_topic = "homeassistant/$component/$topic/$key/config";
    my $state_topic  = "epapers/$topic/state/$key";

    my $bytes = encode_json(
        {
            state_topic => $state_topic,
            device      => {
                manufacturer => 'Michal',
                model        => 'Portal calendar ePaper',
                identifiers  => [$topic],
                name         => $topic,
                hw_version   => '1.0',
                sw_version   => ($self->display ? $self->display->firmware : 'unknown'),
            },
            enabled_by_default  => \1,
            entity_category     => $ha_detail->{entity_category},
            device_class        => $ha_detail->{device_class},
            state_class         => $ha_detail->{state_class},
            unit_of_measurement => $ha_detail->{unit_of_measurement},
            $ha_detail->{icon} ? (icon => $ha_detail->{icon}) : (),
            name      => "${topic} ${key}",    # space is useful for HA, it produces nice names ($topic is removed automatically before displaying)
            unique_id => "${topic}_${key}",
        }
    );

    $self->app->log->debug("publishing retained config topic $config_topic");
    $self->mqtt->retain($config_topic, $bytes);
    $self->app->log->debug("publishing non-retained state topic $state_topic");
    $self->mqtt->publish($state_topic, $value);

    return;
}

1;