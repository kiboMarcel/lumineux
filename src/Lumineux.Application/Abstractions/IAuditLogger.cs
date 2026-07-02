namespace Lumineux.Application.Abstractions;

/// <summary>
/// Journalisation des opérations sensibles et des refus (Constitution VI, FR-019/FR-020).
/// Ne doit jamais consigner de secret ni de donnée personnelle superflue.
/// </summary>
public interface IAuditLogger
{
    void Operation(string action, object? details = null);

    void Refused(string action, string reason, object? details = null);
}
