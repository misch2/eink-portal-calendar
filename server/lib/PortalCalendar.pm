package PortalCalendar;
use Mojo::Base 'Mojolicious';
use Mojo::SQLite;
use Mojo::JSON;
use Mojo::Log;

use Carp;
use DateTime;
use DateTime::Format::Strptime;
use DateTime::Format::ISO8601;
use DDP;
use Time::HiRes;

use PortalCalendar;
use PortalCalendar::Config;
use PortalCalendar::Schema;
use PortalCalendar::Routes;

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
        schema => sub {
            state $schema = PortalCalendar::Schema->connect("dbi:SQLite:local/calendar.db", "", "", { sqlite_unicode => 1 });
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
            if (!$display) {
                $self->log->warn("Display with ID $id not found");
                Carp::cluck("trace");
            }
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

    # $app->plugin(
    #     'Cron' => {

    #         # every 15 minutes
    #         '*/15 * * * *' => sub {
    #             $app->minion->enqueue('generate_image', []);
    #         },
    #     }
    # );

    return;
}

# This method will run once at server start
sub startup {
    my $app = shift;

    # Add another namespace to load commands from
    push @{ $app->commands->namespaces }, 'PortalCalendar::Command';

    $app->setup_plugins();
    $app->setup_logger();
    $app->setup_helpers();

    $app->run_migrations();

    PortalCalendar::Routes->new(app => $app)->setup();

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

    $app->log->level($ENV{MOJO_LOG_LEVEL} // $app->config->{logging}->{level});

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

-- 12 up
ALTER TABLE displays ADD border_top INTEGER NOT NULL DEFAULT 0;
ALTER TABLE displays ADD border_right INTEGER NOT NULL DEFAULT 0;
ALTER TABLE displays ADD border_bottom INTEGER NOT NULL DEFAULT 0;
ALTER TABLE displays ADD border_left INTEGER NOT NULL DEFAULT 0;

-- 13 up
ALTER TABLE displays ADD firmware VARCHAR;

-- 14 up
ALTER TABLE cache ADD created_utc INTEGER NOT NULL DEFAULT 0;

-- 15 up
DROP TABLE cache;
CREATE TABLE cache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    creator VARCHAR(255) NOT NULL,
    display_id INTEGER REFERENCES displays(id),
    key VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT 0,
    expires_at DATETIME NOT NULL DEFAULT 0,
    data BLOB
);
CREATE UNIQUE INDEX cache_creator_key_display_id ON cache (creator, key, display_id);
CREATE INDEX cache_expires_at ON cache (expires_at, creator);

-- 16 up
DROP TABLE cache;
CREATE TABLE cache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    creator VARCHAR(255) NOT NULL,
    key VARCHAR(255) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT 0,
    expires_at DATETIME NOT NULL DEFAULT 0,
    data BLOB
);
CREATE UNIQUE INDEX cache_creator_key ON cache (creator, key);
CREATE INDEX cache_expires_at ON cache (expires_at, creator);
