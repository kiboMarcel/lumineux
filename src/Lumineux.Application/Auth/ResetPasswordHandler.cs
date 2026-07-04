using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Auth;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Auth;

/// <summary>
/// Cas d'usage : réinitialiser son mot de passe avec le jeton reçu par email (US2, FR-005..008).
/// La politique de mot de passe est validée EN PREMIER : un mot de passe non conforme échoue en 400
/// sans consulter ni consommer le jeton (FR-006). Un jeton inexistant/expiré/consommé donne un refus
/// générique 401 indistinct (FR-008, SC-003). Le succès met à jour l'empreinte, consomme le jeton,
/// remet à zéro les compteurs d'échec et lève tout verrouillage (FR-007, SC-007).
/// </summary>
public sealed class ResetPasswordHandler
{
    private const string GenericFailure = "Lien de réinitialisation invalide ou expiré.";

    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IResetTokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly IValidator<ResetPasswordRequest> _validator;

    public ResetPasswordHandler(
        IPasswordResetTokenRepository tokens,
        IResetTokenService tokenService,
        IPasswordHasher passwordHasher,
        IClock clock,
        IAuditLogger audit,
        IValidator<ResetPasswordRequest> validator)
    {
        _tokens = tokens;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _audit = audit;
        _validator = validator;
    }

    public async Task HandleAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        // Politique d'abord : un mot de passe non conforme → 400 SANS toucher au jeton (FR-006).
        await _validator.ValidateAndThrowAsync(request, ct);

        var now = _clock.UtcNow;
        var tokenHash = _tokenService.Hash(request.Token);
        var token = await _tokens.GetByTokenHashAsync(tokenHash, ct);

        if (token is null || !token.IsUsable(now))
        {
            _audit.Refused("PasswordReset", "Jeton invalide, expiré ou consommé");
            throw new UnauthorizedException(GenericFailure);
        }

        var account = token.Account;
        account.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        account.RegisterSuccessfulLogin(now); // reset compteurs + levée du verrouillage (FR-007c)
        token.Consume(now);
        await _tokens.SaveChangesAsync(ct);

        _audit.Operation("PasswordReset", new { account.MemberId });
    }
}
