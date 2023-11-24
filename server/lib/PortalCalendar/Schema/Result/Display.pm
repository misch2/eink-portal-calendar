#<<< skip perltidy formatting
use utf8;
package PortalCalendar::Schema::Result::Display;

# Created by DBIx::Class::Schema::Loader
# DO NOT MODIFY THE FIRST PART OF THIS FILE

=head1 NAME

PortalCalendar::Schema::Result::Display

=cut

use strict;
use warnings;

use base 'DBIx::Class::Core';

=head1 COMPONENTS LOADED

=over 4

=item * L<DBIx::Class::InflateColumn::DateTime>

=back

=cut

__PACKAGE__->load_components("InflateColumn::DateTime");

=head1 TABLE: C<displays>

=cut

__PACKAGE__->table("displays");

=head1 ACCESSORS

=head2 id

  data_type: 'integer'
  is_auto_increment: 1
  is_nullable: 0

=head2 mac

  data_type: 'varchar'
  is_nullable: 0

=head2 name

  data_type: 'varchar'
  is_nullable: 0

=head2 width

  data_type: 'integer'
  is_nullable: 0

=head2 height

  data_type: 'integer'
  is_nullable: 0

=head2 rotation

  data_type: 'integer'
  is_nullable: 0

=head2 colortype

  data_type: 'varchar'
  is_nullable: 0

=head2 gamma

  data_type: 'numeric'
  is_nullable: 1
  size: [4,2]

=head2 border_top

  data_type: 'integer'
  default_value: 0
  is_nullable: 0

=head2 border_right

  data_type: 'integer'
  default_value: 0
  is_nullable: 0

=head2 border_bottom

  data_type: 'integer'
  default_value: 0
  is_nullable: 0

=head2 border_left

  data_type: 'integer'
  default_value: 0
  is_nullable: 0

=head2 firmware

  data_type: 'varchar'
  is_nullable: 1

=cut

__PACKAGE__->add_columns(
  "id",
  { data_type => "integer", is_auto_increment => 1, is_nullable => 0 },
  "mac",
  { data_type => "varchar", is_nullable => 0 },
  "name",
  { data_type => "varchar", is_nullable => 0 },
  "width",
  { data_type => "integer", is_nullable => 0 },
  "height",
  { data_type => "integer", is_nullable => 0 },
  "rotation",
  { data_type => "integer", is_nullable => 0 },
  "colortype",
  { data_type => "varchar", is_nullable => 0 },
  "gamma",
  { data_type => "numeric", is_nullable => 1, size => [4, 2] },
  "border_top",
  { data_type => "integer", default_value => 0, is_nullable => 0 },
  "border_right",
  { data_type => "integer", default_value => 0, is_nullable => 0 },
  "border_bottom",
  { data_type => "integer", default_value => 0, is_nullable => 0 },
  "border_left",
  { data_type => "integer", default_value => 0, is_nullable => 0 },
  "firmware",
  { data_type => "varchar", is_nullable => 1 },
);

=head1 PRIMARY KEY

=over 4

=item * L</id>

=back

=cut

__PACKAGE__->set_primary_key("id");

=head1 UNIQUE CONSTRAINTS

=head2 C<mac_unique>

=over 4

=item * L</mac>

=back

=cut

__PACKAGE__->add_unique_constraint("mac_unique", ["mac"]);

=head2 C<name_unique>

=over 4

=item * L</name>

=back

=cut

__PACKAGE__->add_unique_constraint("name_unique", ["name"]);

=head1 RELATIONS

=head2 configs

Type: has_many

Related object: L<PortalCalendar::Schema::Result::Config>

=cut

__PACKAGE__->has_many(
  "configs",
  "PortalCalendar::Schema::Result::Config",
  { "foreign.display_id" => "self.id" },
  { cascade_copy => 0, cascade_delete => 0 },
);

#>>> end of perltidy skipped block

# Created by DBIx::Class::Schema::Loader v0.07049 @ 2023-11-03 13:53:17
# DO NOT MODIFY THIS OR ANYTHING ABOVE! md5sum:NawCW2RWvO1ctLRInv1Cow

use DateTime;
use DateTime::Format::ISO8601;
use List::Util qw(min max);
use Schedule::Cron::Events;
use Time::Seconds;
use Try::Tiny;

use PortalCalendar::Config;

sub is_default {
    return shift->id == 0;
}

sub virtual_width {
    my $self = shift;
    return $self->width if $self->rotation % 180 == 0;
    return $self->height;
}

sub virtual_height {
    my $self = shift;
    return $self->height if $self->rotation % 180 == 0;
    return $self->width;
}

sub get_config {
    my $self = shift;
    my $name = shift;

    my $config = PortalCalendar::Config->new(display => $self);

    return $config->get_from_schema($self->result_source->schema, $name);
}

sub get_config_without_defaults {
    my $self = shift;
    my $name = shift;

    my $config = PortalCalendar::Config->new(display => $self);

    return $config->get_from_schema_without_defaults($self->result_source->schema, $name);
}

sub set_config {
    my $self  = shift;
    my $name  = shift;
    my $value = shift;

    my $config = PortalCalendar::Config->new(display => $self);
    return $config->set_from_schema($self->result_source->schema, $name, $value);
}

sub voltage {
    my $self    = shift;
    my $voltage = $self->get_config('_last_voltage');
    return undef if $voltage eq '';
    return sprintf("%.3f", $voltage);
}

