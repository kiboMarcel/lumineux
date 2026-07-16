using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Members;
using Lumineux.Application.Members;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class MemberQueryAndUpdateTests
{
    private static readonly DateTime Now = new(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc);

    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IMemberAccountRepository _accounts = Substitute.For<IMemberAccountRepository>();
    private readonly IReferenceLookupRepository _lookup = Substitute.For<IReferenceLookupRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private void GivenManager() => _user.HasPermission(Permissions.ManageMembers).Returns(true);

    private static Member Existing(int id = 7)
    {
        var m = Member.Create("LUM-2026-00007", Now, "Doe", "Jane", "F", 1);
        m.Id = id;
        m.Email = "jane@example.com";
        return m;
    }

    private static UpdateMemberRequest UpdateRequest => new(
        "Doe", "Jane", "F", Mobile: null, Email: "jane@example.com", AntennaId: 1,
        CivilityId: null, BirthDate: null, BirthPlaceId: null, BirthCityId: null,
        Address: "Nouvelle adresse", DistrictId: null, NationalityId: null, IntroducerId: null);

    // ---- Search ----

    [Fact]
    public async Task Search_returns_paged_items()
    {
        GivenManager();
        _members.SearchAsync("Doe", 1, 20, Arg.Any<CancellationToken>())
            .Returns(new MemberPage(new List<Member> { Existing() }, Total: 1, Page: 1, PageSize: 20));

        var handler = new SearchMembersHandler(_members, _user);
        var result = await handler.HandleAsync("Doe", 1, 20);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Reference.Should().Be("LUM-2026-00007");
    }

    [Fact]
    public async Task Search_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageMembers).Returns(false);
        var handler = new SearchMembersHandler(_members, _user);

        var act = () => handler.HandleAsync(null, 1, 20);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ---- Get ----

    [Fact]
    public async Task Get_returns_member()
    {
        GivenManager();
        _members.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Existing());

        var handler = new GetMemberHandler(_members, _accounts, _user);
        var result = await handler.HandleAsync(7);

        result.Reference.Should().Be("LUM-2026-00007");
    }

    [Fact]
    public async Task Get_unknown_member_throws_not_found()
    {
        GivenManager();
        _members.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Member?)null);

        var handler = new GetMemberHandler(_members, _accounts, _user);
        var act = () => handler.HandleAsync(7);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---- Update ----

    private UpdateMemberHandler UpdateHandler() =>
        new(_members, _accounts, _lookup, _user, _audit, new UpdateMemberValidator());

    [Fact]
    public async Task Update_applies_changes_and_saves()
    {
        GivenManager();
        _members.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Existing());
        _lookup.AntennaExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _members.IsContactUsedByActiveAsync(Arg.Any<string>(), Arg.Any<string>(), 7, Arg.Any<CancellationToken>()).Returns(false);

        var result = await UpdateHandler().HandleAsync(7, UpdateRequest);

        result.Address.Should().Be("Nouvelle adresse");
        await _members.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_unknown_member_throws_not_found()
    {
        GivenManager();
        _members.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Member?)null);

        var act = () => UpdateHandler().HandleAsync(7, UpdateRequest);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Update_with_contact_used_by_other_active_throws_conflict()
    {
        GivenManager();
        _members.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Existing());
        _lookup.AntennaExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _members.IsContactUsedByActiveAsync(Arg.Any<string>(), Arg.Any<string>(), 7, Arg.Any<CancellationToken>()).Returns(true);

        var act = () => UpdateHandler().HandleAsync(7, UpdateRequest);

        var ex = await act.Should().ThrowAsync<DuplicateMemberException>();
        ex.Which.Code.Should().Be("contact_in_use");
    }

    [Fact]
    public async Task Update_without_permission_throws_forbidden()
    {
        _user.HasPermission(Permissions.ManageMembers).Returns(false);

        var act = () => UpdateHandler().HandleAsync(7, UpdateRequest);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ---- Feature 030 : profession ----

    private void GivenUpdatableMember(Member member)
    {
        GivenManager();
        _members.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(member);
        _lookup.AntennaExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _members.IsContactUsedByActiveAsync(Arg.Any<string>(), Arg.Any<string>(), 7, Arg.Any<CancellationToken>()).Returns(false);
    }

    [Fact]
    public async Task Update_adds_profession_when_absent()
    {
        var member = Existing();
        member.Profession = null;
        GivenUpdatableMember(member);

        var result = await UpdateHandler().HandleAsync(7, UpdateRequest with { Profession = "  Infirmier  " });

        result.Profession.Should().Be("Infirmier"); // ajouté + trim
    }

    [Fact]
    public async Task Update_replaces_existing_profession()
    {
        var member = Existing();
        member.Profession = "Infirmier";
        GivenUpdatableMember(member);

        var result = await UpdateHandler().HandleAsync(7, UpdateRequest with { Profession = "Cadre" });

        result.Profession.Should().Be("Cadre");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Update_clears_profession_when_blank(string? input)
    {
        var member = Existing();
        member.Profession = "Infirmier";
        GivenUpdatableMember(member);

        var result = await UpdateHandler().HandleAsync(7, UpdateRequest with { Profession = input });

        result.Profession.Should().BeNull();
    }

    [Fact]
    public async Task Update_accepts_profession_at_max_length()
    {
        GivenUpdatableMember(Existing());

        var act = () => UpdateHandler().HandleAsync(7, UpdateRequest with { Profession = new string('a', 150) });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Update_rejects_profession_over_max_length()
    {
        GivenUpdatableMember(Existing());

        var act = () => UpdateHandler().HandleAsync(7, UpdateRequest with { Profession = new string('a', 151) });

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
