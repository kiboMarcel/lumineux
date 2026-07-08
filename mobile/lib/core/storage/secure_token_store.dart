import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Jeton d'accès conservé côté client. Opaque : sa valeur n'est jamais
/// journalisée ni affichée. Persisté uniquement au coffre sécurisé.
class AuthToken {
  const AuthToken({
    required this.value,
    required this.type,
    required this.expiresAt,
  });

  final String value;
  final String type;
  final DateTime expiresAt;

  /// Pré-vérification locale : « potentiellement valide » si l'échéance est
  /// dans le futur. L'API reste l'autorité (un 401 le déclare invalide).
  bool get isPotentiallyValid => expiresAt.isAfter(DateTime.now());

  Map<String, dynamic> toJson() => {
        'value': value,
        'type': type,
        'expiresAt': expiresAt.toIso8601String(),
      };

  factory AuthToken.fromJson(Map<String, dynamic> json) => AuthToken(
        value: json['value'] as String,
        type: (json['type'] as String?) ?? 'Bearer',
        expiresAt: DateTime.parse(json['expiresAt'] as String),
      );
}

/// Coffre sécurisé du jeton (Keychain iOS / EncryptedSharedPreferences via
/// Keystore Android). **Seul** élément persisté durablement par l'app.
class SecureTokenStore {
  SecureTokenStore(this._storage);

  final FlutterSecureStorage _storage;

  static const String _key = 'lumineux_auth_token';

  static const AndroidOptions _androidOptions = AndroidOptions(
    encryptedSharedPreferences: true,
  );

  Future<void> save(AuthToken token) => _storage.write(
        key: _key,
        value: jsonEncode(token.toJson()),
        aOptions: _androidOptions,
      );

  Future<AuthToken?> read() async {
    final raw = await _storage.read(key: _key, aOptions: _androidOptions);
    if (raw == null || raw.isEmpty) return null;
    try {
      final decoded = jsonDecode(raw) as Map<String, dynamic>;
      return AuthToken.fromJson(decoded);
    } catch (_) {
      // Contenu illisible/corrompu → purge défensive.
      await clear();
      return null;
    }
  }

  Future<void> clear() => _storage.delete(key: _key, aOptions: _androidOptions);
}
