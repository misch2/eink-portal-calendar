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

=head2 caches

Type: has_many

Related object: L<PortalCalendar::Schema::Result::Cache>

=cut

__PACKAGE__->has_many(
  "caches",
  "PortalCalendar::Schema::Result::Cache",
  { "foreign.display_id" => "self.id" },
  { cascade_copy => 0, cascade_delete => 0 },
);

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


# Created by DBIx::Class::Schema::Loader v0.07049 @ 2023-10-22 15:57:29
# DO NOT MODIFY THIS OR ANYTHING ABOVE! md5sum:pmRySSZhorHwaKI1lhbYmw



# You can replace this text with custom code or comments, and it will be preserved on regeneration
1;
