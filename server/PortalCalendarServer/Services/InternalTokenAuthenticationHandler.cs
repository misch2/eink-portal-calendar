using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace PortalCalendarServer.Services;

public class InternalTokenAuthenticationOptions : AuthenticationSchemeOptions { }

/// <summary>
/// Authentication handler that accepts requests carrying the transient internal token
/// in the X-Internal-Token header. Used by PageGenerator (Playwright) to access
/// protected calendar HTML endpoints without a user cookie session.
/// </summary>
public class InternalTokenAuthenticationHandler(
    IOptionsMonitor<InternalTokenAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    InternalTokenService tokenService)
    : AuthenticationHandler<InternalTokenAuthenticationOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "InternalToken";
    public const string HeaderName = "X-Internal-Token";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValue))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (headerValue != tokenService.Token)
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal token"));

        var claims = new[] { new Claim(ClaimTypes.Name, "internal-page-generator") };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
