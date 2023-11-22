package PortalCalendar::Routes;

use Mojo::Base 'Mojolicious';

has 'app';

sub setup {
    my $self = shift;

    my $app = $self->app;

    my $r = $app->routes;

    # Data only (API) endpoints
    $r->get('/ping')->to('Data#ping');
    $r->get('/config')->to('Data#config');
    $r->get('/calendar/bitmap')->to('Data#bitmap');
    $r->get('/calendar/bitmap/epaper')->to('Data#bitmap_epaper');

    # UI endpoints
    $r->get('/')->to('UI#select_display');
    $r->get('/home/:display_number')->to('UI#home');
    $r->get('/test/:display_number')->to('UI#test');
    $r->get('/calendar/:display_number/html')->to('UI#calendar_html_default_date');
    $r->get('/calendar/:display_number/html/:date')->to('UI#calendar_html_specific_date');
    $r->get('/config_ui/:display_number')->to('UI#config_ui_show');
    $r->post('/config_ui/:display_number')->to('UI#config_ui_save');
    $r->get('/config_ui/theme/:display_number')->to('UI#config_ui_theme_show');

    # This MUST be first, to match sooner than the next route. Also it can't accept any parameters (Google OAuth restriction)
    $r->get('/auth/googlefit/cb')->to('Other#googlefit_callback');

    # But test of callbacks are not restricted in any way
    $r->get('/auth/googlefit/:display_number')->to('UI#googlefit_redirect');
    $r->get('/auth/googlefit/success/:display_number')->to('UI#googlefit_success');

    return;
}

1;