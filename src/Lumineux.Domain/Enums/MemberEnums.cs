namespace Lumineux.Domain.Enums;

/// <summary>
/// Statuts du membre. Conservé en chaîne (colonne `status`) pour compatibilité avec le modèle
/// existant (feature 001) ; ces constantes évitent les littéraux dispersés.
/// </summary>
public static class MemberStatuses
{
    public const string Active = "Active";
    public const string Archived = "Archived";
}

/// <summary>Valeurs de sexe acceptées.</summary>
public static class Genders
{
    public const string Male = "M";
    public const string Female = "F";

    public static bool IsValid(string? value) => value is Male or Female;
}

/// <summary>État d'activation d'un compte de connexion (distinct du statut du membre).</summary>
public enum AccountActivationState
{
    PendingActivation = 0,
    Active = 1,
}
