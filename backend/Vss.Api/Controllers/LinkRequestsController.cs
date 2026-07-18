using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vss.Api.Auth;
using Vss.Api.Contracts;
using Vss.Api.Mapping;
using Vss.Api.Services;
using Vss.Domain;
using Vss.Infrastructure;
using Vss.Infrastructure.Erp;

namespace Vss.Api.Controllers;

[ApiController]
[Route("api/v1/link-requests")]
[Authorize]
public class LinkRequestsController(VssDbContext db, CurrentUser current, IErpClient erp) : ControllerBase
{
    /// <summary>Submit linking credentials; matches against the ERP and returns the
    /// masked candidate record for the user to confirm.</summary>
    [HttpPost]
    public async Task<ActionResult<LinkMatchResultDto>> Create(LinkRequestCreateDto dto, CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);

        var match = await erp.MatchVendorAsync(new MatchQuery
        {
            Method = dto.Method,
            VendorNumber = dto.VendorNumber,
            Pin = dto.Pin,
            TaxId = dto.TaxId,
            Zip = dto.Zip,
        }, ct);

        var lr = new LinkRequest
        {
            VendorUserId = user.Id,
            Method = dto.Method == "TaxIdZip" ? LinkMethod.TaxIdZip : LinkMethod.VendorNumberPin,
            SubmittedVendorNumber = dto.VendorNumber,
            SubmittedPinMasked = string.IsNullOrEmpty(dto.Pin) ? null : new string('•', dto.Pin!.Length),
            SubmittedTaxId = dto.TaxId,
            SubmittedZip = dto.Zip,
            MatchedVendorNumber = match?.Number,
            Status = match is null ? LinkRequestStatus.Pending : LinkRequestStatus.Matched,
        };
        if (match is not null)
            lr.MatchedVendorId = (await db.Vendors.FirstOrDefaultAsync(v => v.Number == match.Number, ct))?.Id;

        db.LinkRequests.Add(lr);
        await db.SaveChangesAsync(ct);

        return match is null
            ? new LinkMatchResultDto(lr.Id, false, null, null, null, null, null, null, "NoMatch")
            : new LinkMatchResultDto(lr.Id, true, match.Number, match.LegalName, match.RemitCity,
                match.RemitState, match.RemitZip, VendorMapping.MaskTin(match.Tin), lr.Status.ToString());
    }

    /// <summary>Confirm the matched record and link the account. In this phase linking
    /// auto-approves for a smooth vendor-first demo; on the network this becomes a
    /// City-staff approval (see AdminController).</summary>
    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<MeDto>> Confirm(Guid id, CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);
        var lr = await db.LinkRequests.FirstOrDefaultAsync(x => x.Id == id && x.VendorUserId == user.Id, ct);
        if (lr is null) return NotFound();
        if (lr.MatchedVendorNumber is null) return BadRequest("No matched record to confirm.");

        var vendor = await db.Vendors.FirstOrDefaultAsync(v => v.Number == lr.MatchedVendorNumber, ct);
        if (vendor is null)
        {
            var erpVendor = await erp.GetVendorAsync(lr.MatchedVendorNumber, ct);
            if (erpVendor is null) return BadRequest("Matched record no longer available in the ERP.");
            vendor = PortalQueries.FromErp(erpVendor);
            db.Vendors.Add(vendor);
        }

        user.VendorId = vendor.Id;
        user.LinkState = LinkState.Linked;
        lr.Status = LinkRequestStatus.Approved;
        lr.DecidedAt = DateTimeOffset.UtcNow;
        lr.DecidedBy = "auto (dev)";
        await db.SaveChangesAsync(ct);

        return await PortalQueries.BuildMeAsync(db, current.Role, user, ct);
    }
}
