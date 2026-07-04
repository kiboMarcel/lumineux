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

public sealed class LoginTests
{
    private static readonly DateTime Now = new(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc);

    private readonly IMemberAccountRepository _accounts = Substitute.For<IMemberAccountRepository>();
    private readonly IMemberPermissionRepository _permissions = Substitute.For<IMemberPermissionRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenIssuer _tokenIssuer = Substitute.For<ITokenIssuer>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private LoginHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        _hasher.Hash(Arg.Any<string>()).Returns("dummy");
        _tokenIssuer.Issue(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new IssuedToken("access-token", Now.AddMinutes(60)));
        _permissions.GetPermissionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "manage_attendance" });
        return new LoginHandler(_accounts, _permissions, _hasher, _tokenIssuer, _clock, _audit,
            Options.Create(new AuthOptions()), new LoginValidator());
    }

    private static Member ActiveMember(string status = "Active")
    {
        var m = Member.Create("LUM-2026-00001", Now, "Doe", "Jane", "F", 1);
        m.Status = status;
        return m;
    }

    private static MemberAccount ActiveAccount(string status = "Active")
    {
        var account = MemberAccount.Provision(ActiveMember(status), "stored-hash");
        account.ChangePassword("stored-hash"); // mustChangePassword = false
        account.Activate();
        return account;
    }

    private static readonly LoginRequest Request = new("LUM-2026-00001", "Passw0rd");

    [Fact]
    public async Task Login_valid_returns_token()
    {
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ActiveAccount());
        _hasher.Verify(Request.Password, "stored-hash").Returns(true);

        var result = await CreateHandler().HandleAsync(Request);

        result.AccessToken.Should().Be("access-token");
        result.TokenType.Should().Be("Bearer");
        await _accounts.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_unknown_reference_throws_unauthorized()
    {
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((MemberAccount?)null);

        var act = () => CreateHandler().HandleAsync(Request);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _hasher.Received().Hash(Arg.Any<string>()); // hachage factice anti-énumération
    }

    [Fact]
    public async Task Login_wrong_password_registers_failure_and_throws_unauthorized()
    {
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ActiveAccount());
        _hasher.Verify(Request.Password, "stored-hash").Returns(false);

        var act = () => CreateHandler().HandleAsync(Request);

        await act.Should().ThrowAsync<UnauthorizedException>();
        await _accounts.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_when_must_change_password_throws_password_change_required()
    {
        var pending = MemberAccount.Provision(ActiveMember(), "stored-hash"); // mustChangePassword = true
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(pending);
        _hasher.Verify(Request.Password, "stored-hash").Returns(true);

        var act = () => CreateHandler().HandleAsync(Request);

        await act.Should().ThrowAsync<PasswordChangeRequiredException>();
    }

    [Fact]
    public async Task Login_inactive_member_throws_unauthorized()
    {
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(ActiveAccount(status: "Archived"));
        _hasher.Verify(Request.Password, "stored-hash").Returns(true);

        var act = () => CreateHandler().HandleAsync(Request);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_when_locked_out_throws_unauthorized()
    {
        var account = ActiveAccount();
        for (var i = 0; i < 5; i++)
        {
            account.RegisterFailedLogin(Now, 5, TimeSpan.FromMinutes(15));
        }
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(account);
        _hasher.Verify(Request.Password, "stored-hash").Returns(true);

        var act = () => CreateHandler().HandleAsync(Request);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
