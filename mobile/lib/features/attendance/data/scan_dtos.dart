/// DTO miroir du contrat serveur `Lumineux.Application.Contracts.Attendances`.
/// Voir `specs/026-mobile-qr-scan/contracts/scan-api-consumption.md`.
class AttendanceResponse {
  const AttendanceResponse({
    required this.id,
    required this.sessionId,
    required this.memberId,
    required this.memberFullName,
    required this.arrivalTime,
    required this.endTime,
    required this.source,
    required this.status,
    required this.originAntennaId,
  });

  final int id;
  final int sessionId;
  final int memberId;
  final String? memberFullName;
  final DateTime arrivalTime;
  final DateTime? endTime;
  final String source;
  final String status;
  final int? originAntennaId;

  factory AttendanceResponse.fromJson(Map<String, dynamic> json) =>
      AttendanceResponse(
        id: (json['id'] as num).toInt(),
        sessionId: (json['sessionId'] as num).toInt(),
        memberId: (json['memberId'] as num).toInt(),
        memberFullName: json['memberFullName'] as String?,
        arrivalTime: DateTime.parse(json['arrivalTime'] as String),
        endTime: json['endTime'] == null
            ? null
            : DateTime.parse(json['endTime'] as String),
        source: (json['source'] as String?) ?? '',
        status: (json['status'] as String?) ?? '',
        originAntennaId: (json['originAntennaId'] as num?)?.toInt(),
      );
}

/// Résultat client d'un scan : `created=true` si **201** (présence créée),
/// `false` si **200** (déjà présente). Les deux sont des succès.
class ScanOutcome {
  const ScanOutcome({required this.attendance, required this.created});

  final AttendanceResponse attendance;
  final bool created;
}
