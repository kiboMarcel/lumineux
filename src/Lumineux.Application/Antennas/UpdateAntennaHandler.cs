using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Antennas;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Antennas;

/// <summary>Cas d'usage : modification d'une antenne (feature 016, US2, FR-004). Le code est immuable.</summary>
public sealed class UpdateAntennaHandler
{
    private readonly IAntennaRepository _antennas;
    private readonly IReferenceLookupRepository _lookup;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<UpdateAntennaRequest> _validator;

    public UpdateAntennaHandler(
        IAntennaRepository antennas,
        IReferenceLookupRepository lookup,
        ICurrentUser user,
        IAuditLogger audit,
        IValidator<UpdateAntennaRequest> validator)
    {
        _antennas = antennas;
        _lookup = lookup;
        _user = user;
        _audit = audit;
        _validator = validator;
    }

    public async Task<AntennaResponse> HandleAsync(int id, UpdateAntennaRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (!_user.HasPermission(Permissions.ManageReferentials))
        {
            _audit.Refused("UpdateAntenna", "Droit manage_referentials manquant", new { id });
            throw new ForbiddenException("Droit requis pour gérer les référentiels.");
        }

        var antenna = await _antennas.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Antenne introuvable.");

        if (!await _lookup.DistrictExistsAsync(request.DistrictId, ct))
        {
            throw new DomainException("Le district de rattachement n'existe pas.");
        }

        antenna.UpdateDetails(request.Label, request.DistrictId); // code inchangé
        await _antennas.SaveChangesAsync(ct);

        _audit.Operation("UpdateAntenna", new { antenna.Id, antenna.Code });
        return antenna.ToResponse();
    }
}
