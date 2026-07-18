namespace Vss.Domain;

/// <summary>
/// A portal login, keyed to a Microsoft Entra identity. A user becomes able to
/// edit a vendor record only once <see cref="LinkState"/> is <see cref="LinkState.Linked"/>.
/// </summary>
public class VendorUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Entra (AAD) object id — the stable external identity.</summary>
    public string ExternalUuid { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>The linked vendor, once approved.</summary>
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    public LinkState LinkState { get; set; } = LinkState.Unlinked;
}
