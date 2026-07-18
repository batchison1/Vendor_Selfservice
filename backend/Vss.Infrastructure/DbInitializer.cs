using Microsoft.EntityFrameworkCore;
using Vss.Domain;

namespace Vss.Infrastructure;

/// <summary>Creates the schema (dev) and seeds demo data if the database is empty.</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(VssDbContext db, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        if (!await db.Vendors.AnyAsync(ct))
            db.Vendors.AddRange(SeedData.Vendors());

        if (!await db.VendorUsers.AnyAsync(ct))
            db.VendorUsers.Add(SeedData.DanaUser());

        await db.SaveChangesAsync(ct);
    }
}
