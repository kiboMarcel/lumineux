using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Auth;
using Lumineux.Domain.Abstractions;
using Microsoft.Extensions.Options;

namespace Lumineux.Application.Auth;

/// <summary>
/// Cas d'usage : changement de mot de passe par un utilisateur connecté (US3, FR-009/010).
/// Vérifie le mot de passe actuel, applique la politique, remplace l'empreinte. Aucun jeton renvoyé
/// (le jeton courant reste valide jusqu'à expiration).
/// </summary>
public sealed class ChangePasswordHandler
{
    private const string GenericFailure = "Identifiants invalides.";

    private readonly IMemberAccountRepository _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _user;
    private readonly AuthOptions _options;
    private readonly IValidator<ChangePasswordRequest> _validator;

    public ChangePasswordHandler(
        IMemberAccountRepository accounts,
        IPasswordHasher passwordHasher,
        IClock clock,
        IAuditLogger audit,
        ICurrentUser user,
        IOptions<AuthOptions> options,
        IValidator<ChangePasswordRequest> validator)
    {
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _audit = audit;
        _user = user;
        _options = options.Value;
        _validator = validator;
    }

    public async Task HandleAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        var memberId = _user.MemberId;
        if (memberId is null)
        {
            _audit.Refused("ChangePassword", "Contexte utilisateur absent");
            throw new UnauthorizedException(GenericFailure);
        }

        var account = await _accounts.GetByMemberIdForUpdateAsync(memberId.Value, ct);
        if (account is null)
        {
            _audit.Refused("ChangePassword", "Compte introuvable", new { memberId });
            throw new UnauthorizedException(GenericFailure);
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, account.PasswordHash))
        {
            _audit.Refused("ChangePassword", "Mot de passe actuel erroné", new { account.MemberId });
            throw new UnauthorizedException(GenericFailure);
        }

        account.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        account.RegisterSuccessfulLogin(_clock.UtcNow);
        await _accounts.SaveChangesAsync(ct);

        _audit.Operation("ChangePassword", new { account.MemberId });
    }
}
