namespace Vss.Domain;

/// <summary>A compliance document attached to a vendor (W-9, COI, license, ...).</summary>
public class VendorDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VendorId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Stored file reference / filename. Null when nothing uploaded yet.</summary>
    public string? FileRef { get; set; }

    /// <summary>Human-readable validity, e.g. "No expiry" or "Exp. 12/31/2026".</summary>
    public string Validity { get; set; } = "—";

    public DocumentStatus Status { get; set; } = DocumentStatus.AwaitingDocs;
}
