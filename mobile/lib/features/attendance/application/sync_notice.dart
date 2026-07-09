/// Nature d'un avis de synchronisation (data-model.md §2).
enum NoticeKind {
  /// Refus serveur (jeton invalide au scan, hors plage, après clôture).
  rejected,

  /// Échec définitif après plafond de tentatives/âge (FR-013).
  permanentlyFailed,
}

/// Avis de synchronisation présenté au membre — **sans jeton** (SC-004).
///
/// Persisté jusqu'à acquittement, afin que le membre soit informé d'un rejet
/// ou d'un échec définitif même si celui-ci survient application fermée.
class SyncNotice {
  const SyncNotice({
    required this.clientOperationId,
    required this.sessionId,
    required this.kind,
    required this.reason,
    required this.occurredAt,
    this.acknowledged = false,
  });

  final String clientOperationId;
  final int sessionId;
  final NoticeKind kind;
  final String reason;
  final DateTime occurredAt;
  final bool acknowledged;

  SyncNotice copyWith({bool? acknowledged}) => SyncNotice(
        clientOperationId: clientOperationId,
        sessionId: sessionId,
        kind: kind,
        reason: reason,
        occurredAt: occurredAt,
        acknowledged: acknowledged ?? this.acknowledged,
      );

  Map<String, dynamic> toJson() => {
        'clientOperationId': clientOperationId,
        'sessionId': sessionId,
        'kind': kind.name,
        'reason': reason,
        'occurredAt': occurredAt.toUtc().toIso8601String(),
        'acknowledged': acknowledged,
      };

  factory SyncNotice.fromJson(Map<String, dynamic> json) => SyncNotice(
        clientOperationId: json['clientOperationId'] as String,
        sessionId: (json['sessionId'] as num).toInt(),
        kind: NoticeKind.values.firstWhere(
          (k) => k.name == json['kind'],
          orElse: () => NoticeKind.rejected,
        ),
        reason: json['reason'] as String,
        occurredAt: DateTime.parse(json['occurredAt'] as String).toUtc(),
        acknowledged: json['acknowledged'] as bool? ?? false,
      );
}
