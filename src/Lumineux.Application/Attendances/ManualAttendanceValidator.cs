using FluentValidation;
using Lumineux.Application.Contracts.Attendances;

namespace Lumineux.Application.Attendances;

public sealed class ManualAttendanceValidator : AbstractValidator<ManualAttendanceRequest>
{
    public ManualAttendanceValidator() => RuleFor(x => x.MemberId).GreaterThan(0);
}
