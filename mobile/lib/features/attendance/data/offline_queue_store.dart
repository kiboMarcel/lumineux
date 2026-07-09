import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../application/pending_capture.dart';

/// File locale **persistante** des captures hors ligne (research.md D1,
/// FR-003/FR-009/FR-014).
///
/// Sérialise la file comme document JSON unique dans `flutter_secure_storage`
/// (Keychain iOS / EncryptedSharedPreferences via Keystore Android) : le
/// **jeton** est ainsi **protégé au repos** et **purgé** au retrait. Le volume
/// est faible (présence du seul membre), un document JSON suffit.
///
/// Les écritures doivent être **sérialisées** par l'appelant (contrôleur unique)
/// pour éviter les courses de lecture-modification-écriture.
class OfflineQueueStore {
  OfflineQueueStore(this._storage);

  final FlutterSecureStorage _storage;

  static const String _key = 'lumineux_offline_queue';

  static const AndroidOptions _androidOptions = AndroidOptions(
    encryptedSharedPreferences: true,
  );

  /// Lit l'ensemble de la file. Un contenu illisible/corrompu est traité comme
  /// une file vide (purge défensive), jamais propagé.
  Future<List<PendingCapture>> readAll() async {
    final raw = await _storage.read(key: _key, aOptions: _androidOptions);
    if (raw == null || raw.isEmpty) return [];
    try {
      final decoded = jsonDecode(raw);
      if (decoded is! List) return [];
      return decoded
          .whereType<Map>()
          .map((m) => PendingCapture.fromJson(m.cast<String, dynamic>()))
          .toList();
    } catch (_) {
      await _storage.delete(key: _key, aOptions: _androidOptions);
      return [];
    }
  }

  /// Ajoute une capture, avec **déduplication par séance** (FR-014) : si une
  /// capture existe déjà pour `capture.sessionId`, l'existante est **conservée**
  /// et la nouvelle **ignorée**. Renvoie `true` si ajoutée, `false` si dédupée.
  Future<bool> add(PendingCapture capture) async {
    final all = await readAll();
    if (all.any((c) => c.sessionId == capture.sessionId)) {
      return false;
    }
    all.add(capture);
    await _write(all);
    return true;
  }

  /// Remplace une capture (par `clientOperationId`). Sans effet si absente.
  Future<void> update(PendingCapture capture) async {
    final all = await readAll();
    final i = all.indexWhere(
        (c) => c.clientOperationId == capture.clientOperationId);
    if (i == -1) return;
    all[i] = capture;
    await _write(all);
  }

  /// Retire une capture (par `clientOperationId`) — **purge** le jeton associé.
  Future<void> remove(String clientOperationId) async {
    final all = await readAll();
    all.removeWhere((c) => c.clientOperationId == clientOperationId);
    await _write(all);
  }

  /// Indique si une capture existe déjà pour cette séance (dédup FR-014).
  Future<bool> containsSession(int sessionId) async {
    final all = await readAll();
    return all.any((c) => c.sessionId == sessionId);
  }

  Future<void> _write(List<PendingCapture> all) async {
    if (all.isEmpty) {
      await _storage.delete(key: _key, aOptions: _androidOptions);
      return;
    }
    final payload = jsonEncode(all.map((c) => c.toJson()).toList());
    await _storage.write(key: _key, value: payload, aOptions: _androidOptions);
  }
}
