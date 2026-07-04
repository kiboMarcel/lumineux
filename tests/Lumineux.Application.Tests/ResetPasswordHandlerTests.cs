using FluentAssertions;
using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Auth;
using Lumineux.Application.Contracts.Auth;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Lumineux.Application.Tests;

/// <summary>
/// Tests unitaires du cas d'usage « réinitialiser avec le jeton » (US2, T021) : succès (empreinte
/// mise à jour, jeton consommé, compteurs remis à zéro, verrouillage levé), refus générique 401 pour
/// jeton rejoué/expiré/introuvable, et rejet 400 d'un mot de passe faible sans toucher au jeton.
/// </summary>
public sealed class ResetPasswordHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 4, 9, 0, 0, DateTimeKind.Utc);
    private const string ValidPassword = "NewPassw0rd";

    private readonly IPasswordResetTokenRepository _tokens = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IResetTokenService _tokenService = Substitute.For<IResetTokenService>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private ResetPasswordHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        _tokenService.Hash(Arg.Any<string>()).Returns("hash-xyz");
        _hasher.Hash(Arg.Any<string>()).Returns("new-hash");
        return new ResetPasswordHandler(_tokens, _tokenService, _hasher, _clock, _audit,
            new ResetPasswordValidator(Options.Create(new AuthOptions())));
    }

    private static MemberAccount ActiveAccount()
    {
        var member = Member.Create("LUM-2026-00001", Now, "Doe", "Jane", "F", 1);
        var account = MemberAccount.Provision(member, "old-hash");
        account.ChangePassword("old-hash");
        account.Activate();
        return account;
    }

    private static PasswordResetToken TokenFor(MemberAccount account, bool expired = false)
    {
        var issuedAt = expired ? Now.AddMinutes(-60) : Now;
        return PasswordResetToken.Issue(account, "hash-xyz", issuedAt, 30);
    }

    private static ResetPasswordRequest Request(string password = ValidPassword) => new("clear-token", password);

    [Fact]
    public async Task Valid_token_resets_password_consumes_token_and_clears_counters()
    {
        var account = ActiveAccount();
        var token = TokenFor(account);
        _tokens.GetByTokenHashAsync("hash-xyz", Arg.Any<CancellationToken>()).Returns(token);

        await CreateHandler().HandleAsync(Request());

        account.PasswordHash.Should().Be("new-hash");
        account.FailedAttempts.Should().Be(0);
        account.LockoutUntil.Should().BeNull();
        token.ConsumedAt.Should().Be(Now);
        await _tokens.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
        _audit.Received().Operation("PasswordReset", Arg.Any<object?>());
    }

    [Fact]
    public async Task Locked_account_is_unlocked_after_reset()
    {
        var account = ActiveAccount();
        for (var i = 0; i < 5; i++)
        {
            account.RegisterFailedLogin(Now, 5, TimeSpan.FromMinutes(15));
        }
        account.IsLockedOut(Now).Should().BeTrue();
        var token = TokenFor(account);
        _tokens.GetByTokenHashAsync("hash-xyz", Arg.Any<CancellationToken>()).Returns(token);

        await CreateHandler().HandleAsync(Request());

        account.IsLockedOut(Now).Should().BeFalse();
        account.LockoutUntil.Should().BeNull();
    }

    [Fact]
    public async Task Replayed_token_is_refused_with_unauthorized()
    {
        var account = ActiveAccount();
        var token = TokenFor(account);
        token.Consume(Now); // déjà consommé
        _tokens.GetByTokenHashAsync("hash-xyz", Arg.Any<CancellationToken>()).Returns(token);

        var act = () => CreateHandler().HandleAsync(Request());

        await act.Should().ThrowAsync<UnauthorizedException>();
        await _tokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Expired_token_is_refused_with_unauthorized()
    {
        var account = ActiveAccount();
        var token = TokenFor(account, expired: true);
        _tokens.GetByTokenHashAsync("hash-xyz", Arg.Any<CancellationToken>()).Returns(token);

        var act = () => CreateHandler().HandleAsync(Request());

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Unknown_token_is_refused_with_unauthorized()
    {
        _tokens.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((PasswordResetToken?)null);

        var act = () => CreateHandler().HandleAsync(Request());

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Weak_password_is_rejected_without_touching_the_token()
    {
        var act = () => CreateHandler().HandleAsync(Request(password: "weak"));

        await act.Should().ThrowAsync<ValidationException>();
        await _tokens.DidNotReceive().GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _tokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
