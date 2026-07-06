using FluentValidation;
using Lumineux.Application.Contracts.Antennas;

namespace Lumineux.Application.Antennas;

/// <summary>Validation d'entrée (serveur) de la création d'antenne (FR-012).</summary>
public sealed class CreateAntennaValidator : AbstractValidator<CreateAntennaRequest>
{
    public CreateAntennaValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DistrictId).GreaterThan(0);
    }
}

/// <summary>Validation d'entrée (serveur) de la modification d'antenne (code immuable, non fourni).</summary>
public sealed class UpdateAntennaValidator : AbstractValidator<UpdateAntennaRequest>
{
    public UpdateAntennaValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DistrictId).GreaterThan(0);
    }
}
