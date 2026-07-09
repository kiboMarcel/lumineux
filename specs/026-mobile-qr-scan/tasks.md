# Tasks: Application mobile membre — scan de présence par QR

**Input**: Design documents from `/specs/026-mobile-qr-scan/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/{qr-payload, scan-api-consumption, navigation}.md, quickstart.md

**Tests**: INCLUS — le Principe III de la constitution (« Tests en premier ») est **NON-NÉGOCIABLE**. Les tâches de test précèdent leur implémentation et doivent **échouer** avant d'être satisfaites.

**Organization**: Tâches groupées par user story (P1→P3). Racine du client : `mobile/` ; prérequis transverse sur `web/`.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- **[Story]** : user story rattachée (US1–US3)
- Chemins de fichiers exacts inclus dans chaque description

## Path Conventions

Nouveau module `mobile/lib/features/attendance/` (data / application / presentation) réutilisant le socle réseau/session (M0) et le design system. Petite modification du socle (`core/network/dio_client.dart`) et un prérequis SPA (`web/…/qr-panel`). L'API `.NET` sous `src/` est **inchangée**.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Dépendances de scan et configuration plateforme caméra.

> ⚠️ **Prérequis outillage** : T001 exécute `flutter pub add mobile_scanner permission_handler` (**appel réseau** → **approbation explicite requise**, cf. plan.md).

- [X] T001 Ajouter `mobile_scanner` et `permission_handler` à `mobile/pubspec.yaml` puis `flutter pub get`
- [X] T002 [P] Déclarer la permission caméra `<uses-permission android:name="android.permission.CAMERA"/>` dans `mobile/android/app/src/main/AndroidManifest.xml`
- [X] T003 [P] Déclarer `NSCameraUsageDescription` (texte FR : scanner le QR de séance) dans `mobile/ios/Runner/Info.plist`

**Checkpoint**: Packages installés, permissions déclarées ; le projet compile.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Socle réseau (attache du jeton, mapping d'erreurs), DTO, parsing du payload QR, client API et squelette du contrôleur — partagés par toutes les user stories.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

### Tests du socle (écrire d'abord — doivent échouer)

- [X] T004 [P] Test unitaire `QrPayload.parse` (valide / version inconnue / JSON illisible / champ manquant) dans `mobile/test/features/attendance/qr_payload_test.dart`
- [X] T005 [P] Test unitaire d'`mapDioException` pour les statuts de scan (410→`gone`, 409→`conflict`, 404→`notFound`, `detail` remonté) — étendre `mobile/test/core/dio_client_test.dart`
- [X] T006 [P] Test unitaire de l'intercepteur Bearer en **liste de refus** (jeton attaché à `/attendance-sessions/…/scan` et à `/auth/me`, **absent** de `/auth/login|activate|forgot-password|reset-password`) — étendre `mobile/test/core/dio_client_test.dart`
- [X] T007 [P] Test unitaire `AttendanceApi.scan` (`dio` moqué : **201** créée vs **200** déjà présente, corps `{token}`, Bearer attaché) dans `mobile/test/features/attendance/attendance_api_test.dart`

### Implémentation du socle

- [X] T008 Étendre `ApiErrorType` avec `gone` / `conflict` / `notFound` dans `mobile/lib/core/network/api_exception.dart`
- [X] T009 Étendre `mapDioException` : 410→`gone`, 409→`conflict`, 404→`notFound` (conserver `detail`/`title`) dans `mobile/lib/core/network/dio_client.dart` (dépend de T008)
- [X] T010 Inverser l'intercepteur **Bearer** en **liste de refus** des routes anonymes (`/auth/login|activate|forgot-password|reset-password`) → jeton attaché partout ailleurs (scan inclus) dans `mobile/lib/core/network/dio_client.dart`
- [X] T011 Étendre `error_messages` : messages pour `gone`/`conflict`/`notFound` (afficher le `detail` serveur, contexte scan) dans `mobile/lib/core/errors/error_messages.dart` (dépend de T008)
- [X] T012 [P] DTO de scan `AttendanceResponse` (miroir) + `ScanOutcome(created)` dans `mobile/lib/features/attendance/data/scan_dtos.dart`
- [X] T013 [P] `QrPayload` + `QrPayloadResult` (parse/valide `{"v":1,"s","t"}`, rejet version inconnue/structure invalide) dans `mobile/lib/features/attendance/application/qr_payload.dart`
- [X] T014 `AttendanceApi.scan(sessionId, token)` → `POST /attendance-sessions/{id}/scan` (201 vs 200 via statusCode) dans `mobile/lib/features/attendance/data/attendance_api.dart` (dépend de T009, T012)
- [X] T015 [P] Modèles d'état `ScanState` (permissionUnknown/denied/scanning/submitting/result) + `ScanResultView` dans `mobile/lib/features/attendance/application/scan_state.dart`
- [X] T016 Providers (`attendanceApiProvider`, `scanControllerProvider`) dans `mobile/lib/features/attendance/application/providers.dart` (dépend de T014)
- [X] T017 `ScanController` **squelette** (machine à états, permission/scanning, sans soumission) dans `mobile/lib/features/attendance/application/scan_controller.dart` (dépend de T015, T016)

**Checkpoint**: Le socle réseau attache le jeton au scan, mappe les erreurs, et le module `attendance/` parse le QR et sait appeler l'API ; les écrans peuvent être ajoutés.

---

## Phase 3: Prérequis transverse — Payload QR côté console web (SPA)

**Purpose**: Faire encoder au QR du bureau le **payload JSON versionné** attendu par le mobile. Indépendant du code mobile ; nécessaire à la validation de bout en bout (US1).

- [X] T018 Encoder le payload dans `web/src/app/features/attendance/session-run/qr-panel/qr-panel.component.ts` : `this.qrData.set(JSON.stringify({ v: 1, s: this.sessionId(), t: res.token }))` (au lieu de `res.token`) ; jeton toujours non affiché
- [X] T019 Mettre à jour le test unitaire `web/src/app/features/attendance/session-run/qr-panel/qr-panel.component.spec.ts` : `qrdata` = JSON `{"v":1,"s":…,"t":…}`

**Checkpoint**: Le bureau projette un QR conforme au contrat `contracts/qr-payload.md`.

---

## Phase 4: User Story 1 - Enregistrer ma présence en scannant le QR (Priority: P1) 🎯 MVP

**Goal**: Ouvrir l'onglet Scanner, viser un QR de séance valide → présence enregistrée, overlay de succès (nom + heure) ; re-scan de la même séance → « déjà présente », sans doublon.

**Independent Test**: viser un QR valide → overlay « Présence enregistrée » (nom + heure) ; fermer → re-viser le même QR → « Déjà enregistrée » sans doublon.

### Tests pour US1 (écrire d'abord — doivent échouer) ⚠️

- [X] T020 [P] [US1] Test unitaire `ScanController.submit` (payload valide → **201** succès créé ; **200** déjà présente ; garde **anti double-soumission** : détection suspendue en `submitting`) dans `mobile/test/features/attendance/scan_controller_submit_test.dart`
- [X] T021 [P] [US1] Test widget écran Scanner : overlay de **succès** (nom + heure), bouton **« Fermer »** → reprise du scan (scanner **abstrait**, pas de vraie caméra) dans `mobile/test/features/attendance/scanner_screen_test.dart`
- [X] T022 [P] [US1] Test widget `HomeShell` : présence du **3e onglet « Scanner »** dans `mobile/test/features/home/home_shell_scanner_tab_test.dart`
- [X] T023 [US1] Test d'intégration ouverture Scanner → détection simulée → overlay succès → re-scan dans `mobile/integration_test/scan_test.dart` (skip par défaut : caméra + API requis)

### Implémentation pour US1

- [X] T024 [US1] Abstraction du scanner (interface substituable enveloppant `MobileScannerController` : start/stop/onDetect) dans `mobile/lib/features/attendance/application/scanner_facade.dart`
- [X] T025 [US1] `ScanController.submit(QrPayload)` → `AttendanceApi.scan`, positionne `result` (créé 201 / déjà présent 200), **arrête** la détection dans `mobile/lib/features/attendance/application/scan_controller.dart`
- [X] T026 [P] [US1] `ScanResultOverlay` — variantes **succès** / **déjà présente** (icône check, `nom · heure`, avec **repli heure seule si `memberFullName` est nul**, « Fermer » / « Scanner à nouveau ») dans `mobile/lib/features/attendance/presentation/scan_result_overlay.dart`
- [X] T027 [US1] `ScannerScreen` — aperçu caméra + **cadre de visée**, `onDetect` → `QrPayload.parse` → `submit` ; overlay via `ScanResultOverlay` dans `mobile/lib/features/attendance/presentation/scanner_screen.dart` (dépend de T024, T025, T026)
- [X] T028 [US1] Ajouter le **3e onglet « Scanner »** (icône `qr_code_scanner`) à l'`IndexedStack` + barre de nav dans `mobile/lib/features/home/presentation/home_shell.dart`

**Checkpoint**: MVP démontrable — scan d'un QR valide → présence enregistrée + confirmation ; re-scan → « déjà présente ».

---

## Phase 5: User Story 2 - Comprendre et surmonter un échec de scan (Priority: P2)

**Goal**: Messages clairs et distincts pour chaque échec (jeton expiré 410, séance close 409, introuvable 404, membre 403, session expirée 401→connexion, réseau, code non reconnu) avec possibilité de re-scanner.

**Independent Test**: provoquer chaque cas → message distinct approprié, sans faux succès ; 401 → retour connexion ; code étranger → « non reconnu », la caméra continue.

### Tests pour US2 (écrire d'abord — doivent échouer) ⚠️

- [X] T029 [P] [US2] Test unitaire `ScanController` — mapping d'erreurs (410/409/404/403 → `result` erreur avec message ; **401 → purge session** ; réseau → `result` erreur ; **payload non reconnu → indice transitoire non bloquant, détection maintenue, AUCUN appel API et pas de `result`**) dans `mobile/test/features/attendance/scan_controller_errors_test.dart`
- [X] T030 [P] [US2] Test widget écran Scanner : overlay d'**erreur serveur/réseau** (messages mappés 410/409/réseau, bouton **« Scanner à nouveau »** → reprise) **et** cas **« code non reconnu » = indice transitoire** (pas d'overlay, la caméra **continue**) dans `mobile/test/features/attendance/scanner_screen_errors_test.dart`

### Implémentation pour US2

- [X] T031 [US2] `ScanController` — `onDetect` d'un **payload non reconnu** → **indice transitoire non bloquant** « Code non reconnu » (**aucun appel API**, la détection **reste active**, temporisation anti-répétition sur le même code) ; mapper `ApiException` (gone/conflict/notFound/forbidden/network) → `ScanResultView` (overlay) ; **401** délègue la purge au socle dans `mobile/lib/features/attendance/application/scan_controller.dart`
- [X] T032 [US2] `ScannerScreen` + `ScanResultOverlay` — variante **erreur** de l'overlay (titre + message mappé, aller-retour API) **et** rendu de l'**indice transitoire** « Code non reconnu » (bandeau/snackbar non bloquant) dans `mobile/lib/features/attendance/presentation/scan_result_overlay.dart` et `.../scanner_screen.dart`

**Checkpoint**: US1 + US2 fonctionnelles ; tous les échecs de scan donnent un retour clair et une reprise.

---

## Phase 6: User Story 3 - Autoriser et utiliser la caméra (Priority: P3)

**Goal**: Demander l'accès caméra ; en cas de refus, message d'orientation + « Ouvrir les réglages » ; libération/réactivation de la caméra au cycle de vie.

**Independent Test**: refuser l'accès → message + réglages, Accueil/Profil restent accessibles ; accorder → aperçu ; arrière-plan → caméra libérée, retour → réactivée.

### Tests pour US3 (écrire d'abord — doivent échouer) ⚠️

- [X] T033 [P] [US3] Test widget parcours permission (statut **refusé** → message + bouton « Ouvrir les réglages » ; **accordé** → aperçu) via la **façade de permission substituée** dans `mobile/test/features/attendance/scanner_permission_test.dart`

### Implémentation pour US3

- [X] T034 [US3] **Façade de permission caméra** substituable en test — interface (`status()`/`request()`/`openSettings()`) enveloppant `permission_handler`, exposée en provider (symétrique de `scanner_facade`) dans `mobile/lib/features/attendance/application/camera_permission_facade.dart`
- [X] T035 [US3] Gestion de la permission caméra via la **façade** (statut/demande, état `permissionDenied` + « Ouvrir les réglages » → `openSettings`) dans `mobile/lib/features/attendance/presentation/scanner_screen.dart` et `scan_controller.dart` (dépend de T034)
- [X] T036 [US3] Cycle de vie caméra : libérer en arrière-plan / réactiver au retour (`WidgetsBindingObserver`) dans `mobile/lib/features/attendance/presentation/scanner_screen.dart`

**Checkpoint**: US1–US3 fonctionnelles indépendamment.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Qualité, sécurité et validation transverses.

- [X] T037 [P] `flutter analyze` (mobile) — zéro avertissement
- [X] T038 Vérification sécurité : **jeton et payload jamais** affichés/journalisés (auditer `scan_controller`, `attendance_api`, `qr_payload`) ; le jeton n'est pas persisté (SC-003)
- [X] T039 Exécuter la validation `quickstart.md` de bout en bout (appareil + API + SPA à jour) : US1→US3
- [X] T040 [P] Mettre à jour `mobile/README.md` (onglet Scanner, permissions caméra, packages `mobile_scanner`/`permission_handler`)
- [X] T041 Confirmer la suite verte : `flutter test` + `flutter analyze` (mobile) et `npm test` (SPA, `qr-panel`)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : après installation des packages (réseau).
- **Foundational (Phase 2)** : dépend de Setup — **BLOQUE** les user stories.
- **Prérequis SPA (Phase 3)** : indépendant du code mobile ; peut se faire **en parallèle** de Phase 2/4 ; requis pour la validation e2e d'US1.
- **User Stories (Phase 4–6)** : dépendent de Foundational. Ordre P1→P3 recommandé.
- **Polish (Phase 7)** : après les user stories souhaitées.

### User Story Dependencies

- **US1 (P1)** : après Foundational. MVP autonome (chemin de succès + nav Scanner).
- **US2 (P2)** : après Foundational ; réutilise l'écran/overlay d'US1 pour les cas d'erreur, mais testable indépendamment (mapping + overlay erreur).
- **US3 (P3)** : après Foundational ; parcours permission autonome, en amont de l'aperçu.

### Fichiers partagés (édités séquentiellement)

- `core/network/dio_client.dart` : T009 → T010 (socle).
- `scan_controller.dart` : T017 (socle) → T025 (US1) → T031 (US2) → T035 (US3).
- `scanner_screen.dart` : T027 (US1) → T032 (US2, indice transitoire) → T035 (US3) → T036 (US3).
- `scan_result_overlay.dart` : T026 (US1) → T032 (US2).
- `camera_permission_facade.dart` : T034 (US3, nouvelle abstraction) → utilisée par T035.
- `home_shell.dart` : T028 (US1).

### Parallel Opportunities

- **Setup** : T002, T003 en parallèle après T001.
- **Foundational tests** : T004–T007 en parallèle ; impl : T012/T013/T015 en parallèle, puis T008→T009→T011, T010, T014→T016→T017 en chaîne.
- **Prérequis SPA (T018–T019)** en parallèle du travail mobile.
- **Par story** : les tests [P] en parallèle ; les fichiers partagés (`scan_controller`, `scanner_screen`, `scan_result_overlay`) restent séquentiels.

---

## Parallel Example: User Story 1

```bash
# Tests US1 en parallèle (écrire d'abord, doivent échouer) :
Task: "ScanController.submit succès dans mobile/test/features/attendance/scan_controller_submit_test.dart"
Task: "Écran Scanner overlay succès dans mobile/test/features/attendance/scanner_screen_test.dart"
Task: "3e onglet Scanner dans mobile/test/features/home/home_shell_scanner_tab_test.dart"

