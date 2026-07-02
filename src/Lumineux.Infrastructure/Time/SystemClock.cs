using Lumineux.Domain.Abstractions;

namespace Lumineux.Infrastructure.Time;

/// <summary>Source de temps réelle (UTC). Seul endroit autorisé à lire l'horloge système.</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
