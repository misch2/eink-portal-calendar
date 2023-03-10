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

=head1 TABLE: C<cache>

=cut

__PACKAGE__->table("cache");

=head1 ACCESSORS

=head2 id

  data_type: 'varchar'
  is_nullable: 0
  size: 255

=head2 data

  data_type: 'blob'
  is_nullable: 1

=cut

__PACKAGE__->add_columns(
  "id",
  { data_type => "varchar", is_nullable => 0, size => 255 },
  "data",
  { data_type => "blob", is_nullable => 1 },
);

=head1 PRIMARY KEY

=over 4

=item * L</id>

=back

=cut

__PACKAGE__->set_primary_key("id");


# Created by DBIx::Class::Schema::Loader v0.07049 @ 2023-02-21 19:51:20
# DO NOT MODIFY THIS OR ANYTHING ABOVE! md5sum:b5b742zFtKOvCn6ElWeyjQ


# You can replace this text with custom code or comments, and it will be preserved on regeneration
1;
