using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Members;
using Lumineux.Application.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lumineux.Api.Controllers;

[ApiController]
[Route("api/v1/members")]
[Authorize(Policy = Permissions.ManageMembers)]
public sealed class MembersController : ControllerBase
{
    private readonly CreateMemberHandler _createMember;
    private readonly SearchMembersHandler _searchMembers;
    private readonly GetMemberHandler _getMember;
    private readonly UpdateMemberHandler _updateMember;

    public MembersController(
        CreateMemberHandler createMember,
        SearchMembersHandler searchMembers,
        GetMemberHandler getMember,
        UpdateMemberHandler updateMember)
    {
        _createMember = createMember;
        _searchMembers = searchMembers;
        _getMember = getMember;
        _updateMember = updateMember;
    }

    /// <summary>Crée un nouveau membre et provisionne son compte (FR-001).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MemberCreatedResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateMemberRequest request, CancellationToken ct)
    {
        var result = await _createMember.HandleAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { memberId = result.Member.Id }, result);
    }

    /// <summary>Recherche / liste paginée des membres (FR-013).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(MemberListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberListResponse>> Search(
        [FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _searchMembers.HandleAsync(query, page, pageSize, ct));

    /// <summary>Consulte la fiche d'un membre.</summary>
    [HttpGet("{memberId:int}")]
    [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberResponse>> Get(int memberId, CancellationToken ct) =>
        Ok(await _getMember.HandleAsync(memberId, ct));

    /// <summary>Corrige les informations d'un membre (FR-014).</summary>
    [HttpPut("{memberId:int}")]
    [ProducesResponseType(typeof(MemberResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MemberResponse>> Update(
        int memberId, [FromBody] UpdateMemberRequest request, CancellationToken ct) =>
        Ok(await _updateMember.HandleAsync(memberId, request, ct));
}
