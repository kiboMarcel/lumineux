using FluentValidation;
using Lumineux.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Lumineux.Application.Auth;

/// <summary>Règle de validation partagée pour la politique de mot de passe (FR-010).</summary>
internal static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> ApplyPolicy<T>(
        this IRuleBuilder<T, string> rule, IOptions<AuthOptions> options)
    {
        var min = options.Value.PasswordMinLength;
        return rule
            .NotEmpty()
            .MinimumLength(min).WithMessage($"Le mot de passe doit contenir au moins {min} caractères.")
            .Matches("[A-Za-z]").WithMessage("Le mot de passe doit contenir au moins une lettre.")
            .Matches("[0-9]").WithMessage("Le mot de passe doit contenir au moins un chiffre.");
    }
}

public sealed class ActivateAccountValidator : AbstractValidator<Contracts.Auth.ActivateAccountRequest>
{
    public ActivateAccountValidator(IOptions<AuthOptions> options)
    {
        RuleFor(x => x.Reference).NotEmpty();
        RuleFor(x => x.TemporaryPassword).NotEmpty();
        RuleFor(x => x.NewPassword).ApplyPolicy(options);
        RuleFor(x => x)
            .Must(x => x.NewPassword != x.TemporaryPassword)
            .WithMessage("Le nouveau mot de passe doit différer du mot de passe temporaire.");
    }
}

public sealed class LoginValidator : AbstractValidator<Contracts.Auth.LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Reference).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class ForgotPasswordValidator : AbstractValidator<Contracts.Auth.ForgotPasswordRequest>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(60);
    }
}

public sealed class ResetPasswordValidator : AbstractValidator<Contracts.Auth.ResetPasswordRequest>
{
    public ResetPasswordValidator(IOptions<AuthOptions> options)
    {
        RuleFor(x => x.Token).NotEmpty();
        // Politique feature 003 réutilisée ; pas de règle « différent de l'ancien » (le membre a
        // oublié son mot de passe — la comparaison est sans objet, cf. spec Edge Cases).
        RuleFor(x => x.NewPassword).ApplyPolicy(options);
    }
}

public sealed class ChangePasswordValidator : AbstractValidator<Contracts.Auth.ChangePasswordRequest>
{
    public ChangePasswordValidator(IOptions<AuthOptions> options)
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).ApplyPolicy(options);
        RuleFor(x => x)
            .Must(x => x.NewPassword != x.CurrentPassword)
            .WithMessage("Le nouveau mot de passe doit différer de l'ancien.");
    }
}
