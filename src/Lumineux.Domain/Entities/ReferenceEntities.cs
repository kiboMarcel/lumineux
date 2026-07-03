namespace Lumineux.Domain.Entities;

// Nomenclatures réutilisées par la fiche membre (projections minimales — leur CRUD relève
// d'autres fonctionnalités). Servent de cibles de clé étrangère et de validation d'existence (FR-005).

/// <summary>Civilité (M., Mme, Dr, …).</summary>
public class Civility : AbstractEntity
{
    public string Code { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Status { get; set; } = "Active";
}

/// <summary>Pays (avec nationalité) — utilisé pour la nationalité du membre.</summary>
public class Country : AbstractEntity
{
    public string Code { get; set; } = default!;
    public string LabelCountry { get; set; } = default!;
    public string LabelNationality { get; set; } = default!;
    public string Status { get; set; } = "Active";
}

/// <summary>Ville / commune — lieu et ville de naissance.</summary>
public class City : AbstractEntity
{
    public string Code { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Status { get; set; } = "Active";
}

/// <summary>District / quartier de résidence.</summary>
public class District : AbstractEntity
{
    public string Code { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Status { get; set; } = "Active";
}
