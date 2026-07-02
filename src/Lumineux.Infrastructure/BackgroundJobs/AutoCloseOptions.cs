namespace Lumineux.Infrastructure.BackgroundJobs;

/// <summary>Paramètres de la clôture automatique de secours (FR-024), liés à la section "AutoClose".</summary>
public sealed class AutoCloseOptions
{
    public const string SectionName = "AutoClose";

    /// <summary>Active le service de clôture automatique.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Intervalle de scrutation.</summary>
    public int PollingIntervalSeconds { get; set; } = 300;

    /// <summary>Délai maximal d'ouverture après l'heure de réunion avant clôture automatique.</summary>
    public int MaxOpenHours { get; set; } = 6;

    /// <summary>Durée par défaut d'une réunion, utilisée pour l'heure de fin appliquée à la clôture auto.</summary>
    public int DefaultDurationHours { get; set; } = 3;
}
