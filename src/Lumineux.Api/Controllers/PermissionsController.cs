using Lumineux.Application.BureauProfiles;
using Lumineux.Domain.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

[ApiController]
[Route("api/v1/permissions")]
[Authorize]
public sealed class PermissionsController : ControllerBase
{
    private readonly ListPermissionsHandler _list;

    public PermissionsController(ListPermissionsHandler list) => _list = list;

    /// <summary>Référentiel figé des droits fonctionnels (US4, FR-008).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDescriptor>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IReadOnlyList<PermissionDescriptor>> List() => Ok(_list.Handle());
}
