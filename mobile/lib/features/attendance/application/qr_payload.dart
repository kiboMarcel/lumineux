import 'dart:convert';

/// Charge utile décodée d'un QR de séance : identifiant + jeton.
/// Voir `specs/026-mobile-qr-scan/contracts/qr-payload.md`.
class QrPayload {
  const QrPayload({required this.sessionId, required this.token});

  final int sessionId;
  final String token;
}

/// Résultat du décodage : soit une charge **valide**, soit **non reconnue**.
/// La validation ne fait **pas autorité** (le serveur valide le jeton et la
/// séance) — elle extrait `s`/`t` et écarte les QR étrangers/malformés.
class QrPayloadResult {
  const QrPayloadResult._(this.payload);

  final QrPayload? payload;

  bool get isValid => payload != null;

  /// Version de format supportée par ce client.
  static const int supportedVersion = 1;

  /// Parse le contenu brut d'un QR : JSON `{"v":1,"s":<int>,"t":"<str>"}`.
  static QrPayloadResult parse(String raw) {
    try {
      final decoded = jsonDecode(raw);
      if (decoded is! Map) return const QrPayloadResult._(null);

      final version = decoded['v'];
      if (version is! int || version != supportedVersion) {
        return const QrPayloadResult._(null);
      }

      final s = decoded['s'];
      final sessionId = s is int ? s : (s is num ? s.toInt() : null);
      if (sessionId == null || sessionId <= 0) {
        return const QrPayloadResult._(null);
      }

      final t = decoded['t'];
      if (t is! String || t.isEmpty) return const QrPayloadResult._(null);

      return QrPayloadResult._(QrPayload(sessionId: sessionId, token: t));
    } catch (_) {
      return const QrPayloadResult._(null);
    }
  }
}
