import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:lumineux_mobile/features/home/presentation/home_shell.dart';
import 'package:mocktail/mocktail.dart';

import '../../support/harness.dart';

void main() {
  testWidgets('la coquille expose un 3e onglet « Scanner »', (tester) async {
    final store = MockTokenStore();
    when(() => store.clear()).thenAnswer((_) async {});
    // Fakes caméra/permission fournis par défaut par makeContainer.
    final container = makeContainer(api: MockAuthApi(), store: store);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(home: HomeShell()),
      ),
    );
    await tester.pumpAndSettle();

    // L'onglet existe ; son contenu est hors-scène avant sélection.
    expect(find.byKey(const Key('nav-scanner')), findsOneWidget);
    expect(find.byKey(const Key('scanner-hint')), findsNothing);

    await tester.tap(find.byKey(const Key('nav-scanner')));
    await tester.pumpAndSettle();

    // L'écran Scanner est affiché (caméra autorisée via le fake).
    expect(find.byKey(const Key('scanner-hint')), findsOneWidget);
  });
}
