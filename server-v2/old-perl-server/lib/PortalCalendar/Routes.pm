package PortalCalendar::Routes;

use Mojo::Base 'Mojolicious';

has 'app';

sub setup {
    my $self = shift;

    my $app = $self->app;

    my $r = $app->routes;

    # Data only (API) endpoints
    $r->get('/ping')->to('Data#ping'); # ✅ converted to .NET
    $r->get('/config')->to('Data#config'); # ✅ converted to .NET
    $r->get('/calendar/bitmap')->to('Data#bitmap'); # ✅ converted to .NET
    $r->get('/calendar/bitmap/epaper')->to('Data#bitmap_epaper'); # ✅ converted to .NET

    # UI endpoints
    $r->get('/')->to('UI#select_display'); # ✅ converted to .NET
    $r->get('/home/:display_number')->to('UI#home'); # ✅ converted to .NET
    $r->get('/test/:display_number')->to('UI#test'); # ✅ converted to .NET
    $r->post('/delete/:display_number')->to('UI#delete_display'); # ✅ converted to .NET
    $r->get('/calendar/:display_number/html')->to('UI#calendar_html_default_date'); # ✅ converted to .NET
    $r->get('/calendar/:display_number/html/:date')->to('UI#calendar_html_specific_date'); # ✅ converted to .NET
    $r->get('/config_ui/:display_number')->to('UI#config_ui_show'); # ✅ converted to .NET
    $r->post('/config_ui/:display_number')->to('UI#config_ui_save'); # ✅ converted to .NET
    $r->get('/config_ui/theme/:display_number')->to('UI#config_ui_theme_show'); # ✅ converted to .NET

    # This MUST be first, to match sooner than the next route. Also it can't accept any parameters (Google OAuth restriction)
    $r->get('/auth/googlefit/cb')->to('Other#googlefit_callback'); # ✅ converted to .NET

    # But test of callbacks are not restricted in any way
    $r->get('/auth/googlefit/:display_number')->to('UI#googlefit_redirect'); # ✅ converted to .NET
    $r->get('/auth/googlefit/success/:display_number')->to('UI#googlefit_success'); # ✅ converted to .NET

    return;
}

1;