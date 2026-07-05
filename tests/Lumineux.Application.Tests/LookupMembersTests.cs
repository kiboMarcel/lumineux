using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Members;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>
/// Tests unitaires de la recherche membre allégée (feature 015) : accès any-of, terme requis,
/// projection minimale, plafonnement.
/// </summary>
public sealed class LookupMembersTests
{
    private static readonly DateTime Now = new(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc);

    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    private LookupMembersHandler CreateHandler() => new(_members, _user);

    private static Member AMember()
    {
        var m = Member.Create("LUM-2026-00042", Now, "Doe", "Jane", "F", 1);
        m.Id = 42;
        m.Email = "jane@example.org"; // ne doit PAS ressortir dans la projection
        return m;
    }

    [Fact]
    public async Task Refuses_without_attendance_or_members_permission()
    {
        _user.HasPermission(Arg.Any<string>()).Returns(false);

        var act = () => CreateHandler().HandleAsync("Doe");

        await act.Should().ThrowAsync<ForbiddenException>();
        await _members.DidNotReceive().SearchAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_empty_search_term()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);

        var act = () => CreateHandler().HandleAsync("   ");

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Projects_minimal_fields_and_caps_page_size()
    {
        _user.HasPermission(Permissions.ManageAttendance).Returns(true);
        _members.SearchAsync("Doe", 1, 20, Arg.Any<CancellationToken>())
            .Returns(new MemberPage(new List<Member> { AMember() }, 1, 1, 20));

        var result = await CreateHandler().HandleAsync("Doe");

        result.Should().ContainSingle();
        result[0].Should().BeEquivalentTo(new { Id = 42, Reference = "LUM-2026-00042", FullName = "Jane Doe", Status = "Active" });
        // Réutilise la recherche existante avec page 1 et taille plafonnée (20).
        await _members.Received(1).SearchAsync("Doe", 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Allowed_for_members_manager_too()
    {
        _user.HasPermission(Permissions.ManageMembers).Returns(true);
        _members.SearchAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new MemberPage(new List<Member>(), 0, 1, 20));

        var result = await CreateHandler().HandleAsync("Doe");

        result.Should().BeEmpty();
    }
}
