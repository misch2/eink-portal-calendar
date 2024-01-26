package PortalCalendar::Integration::XKCD;

use Mojo::Base qw/PortalCalendar::Integration/;

use Mojo::JSON qw(decode_json);
use Mojo::Util qw(b64_encode);
use Mojo::URL;

use DateTime;
use Imager;
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

has image_data => sub {
    my $self = shift;

    my $url      = Mojo::URL->new($self->json->{img});
    my $response = $self->caching_ua->get($url->to_string);

    die $response->status_line . "\n" . $response->content
        unless $response->is_success;

    my $img = Imager->new(data => $response->decoded_content);
    return $img;
};

has image_is_landscape => sub {
    my $self = shift;

    my $img = $self->image_data;

    return $img->getwidth > $img->getheight;
};

has image_as_data_url => sub {
    my $self = shift;

    my $img = $self->image_data;

    $img->write(
        data => \my $data,
        type => 'png',
    ) or die $img->errstr;

    return 'data:image/png;base64,' . b64_encode($data, '');
};

1;