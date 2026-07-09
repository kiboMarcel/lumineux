/// Port d'horloge substituable (feature 027, research.md D10).
///
/// Rend `utcNow` déterministe en test (backoff, plafond d'âge FR-013) tout en
/// s'appuyant sur l'horloge système en production. L'heure d'arrivée métier
/// reste **bornée par le serveur** — une horloge client fausse mène à un rejet
/// serveur signalé, jamais à une présence erronée (Principe VI).
abstract class Clock {
  DateTime utcNow();
}

/// Implémentation réelle : horloge système en UTC.
class SystemClock implements Clock {
  const SystemClock();

  @override
  DateTime utcNow() => DateTime.now().toUtc();
}
