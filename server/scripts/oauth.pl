# App to get permanent authentication token from Google OAuth2.
#
# I used https://gist.github.com/throughnothing/3726907 as a starting point.

# Run this sample app with:
# morbo oauth.pl --listen http://*:5555
#
# Then open http://localhost:5555/ in your browser.

use Mojolicious::Lite;
use Mojo::Util qw(url_escape);
use Mojo::JSON qw(decode_json encode_json);

use DDP;

my $config = plugin Config => { file => "oauth.conf" };

get '/' => sub { shift->render('home') };

get '/auth' => sub {

    # Redirect user to Google OAuth Login page
    # see https://developers.google.com/identity/protocols/oauth2/web-server#httprest_1
    my $url = "$config->{oauth_auth}" . "?client_id=$config->{client_id}&access_type=offline&response_type=code&scope=$config->{scope}&include_granted_scopes=true&redirect_uri=" . url_escape($config->{cb});
    print "Redirecting to [$url]\n";
    shift->redirect_to($url);
};

# OAuth 2 callback from google
get '/cb' => sub {
    my ($self) = @_;

    print "in callback, received this:\n";
    p $self->req->param('code'),  as => 'code';
    p $self->req->param('scope'), as => 'scope';

    #Get tokens from auth code
    my $res = $self->app->ua->post(
        "$config->{oauth_token}",
        'form',
        {
            code          => $self->req->param('code'),
            client_id     => $config->{client_id},
            client_secret => $config->{secret},
            redirect_uri  => $config->{cb},
            grant_type    => 'authorization_code',

            #scope         => $config->{scope},
        }
    )->res;

    # p $res;
    # p $res->body, as => 'response body';
    if (!$res->is_success) {
        $self->stash(error => decode_json($res->body));
        return $self->render('error');
    }

    # Save both tokens
    $self->session->{access_token}  = $res->json->{access_token};
    $self->session->{refresh_token} = $res->json->{refresh_token};

    $self->redirect_to('/success');
};

get '/success' => sub {
    my ($self) = @_;

    # Read access token from session
    my $a_token = $self->session->{access_token} or die "No access token!";

    $self->stash(a_token => $self->session->{access_token}, r_token => $self->session->{refresh_token});

    $self->render('success');
};

app->start;

  __DATA__
@@ home.html.ep
<a href='/auth'>Click here</a> to authenticate with Google OAuth.

@@ contacts.html.ep
<html><body>
<% for ( @$contacts ) { %>
    <div>
        <% if ( $_->{'gd$email'}[0] ) { %>
            <%= $_->{title}{'$t'} %>
            &lt;<%= $_->{'gd$email'}[0]{'address'} %>&gt;
        <% } %>
    </div>
<% } %>
</body></html>

@@ error.html.ep
<html><body>
<h1><%= $error->{error} %></h1>
<div>
    <i><%== $error->{error_description} %></i>
</div>
<p>
<a href="/">Return back</a>
</body></html>

@@ success.html.ep
<html><body>
<h1>Success!</h1>
Auth token: 
<pre><%= $a_token %></pre>
<br>
Refresh token: 
<pre><%= $r_token %></pre>
<p>
<a href="/">Return back</a>
</body></html>
