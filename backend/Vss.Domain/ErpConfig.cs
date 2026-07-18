namespace Vss.Domain;

/// <summary>
/// The editable ERP connection configuration (single row), maintained by City staff from
/// the admin ERP screen. Holds the non-secret connection settings for each provider;
/// secrets (SAP password, BC client secret) stay in user-secrets / env and are never
/// stored here. Seeded from appsettings/options on first run.
/// </summary>
public class ErpConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ---- SAP Business ByDesign (SOAP / HTTP Basic) ----
    public string SapBaseUrl { get; set; } = "";
    public string SapQuerySupplierPath { get; set; } = "";
    public string SapManageSupplierPath { get; set; } = "";
    public string SapUsername { get; set; } = "";
    public string SapSampleSupplierId { get; set; } = "";

    // ---- Dynamics 365 Business Central (REST / OAuth2) ----
    public string BcBaseUrl { get; set; } = "";
    public string BcCompanyId { get; set; } = "";
    public string BcTenantId { get; set; } = "";
    public string BcClientId { get; set; } = "";
    public string BcScope { get; set; } = "";
    public string BcSampleVendorNumber { get; set; } = "";

    public DateTimeOffset? UpdatedAt { get; set; }
}
