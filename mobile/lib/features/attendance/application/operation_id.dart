import 'dart:math';

/// Générateur d'identifiant d'opération pour l'idempotence de synchronisation
/// (FR-002/FR-008, research.md D7).
///
/// Aléatoire **cryptographiquement sûr** (`Random.secure`), 32 caractères hex
/// (≈ 128 bits), donc bien **≤ 64** caractères. Généré **une seule fois** à la
/// capture et **immuable** ensuite : c'est la clé d'idempotence côté serveur
/// (`GetByClientOperationIdAsync`), garantissant l'absence de doublon même en
/// cas de réessais multiples.
class OperationId {
  const OperationId._();

  /// Produit un identifiant hex de 32 caractères. Un [Random] peut être injecté
  /// en test ; par défaut, une source cryptographiquement sûre est utilisée.
  static String generate([Random? random]) {
    final rnd = random ?? Random.secure();
    final bytes = List<int>.generate(16, (_) => rnd.nextInt(256));
    return bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join();
  }
}
