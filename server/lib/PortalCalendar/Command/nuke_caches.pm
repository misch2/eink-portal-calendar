package PortalCalendar::Command::nuke_caches;
use Mojo::Base 'Mojolicious::Command';

use File::Path;

has description => 'Clears all caches';
has usage       => <<"USAGE";
$0 nuke_caches
$0 nuke_caches --db-only

USAGE

sub run {
    my ($self, @args) = @_;
    my $app = $self->app;

    $app->log->debug("Clearing database cache");
    $app->schema->resultset('Cache')->delete_all;

    return if $args[0] && $args[0] eq '--db-only';  # FIXME disgusting but it works

    my $lwp_cache_dir = $app->home->child('cache/lwp');
    $app->log->debug("Clearing LWP cache: $lwp_cache_dir");
    my $removed_count = File::Path::rmtree($lwp_cache_dir, { verbose => 1, safe => 1, keep_root => 1 });
    $app->log->debug("...removed $removed_count files");
}

1;