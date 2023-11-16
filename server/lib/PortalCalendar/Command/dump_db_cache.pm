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
    foreach my $row ($app->schema->resultset('Cache')->search(undef, { order_by => [ { -desc => ['expires_at'] }, { -asc => ['id'] } ] })->all) {
        my $perldata = Storable::thaw(b64_decode($row->data));
        p $perldata, as => 'cache row id "' . $row->id . '" created by ' . $row->creator . ' with key "' . $row->key . '" at ' . $row->created_at . ', expires at ' . $row->expires_at . ':', output => 'stdout';
        print "\n";
    }
}

1;
