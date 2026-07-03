using FluentValidation;
using Lumineux.Application.Contracts.Members;
using Lumineux.Domain.Enums;

namespace Lumineux.Application.Members;

public sealed class CreateMemberValidator : AbstractValidator<CreateMemberRequest>
{
    public CreateMemberValidator()
    {
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Gender).Must(Genders.IsValid).WithMessage("Le sexe doit être 'M' ou 'F'.");
        RuleFor(x => x.AntennaId).GreaterThan(0);

        // Au moins une coordonnée de contact (mobile OU e-mail) — FR-003.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Mobile) || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Au moins une coordonnée de contact (mobile ou e-mail) est requise.");

        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
