using FluentValidation;

namespace Lumineux.Application.Reports;

/// <summary>Plage de dates d'un rapport (bornes de réunion, inclusives).</summary>
public sealed record ReportPeriod(DateTime From, DateTime To);

/// <summary>
/// Validation d'entrée (serveur) de la plage d'un rapport (feature 018, FR-010) : bornes présentes,
/// <c>To ≥ From</c> et plafond de période pour borner le coût des agrégations.
/// </summary>
public sealed class ReportPeriodValidator : AbstractValidator<ReportPeriod>
{
    /// <summary>Amplitude maximale d'une période (anti-abus).</summary>
    public const int MaxSpanDays = 366;

    public ReportPeriodValidator()
    {
        RuleFor(x => x.From).NotEmpty();
        RuleFor(x => x.To).NotEmpty();
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("La date de fin doit être postérieure ou égale à la date de début.");
        RuleFor(x => x)
            .Must(p => (p.To.Date - p.From.Date).TotalDays <= MaxSpanDays)
            .WithMessage($"La période demandée ne peut excéder {MaxSpanDays} jours.");
    }
}
