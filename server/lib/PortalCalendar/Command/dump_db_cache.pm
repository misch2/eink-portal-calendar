package PortalCalendar::Command::dump_db_cache;
use Mojo::Base 'Mojolicious::Command';

use DDP;
use Storable;
use Mojo::Util qw(b64_decode);

has description => 'Dumps database cache content';
has usage       => <<"USAGE";
$0 dump_db_cache

USAGE

sub run {
    my ($self, @args) = @_;
    my $app = $self->app;

    $app->log->info("Database cache content:");
    foreach my $row ($app->schema->resultset('Cache')->search(undef, { order_by => 'id' })->all) {
        my $perldata = Storable::thaw(b64_decode($row->data));
        p $perldata, as => 'cache row id "' . $row->id . '" created at ' . $row->created_utc . ':', output => 'stdout';
        print "\n";
    }
}

1;