import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/time/clock.dart';

class FixedClock implements Clock {
  FixedClock(this._now);
  DateTime _now;
  set now(DateTime value) => _now = value;
  @override
  DateTime utcNow() => _now;
}

void main() {
  test('SystemClock renvoie une heure UTC proche de maintenant', () {
    final before = DateTime.now().toUtc();
    final now = const SystemClock().utcNow();
    final after = DateTime.now().toUtc();

    expect(now.isUtc, isTrue);
    expect(now.isBefore(before), isFalse);
    expect(now.isAfter(after), isFalse);
  });

  test('FixedClock (double de test) renvoie une valeur déterministe', () {
    final clock = FixedClock(DateTime.utc(2026, 7, 9, 14));
    expect(clock.utcNow(), DateTime.utc(2026, 7, 9, 14));
    clock.now = DateTime.utc(2026, 7, 10);
    expect(clock.utcNow(), DateTime.utc(2026, 7, 10));
  });
}
