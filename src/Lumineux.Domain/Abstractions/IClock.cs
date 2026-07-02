namespace Lumineux.Domain.Abstractions;

/// <summary>
/// Source de temps injectable (Constitution VI). Les heures faisant foi s'appuient
/// sur cette abstraction, jamais sur l'horloge d'un client.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
