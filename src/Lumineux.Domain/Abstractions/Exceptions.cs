namespace Lumineux.Domain.Abstractions;

/// <summary>Exception métier générique (règle du domaine violée).</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>Ressource demandée introuvable (→ 404).</summary>
public sealed class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Conflit d'état (→ 409), ex. session déjà ouverte/clôturée.</summary>
public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Droit manquant pour l'opération (→ 403).</summary>
public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>Ressource/jeton expiré ou invalidé (→ 410), ex. jeton QR périmé.</summary>
public sealed class GoneException : DomainException
{
    public GoneException(string message) : base(message) { }
}