# Après T024/T025, écrans en parallèle :
Task: "ScanResultOverlay dans mobile/lib/features/attendance/presentation/scan_result_overlay.dart"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 : Setup (packages + permissions).
2. Phase 2 : Foundational (socle Bearer/erreurs, payload, API, contrôleur) — **CRITIQUE**.
3. Phase 3 : Prérequis SPA (QR JSON) — en parallèle.
4. Phase 4 : US1 (scan → présence + overlay + onglet).
5. **STOP & VALIDER** : tester US1 (quickstart US1) sur appareil.
6. Démo MVP (la boucle de présence est fermée).

### Incremental Delivery

1. Setup + Foundational + Prérequis SPA → socle prêt.
2. + US1 → tester → démo (MVP).
3. + US2 (échecs clairs) → tester → démo.
4. + US3 (permission caméra) → tester → démo.
5. Polish (analyze, sécurité, quickstart, README, CI verte).

### Notes

- [P] = fichiers différents, aucune dépendance en attente.
- Écrire les tests d'une story et les voir **échouer** avant implémentation (Principe III).
- Commit après chaque tâche ou groupe logique.
- **Jeton/payload jamais** affichés, journalisés ni persistés ; **serveur autorité** (aucune règle métier client).
- Aucune évolution d'API ni de base.
