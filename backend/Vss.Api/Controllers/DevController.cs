using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vss.Infrastructure;

namespace Vss.Api.Controllers;

/// <summary>
/// Dev-only utilities. All endpoints 404 unless <c>Auth:Mode=Dev</c>, so they are
/// inert on the Univerus network.
/// </summary>
[ApiController]
[Route("api/v1/dev")]
[AllowAnonymous]
public class DevController(VssDbContext db, IConfiguration config) : ControllerBase
{
    private bool IsDev => (config["Auth:Mode"] ?? "Dev").Equals("Dev", StringComparison.OrdinalIgnoreCase);

    /// <summary>Wipe all data and restore the canonical Northstar demo seed.</summary>
    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        if (!IsDev) return NotFound();
        await DbInitializer.ReseedAsync(db, ct);
        return Ok(new { reset = true });
    }
}
