import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import '../application/sync_notice.dart';

/// Store persistant des **avis de synchronisation** (rejets / échecs définitifs),
/// data-model.md §2 / research.md D6.
///
/// Les avis ne contiennent **jamais** de jeton. Ils sont conservés jusqu'à
/// acquittement, pour garantir SC-004 (aucune perte silencieuse) même si le
/// rejet survient application fermée. Réutilise le coffre du socle (aucune
/// dépendance supplémentaire) sous une clé dédiée.
class SyncNoticeStore {
  SyncNoticeStore(this._storage);

  final FlutterSecureStorage _storage;

  static const String _key = 'lumineux_sync_notices';

  static const AndroidOptions _androidOptions = AndroidOptions(
    encryptedSharedPreferences: true,
  );

  Future<List<SyncNotice>> readAll() async {
    final raw = await _storage.read(key: _key, aOptions: _androidOptions);
    if (raw == null || raw.isEmpty) return [];
    try {
      final decoded = jsonDecode(raw);
      if (decoded is! List) return [];
      return decoded
          .whereType<Map>()
          .map((m) => SyncNotice.fromJson(m.cast<String, dynamic>()))
          .toList();
    } catch (_) {
      await _storage.delete(key: _key, aOptions: _androidOptions);
      return [];
    }
  }

  /// Ajoute un avis (idempotent par `clientOperationId` : un avis existant pour
  /// la même opération n'est pas dupliqué).
  Future<void> add(SyncNotice notice) async {
    final all = await readAll();
    if (all.any((n) => n.clientOperationId == notice.clientOperationId)) return;
    all.add(notice);
    await _write(all);
  }

  /// Marque un avis comme acquitté (le membre l'a lu/fermé).
  Future<void> acknowledge(String clientOperationId) async {
    final all = await readAll();
    final i =
        all.indexWhere((n) => n.clientOperationId == clientOperationId);
    if (i == -1) return;
    all[i] = all[i].copyWith(acknowledged: true);
    await _write(all);
  }

  Future<void> _write(List<SyncNotice> all) async {
    if (all.isEmpty) {
      await _storage.delete(key: _key, aOptions: _androidOptions);
      return;
    }
    final payload = jsonEncode(all.map((n) => n.toJson()).toList());
    await _storage.write(key: _key, value: payload, aOptions: _androidOptions);
  }
}
