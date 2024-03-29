#<<< skip perltidy formatting
use utf8;
package PortalCalendar::Schema::Result::MojoMigration;

# Created by DBIx::Class::Schema::Loader
# DO NOT MODIFY THE FIRST PART OF THIS FILE

=head1 NAME

PortalCalendar::Schema::Result::MojoMigration

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

=head1 TABLE: C<mojo_migrations>

=cut

__PACKAGE__->table("mojo_migrations");

=head1 ACCESSORS

=head2 name

  data_type: 'text'
  is_nullable: 0

=head2 version

  data_type: 'integer'
  is_nullable: 0

=cut

__PACKAGE__->add_columns(
  "name",
  { data_type => "text", is_nullable => 0 },
  "version",
  { data_type => "integer", is_nullable => 0 },
);

=head1 PRIMARY KEY

=over 4

=item * L</name>

=back

=cut

__PACKAGE__->set_primary_key("name");

#>>> end of perltidy skipped block

# Created by DBIx::Class::Schema::Loader v0.07049 @ 2023-11-03 13:53:17
# DO NOT MODIFY THIS OR ANYTHING ABOVE! md5sum:XiRNHNmcGw41ZDGZe4sZrg


# You can replace this text with custom code or comments, and it will be preserved on regeneration
1;
