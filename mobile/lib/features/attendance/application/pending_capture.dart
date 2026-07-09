/// État d'un élément de la file hors ligne (data-model.md §3).
///
/// Seuls ces trois états sont **persistés**. Les issues définitives (Created,
/// AlreadyPresent, Rejected, échec définitif) entraînent le **retrait** de la
/// file — elles ne sont pas des états stockés.
enum PendingState {
  /// En attente d'une première synchronisation.
  pending,

  /// Tentative de synchronisation en cours.
  inProgress,

  /// Dernière tentative en échec **transitoire** (réseau/5xx) — sera réessayée.
  transientFailed,
}

/// Capture hors ligne en attente (data-model.md §1).
///
/// Élément de la file locale persistante (coffre sécurisé). Le [token] est
/// **sensible** : jamais affiché/journalisé, purgé au retrait de la file.
class PendingCapture {
  const PendingCapture({
    required this.clientOperationId,
    required this.sessionId,
    required this.token,
    required this.clientArrivalTime,
    required this.firstCapturedAt,
    this.attemptCount = 0,
    this.lastAttemptAt,
    this.state = PendingState.pending,
  });

  /// Clé d'idempotence (≤ 64 car.), **immuable**.
  final String clientOperationId;

  /// Séance scannée.
  final int sessionId;

  /// Jeton scanné (sensible).
  final String token;

  /// Heure du scan (UTC) — envoyée telle quelle, bornée par le serveur.
  final DateTime clientArrivalTime;

  /// Première mise en file (UTC) — base du calcul d'âge (plafond FR-013).
  final DateTime firstCapturedAt;

  /// Nombre de tentatives en échec transitoire (jamais incrémenté sur 401).
  final int attemptCount;

  /// Dernière tentative (UTC) — pilote le backoff.
  final DateTime? lastAttemptAt;

  /// État courant.
  final PendingState state;

  PendingCapture copyWith({
    int? attemptCount,
    DateTime? lastAttemptAt,
    PendingState? state,
  }) =>
      PendingCapture(
        clientOperationId: clientOperationId,
        sessionId: sessionId,
        token: token,
        clientArrivalTime: clientArrivalTime,
        firstCapturedAt: firstCapturedAt,
        attemptCount: attemptCount ?? this.attemptCount,
        lastAttemptAt: lastAttemptAt ?? this.lastAttemptAt,
        state: state ?? this.state,
      );

  Map<String, dynamic> toJson() => {
        'clientOperationId': clientOperationId,
        'sessionId': sessionId,
        'token': token,
        'clientArrivalTime': clientArrivalTime.toUtc().toIso8601String(),
        'firstCapturedAt': firstCapturedAt.toUtc().toIso8601String(),
        'attemptCount': attemptCount,
        'lastAttemptAt': lastAttemptAt?.toUtc().toIso8601String(),
        'state': state.name,
      };

  factory PendingCapture.fromJson(Map<String, dynamic> json) => PendingCapture(
        clientOperationId: json['clientOperationId'] as String,
        sessionId: (json['sessionId'] as num).toInt(),
        token: json['token'] as String,
        clientArrivalTime:
            DateTime.parse(json['clientArrivalTime'] as String).toUtc(),
        firstCapturedAt:
            DateTime.parse(json['firstCapturedAt'] as String).toUtc(),
        attemptCount: (json['attemptCount'] as num?)?.toInt() ?? 0,
        lastAttemptAt: json['lastAttemptAt'] == null
            ? null
            : DateTime.parse(json['lastAttemptAt'] as String).toUtc(),
        state: PendingState.values.firstWhere(
          (s) => s.name == json['state'],
          orElse: () => PendingState.pending,
        ),
      );
}
