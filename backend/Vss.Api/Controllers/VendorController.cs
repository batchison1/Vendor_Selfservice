using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vss.Api.Auth;
using Vss.Api.Contracts;
using Vss.Api.Mapping;
using Vss.Domain;
using Vss.Infrastructure;
using Vss.Infrastructure.Erp;

namespace Vss.Api.Controllers;

[ApiController]
[Route("api/v1/vendor")]
[Authorize]
public class VendorController(VssDbContext db, CurrentUser current, IErpClient erp, ILogger<VendorController> log) : ControllerBase
{
    /// <summary>The current user's linked vendor record (all sections, secrets masked).</summary>
    [HttpGet]
    public async Task<ActionResult<VendorDto>> Get(CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);
        if (user.VendorId is null)
            return StatusCode(StatusCodes.Status403Forbidden, "Account is not linked to a vendor record yet.");

        var v = await db.Vendors.Include(x => x.Documents).Include(x => x.CategoryCodes)
            .FirstOrDefaultAsync(x => x.Id == user.VendorId, ct);
        if (v is null) return NotFound();

        // The ERP is the system of record for payment method and bank data, so refresh those
        // from it before display — the banking page then loads the vendor's real payment
        // method (e.g. Check) and current active bank account.
        await RefreshBankingFromErpAsync(v, ct);
        return VendorMapping.ToDto(v);
    }

    private async Task RefreshBankingFromErpAsync(Vendor v, CancellationToken ct)
    {
        try
        {
            var erpDto = await erp.GetVendorAsync(v.Number, ct);
            if (erpDto is null) return;

            var changed = false;
            void Sync(string? incoming, Func<string> get, Action<string> set)
            {
                if (!string.IsNullOrEmpty(incoming) && get() != incoming) { set(incoming); changed = true; }
            }
            Sync(erpDto.PaymentMethod, () => v.PaymentMethod, val => v.PaymentMethod = val);
            Sync(erpDto.RoutingNumber, () => v.RoutingNumber ?? "", val => v.RoutingNumber = val);
            Sync(erpDto.AccountNumber, () => v.AccountNumber ?? "", val => v.AccountNumber = val);
            // AccountType is not reliably returned by SAP (no BankAccountTypeCode), so it is
            // left to the local record rather than clobbered with the DTO default.

            if (changed)
            {
                v.LastSyncedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
        }
        catch (Exception ex)
        {
            // A live ERP hiccup shouldn't break the profile page — serve the local copy.
            log.LogWarning(ex, "ERP banking refresh failed for vendor {Number}; serving local copy", v.Number);
        }
    }
}
