using FluentValidation;
using Lumineux.Application.Contracts.Members;
using Lumineux.Domain.Enums;

namespace Lumineux.Application.Members;

public sealed class UpdateMemberValidator : AbstractValidator<UpdateMemberRequest>
{
    public UpdateMemberValidator()
    {
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Gender).Must(Genders.IsValid).WithMessage("Le sexe doit être 'M' ou 'F'.");
        RuleFor(x => x.AntennaId).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Mobile) || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Au moins une coordonnée de contact (mobile ou e-mail) est requise.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Profession)
            .MaximumLength(150).WithMessage("La profession ne doit pas dépasser 150 caractères.")
            .When(x => x.Profession is not null);
    }
}
