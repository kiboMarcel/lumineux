namespace Lumineux.Infrastructure.BackgroundJobs;

/// <summary>Paramètres de la clôture automatique de secours (FR-024), liés à la section "AutoClose".</summary>
public sealed class AutoCloseOptions
{
    public const string SectionName = "AutoClose";

    /// <summary>Active le service de clôture automatique.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Intervalle de scrutation.</summary>
    public int PollingIntervalSeconds { get; set; } = 300;

    /// <summary>Délai maximal d'ouverture (depuis le démarrage) avant clôture automatique de secours.</summary>
    public int MaxOpenHours { get; set; } = 3;

    /// <summary>Durée par défaut d'une réunion, utilisée pour l'heure de fin appliquée à la clôture auto.</summary>
    public int DefaultDurationHours { get; set; } = 3;
}
