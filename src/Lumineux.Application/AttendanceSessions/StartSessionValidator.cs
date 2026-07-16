using FluentValidation;
using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Entities;
using Lumineux.Domain.Enums;

namespace Lumineux.Application.AttendanceSessions;

public sealed class StartSessionValidator : AbstractValidator<StartSessionRequest>
{
    public StartSessionValidator()
    {
        RuleFor(x => x.AntennaId).GreaterThan(0);

        RuleFor(x => x.QrStepSeconds!.Value)
            .InclusiveBetween(AttendanceSession.MinQrStepSeconds, AttendanceSession.MaxQrStepSeconds)
            .When(x => x.QrStepSeconds.HasValue);

        // Feature 031 : si un type est fourni, il doit appartenir à l'ensemble fermé (sensible à
        // la casse, comme Gender/Status). Absent → défaut AntennaMeeting côté handler.
        RuleFor(x => x.SessionType!)
            .Must(t => Enum.TryParse<SessionType>(t, ignoreCase: false, out _))
            .WithMessage("Type de session inconnu.")
            .When(x => x.SessionType is not null);
    }
}
