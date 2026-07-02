using FluentValidation;
using Lumineux.Application.Contracts.Sessions;
using Lumineux.Domain.Entities;

namespace Lumineux.Application.AttendanceSessions;

public sealed class StartSessionValidator : AbstractValidator<StartSessionRequest>
{
    public StartSessionValidator()
    {
        RuleFor(x => x.AntennaId).GreaterThan(0);

        RuleFor(x => x.QrStepSeconds!.Value)
            .InclusiveBetween(AttendanceSession.MinQrStepSeconds, AttendanceSession.MaxQrStepSeconds)
            .When(x => x.QrStepSeconds.HasValue);
    }
}
