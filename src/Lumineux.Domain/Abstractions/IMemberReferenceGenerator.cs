namespace Lumineux.Domain.Abstractions;

/// <summary>Génère une référence membre unique (= identifiant de connexion, FR-004).</summary>
public interface IMemberReferenceGenerator
{
    Task<string> NextAsync(DateTime nowUtc, CancellationToken ct = default);
}
