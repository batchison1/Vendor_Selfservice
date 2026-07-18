using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vss.Api.Auth;
using Vss.Api.Contracts;
using Vss.Domain;
using Vss.Infrastructure;

namespace Vss.Api.Controllers;

[ApiController]
[Route("api/v1/change-requests")]
[Authorize]
public class ChangeRequestsController(VssDbContext db, CurrentUser current) : ControllerBase
{
    /// <summary>Submit proposed edits to one profile section. Recorded as a pending
    /// change request; nothing is written to the ERP until City staff approve.</summary>
    [HttpPost]
    public async Task<ActionResult<ChangeRequestDto>> Create(ChangeRequestCreateDto dto, CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);
        if (user.LinkState != LinkState.Linked || user.VendorId is null)
            return StatusCode(StatusCodes.Status403Forbidden, "Link your account before submitting changes.");
        if (dto.Diffs.Length == 0)
            return BadRequest("No changes to submit.");

        var vendor = await db.Vendors.FirstAsync(v => v.Id == user.VendorId, ct);

        var cr = new ChangeRequest
        {
            Code = await NextCodeAsync(ct),
            VendorId = vendor.Id,
            Section = dto.Section,
            SubmittedByUserId = user.Id,
            SubmittedByName = user.DisplayName,
            Status = ChangeRequestStatus.PendingReview,
            Diffs = dto.Diffs.Select(d => new ChangeDiff
            {
                Field = d.Field,
                FromValue = d.FromValue,
                ToValue = d.ToValue,
            }).ToList(),
        };
        db.ChangeRequests.Add(cr);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetMine), null, ToDto(cr, vendor.LegalName));
    }

    /// <summary>The current vendor's change requests, newest first.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChangeRequestDto>>> GetMine([FromQuery] bool mine = true, CancellationToken ct = default)
    {
        var user = await current.GetOrProvisionAsync(ct);
        if (user.VendorId is null) return Array.Empty<ChangeRequestDto>();

        var rows = await db.ChangeRequests
            .Include(c => c.Diffs).Include(c => c.Vendor)
            .Where(c => c.VendorId == user.VendorId)
            .OrderByDescending(c => c.SubmittedAt)
            .ToListAsync(ct);

        return rows.Select(c => ToDto(c, c.Vendor?.LegalName ?? "")).ToList();
    }

    private async Task<string> NextCodeAsync(CancellationToken ct)
    {
        var codes = await db.ChangeRequests.Select(c => c.Code).ToListAsync(ct);
        var max = codes
            .Select(c => int.TryParse(c.Replace("CR-", ""), out var n) ? n : 0)
            .DefaultIfEmpty(2043)
            .Max();
        return $"CR-{max + 1}";
    }

    private static ChangeRequestDto ToDto(ChangeRequest c, string vendorName) => new(
        c.Id, c.Code, vendorName, c.Section, c.SubmittedByName, c.SubmittedAt, c.Status.ToString(),
        c.Diffs.Select(d => new ChangeDiffDto(d.Field, d.FromValue, d.ToValue)).ToArray());
}
