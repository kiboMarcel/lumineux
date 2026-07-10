using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Auth;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Lumineux.Application.Auth;

/// <summary>
/// Cas d'usage : première connexion — changement du mot de passe temporaire et activation du compte
/// (US2, FR-007/008). Délivre un jeton d'accès. Anti-énumération : le mot de passe temporaire est
/// vérifié avant toute divulgation (F2).
/// </summary>
public sealed class ActivateAccountHandler
{
    private const string GenericFailure = "Identifiants invalides.";

    private readonly IMemberAccountRepository _accounts;
    private readonly IEffectivePermissionsReader _permissions;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly AuthOptions _options;
    private readonly IValidator<ActivateAccountRequest> _validator;

    public ActivateAccountHandler(
        IMemberAccountRepository accounts,
        IEffectivePermissionsReader permissions,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IClock clock,
        IAuditLogger audit,
        IOptions<AuthOptions> options,
        IValidator<ActivateAccountRequest> validator)
    {
        _accounts = accounts;
        _permissions = permissions;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _clock = clock;
        _audit = audit;
        _options = options.Value;
        _validator = validator;
    }

    public async Task<TokenResponse> HandleAsync(ActivateAccountRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        var now = _clock.UtcNow;
        var reference = request.Reference.Trim();
        var account = await _accounts.GetByLoginIdAsync(reference, ct);

        if (account is null)
        {
            _ = _passwordHasher.Hash(request.TemporaryPassword);
            _audit.Refused("Activate", "Identifiants invalides");
            throw new UnauthorizedException(GenericFailure);
        }

        if (account.IsLockedOut(now))
        {
            _audit.Refused("Activate", "Compte verrouillé", new { account.MemberId });
            throw new UnauthorizedException(GenericFailure);
        }

        if (!_passwordHasher.Verify(request.TemporaryPassword, account.PasswordHash))
        {
            account.RegisterFailedLogin(now, _options.MaxFailedAttempts, TimeSpan.FromMinutes(_options.LockoutMinutes));
            await _accounts.SaveChangesAsync(ct);
            var reason = account.IsLockedOut(now) ? "Verrouillage déclenché" : "Mot de passe temporaire erroné";
            _audit.Refused("Activate", reason, new { account.MemberId });
            throw new UnauthorizedException(GenericFailure);
        }

        // Mot de passe temporaire correct : au-delà, on peut révéler l'état d'activation (F2).
        if (account.ActivationState == AccountActivationState.Active && !account.MustChangePassword)
        {
            throw new ConflictException("Ce compte est déjà activé.");
        }

        account.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        account.Activate();
        account.RegisterSuccessfulLogin(now);
        await _accounts.SaveChangesAsync(ct);

        var permissions = await _permissions.GetEffectivePermissionsAsync(account.MemberId, ct);
        var fullName = account.Member?.FullName ?? reference;
        var token = _tokenIssuer.Issue(account.MemberId, fullName, permissions);

        _audit.Operation("Activate", new { account.MemberId });
        return new TokenResponse(token.AccessToken, "Bearer", token.ExpiresAt);
    }
}
