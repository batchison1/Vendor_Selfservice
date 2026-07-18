using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Vss.Api.Auth;

/// <summary>
/// Local-development authentication. Authenticates every request as a seeded user so
/// the portal runs without Entra. The frontend (AUTH_MODE=dev) may send headers to
/// switch identity/role; defaults to Dana Whitfield (vendor). Never registered when
/// Auth:Mode is "Entra".
/// </summary>
public class DevAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Dev";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var uuid = Header("X-Dev-Uuid", "dev-dana-northstar");
        var email = Header("X-Dev-Email", "dana@northstarsupply.com");
        var name = Header("X-Dev-Name", "Dana Whitfield");
        var role = Header("X-Dev-Role", "vendor");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, uuid),
            new Claim("oid", uuid),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private string Header(string key, string fallback)
        => Request.Headers.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : fallback;
}
