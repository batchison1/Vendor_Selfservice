using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vss.Api.Auth;
using Vss.Api.Contracts;
using Vss.Domain;
using Vss.Infrastructure;

namespace Vss.Api.Controllers;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public class DocumentsController(VssDbContext db, CurrentUser current) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> Get(CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);
        if (user.VendorId is null) return Array.Empty<DocumentDto>();

        var docs = await db.Documents.Where(d => d.VendorId == user.VendorId).ToListAsync(ct);
        return docs.Select(d => new DocumentDto(d.Id, d.Name, d.FileRef, d.Validity, d.Status.ToString())).ToList();
    }

    /// <summary>Attach/replace a compliance document (metadata + stored file reference).</summary>
    [HttpPost]
    public async Task<ActionResult<DocumentDto>> Upload(DocumentUploadDto dto, CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);
        if (user.LinkState != LinkState.Linked || user.VendorId is null)
            return StatusCode(StatusCodes.Status403Forbidden, "Link your account first.");

        var doc = await db.Documents
            .FirstOrDefaultAsync(d => d.VendorId == user.VendorId && d.Name == dto.Name, ct);
        if (doc is null)
        {
            doc = new VendorDocument { VendorId = user.VendorId!.Value, Name = dto.Name };
            db.Documents.Add(doc);
        }
        doc.FileRef = dto.FileRef;
        doc.Status = DocumentStatus.Current;
        await db.SaveChangesAsync(ct);

        return new DocumentDto(doc.Id, doc.Name, doc.FileRef, doc.Validity, doc.Status.ToString());
    }
}
