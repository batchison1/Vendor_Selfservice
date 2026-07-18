using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vss.Domain;
using Vss.Infrastructure;

namespace Vss.Api.Auth;

/// <summary>
/// Resolves the <see cref="VendorUser"/> for the current principal, provisioning one
/// on first sight (JIT). Works the same for dev and Entra: the external identity is
/// the Entra object id (<c>oid</c>) claim.
/// </summary>
public class CurrentUser(VssDbContext db, IHttpContextAccessor http)
{
    private readonly ClaimsPrincipal _principal =
        http.HttpContext?.User ?? new ClaimsPrincipal();

    public string ExternalUuid =>
        _principal.FindFirstValue("oid")
        ?? _principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("No external identity on the current principal.");

    public string Role => _principal.FindFirstValue(ClaimTypes.Role) ?? "vendor";
    public bool IsAdmin => Role.Equals("admin", StringComparison.OrdinalIgnoreCase);

    public async Task<VendorUser> GetOrProvisionAsync(CancellationToken ct = default)
    {
        var uuid = ExternalUuid;
        var user = await db.VendorUsers.Include(u => u.Vendor)
            .FirstOrDefaultAsync(u => u.ExternalUuid == uuid, ct);
        if (user is not null) return user;

        user = new VendorUser
        {
            ExternalUuid = uuid,
            Email = _principal.FindFirstValue(ClaimTypes.Email) ?? "",
            DisplayName = _principal.FindFirstValue(ClaimTypes.Name) ?? "",
            LinkState = LinkState.Unlinked,
        };
        db.VendorUsers.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
