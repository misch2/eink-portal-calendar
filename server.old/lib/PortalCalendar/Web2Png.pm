package PortalCalendar::Web2Png;

use Mojo::Base -base;

use DDP;
use File::pushd;
use File::Copy;

#use File::Temp;

has 'pageres_command';

sub convert_url {
    my $self    = shift;
    my $url     = shift;
    my $width   = shift;
    my $height  = shift;
    my $dstfile = shift;

    # Pageres generates file relative to cwd(), always. It doesn't support absolute paths
    my $tmpfile = "web2png.tmp";                                                                                                  # only filename, NEVER with a path
    my @cmd     = ($self->pageres_command, $url, "${width}x${height}", "--filename=${tmpfile}", "--overwrite", "--format=png", "--delay=2");    # delay for web fonts

    {   my $dir = tempd();
        system(@cmd) == 0
            or die "system() failed: $?";

        copy("$dir/${tmpfile}.png", $dstfile) || die "Can't copy file: $!";

        unlink "$dir/${tmpfile}.png";
    }

    return;
}

1;