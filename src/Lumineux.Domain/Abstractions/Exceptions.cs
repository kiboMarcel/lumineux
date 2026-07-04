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

    public ConflictException(string message, string code) : base(message) => Code = code;

    /// <summary>Code métier optionnel exposé dans la réponse (ex. <c>last_administrator</c>).</summary>
    public string? Code { get; }
}

/// <summary>Droit manquant pour l'opération (→ 403).</summary>
public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string message) : base(message) { }
}

/// <summary>Échec d'authentification (→ 401), message générique anti-énumération.</summary>
public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message) : base(message) { }
}

/// <summary>Changement de mot de passe requis avant tout accès (→ 403, code password_change_required).</summary>
public sealed class PasswordChangeRequiredException : DomainException
{
    public PasswordChangeRequiredException(string message) : base(message) { }
}

/// <summary>Ressource/jeton expiré ou invalidé (→ 410), ex. jeton QR périmé.</summary>
public sealed class GoneException : DomainException
{
    public GoneException(string message) : base(message) { }
}

/// <summary>
/// Conflit de doublon membre (→ 409) : homonyme non confirmé (<c>duplicate_name</c>) ou
/// coordonnée déjà utilisée par un membre actif (<c>contact_in_use</c>).
/// </summary>
public sealed class DuplicateMemberException : DomainException
{
    public DuplicateMemberException(string message, string code, IReadOnlyList<int> duplicateMemberIds)
        : base(message)
    {
        Code = code;
        DuplicateMemberIds = duplicateMemberIds;
    }

    public string Code { get; }

    public IReadOnlyList<int> DuplicateMemberIds { get; }
}
