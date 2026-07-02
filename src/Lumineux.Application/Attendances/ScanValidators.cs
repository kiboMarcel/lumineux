using FluentValidation;
using Lumineux.Application.Contracts.Attendances;

namespace Lumineux.Application.Attendances;

public sealed class ScanRequestValidator : AbstractValidator<ScanRequest>
{
    public ScanRequestValidator() => RuleFor(x => x.Token).NotEmpty();
}

public sealed class OfflineScanBatchValidator : AbstractValidator<OfflineScanBatchRequest>
{
    public OfflineScanBatchValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ClientOperationId).NotEmpty().MaximumLength(64);
            item.RuleFor(i => i.Token).NotEmpty();
        });
    }
}
