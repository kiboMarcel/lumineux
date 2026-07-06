namespace Lumineux.Application.Contracts.Antennas;

/// <summary>Antenne renvoyée par l'API de gestion (feature 016). DTO dédié, pas d'entité exposée.</summary>
public sealed record AntennaResponse(int Id, string Code, string Label, int DistrictId, string Status);

/// <summary>Requête de création d'une antenne (code unique, libellé, district de rattachement).</summary>
public sealed record CreateAntennaRequest(string Code, string Label, int DistrictId);

/// <summary>Requête de modification d'une antenne (libellé + district ; le code est immuable).</summary>
public sealed record UpdateAntennaRequest(string Label, int DistrictId);
