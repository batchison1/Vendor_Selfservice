using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vss.Api.Contracts;
using Vss.Domain;
using Vss.Infrastructure;
using Vss.Infrastructure.Erp;

namespace Vss.Api.Controllers;

/// <summary>
/// City-staff endpoints. Vendor-first phase ships the change-request approval path
/// (which completes the ERP round-trip) plus read lists; the full admin UI/endpoints
/// are the next phase.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Admin")]
public class AdminController(VssDbContext db, IErpClient erp) : ControllerBase
{
    [HttpGet("change-requests")]
    public async Task<ActionResult<IEnumerable<ChangeRequestDto>>> ChangeRequests(CancellationToken ct)
    {
        var rows = await db.ChangeRequests.Include(c => c.Diffs).Include(c => c.Vendor)
            .OrderByDescending(c => c.SubmittedAt).ToListAsync(ct);
        return rows.Select(c => new ChangeRequestDto(
            c.Id, c.Code, c.Vendor?.LegalName ?? "", c.Section, c.SubmittedByName, c.SubmittedAt, c.Status.ToString(),
            c.Diffs.Select(d => new ChangeDiffDto(d.Field, d.FromValue, d.ToValue)).ToArray())).ToList();
    }

    /// <summary>Approve a change request: apply the diff to the local record and push
    /// it to the ERP vendor master via <see cref="IErpClient"/>.</summary>
    [HttpPost("change-requests/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, ReviewDecisionDto? decision, CancellationToken ct)
    {
        var cr = await db.ChangeRequests.Include(c => c.Diffs).Include(c => c.Vendor)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cr is null) return NotFound();
        if (cr.Vendor is null) return BadRequest("Change request has no vendor.");

        var patch = new VendorMasterPatch();
        foreach (var d in cr.Diffs)
        {
            var prop = typeof(Vendor).GetProperty(d.Field);
            if (prop is not null && prop.PropertyType == typeof(string))
                prop.SetValue(cr.Vendor, d.ToValue);
            patch.Fields[d.Field] = d.ToValue;
        }

        await erp.UpdateVendorMasterAsync(cr.Vendor.Number, patch, ct);
        cr.Vendor.LastSyncedAt = DateTimeOffset.UtcNow;

        cr.Status = ChangeRequestStatus.Approved;
        cr.DecidedAt = DateTimeOffset.UtcNow;
        cr.DecisionNote = decision?.Note;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("change-requests/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, ReviewDecisionDto? decision, CancellationToken ct)
    {
        var cr = await db.ChangeRequests.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (cr is null) return NotFound();
        cr.Status = ChangeRequestStatus.Rejected;
        cr.DecidedAt = DateTimeOffset.UtcNow;
        cr.DecisionNote = decision?.Note;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("vendors")]
    public async Task<ActionResult<IEnumerable<object>>> Vendors(CancellationToken ct)
    {
        var rows = await db.Vendors.Include(v => v.CategoryCodes).OrderBy(v => v.Number).ToListAsync(ct);
        return rows.Select(v => (object)new
        {
            v.Number,
            v.LegalName,
            Category = v.CategoryCodes.FirstOrDefault()?.Code ?? "",
            LastSync = v.LastSyncedAt,
            v.Status,
        }).ToList();
    }
}
