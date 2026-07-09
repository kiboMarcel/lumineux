import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/time/clock.dart';
import '../../auth/application/providers.dart'
    show dioProvider, secureStorageProvider;
import '../data/attendance_api.dart';
import '../data/offline_queue_store.dart';
import '../data/sync_notice_store.dart';
import 'backoff_policy.dart';
import 'camera_permission_facade.dart';
import 'connectivity_facade.dart';
import 'scan_controller.dart';
import 'scan_state.dart';
import 'scanner_facade.dart';
import 'sync_controller.dart';
import 'sync_state.dart';

/// Client de l'API de présence (scan), sur le socle `dio` du lot M0.
final attendanceApiProvider = Provider<AttendanceApi>((ref) {
  return AttendanceApi(ref.watch(dioProvider));
});

/// Horloge substituable (heure du scan, âge des captures). Feature 027.
final clockProvider = Provider<Clock>((ref) => const SystemClock());

/// File hors ligne persistée au coffre sécurisé (partage le `FlutterSecureStorage`
/// du socle M0). Feature 027.
final offlineQueueStoreProvider = Provider<OfflineQueueStore>((ref) {
  return OfflineQueueStore(ref.watch(secureStorageProvider));
});

/// Scanner caméra (substitué en test).
final scannerFacadeProvider = Provider<ScannerFacade>((ref) {
  return MobileScannerFacade();
});

/// Permission caméra (substituée en test).
final cameraPermissionProvider = Provider<CameraPermissionFacade>((ref) {
  return RealCameraPermissionFacade();
});

/// État/logique de l'écran Scanner.
final scanControllerProvider =
    NotifierProvider<ScanController, ScanState>(ScanController.new);

/// Store des avis de synchro (rejets/échecs), partage le coffre du socle M0.
final syncNoticeStoreProvider = Provider<SyncNoticeStore>((ref) {
  return SyncNoticeStore(ref.watch(secureStorageProvider));
});

/// Politique de backoff + plafond FR-013 (valeurs de conception configurables).
final backoffPolicyProvider =
    Provider<BackoffPolicy>((ref) => const BackoffPolicy());

/// Connectivité réseau (événementielle), substituée en test.
final connectivityFacadeProvider = Provider<ConnectivityFacade>((ref) {
  return RealConnectivityFacade();
});

/// Orchestrateur de la synchronisation hors ligne (US2/US3).
final syncControllerProvider =
    NotifierProvider<SyncController, SyncStatus>(SyncController.new);
