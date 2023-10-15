package PortalCalendar::Command::nuke_caches;
use Mojo::Base 'Mojolicious::Command';

use File::Path;

has description => 'Clears all caches';
has usage       => <<"USAGE";
$0 nuke_caches

USAGE

sub run {
    my ($self, @args) = @_;
    my $app = $self->app;

    $app->log->debug("Clearing database cache");
    $app->schema->resultset('Cache')->delete_all;

    my $cache_dir = $app->home->child('cache/lwp');
    $app->log->debug("Clearing LWP cache: $cache_dir");
    my $removed_count = File::Path::rmtree($cache_dir, { verbose => 1, safe => 1, keep_root => 1 });
    $app->log->debug("...removed $removed_count files");
}

1;