using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vss.Api.Auth;
using Vss.Api.Contracts;
using Vss.Api.Services;
using Vss.Infrastructure;

namespace Vss.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController(VssDbContext db, CurrentUser current) : ControllerBase
{
    /// <summary>Current user + link state (+ vendor summary once linked).</summary>
    [HttpGet]
    public async Task<ActionResult<MeDto>> Get(CancellationToken ct)
    {
        var user = await current.GetOrProvisionAsync(ct);
        return await PortalQueries.BuildMeAsync(db, current.Role, user, ct);
    }
}
