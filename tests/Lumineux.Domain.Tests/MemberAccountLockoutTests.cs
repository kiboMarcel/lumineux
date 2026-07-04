using FluentAssertions;
using Lumineux.Domain.Entities;
using Xunit;

namespace Lumineux.Domain.Tests;

public sealed class MemberAccountLockoutTests
{
    private static readonly DateTime Now = new(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Lockout = TimeSpan.FromMinutes(15);

    private static MemberAccount NewAccount()
    {
        var member = Member.Create("LUM-2026-00001", Now, "Doe", "Jane", "F", 1);
        return MemberAccount.Provision(member, "initial-hash");
    }

    [Fact]
    public void Below_threshold_is_not_locked()
    {
        var account = NewAccount();
        for (var i = 0; i < 4; i++)
        {
            account.RegisterFailedLogin(Now, maxAttempts: 5, Lockout);
        }

        account.IsLockedOut(Now).Should().BeFalse();
    }

    [Fact]
    public void At_threshold_locks_for_duration()
    {
        var account = NewAccount();
        for (var i = 0; i < 5; i++)
        {
            account.RegisterFailedLogin(Now, maxAttempts: 5, Lockout);
        }

        account.IsLockedOut(Now).Should().BeTrue();
        account.IsLockedOut(Now.Add(Lockout).AddSeconds(1)).Should().BeFalse(); // expiré
    }

    [Fact]
    public void Successful_login_clears_lockout()
    {
        var account = NewAccount();
        for (var i = 0; i < 5; i++)
        {
            account.RegisterFailedLogin(Now, maxAttempts: 5, Lockout);
        }

        account.RegisterSuccessfulLogin(Now);

        account.IsLockedOut(Now).Should().BeFalse();
        account.FailedAttempts.Should().Be(0);
    }
}
