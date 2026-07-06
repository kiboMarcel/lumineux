using Lumineux.Application.Abstractions;
using Lumineux.Application.Contracts.Antennas;
using Lumineux.Domain.Abstractions;

namespace Lumineux.Application.Antennas;

/// <summary>
/// Cas d'usage : activation / désactivation d'une antenne (feature 016, US3, FR-005/005a/009).
/// La désactivation est refusée si l'antenne porte une session de présence encore ouverte.
/// </summary>
public sealed class SetAntennaActiveHandler
{
    private readonly IAntennaRepository _antennas;
    private readonly IAttendanceSessionRepository _sessions;
    private readonly ICurrentUser _user;
    private readonly IAuditLogger _audit;

    public SetAntennaActiveHandler(
        IAntennaRepository antennas,
        IAttendanceSessionRepository sessions,
        ICurrentUser user,
        IAuditLogger audit)
    {
        _antennas = antennas;
        _sessions = sessions;
        _user = user;
        _audit = audit;
    }

    public async Task<AntennaResponse> HandleAsync(int id, bool active, CancellationToken ct = default)
    {
        var operation = active ? "ActivateAntenna" : "DeactivateAntenna";

        if (!_user.HasPermission(Permissions.ManageReferentials))
        {
            _audit.Refused(operation, "Droit manage_referentials manquant", new { id });
            throw new ForbiddenException("Droit requis pour gérer les référentiels.");
        }

        var antenna = await _antennas.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Antenne introuvable.");

        if (active)
        {
            antenna.Activate();
        }
        else
        {
            if (await _sessions.HasOpenSessionForAntennaAsync(id, ct))
            {
                _audit.Refused(operation, "Antenne rattachée à une session ouverte", new { id });
                throw new ConflictException(
                    "Impossible de désactiver l'antenne : une session de présence est encore ouverte.",
                    "antenna_has_open_sessions");
            }

            antenna.Deactivate();
        }

        await _antennas.SaveChangesAsync(ct);
        _audit.Operation(operation, new { antenna.Id, antenna.Code, antenna.Status });
        return antenna.ToResponse();
    }
}
