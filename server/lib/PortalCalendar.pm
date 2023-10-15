package PortalCalendar;
use Mojo::Base 'Mojolicious';

use Mojo::SQLite;
use DateTime;
use DateTime::Format::Strptime;
use DateTime::Format::ISO8601;
use DDP;
use Time::HiRes;

# FIXME for oauth:
use Mojo::Util qw(url_escape);
use Mojo::JSON qw(decode_json encode_json);
use Mojo::Log;

use PortalCalendar;
use PortalCalendar::Config;
use PortalCalendar::Minion;
use PortalCalendar::Schema;

sub setup_routes {
    my $app = shift;

    # Router
    my $r = $app->routes;

    # Route
    $r->get('/ping')->to('Data#ping');
    $r->get('/config')->to('Data#config');
    $r->get('/calendar/bitmap')->to('Data#bitmap');
    $r->get('/calendar/bitmap/epapermono')->to('Data#bitmap_epaper_mono');
    $r->get('/calendar/bitmap/epapergray')->to('Data#bitmap_epaper_gray');

    $r->get('/')->to('UI#select_display');
    $r->get('/home/:display_number')->to('UI#home');
    $r->get('/test/:display_number')->to('UI#test');
    $r->get('/calendar/:display_number/html')->to('UI#calendar_html_default_date');
    $r->get('/calendar/:display_number/html/:date')->to('UI#calendar_html_specific_date');
    $r->get('/config_ui/:display_number')->to('UI#config_ui_show');
    $r->post('/config_ui/:display_number')->to('UI#config_ui_save');

    $r->get('/auth/googlefit/cb')->to('UI#googlefit_callback'); # has to be fixed format, without any parameters
    #^ must be first, to match sooner than the next route
    $r->get('/auth/googlefit/:display_number')->to('UI#googlefit_redirect');
    $r->get('/auth/googlefit/success/:display_number')->to('UI#googlefit_success');

    return;
}

sub enqueue_task_only_once {
    my $app  = shift;
    my $name = shift;

    my $new_id;
    if (my $total = $app->minion->jobs({ states => ['inactive'], tasks => [$name] })->total) {
        $app->log->warn("Task '$name' already enqueued, skipping it");
    } else {
        $app->log->info("Enqueuing task '$name'");
        $new_id = $app->minion->enqueue($name, []);
    }
    return $new_id;
}

sub setup_helpers {
    my $app = shift;

    $app->helper(
        foo => sub {
            warn "test";
        }
    );

    $app->helper(
        schema => sub {
            state $schema = PortalCalendar::Schema->connect("dbi:SQLite:local/calendar.db");
        }
    );

    $app->helper(
        get_display_by_mac => sub {
            my $self = shift;
            my $mac  = shift;

            $mac = lc($mac);
            my $display = $self->schema->resultset('Display')->find({ mac => $mac });
            $self->log->warn("Display with MAC [$mac] not found") unless $display;
            return $display;
        }
    );

    $app->helper(
        get_display_by_id => sub {
            my $self = shift;
            my $id   = shift;

            my $display = $self->schema->resultset('Display')->find({ id => $id });
            $self->log->warn("Display with ID $id not found") unless $display;
            return $display;
        }
    );

    $app->helper(
        encode_json => sub {
            my $app  = shift;
            my $data = shift;
            return Mojo::JSON::encode_json($data);
        }
    );

    return;
}

sub setup_plugins {
    my $app = shift;

    $app->plugin('Config');    # loads config from app.<mode>.conf (production/development) or from app.conf as a fallback
    $app->plugin('TagHelpers');
    $app->plugin('RenderFile');
    $app->plugin(
        'Minion' => {
            SQLite => 'sqlite:' . "local/minion.db",    # $app->home->child('minion.db'),

        }
    );
    $app->plugin(
        'Minion::Admin' => {                            # Host Admin UI
            route => $app->routes->any('/admin'),
        }
    );
    $app->plugin(
        'Cron' => {

            # every hour
            '0 * * * *' => sub {
                my $id1 = $app->minion->enqueue('parse_calendars', []);
                my $id2 = $app->minion->enqueue('parse_weather',   []);
                my $id3 = $app->minion->enqueue('parse_googlefit', []);
                $app->minion->enqueue('generate_image', [], { parents => [ $id1, $id2, $id3 ] });
            },
        }
    );

    return;
}

