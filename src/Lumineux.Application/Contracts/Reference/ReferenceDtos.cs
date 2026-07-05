namespace Lumineux.Application.Contracts.Reference;

/// <summary>Entrée générique de nomenclature (antenne, civilité, ville, district) — feature 010.</summary>
public sealed record ReferenceItemResponse(int Id, string Code, string Label);

/// <summary>Pays / nationalité : libellé de pays et libellé de nationalité distincts (feature 010).</summary>
public sealed record CountryResponse(int Id, string Code, string Country, string Nationality);
