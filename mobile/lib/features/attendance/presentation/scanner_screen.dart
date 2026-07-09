import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/widgets/lum_buttons.dart';
import '../application/camera_permission_facade.dart';
import '../application/providers.dart';
import '../application/scan_state.dart';
import '../application/scanner_facade.dart';
import 'scan_result_overlay.dart';
import 'sync_status_banner.dart';

/// Onglet Scanner : aperçu caméra + cadre de visée, détection du QR de séance,
/// overlay de résultat, gestion de la permission et du cycle de vie caméra.
class ScannerScreen extends ConsumerStatefulWidget {
  const ScannerScreen({super.key});

  @override
  ConsumerState<ScannerScreen> createState() => _ScannerScreenState();
}

class _ScannerScreenState extends ConsumerState<ScannerScreen>
    with WidgetsBindingObserver {
  // Capturés en initState : `ref` ne doit pas être lu dans dispose().
  late final ScannerFacade _facade;
  late final CameraPermissionFacade _permission;

  @override
  void initState() {
    super.initState();
    _facade = ref.read(scannerFacadeProvider);
    _permission = ref.read(cameraPermissionProvider);
    WidgetsBinding.instance.addObserver(this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _resolvePermission();
      // Déclencheur « lancement » : tenter de synchroniser les captures en
      // attente dès l'ouverture (FR-006). Instancie aussi le SyncController
      // (abonnement connectivité).
      ref.read(syncControllerProvider.notifier).syncNow();
    });
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _facade.dispose();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState lifecycle) {
    final status = ref.read(scanControllerProvider).status;
    if (lifecycle == AppLifecycleState.paused ||
        lifecycle == AppLifecycleState.inactive) {
      _facade.stop();
    } else if (lifecycle == AppLifecycleState.resumed) {
      if (status == ScanStatus.permissionDenied) {
        _resolvePermission();
      } else if (status == ScanStatus.scanning) {
        _facade.start();
      }
      // Déclencheur « reprise » : nouvelle tentative de synchro (FR-006).
      ref.read(syncControllerProvider.notifier).syncNow();
    }
  }

  Future<void> _resolvePermission() async {
    var granted = await _permission.isGranted();
    if (!granted) granted = await _permission.request();
    if (!mounted) return;
    ref.read(scanControllerProvider.notifier).onPermissionResolved(granted);
    if (granted) await _facade.start();
  }

  @override
  Widget build(BuildContext context) {
    // Indice transitoire « code non reconnu » : non bloquant, la caméra continue.
    ref.listen<ScanState>(scanControllerProvider, (previous, next) {
      if (next.status == ScanStatus.scanning && next.hint != null) {
        ScaffoldMessenger.of(context)
          ..hideCurrentSnackBar()
          ..showSnackBar(
            SnackBar(
              content: Text(next.hint!),
              duration: const Duration(seconds: 2),
            ),
          );
        ref.read(scanControllerProvider.notifier).clearHint();
      }
    });

    final scan = ref.watch(scanControllerProvider);

    return SafeArea(
      bottom: false,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Padding(
            padding: EdgeInsets.fromLTRB(20, 20, 20, 8),
            child: Text(
              'Scanner',
              style: TextStyle(
                  fontSize: 20,
                  fontWeight: FontWeight.w800,
                  color: AppColors.ink),
            ),
          ),
          // Indicateur de synchro hors ligne (FR-011) : masqué si rien en attente.
          const SyncStatusBanner(),
          Expanded(child: _body(scan)),
        ],
      ),
    );
  }

  Widget _body(ScanState scan) {
    switch (scan.status) {
      case ScanStatus.permissionUnknown:
        return const Center(child: CircularProgressIndicator());
      case ScanStatus.permissionDenied:
        return _deniedView();
      case ScanStatus.scanning:
      case ScanStatus.submitting:
      case ScanStatus.result:
        return _cameraView(scan);
    }
  }

  Widget _deniedView() {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.no_photography_outlined,
              size: 48, color: AppColors.ink3),
          const SizedBox(height: 16),
          const Text(
            'La caméra est nécessaire pour scanner le code QR de la séance.',
            key: Key('scanner-permission-denied'),
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 14, color: AppColors.ink2),
          ),
          const SizedBox(height: 24),
          LumOutlineButton(
            key: const Key('scanner-open-settings'),
            label: 'Ouvrir les réglages',
            icon: Icons.settings,
            onPressed: () => _permission.openSettings(),
          ),
        ],
      ),
    );
  }

  Widget _cameraView(ScanState scan) {
    return Stack(
      fit: StackFit.expand,
      children: [
        ColoredBox(
          color: AppColors.ink,
          child: _facade.buildPreview(
            onCode: (raw) =>
                ref.read(scanControllerProvider.notifier).onDetect(raw),
          ),
        ),
        const _Viewfinder(),
        const Positioned(
          left: 24,
          right: 24,
          bottom: 24,
          child: Text(
            'Placez le code QR de la séance dans le cadre',
            key: Key('scanner-hint'),
            textAlign: TextAlign.center,
            style: TextStyle(color: Colors.white, fontSize: 14),
          ),
        ),
        if (scan.status == ScanStatus.submitting)
          const Positioned.fill(
            child: ColoredBox(
              color: Color(0x66221F1A),
              child: Center(
                  child: CircularProgressIndicator(color: Colors.white)),
            ),
          ),
        if (scan.result != null)
          ScanResultOverlay(
            result: scan.result!,
            onDismiss: () =>
                ref.read(scanControllerProvider.notifier).dismissResult(),
          ),
      ],
    );
  }
}

/// Cadre de visée : 4 coins en L blancs (design template).
class _Viewfinder extends StatelessWidget {
  const _Viewfinder();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: SizedBox(
        width: 240,
        height: 240,
        child: Stack(
          children: const [
            _Corner(top: true, left: true),
            _Corner(top: true, left: false),
            _Corner(top: false, left: true),
            _Corner(top: false, left: false),
          ],
        ),
      ),
    );
  }
}

class _Corner extends StatelessWidget {
  const _Corner({required this.top, required this.left});

  final bool top;
  final bool left;

  @override
  Widget build(BuildContext context) {
    const side = BorderSide(color: Colors.white, width: 3);
    return Positioned(
      top: top ? 0 : null,
      bottom: top ? null : 0,
      left: left ? 0 : null,
      right: left ? null : 0,
      child: Container(
        width: 28,
        height: 28,
        decoration: BoxDecoration(
          border: Border(
            top: top ? side : BorderSide.none,
            bottom: top ? BorderSide.none : side,
            left: left ? side : BorderSide.none,
            right: left ? BorderSide.none : side,
          ),
        ),
      ),
    );
  }
}
