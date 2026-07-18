namespace Vss.Domain;

/// <summary>A commodity / NIGP code the vendor supplies against, e.g. "31000 · Industrial supplies".</summary>
public class VendorCategoryCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VendorId { get; set; }
    public string Code { get; set; } = string.Empty;
}
