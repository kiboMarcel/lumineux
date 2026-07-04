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

/// <summary>
/// Tests unitaires du cas d'usage « demander la réinitialisation » (US1, T014). Vérifient la réponse
/// générique dans tous les cas (anti-énumération), l'émission/l'envoi uniquement pour un compte
/// éligible, l'opération factice anti-timing sinon, la non-persistance du jeton en clair, et la
/// robustesse à un échec d'envoi d'email (réponse toujours 200 générique — FR-011).
/// </summary>
public sealed class RequestPasswordResetHandlerTests
{
    private static readonly DateTime Now = new(2026, 7, 4, 9, 0, 0, DateTimeKind.Utc);

    private readonly IMemberAccountRepository _accounts = Substitute.For<IMemberAccountRepository>();
    private readonly IPasswordResetTokenRepository _tokens = Substitute.For<IPasswordResetTokenRepository>();
    private readonly IResetTokenService _tokenService = Substitute.For<IResetTokenService>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private RequestPasswordResetHandler CreateHandler()
    {
        _clock.UtcNow.Returns(Now);
        _tokenService.Generate().Returns(("clear-abc", "hash-xyz"));
        _email.SendPasswordResetAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmailSendOutcome.Sent);
        return new RequestPasswordResetHandler(_accounts, _tokens, _tokenService, _email, _clock, _audit,
            Options.Create(new AuthOptions()), new ForgotPasswordValidator());
    }

    private static MemberAccount EligibleAccount(string email = "jane@example.org", string status = "Active")
    {
        var member = Member.Create("LUM-2026-00001", Now, "Doe", "Jane", "F", 1);
        member.Email = email;
        member.Status = status;
        var account = MemberAccount.Provision(member, "stored-hash");
        account.ChangePassword("stored-hash");
        account.Activate();
        return account;
    }

    private static readonly ForgotPasswordRequest Request = new("LUM-2026-00001");

    [Fact]
    public async Task Eligible_account_issues_token_sends_email_and_returns_generic()
    {
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(EligibleAccount());

        var response = await CreateHandler().HandleAsync(Request);

        response.Message.Should().NotBeNullOrEmpty();
        // Seule l'empreinte est persistée — jamais le jeton en clair (FR-009).
        await _tokens.Received(1).AddAsync(
            Arg.Is<PasswordResetToken>(t => t.TokenHash == "hash-xyz"), Arg.Any<CancellationToken>());
        await _tokens.DidNotReceive().AddAsync(
            Arg.Is<PasswordResetToken>(t => t.TokenHash == "clear-abc"), Arg.Any<CancellationToken>());
        await _tokens.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _email.Received(1).SendPasswordResetAsync(
            "jane@example.org", Arg.Is<string>(l => l.Contains("clear-abc")), Arg.Any<CancellationToken>());
        _audit.Received().Operation("PasswordResetRequest", Arg.Any<object?>());
    }

    [Fact]
    public async Task Unknown_reference_returns_generic_without_email_and_runs_dummy_work()
    {
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((MemberAccount?)null);

        var response = await CreateHandler().HandleAsync(Request);

        response.Message.Should().NotBeNullOrEmpty();
        _tokenService.Received().Generate(); // opération factice anti-timing
        await _tokens.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _audit.Received().Refused("PasswordResetRequest", Arg.Any<string>());
    }

    [Fact]
    public async Task Account_without_email_returns_generic_without_sending()
    {
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EligibleAccount(email: null!));

        var response = await CreateHandler().HandleAsync(Request);

        response.Message.Should().NotBeNullOrEmpty();
        await _tokens.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Inactive_member_returns_generic_without_sending()
    {
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EligibleAccount(status: "Archived"));

        var response = await CreateHandler().HandleAsync(Request);

        response.Message.Should().NotBeNullOrEmpty();
        await _tokens.DidNotReceive().AddAsync(Arg.Any<PasswordResetToken>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceive().SendPasswordResetAsync(
            Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Email_send_failure_still_returns_generic_and_is_audited()
    {
        _accounts.GetByLoginIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(EligibleAccount());
        var handler = CreateHandler();
        _email.SendPasswordResetAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(EmailSendOutcome.Failed);

        var response = await handler.HandleAsync(Request);

        response.Message.Should().NotBeNullOrEmpty(); // toujours 200 générique (FR-011)
        _audit.Received().Refused("PasswordResetRequest", "Échec d'envoi de l'email", Arg.Any<object?>());
    }
}
