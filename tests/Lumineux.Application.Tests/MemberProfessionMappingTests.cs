using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Members;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>
/// Feature 030 — la profession du membre est restituée dans <see cref="Lumineux.Application.Contracts.Members.MemberResponse"/>
/// via le mapping, dans les deux cas (renseignée et absente). Le cas null couvre SC-004
/// (un membre sans profession se lit sans valeur fictive).
/// </summary>
public sealed class MemberProfessionMappingTests
{
    private static readonly DateTime Now = new(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc);

    private readonly IMemberRepository _members = Substitute.For<IMemberRepository>();
    private readonly IMemberAccountRepository _accounts = Substitute.For<IMemberAccountRepository>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    private static Member Existing(string? profession)
    {
        var m = Member.Create("LUM-2026-00007", Now, "Doe", "Jane", "F", 1);
        m.Id = 7;
        m.Profession = profession;
        return m;
    }

    private GetMemberHandler Handler()
    {
        _user.HasPermission(Permissions.ManageMembers).Returns(true);
        return new GetMemberHandler(_members, _accounts, _user);
    }

    [Fact]
    public async Task Response_exposes_profession_when_set()
    {
        _members.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Existing("Enseignant"));

        var result = await Handler().HandleAsync(7);

        result.Profession.Should().Be("Enseignant");
    }

    [Fact]
    public async Task Response_profession_is_null_when_absent()
    {
        _members.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(Existing(profession: null));

        var result = await Handler().HandleAsync(7);

        result.Profession.Should().BeNull();
    }
}
