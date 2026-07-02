namespace Lumineux.Domain.Entities;

/// <summary>
/// Classe de base fournissant les champs d'audit communs à toutes les entités
/// (voir Database Entities Documentation). Les valeurs sont peuplées automatiquement
/// par l'intercepteur d'audit EF (Infrastructure).
/// </summary>
public abstract class AbstractEntity
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
