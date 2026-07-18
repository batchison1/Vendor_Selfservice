using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vss.Api.Auth;
using Vss.Api.Contracts;
using Vss.Domain;
using Vss.Infrastructure;
using Vss.Infrastructure.Documents;

namespace Vss.Api.Controllers;

[ApiController]
[Route("api/v1/documents")]
[Authorize]
public class DocumentsController(VssDbContext db, CurrentUser current, IDocumentStore store) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> Get(CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);
        if (user.VendorId is null) return Array.Empty<DocumentDto>();

        var docs = await db.Documents.Where(d => d.VendorId == user.VendorId).ToListAsync(ct);
        return docs.Select(d => new DocumentDto(d.Id, d.Name, d.FileRef, d.Validity, d.Status.ToString(), d.DocumentTypeCode)).ToList();
    }

    /// <summary>Upload a document's bytes: store them via <see cref="IDocumentStore"/> and
    /// open a "Documents" change request so City staff can review (and preview) it before it
    /// is attached to the ERP supplier master on approval.</summary>
    [HttpPost]
    public async Task<ActionResult<DocumentDto>> Upload(DocumentUploadDto dto, CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);
        if (user.LinkState != LinkState.Linked || user.VendorId is null)
            return StatusCode(StatusCodes.Status403Forbidden, "Link your account first.");

        // Resolve the selected document type from the configuration table.
        var type = await db.DocumentTypes.FirstOrDefaultAsync(t => t.Code == dto.TypeCode && t.IsActive, ct);
        if (type is null) return BadRequest($"Unknown or inactive document type '{dto.TypeCode}'.");

        byte[] bytes;
        try { bytes = Convert.FromBase64String(dto.ContentBase64); }
        catch (FormatException) { return BadRequest("ContentBase64 is not valid base64."); }
        if (bytes.Length == 0) return BadRequest("Empty file.");

        var contentType = string.IsNullOrWhiteSpace(dto.ContentType) ? "application/octet-stream" : dto.ContentType;
        var storageRef = await store.SaveAsync(dto.FileName, contentType, bytes, ct);

        // One document per type per vendor: replace the existing one, else create it.
        var doc = await db.Documents.FirstOrDefaultAsync(d => d.VendorId == user.VendorId && d.DocumentTypeCode == type.Code, ct);
        if (doc is null)
        {
            doc = new VendorDocument { VendorId = user.VendorId!.Value, DocumentTypeCode = type.Code };
            db.Documents.Add(doc);
        }
        doc.Name = type.Description;
        doc.FileRef = dto.FileName;
        doc.StorageRef = storageRef;
        doc.ContentType = contentType;
        doc.SizeBytes = bytes.Length;
        doc.Status = DocumentStatus.PendingReview;
        await db.SaveChangesAsync(ct); // persist doc.Id before referencing it on the change request

        // Open a review item so the document appears in the City staff review queue.
        var vendor = await db.Vendors.FirstAsync(v => v.Id == user.VendorId, ct);
        var codes = await db.ChangeRequests.Select(c => c.Code).ToListAsync(ct);
        var next = codes.Select(c => int.TryParse(c.Replace("CR-", ""), out var n) ? n : 0).DefaultIfEmpty(2043).Max() + 1;
        db.ChangeRequests.Add(new ChangeRequest
        {
            Code = $"CR-{next}",
            VendorId = vendor.Id,
            Section = "Documents",
            DocumentId = doc.Id,
            SubmittedByUserId = user.Id,
            SubmittedByName = user.DisplayName,
            Diffs = { new ChangeDiff { Field = type.Description, FromValue = null, ToValue = dto.FileName } },
        });
        await db.SaveChangesAsync(ct);

        return new DocumentDto(doc.Id, doc.Name, doc.FileRef, doc.Validity, doc.Status.ToString(), doc.DocumentTypeCode);
    }

    /// <summary>Active document types for the upload dropdown (from the config table).</summary>
    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<DocumentTypeDto>>> Types(CancellationToken ct)
    {
        var types = await db.DocumentTypes.Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Description).ToListAsync(ct);
        return types.Select(t => new DocumentTypeDto(t.Id, t.Code, t.Description, t.IsActive, t.SortOrder)).ToList();
    }

    /// <summary>Streams a stored document's bytes for preview/download. The owning vendor or
    /// City staff (Admin) may read it.</summary>
    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> Content(Guid id, CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);
        var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null || doc.StorageRef is null) return NotFound();

        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!isAdmin && doc.VendorId != user.VendorId) return Forbid();

        var file = await store.GetAsync(doc.StorageRef, ct);
        if (file is null) return NotFound();
        return File(file.Content, file.ContentType, file.FileName);
    }
}