sub battery_percent {
    my $self = shift;
    my $min  = $self->get_config('_min_voltage');
    my $max  = $self->get_config('_max_voltage');

    my $cur = $self->voltage;

    # warn "min: $min, max: $max, cur: $cur";
    return undef unless $min && $max && $cur;
    my $percentage = 100 * ($cur - $min) / ($max - $min);
    $percentage = min(100, max(0, $percentage));    # clip to 0-100

    return sprintf("%.1f", $percentage);
}

sub reset_missed_connects_count {
    my $self = shift;
    $self->set_config('_missed_connects', 0);
}

sub increase_missed_connects_count {
    my $self = shift;
    $self->set_config('_missed_connects', 1 + $self->missed_connects);
}

sub missed_connects {
    my $self = shift;
    return $self->get_config('_missed_connects') // 0;
}

sub last_visit {
    my $self = shift;

    my $last_visit = undef;
    if (my $raw = $self->get_config('_last_visit')) {
        try {
            $last_visit = DateTime::Format::ISO8601->parse_datetime($raw);
            $last_visit->set_time_zone('UTC');
        } catch {
            warn "Error while parsing last_visit: $_";
        };
    }
    return $last_visit;
}

sub color_variants {
    my $self = shift;

    return {
        epd_black  => { preview => '#111111', pure => '#000000' },
        epd_white  => { preview => '#dddddd', pure => '#ffffff' },
        epd_red    => { preview => '#aa0000', pure => '#ff0000' },
        epd_yellow => { preview => '#dddd00', pure => '#ffff00' },
    };
}

sub css_color_map {
    my $self        = shift;
    my $for_preview = shift // 0;

    my $colors = {};
    foreach my $key (%{ $self->color_variants }) {
        if ($for_preview) {
            $colors->{$key} = $self->color_variants->{$key}->{preview};
        } else {
            $colors->{$key} = $self->color_variants->{$key}->{pure};
        }
    }

    return $colors;
}

sub colortype_formatted {
    my $self      = shift;
    my $colortype = $self->colortype;
    return 'Black & White'                         if $colortype eq 'BW';
    return 'Grayscale, 4 levels'                   if $colortype eq '4G';
    return 'Black & White & Color (red or yellow)' if $colortype eq '3C';
    return $colortype;
}

sub num_colors {
    my $self      = shift;
    my $colortype = $self->colortype;
    return 2  if $colortype eq 'BW';
    return 4  if $colortype eq '4G';
    return 3  if $colortype eq '3C';
    return 16 if $colortype eq '16G';
    return 256;
}

sub color_palette {
    my $self        = shift;
    my $for_preview = shift // 0;

    my $colortype = $self->colortype;
    my $colors    = $self->css_color_map($for_preview);

    if ($colortype eq 'BW') {
        return [ $colors->{epd_black}, $colors->{epd_white} ];
    } elsif ($colortype eq '4G') {
        return [ '#000000', '#555555', '#aaaaaa', '#ffffff' ];    # FIXME
    } elsif ($colortype eq '16G') {
        return [ '#000000', '#111111', '#222222', '#333333', '#444444', '#555555', '#666666', '#777777', '#888888', '#999999', '#aaaaaa', '#bbbbbb', '#cccccc', '#dddddd', '#eeeeee', '#ffffff' ];
    } elsif ($colortype eq '3C') {
        return [ $colors->{epd_black}, $colors->{epd_white}, $colors->{epd_red}, $colors->{epd_yellow} ];
    }
    return [];                                                    # FIXME
}

sub _next_wakeup_time_for_datetime {
    my $self     = shift;
    my $schedule = shift;
    my $dt       = shift;

    # crontab definitions are in SERVER LOCAL time zone, not display dependent
    return $dt->clone->set_time_zone('local')->truncate(to => 'day')->add(days => 1) unless $schedule;    # no schedule, wake up tomorrow
    my $cron = Schedule::Cron::Events->new($schedule, Seconds => $dt->epoch) or die "Invalid crontab schedule";

    my ($seconds, $minutes, $hours, $dayOfMonth, $month, $year) = $cron->nextEvent;
    my $next_time = DateTime->new(year => 1900 + $year, month => 1 + $month, day => $dayOfMonth, hour => $hours, minute => $minutes, second => $seconds, time_zone => 'local');

    return $next_time;
}

sub next_wakeup_time {
    my $self = shift;
    my $now  = shift // DateTime->now(time_zone => 'local');

    my $schedule  = $self->get_config('wakeup_schedule');
    my $next_time = $self->_next_wakeup_time_for_datetime($schedule, $now);

    # If nextEvent is too close to now, it should return the next-next event.
    # E.g. consider this sequence of actions:
    #   1. server asks display to wake up at 7:00
    #   2. display wakes up at 6:58 due to an inaccurate clock
    #   3. !!! server sends the content to display but it AGAIN suggests to wake up at 7:00
    #   4. !!! display unnecessarily wakes up at approx. 7:00 and displays the content. Or it wakes at 6:59:45 and the whole loop repeats (see point 2)
    #
    # Fixed here by accepting a small (~5 minutes) time difference:
    my $diff_seconds = $next_time->epoch - $now->epoch;
    if ($diff_seconds <= 5 * ONE_MINUTE) {

        # warn "wakeup time ($next_time) too close to now ($now), moving to next event";
        $next_time = $self->_next_wakeup_time_for_datetime($schedule, $next_time->clone->add(seconds => 1));

        # warn " -> updated next: $next_time";
    }

    my $sleep_in_seconds = $next_time->epoch - $now->epoch;

    # warn "($next_time [$local_timezone], $sleep_in_seconds, $schedule)";
    return ($next_time, $sleep_in_seconds, $schedule);
}

# You can replace this text with custom code or comments, and it will be preserved on regeneration
1;
