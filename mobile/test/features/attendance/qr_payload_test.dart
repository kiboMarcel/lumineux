import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/qr_payload.dart';

void main() {
  group('QrPayloadResult.parse', () {
    test('payload valide → sessionId + token', () {
      final result = QrPayloadResult.parse('{"v":1,"s":123,"t":"tok-abc"}');
      expect(result.isValid, isTrue);
      expect(result.payload!.sessionId, 123);
      expect(result.payload!.token, 'tok-abc');
    });

    test('version inconnue → non reconnu', () {
      expect(QrPayloadResult.parse('{"v":2,"s":1,"t":"x"}').isValid, isFalse);
    });

    test('JSON illisible → non reconnu', () {
      expect(QrPayloadResult.parse('pas-du-json').isValid, isFalse);
      expect(QrPayloadResult.parse('42').isValid, isFalse);
    });

    test('champ manquant / invalide → non reconnu', () {
      expect(QrPayloadResult.parse('{"v":1,"s":1}').isValid, isFalse); // t absent
      expect(QrPayloadResult.parse('{"v":1,"t":"x"}').isValid, isFalse); // s absent
      expect(QrPayloadResult.parse('{"v":1,"s":0,"t":"x"}').isValid, isFalse); // s<=0
      expect(QrPayloadResult.parse('{"v":1,"s":1,"t":""}').isValid, isFalse); // t vide
    });

    test('accepte un sessionId numérique', () {
      final raw = jsonEncode({'v': 1, 's': 77, 't': 'jeton'});
      final result = QrPayloadResult.parse(raw);
      expect(result.isValid, isTrue);
      expect(result.payload!.sessionId, 77);
    });
  });
}
