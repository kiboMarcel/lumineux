---
description: "Task list — Capture hors ligne & synchronisation des présences (M2)"
---

# Tasks: Application mobile membre — capture hors ligne et synchronisation des présences

**Input**: Design documents from `specs/027-mobile-offline-sync/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — la Constitution (Principe III, NON-NÉGOCIABLE) impose des tests unitaires sur la logique
métier/applicative, écrits avant ou conjointement à l'implémentation (cycle rouge → vert). Les tests de la
logique (réconciliation, dédup, backoff, plafond FR-013, validation structurelle) sont donc obligatoires ;
widget/intégration complètent l'UX.

**Organization**: Tâches groupées par user story pour une implémentation et un test indépendants.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, sans dépendance sur une tâche incomplète)
- **[Story]** : US1 / US2 / US3 (voir spec.md) — absent en Setup / Foundational / Polish
- Chemins de fichiers **exacts** (racine `mobile/`)

## Path Conventions

Client Flutter existant sous `mobile/` : `mobile/lib/**` (source), `mobile/test/**` (unitaires + widget),
`mobile/integration_test/**` (parcours). Aucune modification serveur (`src/` .NET **inchangé**).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prérequis d'environnement et dépendances.

- [X] T001 Ajouter la dépendance `connectivity_plus` dans `mobile/pubspec.yaml` puis `flutter pub get` **(appel réseau — APPROBATION EXPLICITE REQUISE avant exécution, cf. plan.md D2)**. En cas de refus, appliquer le repli sans dépendance (déclencheurs lancement/reprise + backoff) et l'inscrire dans `research.md` D2.
- [X] T002 [P] Vérifier la présence de la config d'environnement `mobile/env/*.json` (apiRoot HTTPS, exception TLS dev) et qu'aucun test n'exige de réseau réel par défaut (parcours d'intégration `skip` sauf opt-in).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Primitives partagées par toutes les user stories (horloge, identifiant, modèle de file, store persistant). 

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [X] T003 [P] Écrire les tests unitaires du port horloge dans `mobile/test/core/time/clock_test.dart` (horloge réelle vs `FixedClock`/`FakeClock` de test ; `utcNow`).
- [X] T004 [P] Créer le port `Clock` (horloge substituable) dans `mobile/lib/core/time/clock.dart` (`SystemClock` réel + fabrique de test), rendant `utcNow` déterministe (research.md D10).
- [X] T005 [P] Écrire les tests unitaires du générateur d'identifiant dans `mobile/test/features/attendance/application/operation_id_test.dart` (non vide, **≤ 64** car., unicité sur N tirages, format hex).
- [X] T006 [P] Créer `OperationId` (aléatoire cryptographiquement sûr, 32 hex, `Random.secure`) dans `mobile/lib/features/attendance/application/operation_id.dart` (research.md D7, FR-002).
- [X] T007 [P] Écrire les tests du modèle de file dans `mobile/test/features/attendance/application/pending_capture_test.dart` (sérialisation JSON aller-retour ; états `PendingState` ; champs `attemptCount`/`firstCapturedAt`).
- [X] T008 Créer le modèle `PendingCapture` + enum `PendingState { pending, inProgress, transientFailed }` (sérialisation JSON, jeton marqué sensible) dans `mobile/lib/features/attendance/application/pending_capture.dart` (data-model.md §1/§3).
- [X] T009 Écrire les tests du store de file dans `mobile/test/features/attendance/data/offline_queue_store_test.dart` avec un `FlutterSecureStorage` mocké (mocktail) : `add`/`readAll`/`remove`/`update`, **persistance** relue, **dédup par `sessionId`** (au plus 1, FR-014), purge du jeton au retrait (FR-009).
- [X] T010 Créer `OfflineQueueStore` (JSON sérialisé dans `flutter_secure_storage`, écritures sérialisées, dédup séance à l'insertion) dans `mobile/lib/features/attendance/data/offline_queue_store.dart` (research.md D1, FR-003/FR-014).
- [X] T011 Ajouter le câblage DI de base (providers du `Clock`, `OperationId`, `OfflineQueueStore`) dans `mobile/lib/features/attendance/application/providers.dart` (extension du fichier M1 existant).

**Checkpoint**: Fondation prête — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Enregistrer ma présence même sans réseau (Priority: P1) 🎯 MVP

**Goal**: Sur scan hors réseau d'un QR valide, **capturer** localement (file persistante) et **confirmer**
« enregistrée hors ligne, à synchroniser » — aucune présence perdue, survit au redémarrage.

**Independent Test**: Couper le réseau, scanner un QR de séance valide → confirmation « enregistrée hors
ligne » (< 3 s), **sans** message d'échec ; fermer/relancer l'app → la capture est **toujours** en file.

### Tests for User Story 1 ⚠️ (écrire d'abord, doivent échouer)

- [X] T012 [P] [US1] Test unitaire du contrôleur de scan (chemin hors ligne) dans `mobile/test/features/attendance/application/scan_controller_offline_test.dart` : sur `ApiErrorType.network`, la capture est **mise en file** (opId généré, heure de scan, sessionId, jeton) et l'état passe à un **résultat « hors ligne »** ; les autres chemins M1 (201/200, autres erreurs, 401) restent **inchangés** (FR-004).
- [X] T013 [P] [US1] Test unitaire de **dédup** dans `mobile/test/features/attendance/application/scan_controller_dedup_test.dart` : re-scan hors ligne d'une séance déjà en file → **ignoré** (1 seule entrée), retour « déjà capturée hors ligne » (FR-014).
- [X] T014 [P] [US1] Test unitaire de **validation structurelle** dans `mobile/test/features/attendance/application/scan_controller_structural_test.dart` : un QR sans `s`/`t` (payload non reconnu) hors réseau n'est **jamais** mis en file (FR-001a).
- [X] T015 [P] [US1] Test widget de l'overlay dans `mobile/test/features/attendance/presentation/scan_result_overlay_offline_test.dart` : variante « Enregistrée hors ligne » (titre/sous-titre, ton neutre, actions Fermer/Scanner à nouveau), le jeton n'apparaît nulle part (SC-005).

### Implementation for User Story 1

- [X] T016 [US1] Ajouter `ScanResultKind.offlineQueued` et la fabrique correspondante (« Enregistrée hors ligne » / « À synchroniser dès le retour du réseau » ; variante « Déjà capturée hors ligne ») dans `mobile/lib/features/attendance/application/scan_state.dart`.
- [X] T017 [US1] Modifier `_submit`/`onDetect` pour, **uniquement** sur `ApiErrorType.network`, appeler la capture hors ligne au lieu d'afficher une erreur, en préservant tous les autres comportements M1, dans `mobile/lib/features/attendance/application/scan_controller.dart` (research.md D9, FR-001/FR-004).
- [X] T018 [US1] Implémenter la logique de capture (générer opId immuable, horodater via `Clock`, construire `PendingCapture`, insérer via `OfflineQueueStore` avec dédup séance) dans `mobile/lib/features/attendance/application/scan_controller.dart` (FR-001/FR-002/FR-014).
- [X] T019 [US1] Rendre la variante hors ligne dans l'overlay de résultat dans `mobile/lib/features/attendance/presentation/scan_result_overlay.dart` (contracts/offline-sync-ui.md §1).
- [X] T020 [US1] Journalisation **minimale sans secret** de la capture (compteur/issue, **jamais** le jeton) au chemin hors ligne du contrôleur (Principe VI, FR-009).

**Checkpoint**: US1 pleinement fonctionnelle et testable seule — MVP démontrable (capture + persistance).

---

## Phase 4: User Story 2 — Synchroniser automatiquement au retour du réseau (Priority: P2)

**Goal**: Envoyer les captures en file par **lot groupé par séance** via `/scan/batch`, **réconcilier** les
issues (retirer succès/rejets, conserver les transitoires), avec **idempotence** et déclenchement
automatique (connectivité, lancement/reprise) + **backoff** borné par le plafond FR-013.

**Independent Test**: Avec des captures en file, rétablir le réseau → envoi par lot, retrait selon l'issue,
re-synchro **sans doublon** ; erreur transitoire → éléments non traités **restent** en file.

### Tests for User Story 2 ⚠️ (écrire d'abord, doivent échouer)

- [X] T021 [P] [US2] Tests unitaires des DTO dans `mobile/test/features/attendance/data/offline_scan_dtos_test.dart` : sérialisation `OfflineScanBatchRequest` et parsing `OfflineScanBatchResponse`/`OfflineScanResult` (issues `Created`/`AlreadyPresent`/`Rejected`, `reason`, `attendanceId`) conformes au contrat.
- [X] T022 [P] [US2] Tests unitaires de la **politique de backoff/plafond** dans `mobile/test/features/attendance/application/backoff_policy_test.dart` : délais croissants (2 s ×2, cap, jitter borné) ; **échec définitif** quand `attemptCount ≥ maxAttempts` **ou** âge `≥ maxAge` (FR-013).
- [X] T023 [P] [US2] Tests unitaires de **réconciliation** dans `mobile/test/features/attendance/application/sync_controller_reconcile_test.dart` (API mockée) : `Created`/`AlreadyPresent` → retirés ; `Rejected` → retiré + `SyncNotice` ; réseau/5xx → conservé + `attemptCount++` ; **401** → conservé sans incrément (research.md D5, FR-007).
- [X] T024 [P] [US2] Test unitaire d'**idempotence** dans `mobile/test/features/attendance/application/sync_controller_idempotency_test.dart` : ré-envoi d'un opId déjà traité (mock `AlreadyPresent`) → **aucun doublon**, élément retiré (FR-008, SC-003).
- [X] T025 [P] [US2] Test unitaire de **groupement par séance** dans `mobile/test/features/attendance/application/sync_controller_grouping_test.dart` : captures multi-séances → **un POST par séance** vers `.../{sessionId}/scan/batch` (FR-005, D8).
- [X] T026 [P] [US2] Test unitaire des **déclencheurs** dans `mobile/test/features/attendance/application/sync_controller_triggers_test.dart` : signal de connectivité (facade fake) et reprise d'app déclenchent une synchro ; file vide → aucun envoi (FR-006).
- [X] T027 [P] [US2] Test d'intégration (skip par défaut) du parcours capture→synchro dans `mobile/integration_test/offline_sync_test.dart` (quickstart Scénario B).

### Implementation for User Story 2

- [X] T028 [P] [US2] Créer les DTO miroir `OfflineScanItem`/`OfflineScanBatchRequest`/`OfflineScanResult`/`OfflineScanBatchResponse` + constantes `Created`/`AlreadyPresent`/`Rejected` dans `mobile/lib/features/attendance/data/offline_scan_dtos.dart` (contracts/batch-sync-api-consumption.md).
- [X] T029 [US2] Étendre `AttendanceApi` avec `syncBatch(int sessionId, List<OfflineScanItem>) → OfflineScanBatchResponse` (`POST /attendance-sessions/{id}/scan/batch`, mapping d'erreurs via `mapDioException`) dans `mobile/lib/features/attendance/data/attendance_api.dart` (FR-005).
- [X] T030 [P] [US2] Créer `BackoffPolicy` — backoff exponentiel (initial 2 s, ×2, cap 5 min, jitter ±20 %) et plafond `maxAttempts = 8` **ou** `maxAge = 7 j` (constantes configurables) → échec définitif — dans `mobile/lib/features/attendance/application/backoff_policy.dart` (research.md D3/D4, FR-013).
- [X] T031 [P] [US2] Créer `ConnectivityFacade` (port + implémentation `connectivity_plus`, substituable en test) dans `mobile/lib/features/attendance/application/connectivity_facade.dart` (research.md D2, FR-006). **Bloqué par la décision T001** : si l'ajout de `connectivity_plus` est refusé, implémenter le repli (déclencheurs lancement/reprise + backoff seul) — le port reste identique, mais **SC-002 (<30 s) devient « meilleur effort »** et doit être re-noté dans `research.md` D2.
- [X] T032 [US2] Créer `SyncNoticeStore` (avis persistés **sans jeton** : rejet/échec définitif) dans `mobile/lib/features/attendance/data/sync_notice_store.dart` (data-model.md §2, D6).
- [X] T033 [US2] Implémenter `SyncController` : lire la file, **grouper par séance**, appeler `syncBatch`, **réconcilier** chaque `result` (retrait / `SyncNotice` / conservation), gérer `attemptCount`, plafond FR-013, 401 (conservation), dans `mobile/lib/features/attendance/application/sync_controller.dart` (FR-007/FR-008, D5).
- [X] T034 [US2] Câbler les **déclencheurs** : abonnement `ConnectivityFacade` (réinitialise le backoff + synchro), `WidgetsBindingObserver` (lancement/reprise), et la boucle de **backoff** tant que l'app est active et la file non vide, dans `mobile/lib/features/attendance/application/sync_controller.dart` (FR-006, D3).
- [X] T035 [US2] Étendre le câblage DI (providers `attendanceApi.syncBatch`, `backoffPolicy`, `connectivityFacade`, `syncNoticeStore`, `syncController`) dans `mobile/lib/features/attendance/application/providers.dart`.
- [X] T036 [US2] Journalisation minimale sans secret des cycles de synchro (compteurs, issues, tentatives) — **jamais** le jeton (Principe VI, FR-009).

**Checkpoint**: US1 + US2 fonctionnent indépendamment ; les captures se synchronisent et se retirent sans doublon.

---

## Phase 5: User Story 3 — Suivre l'état de synchronisation et les rejets (Priority: P3)

**Goal**: Afficher les compteurs (en attente / en cours / rejetés), présenter chaque **rejet/échec avec sa
raison** (acquittable), et permettre une **relance manuelle**.

**Independent Test**: Capturer plusieurs scans hors ligne → l'indicateur montre le **nombre en attente** ;
après synchro, un rejet est **signalé avec raison** et **retiré** ; un bouton **relance** la synchro.

### Tests for User Story 3 ⚠️ (écrire d'abord, doivent échouer)

- [X] T037 [P] [US3] Test unitaire de l'agrégat `SyncStatus` dans `mobile/test/features/attendance/application/sync_state_test.dart` : `pendingCount`/`inProgressCount`/`unacknowledgedNotices`/`lastSync*` dérivés des deux stores (FR-011, SC-006).
- [X] T038 [P] [US3] Test widget du bandeau dans `mobile/test/features/attendance/presentation/sync_status_banner_test.dart` : affichage des compteurs, liste d'avis avec **raison**, bouton **Réessayer** (déclenche la relance), **aucun** jeton affiché.
- [X] T039 [P] [US3] Test unitaire d'**acquittement** d'avis dans `mobile/test/features/attendance/application/sync_notice_ack_test.dart` : marquer un `SyncNotice` `acknowledged=true` le retire de la liste active et **persiste** (SC-004).

### Implementation for User Story 3

- [X] T040 [US3] Créer l'agrégat `SyncStatus` (+ `lastSyncOutcome`) et son exposition (provider dérivé) dans `mobile/lib/features/attendance/application/sync_state.dart` (data-model.md §4).
- [X] T041 [US3] Ajouter l'action `retryNow()` (relance manuelle) et `acknowledgeNotice(id)` au `SyncController` dans `mobile/lib/features/attendance/application/sync_controller.dart` (FR-006, SC-004).
- [X] T042 [US3] Créer le widget `SyncStatusBanner` (compteurs, avis de rejet/échec avec raison + « J'ai compris », bouton « Réessayer », horodatage) dans `mobile/lib/features/attendance/presentation/sync_status_banner.dart` (contracts/offline-sync-ui.md §2/§3).
- [X] T043 [US3] Intégrer le bandeau dans l'écran Scanner (et surface d'accueil membre) dans `mobile/lib/features/attendance/presentation/scanner_screen.dart` (FR-011).

**Checkpoint**: Les trois user stories sont indépendamment fonctionnelles.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Qualité transverse et validation finale.

- [X] T044 [P] Audit sécurité des nouveaux chemins (hors ligne/synchro) : (a) aucun `print`/log ne contient le jeton (SC-005) ; (b) **tous** les appels réseau passent par le client `dio` du socle M0 (base HTTPS, aucun `HttpClient`/URL `http://` ad hoc) — FR-010, Principe IV/VI.
- [X] T045 [P] Localisation FR complète des nouveaux libellés (capture, en cours, terminée, rejet) — FR-012 ; cibles tactiles ≥ 44 dp.
- [X] T046 `flutter analyze` sans erreur puis `flutter test` vert (unitaires + widget) dans `mobile/`.
- [ ] T047 Exécuter la validation `specs/027-mobile-offline-sync/quickstart.md` (Scénarios A→E) sur émulateur/device.
- [X] T048 [P] Mettre à jour `specs/027-mobile-offline-sync/spec.md` (Status → Ready) et vérifier la cohérence FR ↔ tâches ; consigner les valeurs finales `maxAttempts`/`maxAge`/backoff dans `research.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance — démarrage immédiat (T001 sous approbation).
- **Foundational (Phase 2)** : dépend du Setup — **BLOQUE** toutes les user stories.
- **User Stories (Phase 3+)** : dépendent de la Fondation.
  - US1 (P1) indépendante. US2 (P2) consomme la file produite par US1 mais est testable seule (file pré-remplie via store). US3 (P3) consomme les stores US1/US2 mais testable seule (données injectées).
  - **Gate connectivité** : la décision T001 (`connectivity_plus`, approbation réseau) doit être tranchée **avant de démarrer US2 (T031/T034)**. Un refus bascule sur le repli sans dépendance (impact SC-002, cf. F4).
- **Polish (Phase 6)** : après les user stories visées.

### User Story Dependencies

- **US1 (P1)** : après Fondation. Aucune dépendance sur les autres stories.
- **US2 (P2)** : après Fondation. Réutilise `OfflineQueueStore`/`PendingCapture` ; testable indépendamment avec une file pré-remplie.
- **US3 (P3)** : après Fondation. Réutilise `OfflineQueueStore`/`SyncNoticeStore`/`SyncController` ; testable avec des stores injectés.

### Within Each User Story

- Tests écrits d'abord et **en échec** avant implémentation (Principe III).
- Modèles avant services ; services avant UI ; logique avant intégration.

### Parallel Opportunities

- Phase 2 : T003/T004 (clock), T005/T006 (opId), T007 sont [P] ; T008→T011 s'enchaînent (mêmes fichiers/DI).
- US1 : tests T012–T015 [P] ensemble ; implémentations T016–T020 majoritairement séquentielles (même `scan_controller.dart`).
- US2 : tests T021–T027 [P] ensemble ; T028/T030/T031 [P] (fichiers distincts) avant T033/T034 (même `sync_controller.dart`, séquentiels).
- US3 : tests T037–T039 [P] ensemble ; T040/T042 [P] avant intégration T043.
- Une fois la Fondation faite, US1/US2/US3 peuvent être menées **en parallèle** par des développeurs distincts.

---

## Parallel Example: User Story 2

```bash
# Lancer d'abord tous les tests US2 (rouge) :
Task: "T023 réconciliation dans sync_controller_reconcile_test.dart"
Task: "T024 idempotence dans sync_controller_idempotency_test.dart"
Task: "T025 groupement par séance dans sync_controller_grouping_test.dart"
Task: "T022 backoff/plafond dans backoff_policy_test.dart"

# Puis les briques indépendantes en parallèle :
Task: "T028 DTO miroir dans offline_scan_dtos.dart"
Task: "T030 BackoffPolicy dans backoff_policy.dart"
Task: "T031 ConnectivityFacade dans connectivity_facade.dart"
```

---

## Implementation Strategy

### MVP First (User Story 1 uniquement)

1. Phase 1 (Setup) → 2. Phase 2 (Foundational, CRITIQUE) → 3. Phase 3 (US1).
4. **STOP & VALIDATE** : capture hors ligne confirmée + survie au redémarrage (Scénario A). MVP démontrable.

### Incremental Delivery

1. Setup + Foundational → fondation prête.
2. US1 → capture/persistance (MVP) → démo.
3. US2 → synchronisation automatique sans doublon → démo.
4. US3 → transparence (compteurs, rejets, relance) → démo.

### Parallel Team Strategy

Après la Fondation : Dév A → US1, Dév B → US2, Dév C → US3 ; intégration indépendante via les stores/ports partagés.

---

## Notes

- [P] = fichiers différents, sans dépendance.
- `src/` (.NET) et la base **inchangés** : aucune tâche serveur (contrat `/scan/batch` déjà livré).
- **Sécurité** : jeton au coffre, jamais journalisé/affiché, purgé à l'issue définitive (FR-009/SC-005).
- **Approbation** requise pour T001 (`connectivity_plus`, appel réseau) ; repli documenté si refus.
- Commit après chaque tâche ou groupe logique ; s'arrêter à chaque checkpoint pour valider la story.
