#<<< skip perltidy formatting
use utf8;
package PortalCalendar::Schema::Result::Cache;

# Created by DBIx::Class::Schema::Loader
# DO NOT MODIFY THE FIRST PART OF THIS FILE

=head1 NAME

PortalCalendar::Schema::Result::Cache

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

=head1 TABLE: C<cache>

=cut

__PACKAGE__->table("cache");

=head1 ACCESSORS

=head2 id

  data_type: 'integer'
  is_auto_increment: 1
  is_nullable: 0

=head2 creator

  data_type: 'varchar'
  is_nullable: 0
  size: 255

=head2 key

  data_type: 'varchar'
  is_nullable: 0
  size: 255

=head2 created_at

  data_type: 'datetime'
  default_value: 0
  is_nullable: 0

=head2 expires_at

  data_type: 'datetime'
  default_value: 0
  is_nullable: 0

=head2 data

  data_type: 'blob'
  is_nullable: 1

=cut

__PACKAGE__->add_columns(
  "id",
  { data_type => "integer", is_auto_increment => 1, is_nullable => 0 },
  "creator",
  { data_type => "varchar", is_nullable => 0, size => 255 },
  "key",
  { data_type => "varchar", is_nullable => 0, size => 255 },
  "created_at",
  { data_type => "datetime", default_value => 0, is_nullable => 0 },
  "expires_at",
  { data_type => "datetime", default_value => 0, is_nullable => 0 },
  "data",
  { data_type => "blob", is_nullable => 1 },
);

=head1 PRIMARY KEY

=over 4

=item * L</id>

=back

=cut

__PACKAGE__->set_primary_key("id");

=head1 UNIQUE CONSTRAINTS

=head2 C<creator_key_unique>

=over 4

=item * L</creator>

=item * L</key>

=back

=cut

__PACKAGE__->add_unique_constraint("creator_key_unique", ["creator", "key"]);

#>>> end of perltidy skipped block

# Created by DBIx::Class::Schema::Loader v0.07049 @ 2023-11-03 13:53:17
# DO NOT MODIFY THIS OR ANYTHING ABOVE! md5sum:QPQYmSqgT4J2ntfTWanYmA


# You can replace this text with custom code or comments, and it will be preserved on regeneration
1;
