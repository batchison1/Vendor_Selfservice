namespace Vss.Domain;

/// <summary>
/// A request from a portal user to link their account to an existing ERP vendor
/// record. Created when the user submits matching credentials; approved by City staff.
/// </summary>
public class LinkRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VendorUserId { get; set; }
    public VendorUser? VendorUser { get; set; }

    public LinkMethod Method { get; set; }

    // Submitted credentials — secrets are stored masked, never in the clear.
    public string? SubmittedVendorNumber { get; set; }
    public string? SubmittedPinMasked { get; set; }
    public string? SubmittedTaxId { get; set; }
    public string? SubmittedZip { get; set; }

    // Result of matching against the ERP.
    public Guid? MatchedVendorId { get; set; }
    public string? MatchedVendorNumber { get; set; }

    public LinkRequestStatus Status { get; set; } = LinkRequestStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }
}
