# Controller
package PortalCalendar::Controller::Data;
use Mojo::Base 'PortalCalendar::Controller';

use DateTime;
use DateTime::Format::ISO8601;
use WWW::Telegram::BotAPI;

has mac => sub {
    my $self = shift;
    my $mac  = $self->req->param('mac');
    return undef unless $mac;
    return lc($mac);
};

has display => sub {
    my $self = shift;
    return $self->get_display_by_mac($self->mac);
};

sub ping {
    my $self = shift;
    return $self->render(json => { status => 'ok' });
}

# Return configuration data to client (ePaper display):
sub config {
    my $self = shift;

    my $display = $self->display;
    if ($display) {
        $display->firmware($self->req->param('fw') // '');
        $display->update;
    } else {
        $display = $self->schema->resultset('Display')->create(
            {
                # Physical properties, not to be changed by the user:
                mac       => $self->mac,
                name      => "New display with MAC " . uc($self->mac) . " added on " . DateTime->now->stringify,
                width     => $self->req->param('w'),
                height    => $self->req->param('h'),
                colortype => $self->req->param('c'),
                firmware  => $self->req->param('fw') // '',

                # Logical properties, updatable by the user:
                rotation      => 0,
                gamma         => 2.2,
                border_top    => 0,
                border_right  => 0,
                border_bottom => 0,
                border_left   => 0,
            }
        );
        $display->set_config('theme', 'default');
        $self->app->minion->enqueue('regenerate_image', [ $display->id ]);
    }

    $display->set_config('_last_visit', DateTime::Format::ISO8601->format_datetime(DateTime->now(time_zone => 'UTC')));

    if ($display->missed_connects > 0) {
        if ($display->get_config('_frozen_notification_sent') // 0) {
            my $last_visit = $display->last_visit()->set_time_zone('local');
            my $message    = $self->app->render_anything(
                template   => 'display_unfrozen',
                format     => 'txt',
                display    => $display,
                last_visit => $last_visit,
                now        => DateTime->now->set_time_zone('local'),
            );
            $self->app->log->warn($message);

            my $token = $display->get_config('telegram_api_key');
            if ($token) {
                $self->app->log->debug("Sending telegram message to " . $display->get_config('telegram_chat_id'));
                my $telegram = WWW::Telegram::BotAPI->new(token => $token);
                $telegram->sendMessage(
                    {
                        chat_id => $display->get_config('telegram_chat_id'),
                        text    => $message,
                    }
                );
            }

            $display->set_config('_frozen_notification_sent', 0);
        }
        $display->reset_missed_connects_count();
    }

    # config values have a NOT NULL restriction
    $display->set_config('_last_voltage_raw', $self->req->param('adc')    // $self->req->param('voltage_raw') // '');
    $display->set_config('_last_voltage',     $self->req->param('v')      // '');
    $display->set_config('_min_voltage',      $self->req->param('vmin')   // '');
    $display->set_config('_max_voltage',      $self->req->param('vmax')   // '');
    $display->set_config('_reset_reason',     $self->req->param('reset')  // '');
    $display->set_config('_wakeup_reason',    $self->req->param('wakeup') // '');

    my $util = PortalCalendar::Util->new(app => $self->app, display => $display);
    my ($next_wakeup, $sleep_in_seconds, $schedule) = $display->next_wakeup_time();
    $self->app->log->info("Next wakeup at $next_wakeup (in $sleep_in_seconds seconds) according to crontab schedule '$schedule'");

    $util->update_mqtt('voltage',         $display->voltage,                         1);
    $util->update_mqtt('battery_percent', $display->battery_percent(),               1);
    $util->update_mqtt('voltage_raw',     $display->get_config('_last_voltage_raw'), 1);
    $util->update_mqtt('min_voltage',     $display->get_config('_min_voltage'),      1);
    $util->update_mqtt('max_voltage',     $display->get_config('_max_voltage'),      1);
    $util->update_mqtt('last_visit',      DateTime->now()->rfc3339);
    $util->update_mqtt('sleep_time',      $sleep_in_seconds,                      1);
    $util->update_mqtt('reset_reason',    $display->get_config('_reset_reason'),  1);
    $util->update_mqtt('wakeup_reason',   $display->get_config('_wakeup_reason'), 1);

    $util->update_mqtt('last_visit', DateTime->now()->rfc3339);    # final message, FIXME ugly hack, workaround for the wakeup_reason not being updated although it is sent
    $util->disconnect_mqtt;

    my $ret = {

        # {// undef} to force scalars
        sleep           => $sleep_in_seconds,
        battery_percent => $display->battery_percent() // undef,
        ota_mode        => ($display->get_config('ota_mode') ? \1 : \0),    # JSON true/false
    };

    $self->render(json => $ret);
}

# /calendar/bitmap
# /calendar/bitmap?rotate=1&format=png
# /calendar/bitmap?rotate=2&flip=x
# /calendar/bitmap?rotate=3&flip=xy&format=raw8bpp
sub bitmap {
    my $self = shift;

    my $rotate        = $self->req->param('rotate')        // 0;
    my $flip          = $self->req->param('flip')          // '';
    my $gamma         = $self->req->param('gamma')         // $self->display->gamma;
    my $numcolors     = $self->req->param('colors')        // $self->display->num_colors;
    my $colormap_name = $self->req->param('colormap_name') // 'none';
    my $color_palette = $self->display->color_palette($self->req->param('preview_colors'));
    my $format        = $self->req->param('format') // 'png';

    $colormap_name = 'webmap' if scalar(@$color_palette) == 0;

    my $util = PortalCalendar::Util->new(app => $self->app, display => $self->display);
    return $self->render(
        $util->generate_bitmap(
            {
                rotate          => $rotate,
                flip            => $flip,
                gamma           => $gamma,
                numcolors       => $numcolors,
                colormap_name   => $colormap_name,
                colormap_colors => $color_palette,
                format          => $format,
                display_type    => $self->display->colortype,
            }
        )
    );
}

# Return bitmap to client (ePaper display, special format of bitmnap) and update last_visit timestamp:
sub bitmap_epaper {
    my $self = shift;

    # No, don't update last_visit here, because this is called by the UI itself too, and it would reset the missed_connects counter:when looking at a preview image
    #$display->set_config('_last_visit', DateTime->now()->iso8601);

    my $rotate    = $self->display->rotation;
    my $numcolors = $self->display->num_colors;
    my $format    = 'epaper_native';

    my $color_palette = $self->display->color_palette($self->req->param('preview_colors'));
    my $colormap_name = (scalar(@$color_palette) > 0 ? 'none' : 'webmap');

    if ($self->req->param('web_format')) {
        $format = 'png';
        $rotate = 0;
    }

    my $util = PortalCalendar::Util->new(app => $self->app, display => $self->display);

    # FIXME?
    # $self->app->res->headers->content_type('application/octet-stream');
    # $self->app->res->headers->header('Content-Transfer-Encoding' => 'binary');
    return $self->render(
        $util->generate_bitmap(
            {
                rotate          => $rotate,
                flip            => 0,
                gamma           => $self->display->gamma,
                numcolors       => $numcolors,
                colormap_name   => $colormap_name,
                colormap_colors => $color_palette,
                format          => $format,
                display_type    => $self->display->colortype,
            }
        )
    );
}

1;