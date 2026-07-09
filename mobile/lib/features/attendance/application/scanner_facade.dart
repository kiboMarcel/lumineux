import 'package:flutter/widgets.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

/// Abstraction du scanner caméra — **substituable en test** (pas de vraie
/// caméra). Encapsule `mobile_scanner` (Principe I : dépendance externe derrière
/// un port).
abstract class ScannerFacade {
  /// Construit l'aperçu caméra ; `onCode` reçoit le contenu brut d'un QR détecté.
  Widget buildPreview({required void Function(String raw) onCode});

  Future<void> start();
  Future<void> stop();
  Future<void> dispose();
}

/// Implémentation réelle sur `mobile_scanner`.
class MobileScannerFacade implements ScannerFacade {
  MobileScannerFacade()
      : _controller = MobileScannerController(
          detectionSpeed: DetectionSpeed.noDuplicates,
          formats: const [BarcodeFormat.qrCode],
        );

  final MobileScannerController _controller;

  @override
  Widget buildPreview({required void Function(String raw) onCode}) {
    return MobileScanner(
      controller: _controller,
      onDetect: (capture) {
        for (final barcode in capture.barcodes) {
          final raw = barcode.rawValue;
          if (raw != null && raw.isNotEmpty) {
            onCode(raw);
            break;
          }
        }
      },
    );
  }

  @override
  Future<void> start() => _controller.start();

  @override
  Future<void> stop() => _controller.stop();

  @override
  Future<void> dispose() => _controller.dispose();
}
