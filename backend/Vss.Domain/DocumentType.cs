namespace Vss.Domain;

/// <summary>
/// A configurable document type the vendor portal offers for upload (e.g. code "W9" →
/// "W-9 Tax Form"). Maintained by City staff; the upload dropdown is driven from the
/// active rows here.
/// </summary>
public class DocumentType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Short stable code, e.g. "W9" or "COI".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable label shown to vendors, e.g. "W-9 Tax Form".</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Inactive types are hidden from the upload dropdown but kept for history.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Ascending display order in the dropdown / admin list.</summary>
    public int SortOrder { get; set; }
}
