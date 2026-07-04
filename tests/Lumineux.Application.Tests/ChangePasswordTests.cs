using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Auth;
using Lumineux.Application.Contracts.Auth;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class ChangePasswordTests
{
    private static readonly DateTime Now = new(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc);

    private readonly IMemberAccountRepository _accounts = Substitute.For<IMemberAccountRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();

    private ChangePasswordHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        _hasher.Hash(Arg.Any<string>()).Returns(ci => "hash:" + ci.Arg<string>());
        _user.MemberId.Returns(42);
        _user.IsAuthenticated.Returns(true);
        return new ChangePasswordHandler(_accounts, _hasher, _clock, _audit, _user,
            Options.Create(new AuthOptions()), new ChangePasswordValidator(Options.Create(new AuthOptions())));
    }

    private static MemberAccount ActiveAccount(string storedHash = "hash:Current1")
    {
        var member = Member.Create("LUM-2026-00042", Now, "Doe", "Jane", "F", 1);
        member.Status = "Active";
        var account = MemberAccount.Provision(member, storedHash);
        account.ChangePassword(storedHash);
        account.Activate();
        return account;
    }

    private static readonly ChangePasswordRequest Request = new("Current1", "NewPass1");

    [Fact]
    public async Task ChangePassword_valid_updates_hash_and_persists()
    {
        var account = ActiveAccount();
        _accounts.GetByMemberIdForUpdateAsync(42, Arg.Any<CancellationToken>()).Returns(account);
        _hasher.Verify(Request.CurrentPassword, "hash:Current1").Returns(true);

        await CreateHandler().HandleAsync(Request);

        account.PasswordHash.Should().Be("hash:NewPass1");
        await _accounts.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangePassword_wrong_current_throws_unauthorized()
    {
        var account = ActiveAccount();
        _accounts.GetByMemberIdForUpdateAsync(42, Arg.Any<CancellationToken>()).Returns(account);
        _hasher.Verify(Request.CurrentPassword, "hash:Current1").Returns(false);

        var act = () => CreateHandler().HandleAsync(Request);

        await act.Should().ThrowAsync<UnauthorizedException>();
        account.PasswordHash.Should().Be("hash:Current1"); // inchangé
    }

    [Fact]
    public async Task ChangePassword_weak_new_password_fails_validation()
    {
        var act = () => CreateHandler().HandleAsync(Request with { NewPassword = "weak" });

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task ChangePassword_when_not_authenticated_throws_unauthorized()
    {
        _user.MemberId.Returns((int?)null);
        _user.IsAuthenticated.Returns(false);

        var act = () => CreateHandler().HandleAsync(Request);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task ChangePassword_when_account_missing_throws_unauthorized()
    {
        _accounts.GetByMemberIdForUpdateAsync(42, Arg.Any<CancellationToken>()).Returns((MemberAccount?)null);

        var act = () => CreateHandler().HandleAsync(Request);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
