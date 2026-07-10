namespace Lumineux.Domain.Abstractions;

/// <summary>
/// Lecture des **droits effectifs** d'un membre, dérivés **exclusivement** de ses **profils du bureau**
/// (source unique, features 004/011). Lus à la connexion/activation pour peupler les claims du jeton.
/// </summary>
public interface IEffectivePermissionsReader
{
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(int memberId, CancellationToken ct = default);
}
