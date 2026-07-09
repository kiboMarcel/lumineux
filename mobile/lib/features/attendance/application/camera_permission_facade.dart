import 'package:permission_handler/permission_handler.dart';

/// Abstraction de la permission caméra — **substituable en test** (pas de canal
/// plateforme réel). Encapsule `permission_handler`.
abstract class CameraPermissionFacade {
  /// Vrai si la permission caméra est déjà accordée.
  Future<bool> isGranted();

  /// Demande la permission ; renvoie vrai si accordée.
  Future<bool> request();

  /// Ouvre les réglages système de l'application (refus permanent).
  Future<void> openSettings();
}

/// Implémentation réelle sur `permission_handler`.
class RealCameraPermissionFacade implements CameraPermissionFacade {
  @override
  Future<bool> isGranted() => Permission.camera.isGranted;

  @override
  Future<bool> request() async {
    final status = await Permission.camera.request();
    return status.isGranted;
  }

  @override
  Future<void> openSettings() async {
    await openAppSettings();
  }
}
