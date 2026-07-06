using FluentValidation;
using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Antennas;
using Lumineux.Domain.Abstractions;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.Antennas;

/// <summary>Cas d'usage : création d'une antenne (feature 016, US1, FR-001/002/003).</summary>
public sealed class CreateAntennaHandler
{
    private readonly IAntennaRepository _antennas;
    private readonly IReferenceLookupRepository _lookup;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreateAntennaRequest> _validator;

    public CreateAntennaHandler(
        IAntennaRepository antennas,
        IReferenceLookupRepository lookup,
        ICurrentUser user,
        IAuditLogger audit,
        IValidator<CreateAntennaRequest> validator)
    {
        _antennas = antennas;
        _lookup = lookup;
        _user = user;
        _audit = audit;
        _validator = validator;
    }

    public async Task<AntennaResponse> HandleAsync(CreateAntennaRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (!_user.HasPermission(Permissions.ManageReferentials))
        {
            _audit.Refused("CreateAntenna", "Droit manage_referentials manquant");
            throw new ForbiddenException("Droit requis pour gérer les référentiels.");
        }

        if (!await _lookup.DistrictExistsAsync(request.DistrictId, ct))
        {
            throw new DomainException("Le district de rattachement n'existe pas.");
        }

        if (await _antennas.GetByCodeAsync(request.Code, ct) is not null)
        {
            _audit.Refused("CreateAntenna", "Code d'antenne déjà utilisé", new { request.Code });
            throw new ConflictException("Une antenne avec ce code existe déjà.", "duplicate_code");
        }

        var antenna = Antenna.Create(request.Code, request.Label, request.DistrictId);
        await _antennas.AddAsync(antenna, ct);
        await _antennas.SaveChangesAsync(ct);

        _audit.Operation("CreateAntenna", new { antenna.Id, antenna.Code });
        return antenna.ToResponse();
    }
}
