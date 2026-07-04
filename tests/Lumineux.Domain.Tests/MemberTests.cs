using FluentAssertions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Xunit;

namespace Lumineux.Domain.Tests;

public sealed class MemberTests
{
    private static readonly DateTime Now = new(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_sets_reference_entry_date_and_active_status()
    {
        var member = Member.Create("LUM-2026-00001", Now, "Doe", "Jane", "F", antennaId: 1);

        member.Reference.Should().Be("LUM-2026-00001");
        member.EntryDate.Should().Be(Now);
        member.Status.Should().Be(MemberStatuses.Active);
        member.IsActive.Should().BeTrue();
        member.Gender.Should().Be("F");
    }

    [Theory]
    [InlineData("X")]
    [InlineData("")]
    public void Create_with_invalid_gender_throws(string gender)
    {
        var act = () => Member.Create("LUM-2026-00001", Now, "Doe", "Jane", gender, 1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_with_empty_reference_throws()
    {
        var act = () => Member.Create("  ", Now, "Doe", "Jane", "F", 1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_with_invalid_antenna_throws()
    {
        var act = () => Member.Create("LUM-2026-00001", Now, "Doe", "Jane", "F", 0);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Provision_account_sets_login_and_must_change_password()
    {
        var member = Member.Create("LUM-2026-00007", Now, "Doe", "Jane", "F", 1);

        var account = MemberAccount.Provision(member, passwordHash: "hashed-secret");

        account.LoginId.Should().Be("LUM-2026-00007");
        account.PasswordHash.Should().Be("hashed-secret");
        account.MustChangePassword.Should().BeTrue();
        account.ActivationState.Should().Be(AccountActivationState.PendingActivation);
    }

    [Fact]
    public void Provision_with_empty_hash_throws()
    {
        var member = Member.Create("LUM-2026-00007", Now, "Doe", "Jane", "F", 1);
        var act = () => MemberAccount.Provision(member, "  ");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_with_nullable_antenna_null_creates_member_without_antenna()
    {
        var member = Member.Create("LUM-2026-00042", Now, "Doe", "Jane", "F", antennaId: (int?)null);

        member.AntennaId.Should().BeNull();
        member.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_with_nullable_antenna_value_creates_member_with_antenna()
    {
        var member = Member.Create("LUM-2026-00043", Now, "Doe", "Jane", "F", antennaId: (int?)7);

        member.AntennaId.Should().Be(7);
    }

    [Fact]
    public void Create_with_nullable_antenna_zero_throws()
    {
        var act = () => Member.Create("LUM-2026-00044", Now, "Doe", "Jane", "F", antennaId: (int?)0);
        act.Should().Throw<DomainException>();
    }
}
