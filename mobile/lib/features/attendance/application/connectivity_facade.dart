import 'package:connectivity_plus/connectivity_plus.dart';

/// Abstraction de connectivité réseau (research.md D2, FR-006), substituable en
/// test. Événementielle (aucun polling) : s'appuie sur les broadcasts de l'OS
/// via `connectivity_plus`.
abstract class ConnectivityFacade {
  /// Émet l'état de connectivité (`true` = en ligne) à chaque changement.
  Stream<bool> get onStatusChange;

  /// État courant (`true` = au moins une interface réseau disponible).
  Future<bool> isOnline();
}

/// Implémentation réelle basée sur `connectivity_plus`.
class RealConnectivityFacade implements ConnectivityFacade {
  RealConnectivityFacade([Connectivity? connectivity])
      : _connectivity = connectivity ?? Connectivity();

  final Connectivity _connectivity;

  static bool _isOnline(List<ConnectivityResult> results) =>
      results.any((r) => r != ConnectivityResult.none);

  @override
  Stream<bool> get onStatusChange =>
      _connectivity.onConnectivityChanged.map(_isOnline);

  @override
  Future<bool> isOnline() async =>
      _isOnline(await _connectivity.checkConnectivity());
}
