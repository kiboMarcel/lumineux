import 'dart:math';

import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/attendance/application/operation_id.dart';

void main() {
  test('généré non vide et ≤ 64 caractères (contrainte serveur FR-002)', () {
    final id = OperationId.generate();
    expect(id, isNotEmpty);
    expect(id.length, lessThanOrEqualTo(64));
  });

  test('format hexadécimal de 32 caractères', () {
    final id = OperationId.generate();
    expect(id.length, 32);
    expect(RegExp(r'^[0-9a-f]{32}$').hasMatch(id), isTrue);
  });

  test('unicité sur un grand nombre de tirages', () {
    final ids = <String>{};
    for (var i = 0; i < 10000; i++) {
      ids.add(OperationId.generate());
    }
    expect(ids.length, 10000);
  });

  test('accepte un Random injecté (déterminisme en test)', () {
    final a = OperationId.generate(Random(1));
    final b = OperationId.generate(Random(1));
    expect(a, b);
  });
}
