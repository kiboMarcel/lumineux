using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Auth;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Lumineux.Application.Auth;

/// <summary>
/// Cas d'usage : demander la réinitialisation de son mot de passe (US1, FR-001..004, FR-011/012).
/// Réponse TOUJOURS générique (anti-énumération, SC-002). Émet un jeton usage unique seulement pour
/// un compte actif disposant d'un email ; sinon exécute une opération factice pour égaliser le coût
/// de calcul (anti-timing, même stratégie que <see cref="LoginHandler"/>).
/// </summary>
public sealed class RequestPasswordResetHandler
{
    private const string GenericMessage =
        "Si un compte correspond à cette référence et qu'un email est enregistré, un lien de " +
        "réinitialisation vient d'être envoyé.";

    private readonly IMemberAccountRepository _accounts;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IResetTokenService _tokenService;
    private readonly IEmailSender _email;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly AuthOptions _options;
    private readonly IValidator<ForgotPasswordRequest> _validator;

    public RequestPasswordResetHandler(
        IMemberAccountRepository accounts,
        IPasswordResetTokenRepository tokens,
        IResetTokenService tokenService,
        IEmailSender email,
        IClock clock,
        IAuditLogger audit,
        IOptions<AuthOptions> options,
        IValidator<ForgotPasswordRequest> validator)
    {
        _accounts = accounts;
        _tokens = tokens;
        _tokenService = tokenService;
        _email = email;
        _clock = clock;
        _audit = audit;
        _options = options.Value;
        _validator = validator;
    }

    public async Task<GenericMessageResponse> HandleAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        var now = _clock.UtcNow;
        var reference = request.Reference.Trim();
        var account = await _accounts.GetByLoginIdAsync(reference, ct);

        var eligible = account is { Member: { } member }
            && member.IsActive
            && !string.IsNullOrWhiteSpace(member.Email);

        if (!eligible)
        {
            // Anti-timing : opération factice pour égaliser le coût de calcul avec le chemin nominal
            // (couvre le calcul, pas l'I/O BD/email — limite résiduelle assumée, cf. research.md §3).
            _ = _tokenService.Generate();
            _audit.Refused("PasswordResetRequest", "Compte non éligible");
            return new GenericMessageResponse(GenericMessage);
        }

        var (clearToken, tokenHash) = _tokenService.Generate();
        var token = PasswordResetToken.Issue(account!, tokenHash, now, _options.PasswordResetMinutes);
        await _tokens.AddAsync(token, ct);
        await _tokens.SaveChangesAsync(ct);

        var link = BuildLink(clearToken);
        var outcome = await _email.SendPasswordResetAsync(account.Member!.Email, link, ct);

        if (outcome == EmailSendOutcome.Failed)
        {
            // L'échec d'envoi n'altère pas la réponse (générique, FR-011) ; il est journalisé sans lien.
            _audit.Refused("PasswordResetRequest", "Échec d'envoi de l'email", new { account.MemberId });
        }
        else
        {
            _audit.Operation("PasswordResetRequest", new { account.MemberId });
        }

        return new GenericMessageResponse(GenericMessage);
    }

    private string BuildLink(string clearToken)
    {
        var baseUrl = (_options.PasswordResetUrlBase ?? string.Empty).TrimEnd('/');
        var separator = baseUrl.Contains('?') ? '&' : '?';
        return $"{baseUrl}{separator}token={Uri.EscapeDataString(clearToken)}";
    }
}
