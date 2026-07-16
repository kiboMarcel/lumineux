namespace Lumineux.Domain.Enums;

/// <summary>
/// Nature d'une session de présence (feature 031). Ensemble fermé, fixé à la création,
/// immuable ensuite. <see cref="Teaching"/> est préparé pour le futur domaine des
/// enseignements mais ne déclenche aujourd'hui aucune règle métier distincte.
/// </summary>
public enum SessionType
{
    /// <summary>Réunion d'antenne / prière (défaut — comportement historique).</summary>
    AntennaMeeting = 0,

    /// <summary>Séance d'enseignement (préparé, sans logique métier dédiée à ce stade).</summary>
    Teaching = 1,
}
