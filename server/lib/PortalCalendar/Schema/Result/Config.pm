use utf8;
package PortalCalendar::Schema::Result::Config;

# Created by DBIx::Class::Schema::Loader
# DO NOT MODIFY THE FIRST PART OF THIS FILE

=head1 NAME

PortalCalendar::Schema::Result::Config

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

=head1 TABLE: C<config>

=cut

__PACKAGE__->table("config");

=head1 ACCESSORS

=head2 id

  data_type: 'integer'
  is_auto_increment: 1
  is_nullable: 0

=head2 name

  data_type: 'varchar'
  is_nullable: 0

=head2 value

  data_type: 'varchar'
  is_nullable: 0

=head2 display_id

  data_type: 'integer'
  is_foreign_key: 1
  is_nullable: 1

=cut

__PACKAGE__->add_columns(
  "id",
  { data_type => "integer", is_auto_increment => 1, is_nullable => 0 },
  "name",
  { data_type => "varchar", is_nullable => 0 },
  "value",
  { data_type => "varchar", is_nullable => 0 },
  "display_id",
  { data_type => "integer", is_foreign_key => 1, is_nullable => 1 },
);

=head1 PRIMARY KEY

=over 4

=item * L</id>

=back

=cut

__PACKAGE__->set_primary_key("id");

=head1 UNIQUE CONSTRAINTS

=head2 C<name_display_id_unique>

=over 4

=item * L</name>

=item * L</display_id>

=back

=cut

__PACKAGE__->add_unique_constraint("name_display_id_unique", ["name", "display_id"]);

=head1 RELATIONS

=head2 display

Type: belongs_to

Related object: L<PortalCalendar::Schema::Result::Display>

=cut

__PACKAGE__->belongs_to(
  "display",
  "PortalCalendar::Schema::Result::Display",
  { id => "display_id" },
  {
    is_deferrable => 0,
    join_type     => "LEFT",
    on_delete     => "NO ACTION",
    on_update     => "NO ACTION",
  },
);


# Created by DBIx::Class::Schema::Loader v0.07049 @ 2023-10-22 15:57:29
# DO NOT MODIFY THIS OR ANYTHING ABOVE! md5sum:qA0MNRx9WM2kiwAze/eCJQ


# You can replace this text with custom code or comments, and it will be preserved on regeneration
1;
