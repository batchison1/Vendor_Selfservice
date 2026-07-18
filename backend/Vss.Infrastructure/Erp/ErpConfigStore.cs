using Microsoft.Extensions.Options;
using Vss.Domain;

namespace Vss.Infrastructure.Erp;

/// <summary>
/// Source of truth for the (non-secret) ERP connection settings. Reads/writes the single
/// <see cref="ErpConfig"/> row, seeding it from appsettings/options the first time. The
/// connectors resolve their options from here at request time, so edits saved from the
/// admin screen take effect without a redeploy. Secrets are never stored — they are read
/// from options (user-secrets / env / credentials file).
/// </summary>
public class ErpConfigStore(VssDbContext db, IOptions<ErpOptions> options)
{
    /// <summary>The running provider (chosen at startup from appsettings), e.g. "SapByDesign".</summary>
    public string Provider => options.Value.Provider;

    /// <summary>The stored config row, seeded from options on first access.</summary>
    public ErpConfig Get()
    {
        var row = db.Set<ErpConfig>().FirstOrDefault();
        if (row is null)
        {
            row = FromOptions(options.Value);
            db.Add(row);
            db.SaveChanges();
        }
        return row;
    }

    /// <summary>Effective SAP options = stored connection settings + secret from options.</summary>
    public SapByDesignOptions EffectiveSap()
    {
        var row = Get();
        var o = options.Value.SapByDesign;
        return new SapByDesignOptions
        {
            BaseUrl = row.SapBaseUrl,
            QuerySupplierPath = row.SapQuerySupplierPath,
            ManageSupplierPath = row.SapManageSupplierPath,
            Username = row.SapUsername,
            SampleSupplierId = row.SapSampleSupplierId,
            CredentialsFile = o.CredentialsFile,
            Password = ResolveSapPassword(o),
        };
    }

    /// <summary>True when the active provider's secret is configured (never returns the value).</summary>
    public bool SecretConfigured() => Provider.Equals("BusinessCentral", StringComparison.OrdinalIgnoreCase)
        ? !string.IsNullOrEmpty(options.Value.BusinessCentral.ClientSecret)
        : !string.IsNullOrEmpty(ResolveSapPassword(options.Value.SapByDesign));

    public async Task UpdateAsync(ErpConfig update, CancellationToken ct = default)
    {
        var row = Get();
        row.SapBaseUrl = update.SapBaseUrl.Trim();
        row.SapQuerySupplierPath = update.SapQuerySupplierPath.Trim();
        row.SapManageSupplierPath = update.SapManageSupplierPath.Trim();
        row.SapUsername = update.SapUsername.Trim();
        row.SapSampleSupplierId = update.SapSampleSupplierId.Trim();
        row.BcBaseUrl = update.BcBaseUrl.Trim();
        row.BcCompanyId = update.BcCompanyId.Trim();
        row.BcTenantId = update.BcTenantId.Trim();
        row.BcClientId = update.BcClientId.Trim();
        row.BcScope = update.BcScope.Trim();
        row.BcSampleVendorNumber = update.BcSampleVendorNumber.Trim();
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Password from user-secrets/env, else the credentials file's last non-empty line.</summary>
    internal static string ResolveSapPassword(SapByDesignOptions o)
    {
        if (!string.IsNullOrEmpty(o.Password)) return o.Password;
        if (!string.IsNullOrWhiteSpace(o.CredentialsFile) && File.Exists(o.CredentialsFile))
        {
            var last = File.ReadAllLines(o.CredentialsFile).Select(l => l.Trim()).LastOrDefault(l => l.Length > 0);
            if (last is not null) return last;
        }
        return "";
    }

    public static ErpConfig FromOptions(ErpOptions e) => new()
    {
        SapBaseUrl = e.SapByDesign.BaseUrl,
        SapQuerySupplierPath = e.SapByDesign.QuerySupplierPath,
        SapManageSupplierPath = e.SapByDesign.ManageSupplierPath,
        SapUsername = e.SapByDesign.Username,
        SapSampleSupplierId = e.SapByDesign.SampleSupplierId,
        BcBaseUrl = e.BusinessCentral.BaseUrl,
        BcCompanyId = e.BusinessCentral.CompanyId,
        BcTenantId = e.BusinessCentral.TenantId,
        BcClientId = e.BusinessCentral.ClientId,
        BcScope = e.BusinessCentral.Scope,
        BcSampleVendorNumber = e.BusinessCentral.SampleVendorNumber,
    };
}
