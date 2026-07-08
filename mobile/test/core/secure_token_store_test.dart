import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/core/storage/secure_token_store.dart';
import 'package:mocktail/mocktail.dart';

class MockSecureStorage extends Mock implements FlutterSecureStorage {}

void main() {
  setUpAll(() {
    registerFallbackValue(const AndroidOptions());
  });

  late MockSecureStorage storage;
  late SecureTokenStore store;

  setUp(() {
    storage = MockSecureStorage();
    store = SecureTokenStore(storage);
  });

  test('save écrit le jeton sérialisé dans le coffre', () async {
    String? written;
    when(() => storage.write(
          key: any(named: 'key'),
          value: any(named: 'value'),
          aOptions: any(named: 'aOptions'),
        )).thenAnswer((invocation) async {
      written = invocation.namedArguments[#value] as String?;
    });

    final token = AuthToken(
      value: 'secret-token',
      type: 'Bearer',
      expiresAt: DateTime.utc(2999, 1, 1),
    );
    await store.save(token);

    expect(written, isNotNull);
    expect(written, contains('secret-token'));
    expect(written, contains('2999'));
  });

  test('read reconstruit le jeton depuis le coffre', () async {
    when(() => storage.read(
          key: any(named: 'key'),
          aOptions: any(named: 'aOptions'),
        )).thenAnswer((_) async =>
        '{"value":"t","type":"Bearer","expiresAt":"2999-01-01T00:00:00.000Z"}');

    final token = await store.read();

    expect(token, isNotNull);
    expect(token!.value, 't');
    expect(token.isPotentiallyValid, isTrue);
  });

  test('read renvoie null quand le coffre est vide', () async {
    when(() => storage.read(
          key: any(named: 'key'),
          aOptions: any(named: 'aOptions'),
        )).thenAnswer((_) async => null);

    expect(await store.read(), isNull);
  });

  test('clear supprime le jeton', () async {
    when(() => storage.delete(
          key: any(named: 'key'),
          aOptions: any(named: 'aOptions'),
        )).thenAnswer((_) async {});

    await store.clear();

    verify(() => storage.delete(
          key: any(named: 'key'),
          aOptions: any(named: 'aOptions'),
        )).called(1);
  });
}
