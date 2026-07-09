/// DTO miroir du contrat serveur **existant** de synchronisation par lot
/// (`Lumineux.Application.Contracts.Attendances`). Voir
/// `specs/027-mobile-offline-sync/contracts/batch-sync-api-consumption.md`.
/// Aucune évolution d'API : ces formes reproduisent exactement le contrat.
library;

/// Un scan hors ligne à synchroniser (corps de requête).
class OfflineScanItem {
  const OfflineScanItem({
    required this.clientOperationId,
    required this.token,
    required this.clientArrivalTime,
  });

  final String clientOperationId;
  final String token;
  final DateTime clientArrivalTime;

  Map<String, dynamic> toJson() => {
        'clientOperationId': clientOperationId,
        'token': token,
        'clientArrivalTime': clientArrivalTime.toUtc().toIso8601String(),
      };
}

/// Lot de scans hors ligne (une séance, portée par la route).
class OfflineScanBatchRequest {
  const OfflineScanBatchRequest(this.items);

  final List<OfflineScanItem> items;

  Map<String, dynamic> toJson() =>
      {'items': items.map((i) => i.toJson()).toList()};
}

/// Résultat de synchronisation d'un élément (réponse serveur).
class OfflineScanResult {
  const OfflineScanResult({
    required this.clientOperationId,
    required this.outcome,
    this.reason,
    this.attendanceId,
  });

  final String clientOperationId;

  /// `Created` | `AlreadyPresent` | `Rejected` (voir [OfflineScanOutcome]).
  final String outcome;

  /// Renseigné uniquement si `outcome == Rejected`.
  final String? reason;

  /// Renseigné pour `Created`/`AlreadyPresent`.
  final int? attendanceId;

  factory OfflineScanResult.fromJson(Map<String, dynamic> json) =>
      OfflineScanResult(
        clientOperationId: json['clientOperationId'] as String,
        outcome: json['outcome'] as String,
        reason: json['reason'] as String?,
        attendanceId: (json['attendanceId'] as num?)?.toInt(),
      );
}

/// Réponse de synchronisation d'un lot.
class OfflineScanBatchResponse {
  const OfflineScanBatchResponse(this.results);

  final List<OfflineScanResult> results;

  factory OfflineScanBatchResponse.fromJson(Map<String, dynamic> json) =>
      OfflineScanBatchResponse(
        ((json['results'] as List?) ?? const [])
            .whereType<Map>()
            .map((m) => OfflineScanResult.fromJson(m.cast<String, dynamic>()))
            .toList(),
      );
}

/// Issues possibles d'un élément de synchronisation (constantes serveur).
class OfflineScanOutcome {
  const OfflineScanOutcome._();
  static const String created = 'Created';
  static const String alreadyPresent = 'AlreadyPresent';
  static const String rejected = 'Rejected';
}
