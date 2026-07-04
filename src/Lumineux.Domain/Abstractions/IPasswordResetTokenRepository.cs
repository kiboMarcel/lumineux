using Lumineux.Domain.Entities;

namespace Lumineux.Domain.Abstractions;

/// <summary>Port de persistance des jetons de réinitialisation de mot de passe (feature 006).</summary>
public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);

    /// <summary>
    /// Charge un jeton par son empreinte, avec le compte (et son membre) suivi pour mise à jour.
    /// Retourne null si aucune empreinte ne correspond.
    /// </summary>
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
