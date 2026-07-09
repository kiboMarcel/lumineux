import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth/application/providers.dart' show dioProvider;
import '../data/attendance_api.dart';
import 'camera_permission_facade.dart';
import 'scan_controller.dart';
import 'scan_state.dart';
import 'scanner_facade.dart';

/// Client de l'API de présence (scan), sur le socle `dio` du lot M0.
final attendanceApiProvider = Provider<AttendanceApi>((ref) {
  return AttendanceApi(ref.watch(dioProvider));
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
