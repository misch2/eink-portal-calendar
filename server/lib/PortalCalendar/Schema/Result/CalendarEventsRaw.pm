use utf8;
package PortalCalendar::Schema::Result::CalendarEventsRaw;

# Created by DBIx::Class::Schema::Loader
# DO NOT MODIFY THE FIRST PART OF THIS FILE

=head1 NAME

PortalCalendar::Schema::Result::CalendarEventsRaw

=cut

use strict;
use warnings;

use base 'DBIx::Class::Core';

=head1 TABLE: C<calendar_events_raw>

=cut

__PACKAGE__->table("calendar_events_raw");

=head1 ACCESSORS

=head2 calendar_id

  data_type: 'integer'
  is_auto_increment: 1
  is_nullable: 0

=head2 events_raw

  data_type: 'blob'
  is_nullable: 1

=cut

__PACKAGE__->add_columns(
  "calendar_id",
  { data_type => "integer", is_auto_increment => 1, is_nullable => 0 },
  "events_raw",
  { data_type => "blob", is_nullable => 1 },
);

=head1 PRIMARY KEY

=over 4

=item * L</calendar_id>

=back

=cut

__PACKAGE__->set_primary_key("calendar_id");


# Created by DBIx::Class::Schema::Loader v0.07049 @ 2023-02-19 11:36:49
# DO NOT MODIFY THIS OR ANYTHING ABOVE! md5sum:+Ot923kJQg2rSZ6jO63qVg


# You can replace this text with custom code or comments, and it will be preserved on regeneration
1;
