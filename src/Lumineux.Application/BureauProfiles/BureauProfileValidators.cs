using FluentValidation;
using Lumineux.Application.Contracts.BureauProfiles;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.BureauProfiles;

public sealed class BureauProfileWriteValidator : AbstractValidator<BureauProfileWriteRequest>
{
    public BureauProfileWriteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(BureauProfile.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(BureauProfile.DescriptionMaxLength);
        RuleFor(x => x.Permissions).NotNull();
    }
}

public sealed class AssignProfileValidator : AbstractValidator<AssignProfileRequest>
{
    public AssignProfileValidator()
    {
        RuleFor(x => x.ProfileId).GreaterThan(0);
    }
}
