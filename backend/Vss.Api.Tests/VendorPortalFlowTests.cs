using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vss.Api.Contracts;
using Vss.Infrastructure;
using Vss.Infrastructure.Erp;
using Xunit;

namespace Vss.Api.Tests;

/// <summary>
/// Boots the API against a private SQLite in-memory database (a real relational
/// provider — so SQL translation issues surface here, unlike the EF InMemory provider).
/// The connection is held open for the factory's lifetime to keep the DB alive.
/// </summary>
internal sealed class VssAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _conn = new("Data Source=:memory:");

    public VssAppFactory() => _conn.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            // Remove the SQLite-file registration (options, the options-configuration
            // descriptor AddDbContext adds in EF Core 10, and the context) so only our
            // in-memory connection is configured.
            foreach (var d in services.Where(s =>
                         s.ServiceType == typeof(VssDbContext) ||
                         (s.ServiceType.FullName?.Contains("DbContextOptions") ?? false)).ToList())
                services.Remove(d);

            services.AddDbContext<VssDbContext>(o => o.UseSqlite(_conn));

            // Force the in-memory ERP stub regardless of ambient Erp:Provider config
            // (e.g. a machine with user-secrets pointing at a real ERP).
            foreach (var d in services.Where(s => s.ServiceType == typeof(IErpClient)).ToList())
                services.Remove(d);
            services.AddSingleton<IErpClient, StubErpClient>();
        });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conn.Dispose();
    }
}

/// <summary>End-to-end tests over the vendor-first flow: link → read → change → approve.</summary>
public class VendorPortalFlowTests
{
    private static VssAppFactory NewApp() => new();

    /// <summary>A client acting as a fresh (JIT-provisioned, unlinked) dev user, so tests
    /// don't depend on the seeded user's link state.</summary>
    private static HttpClient FreshClient(VssAppFactory app, string uuid)
    {
        var c = app.CreateClient();
        c.DefaultRequestHeaders.Add("X-Dev-Uuid", uuid);
        c.DefaultRequestHeaders.Add("X-Dev-Email", $"{uuid}@test.local");
        c.DefaultRequestHeaders.Add("X-Dev-Name", "Test User");
        return c;
    }

    [Fact]
    public async Task Unlinked_user_sees_no_vendor()
    {
        using var app = NewApp();
        var client = FreshClient(app, "unlinked-user");

        var me = await client.GetFromJsonAsync<MeDto>("/api/v1/me");

        Assert.NotNull(me);
        Assert.Equal("Unlinked", me!.LinkState);
        Assert.Null(me.VendorNumber);
    }

    [Fact]
    public async Task Vendor_cannot_submit_changes_before_linking()
    {
        using var app = NewApp();
        var client = FreshClient(app, "nolink-user");

        var res = await client.PostAsJsonAsync("/api/v1/change-requests",
            new ChangeRequestCreateDto("Banking & remittance",
                new[] { new ChangeDiffDto("BankName", "A", "B") }));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Wrong_pin_does_not_match()
    {
        using var app = NewApp();
        var client = app.CreateClient();

        var result = await (await client.PostAsJsonAsync("/api/v1/link-requests",
            new LinkRequestCreateDto("VendorNumberPin", "V-10485", "0000", null, null)))
            .Content.ReadFromJsonAsync<LinkMatchResultDto>();

        Assert.NotNull(result);
        Assert.False(result!.Matched);
    }

    [Fact]
    public async Task Full_flow_link_then_change_then_admin_approve_pushes_to_erp()
    {
        using var app = NewApp();
        var client = FreshClient(app, "flow-user");

        // 1. Link by vendor number + PIN.
        var match = await (await client.PostAsJsonAsync("/api/v1/link-requests",
            new LinkRequestCreateDto("VendorNumberPin", "V-10485", "4820", null, null)))
            .Content.ReadFromJsonAsync<LinkMatchResultDto>();
        Assert.True(match!.Matched);
        Assert.Equal("V-10485", match.VendorNumber);
        Assert.Equal("Northstar Supply Co.", match.VendorName);

        // 2. Confirm → account becomes linked.
        var me = await (await client.PostAsync($"/api/v1/link-requests/{match.LinkRequestId}/confirm", null))
            .Content.ReadFromJsonAsync<MeDto>();
        Assert.Equal("Linked", me!.LinkState);
        Assert.Equal("V-10485", me.VendorNumber);

        // 3. Read the record — secrets are masked.
        var vendor = await client.GetFromJsonAsync<VendorDto>("/api/v1/vendor");
        Assert.Equal("Northstar Supply Co.", vendor!.LegalName);
        Assert.Contains("•", vendor.Tax.TinMasked!);
        Assert.Contains("•", vendor.Banking.AccountNumberMasked!);

        // 4. Submit a banking change.
        var created = await (await client.PostAsJsonAsync("/api/v1/change-requests",
            new ChangeRequestCreateDto("Banking & remittance", new[]
            {
                new ChangeDiffDto("BankName", "First Interstate Bank", "Rocky Mountain Bank"),
            }))).Content.ReadFromJsonAsync<ChangeRequestDto>();
        Assert.StartsWith("CR-", created!.Code);
        Assert.Equal("PendingReview", created.Status);

        // 5. It shows in the vendor's list.
        var mine = await client.GetFromJsonAsync<List<ChangeRequestDto>>("/api/v1/change-requests?mine=true");
        Assert.Contains(mine!, c => c.Code == created.Code);

        // 6. Admin approves → change applied and pushed to ERP.
        var admin = app.CreateClient();
        admin.DefaultRequestHeaders.Add("X-Dev-Role", "admin");
        var adminList = await admin.GetFromJsonAsync<List<ChangeRequestDto>>("/api/v1/admin/change-requests");
        Assert.Contains(adminList!, c => c.Id == created.Id);
        var approve = await admin.PostAsync($"/api/v1/admin/change-requests/{created.Id}/approve", null);
        Assert.Equal(HttpStatusCode.NoContent, approve.StatusCode);

        // 7. The vendor record now reflects the approved value.
        var updated = await client.GetFromJsonAsync<VendorDto>("/api/v1/vendor");
        Assert.Equal("Rocky Mountain Bank", updated!.Banking.BankName);
    }
}
