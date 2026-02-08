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

    my $cache = $self->db_cache;
    $cache->max_age( 15 * ONE_MINUTE );

    my $url = Mojo::URL->new('https://xkcd.com/info.0.json');

    my $info = $cache->get_or_set(
        sub {
            $self->app->log->debug("Really fetching XKCD JSON");
            my $response = $self->caching_ua->get( $url->to_string );

            die $response->status_line . "\n" . $response->content
              unless $response->is_success;

            return { content => $response->decoded_content };
        },
        { url => $url->to_unsafe_string },
    );

    return $info->{content};
};

has json => sub {
    my $self = shift;
    my $text = $self->raw_json_from_web();
    return decode_json($text);
};

has image_data => sub {
    my $self = shift;

    my $cache = $self->db_cache;
    $cache->max_age( 14 * ONE_DAY );

    my $url = Mojo::URL->new( $self->json->{img} );

    my $info = $cache->get_or_set(
        sub {
            $self->app->log->debug("Really fetching XKCD image");
            my $response = $self->caching_ua->get( $url->to_string );

            die $response->status_line . "\n" . $response->content
              unless $response->is_success;

            return { content => $response->decoded_content };
        },
        { url => $url->to_unsafe_string },
    );

    my $img = Imager->new( data => $info->{content} ) or die Imager->errstr;
    return $img;
};

has image_is_landscape => sub {
    my $self = shift;

    my $img = $self->image_data;

    return 0 if $img->getwidth == 0 || $img->getheight == 0;
    return 1
      if $img->getwidth / $img->getheight >
      4 / 3;    # only significantly wider images are considered landscape
    return 0;
};

has image_as_data_url => sub {
    my $self = shift;

    my $img = $self->image_data;

    $img->write(
        data => \my $data,
        type => 'png',
    ) or die $img->errstr;

    return 'data:image/png;base64,' . b64_encode( $data, '' );
};

1;
