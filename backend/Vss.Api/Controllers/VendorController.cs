using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vss.Api.Auth;
using Vss.Api.Contracts;
using Vss.Api.Mapping;
using Vss.Infrastructure;

namespace Vss.Api.Controllers;

[ApiController]
[Route("api/v1/vendor")]
[Authorize]
public class VendorController(VssDbContext db, CurrentUser current) : ControllerBase
{
    /// <summary>The current user's linked vendor record (all sections, secrets masked).</summary>
    [HttpGet]
    public async Task<ActionResult<VendorDto>> Get(CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);
        if (user.VendorId is null)
            return StatusCode(StatusCodes.Status403Forbidden, "Account is not linked to a vendor record yet.");

        var v = await db.Vendors.Include(x => x.Documents).Include(x => x.CategoryCodes)
            .FirstOrDefaultAsync(x => x.Id == user.VendorId, ct);
        return v is null ? NotFound() : VendorMapping.ToDto(v);
    }
}
