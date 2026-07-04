using FluentAssertions;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Auth;
using Lumineux.Application.Contracts.Auth;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

public sealed class ActivateAccountTests
{
    private static readonly DateTime Now = new(2026, 7, 3, 9, 0, 0, DateTimeKind.Utc);

    private readonly IMemberAccountRepository _accounts = Substitute.For<IMemberAccountRepository>();
    private readonly IMemberPermissionRepository _permissions = Substitute.For<IMemberPermissionRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenIssuer _tokenIssuer = Substitute.For<ITokenIssuer>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private ActivateAccountHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        _hasher.Hash(Arg.Any<string>()).Returns("new-hash");
        _tokenIssuer.Issue(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new IssuedToken("access-token", Now.AddMinutes(60)));
        _permissions.GetPermissionsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<string>());
        return new ActivateAccountHandler(_accounts, _permissions, _hasher, _tokenIssuer, _clock, _audit,
            Options.Create(new AuthOptions()), new ActivateAccountValidator(Options.Create(new AuthOptions())));
    }

    private static Member NewMember()
    {
        var m = Member.Create("LUM-2026-00001", Now, "Doe", "Jane", "F", 1);
        return m;
    }

    private static readonly ActivateAccountRequest Request = new("LUM-2026-00001", "Temp1234", "NewPass1");

    [Fact]
    public async Task Activate_valid_activates_and_returns_token()
    {
        var account = MemberAccount.Provision(NewMember(), "temp-hash"); // pending
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(account);
        _hasher.Verify(Request.TemporaryPassword, "temp-hash").Returns(true);

        var result = await CreateHandler().HandleAsync(Request);

        result.AccessToken.Should().Be("access-token");
        account.ActivationState.Should().Be(AccountActivationState.Active);
        account.MustChangePassword.Should().BeFalse();
        await _accounts.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Activate_wrong_temporary_password_throws_unauthorized()
    {
        var account = MemberAccount.Provision(NewMember(), "temp-hash");
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(account);
        _hasher.Verify(Request.TemporaryPassword, "temp-hash").Returns(false);

        var act = () => CreateHandler().HandleAsync(Request);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Activate_already_active_after_valid_temp_throws_conflict()
    {
        var account = MemberAccount.Provision(NewMember(), "temp-hash");
        account.ChangePassword("temp-hash"); // mustChangePassword = false
        account.Activate();
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(account);
        _hasher.Verify(Request.TemporaryPassword, "temp-hash").Returns(true);

        var act = () => CreateHandler().HandleAsync(Request);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Activate_with_weak_new_password_fails_validation()
    {
        var act = () => CreateHandler().HandleAsync(Request with { NewPassword = "weak" });

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}
