namespace Lumineux.Domain.Enums;

/// <summary>Origine de l'enregistrement d'une présence (FR-015).</summary>
public enum AttendanceSource
{
    QrScan = 0,
    Manual = 1,
}

/// <summary>Validité d'une présence (le retrait est tracé, FR-016).</summary>
public enum AttendanceStatus
{
    Valid = 0,
    Cancelled = 1,
}
