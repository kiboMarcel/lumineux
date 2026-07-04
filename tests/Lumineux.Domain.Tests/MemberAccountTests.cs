using FluentAssertions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Xunit;

namespace Lumineux.Domain.Tests;

public sealed class MemberAccountTests
{
    private static readonly DateTime Now = new(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc);

    private static MemberAccount NewAccount()
    {
        var member = Member.Create("LUM-2026-00001", Now, "Doe", "Jane", "F", 1);
        return MemberAccount.Provision(member, "initial-hash");
    }

    [Fact]
    public void Provision_starts_pending_with_must_change()
    {
        var account = NewAccount();
        account.MustChangePassword.Should().BeTrue();
        account.ActivationState.Should().Be(AccountActivationState.PendingActivation);
        account.FailedAttempts.Should().Be(0);
    }

    [Fact]
    public void ChangePassword_updates_hash_and_clears_must_change()
    {
        var account = NewAccount();
        account.ChangePassword("new-hash");
        account.PasswordHash.Should().Be("new-hash");
        account.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public void Activate_sets_active_state()
    {
        var account = NewAccount();
        account.Activate();
        account.ActivationState.Should().Be(AccountActivationState.Active);
    }

    [Fact]
    public void RegisterSuccessfulLogin_resets_counters_and_sets_last_login()
    {
        var account = NewAccount();
        account.RegisterFailedLogin(Now, maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15));

        account.RegisterSuccessfulLogin(Now.AddMinutes(1));

        account.FailedAttempts.Should().Be(0);
        account.LockoutUntil.Should().BeNull();
        account.LastLoginAt.Should().Be(Now.AddMinutes(1));
    }
}
