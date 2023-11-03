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
use List::Util qw(min max);
use Schedule::Cron::Events;
use Time::Seconds;

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
    return $self->configs->search({ name => $name })->first->value // undef;
}

sub voltage {
    my $self = shift;

    my $raw_adc_reading       = $self->get_config('_last_voltage_raw');
    my $voltage_divider_ratio = $self->get_config('voltage_divider_ratio');
    return undef unless $raw_adc_reading && $voltage_divider_ratio;

    my $adc_reference_voltage = 3.3;
    my $adc_resolution        = 4095;

    my $voltage = $raw_adc_reading * $adc_reference_voltage / $adc_resolution * $voltage_divider_ratio;
    return sprintf("%.3f", $voltage);
}

sub battery_percent {
    my $self = shift;
    my $min  = $self->get_config('min_voltage');
    my $max  = $self->get_config('max_voltage');

    my $cur = $self->voltage;
    return unless $min && $max && $cur;
    my $percentage = 100 * ($cur - $min) / ($max - $min);
    $percentage = min(100, max(0, $percentage));    # clip to 0-100

    return sprintf("%.1f", $percentage);
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

sub next_wakeup_time {
    my $self = shift;

    # crontab definitions are in SERVER LOCAL time zone, not display dependent
    my $local_timezone = DateTime::TimeZone->new(name => 'local');

    # Parse the crontab-like schedule
    my $schedule = $self->get_config('wakeup_schedule');

    my $now  = DateTime->now(time_zone => $local_timezone);
    my $cron = Schedule::Cron::Events->new($schedule, Seconds => $now->epoch) or die "Invalid crontab schedule";

    my ($seconds, $minutes, $hours, $dayOfMonth, $month, $year) = $cron->nextEvent;
    my $next_time = DateTime->new(year => 1900 + $year, month => 1 + $month, day => $dayOfMonth, hour => $hours, minute => $minutes, second => $seconds, time_zone => $local_timezone);

    # If nextEvent is too close to now, it should return the next-next event.
    # E.g. consider this sequence of actions:
    #   1. server asks display to wake up at 7:00
    #   2. display wakes up at 6:58 due to an inaccurate clock
    #   3. !!! server sends the content to display but it AGAIN suggests to wake up at 7:00
    #   4. !!! display unnecessarily wakes up at approx. 7:00 and displays the content. Or it wakes at 6:59:45 and the whole loop repeats (see point 2)
    #
    # Fixed here by accepting a small (~5 minutes) time difference:
    my $diff_seconds = $next_time->epoch - $now->epoch;

    # warn "diff_seconds: $diff_seconds";
    if ($diff_seconds <= 5 * ONE_MINUTE) {
        warn "wakeup time ($next_time) too close to now ($now), moving to next event";
        $cron = Schedule::Cron::Events->new($schedule, Seconds => $next_time->epoch) or die "Invalid crontab schedule";

        ($seconds, $minutes, $hours, $dayOfMonth, $month, $year) = $cron->nextEvent;
        $next_time = DateTime->new(year => 1900 + $year, month => 1 + $month, day => $dayOfMonth, hour => $hours, minute => $minutes, second => $seconds, time_zone => $local_timezone);

        warn " -> updated next: $next_time";
    }

    my $sleep_in_seconds = $next_time->epoch - $now->epoch;

    # warn "($next_time [$local_timezone], $sleep_in_seconds, $schedule)";
    return ($next_time, $sleep_in_seconds, $schedule);
}

# You can replace this text with custom code or comments, and it will be preserved on regeneration
1;
