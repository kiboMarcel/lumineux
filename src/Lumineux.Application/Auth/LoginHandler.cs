using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Auth;
using Lumineux.Domain.Abstractions;
using Microsoft.Extensions.Options;

namespace Lumineux.Application.Auth;

/// <summary>
/// Cas d'usage : connexion et émission d'un jeton d'accès (US1, FR-001..006, FR-011/012).
/// Messages génériques (anti-énumération), verrouillage temporaire, jeton porteur des droits.
/// </summary>
public sealed class LoginHandler
{
    private const string GenericFailure = "Identifiants invalides.";

    private readonly IMemberAccountRepository _accounts;
    private readonly IEffectivePermissionsReader _permissions;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IClock _clock;
    private readonly IAuditLogger _audit;
    private readonly AuthOptions _options;
    private readonly IValidator<LoginRequest> _validator;

    public LoginHandler(
        IMemberAccountRepository accounts,
        IEffectivePermissionsReader permissions,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IClock clock,
        IAuditLogger audit,
        IOptions<AuthOptions> options,
        IValidator<LoginRequest> validator)
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

    public async Task<TokenResponse> HandleAsync(LoginRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        var now = _clock.UtcNow;
        var reference = request.Reference.Trim();
        var account = await _accounts.GetByLoginIdAsync(reference, ct);

        if (account is null)
        {
            // Égalise le coût de calcul pour éviter un canal temporel (anti-énumération).
            _ = _passwordHasher.Hash(request.Password);
            _audit.Refused("Login", "Identifiants invalides");
            throw new UnauthorizedException(GenericFailure);
        }

        if (account.IsLockedOut(now))
        {
            _audit.Refused("Login", "Compte verrouillé", new { account.MemberId });
            throw new UnauthorizedException(GenericFailure);
        }

        if (!_passwordHasher.Verify(request.Password, account.PasswordHash))
        {
            account.RegisterFailedLogin(now, _options.MaxFailedAttempts, TimeSpan.FromMinutes(_options.LockoutMinutes));
            await _accounts.SaveChangesAsync(ct);
            var reason = account.IsLockedOut(now) ? "Verrouillage déclenché" : "Mot de passe erroné";
            _audit.Refused("Login", reason, new { account.MemberId });
            throw new UnauthorizedException(GenericFailure);
        }

        // Mot de passe correct : réinitialise le compteur d'échecs.
        account.RegisterSuccessfulLogin(now);
        await _accounts.SaveChangesAsync(ct);

        if (account.MustChangePassword)
        {
            _audit.Refused("Login", "Changement de mot de passe requis", new { account.MemberId });
            throw new PasswordChangeRequiredException("Changement de mot de passe requis avant connexion.");
        }

        if (account.Member is null || !account.Member.IsActive)
        {
            _audit.Refused("Login", "Membre non actif", new { account.MemberId });
            throw new UnauthorizedException(GenericFailure);
        }

        var permissions = await _permissions.GetEffectivePermissionsAsync(account.MemberId, ct);
        var token = _tokenIssuer.Issue(account.MemberId, account.Member.FullName, permissions);

        _audit.Operation("Login", new { account.MemberId });
        return new TokenResponse(token.AccessToken, "Bearer", token.ExpiresAt);
    }
}
