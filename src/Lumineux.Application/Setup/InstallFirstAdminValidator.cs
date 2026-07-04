using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Auth;
using Lumineux.Application.Contracts.Setup;
using Lumineux.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Lumineux.Application.Setup;

/// <summary>Validation du payload d'installation (feature 005, FR-002/FR-003).</summary>
public sealed class InstallFirstAdminValidator : AbstractValidator<InstallFirstAdminRequest>
{
    public InstallFirstAdminValidator(IOptions<AuthOptions> options)
    {
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Gender).NotEmpty().Must(Genders.IsValid)
            .WithMessage("Le sexe doit être 'M' ou 'F'.");
        RuleFor(x => x.Password).ApplyPolicy(options);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(254)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Mobile).MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.Mobile));
    }
}
