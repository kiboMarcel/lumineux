using FluentAssertions;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Xunit;

namespace Lumineux.Domain.Tests;

/// <summary>
/// Tests unitaires du domaine pour <see cref="PasswordResetToken"/> (feature 006, T013) :
/// invariants de la fabrique, utilisabilité (actif/expiré/consommé) et usage unique.
/// </summary>
public sealed class PasswordResetTokenTests
{
    private static readonly DateTime Now = new(2026, 7, 4, 9, 0, 0, DateTimeKind.Utc);

    private static MemberAccount AnAccount()
    {
        var member = Member.Create("LUM-2026-00001", Now, "Doe", "Jane", "F", 1);
        return MemberAccount.Provision(member, "stored-hash");
    }

    [Fact]
    public void Issue_valid_sets_expiry_and_leaves_unconsumed()
    {
        var token = PasswordResetToken.Issue(AnAccount(), "hash-abc", Now, 30);

        token.TokenHash.Should().Be("hash-abc");
        token.ExpiresAt.Should().Be(Now.AddMinutes(30));
        token.ConsumedAt.Should().BeNull();
        token.Account.Should().NotBeNull();
    }

    [Fact]
    public void Issue_null_account_throws()
    {
        var act = () => PasswordResetToken.Issue(null!, "hash-abc", Now, 30);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Issue_blank_hash_throws_domain(string? hash)
    {
        var act = () => PasswordResetToken.Issue(AnAccount(), hash!, Now, 30);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Issue_non_positive_lifetime_throws_domain(int minutes)
    {
        var act = () => PasswordResetToken.Issue(AnAccount(), "hash-abc", Now, minutes);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsUsable_active_token_is_true()
    {
        var token = PasswordResetToken.Issue(AnAccount(), "hash-abc", Now, 30);

        token.IsUsable(Now.AddMinutes(10)).Should().BeTrue();
    }

    [Fact]
    public void IsUsable_expired_token_is_false()
    {
        var token = PasswordResetToken.Issue(AnAccount(), "hash-abc", Now, 30);

        token.IsUsable(Now.AddMinutes(31)).Should().BeFalse();
    }

    [Fact]
    public void IsUsable_consumed_token_is_false()
    {
        var token = PasswordResetToken.Issue(AnAccount(), "hash-abc", Now, 30);
        token.Consume(Now.AddMinutes(5));

        token.IsUsable(Now.AddMinutes(10)).Should().BeFalse();
    }

    [Fact]
    public void Consume_marks_consumed_at()
    {
        var token = PasswordResetToken.Issue(AnAccount(), "hash-abc", Now, 30);

        token.Consume(Now.AddMinutes(5));

        token.ConsumedAt.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void Consume_twice_throws_domain()
    {
        var token = PasswordResetToken.Issue(AnAccount(), "hash-abc", Now, 30);
        token.Consume(Now.AddMinutes(5));

        var act = () => token.Consume(Now.AddMinutes(6));

        act.Should().Throw<DomainException>();
    }
}