# This method will run once at server start
sub startup {
    my $app = shift;

    # Add another namespace to load commands from
    push @{$app->commands->namespaces}, 'PortalCalendar::Command';

    $app->setup_plugins();
    $app->setup_logger();
    $app->setup_helpers();

    $app->run_migrations();
    $app->setup_routes();

    # define minion tasks
    $app->minion->add_task(
        #
        generate_image => sub {
            PortalCalendar::Minion::regenerate_image(@_);
        }
    );
    $app->minion->add_task(
        parse_calendars => sub {
            PortalCalendar::Minion::reload_calendars(@_);
        }
    );
    $app->minion->add_task(
        parse_weather => sub {
            PortalCalendar::Minion::reload_weather(@_);
        }
    );
    $app->minion->add_task(
        parse_googlefit => sub {
            PortalCalendar::Minion::reload_googlefit(@_);
        }
    );

    $app->renderer->cache->max_keys(0) if $app->config->{disable_renderer_cache};    # do not cache CSS etc. in devel mode
    $app->secrets([ $app->config->{mojo_passphrase} ]);

    DateTime->DefaultLocale($app->config->{datetime_locale});

    $app->log->info("Application starting");
}

########################################################################

sub setup_logger {
    my $app = shift;

    # redirect warnings
    $SIG{__WARN__} = sub {
        my $message = shift;
        $message =~ s/\n$//;
        @_ = ($app->log, $message);
        goto &Mojo::Log::warn;
    };

    $app->log->level($app->config->{logging}->{level});

    if ($app->config->{logging}->{file} eq 'STDERR') {

        # logging to STDERR (for morbo command line usage):
        $app->log->handle(\*STDERR);
    } else {
        $app->log->path($app->config->{logging}->{file});
    }

    $app->log->info("Starting app...");
    return;
}

sub run_migrations {
    my $app = shift;

    my $db = Mojo::SQLite->new("sqlite:local/calendar.db");
    $db->auto_migrate(1)->migrations->from_data();

    # execute any SQL via this handle to run the migrations automatically:
    my $version = $db->db->query('select sqlite_version() as version');

    return;
}

1;

__DATA__
@@ migrations
-- 1 up
CREATE TABLE config (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name VARCHAR NOT NULL,
    value VARCHAR NOT NULL
);

-- 2 up
-- INSERT INTO config (name, value) VALUES ('broken_glass', '0');

-- 3 up
-- INSERT INTO config (name, value) VALUES ('sleep_time', '3600');

-- 4 up
-- INSERT INTO config (name, value) VALUES ('web_calendar1', '0');
-- INSERT INTO config (name, value) VALUES ('web_calendar_ics_url1', '');
-- INSERT INTO config (name, value) VALUES ('web_calendar2', '0');
-- INSERT INTO config (name, value) VALUES ('web_calendar_ics_url2', '');
-- INSERT INTO config (name, value) VALUES ('web_calendar3', '0');
-- INSERT INTO config (name, value) VALUES ('web_calendar_ics_url3', '');
-- INSERT INTO config (name, value) VALUES ('max_icons_for_calendar', '5');
--
-- INSERT INTO config (name, value) VALUES ('totally_random_icon', '0');
-- INSERT INTO config (name, value) VALUES ('min_random_icons', '4');
-- INSERT INTO config (name, value) VALUES ('max_random_icons', '10');
-- INSERT INTO config (name, value) VALUES ('max_icons_with_calendar', '5');

-- 5 up
CREATE TABLE calendar_events_raw (
    calendar_id INTEGER NOT NULL PRIMARY KEY,
    events_raw BLOB
);

-- 6 up
ALTER TABLE calendar_events_raw RENAME TO cache;
ALTER TABLE cache RENAME COLUMN calendar_id TO id;
ALTER TABLE cache RENAME COLUMN events_raw TO data;

-- 7 up
DROP TABLE cache;
CREATE TABLE cache (
    id VARCHAR(255) NOT NULL PRIMARY KEY,
    data BLOB
);

-- 8 up
CREATE TABLE displays (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    mac VARCHAR NOT NULL UNIQUE,
    name VARCHAR NOT NULL UNIQUE,
    width INTEGER NOT NULL,
    height INTEGER NOT NULL,
    rotation INTEGER NOT NULL,
    colortype VARCHAR NOT NULL
);

-- 9 up
ALTER TABLE config ADD display_id INTEGER REFERENCES displays(id);

-- 10 up
CREATE UNIQUE INDEX config_name_display ON config (name, display_id);
UPDATE config SET display_id=1 WHERE display_id IS NULL;

-- 11 up
ALTER TABLE displays ADD gamma NUMERIC(4,2);
UPDATE displays SET gamma=1.8 WHERE gamma IS NULL;
