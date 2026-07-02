---
description: "Task list — Gestion de la présence aux réunions"
---

# Tasks: Gestion de la présence aux réunions

**Input**: Design documents from `specs/001-attendance-management/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUS — imposés par la Constitution Lumineux v1.0.0, Principe III (Tests en premier,
NON-NÉGOCIABLE). Les couches Domain/Application sont couvertes par des tests unitaires ; les couches
Infrastructure/API par des tests d'intégration.

**Organization**: Tâches regroupées par user story (US1→US4) pour une implémentation et une
validation indépendantes. Architecture Onion (Domain / Application / Infrastructure / Api).

**État d'avancement** : Phases 1–2 (socle) + Phase 3 (US1) + Phase 4 (US2) + Phase 5 (US3) + Phase 6 (US4) + Phase 7 (Polish) **implémentées et vérifiées**
(build .NET 10 OK, **67 tests verts**, migrations EF `InitialAttendance` + `AddAttendances`) le 2026-07-02.
**Toutes les phases (T001-T074) sont terminées.**

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US4 (uniquement pour les phases de user story)
- Chaque tâche indique le chemin de fichier exact

## Path Conventions

Solution .NET en Onion : `src/Lumineux.Domain`, `src/Lumineux.Application`,
`src/Lumineux.Infrastructure`, `src/Lumineux.Api` ; tests sous `tests/Lumineux.*.Tests`.

---

## Phase 1: Setup (Infrastructure partagée)

**Purpose**: Initialisation de la solution et de la structure Onion.

- [x] T001 Créer la solution (`Lumineux.slnx`) et les 4 projets Onion (`src/Lumineux.Domain`, `src/Lumineux.Application`, `src/Lumineux.Infrastructure`, `src/Lumineux.Api`) + 4 projets de tests (`tests/Lumineux.Domain.Tests`, `tests/Lumineux.Application.Tests`, `tests/Lumineux.Infrastructure.Tests`, `tests/Lumineux.Api.Tests`) ciblant .NET 10
- [x] T002 Configurer les références inter-projets pour matérialiser la règle de dépendance Onion (Domain sans dépendance sortante ; Application→Domain ; Infrastructure→Application/Domain ; Api→Application/Domain)
- [x] T003 [P] Ajouter les packages NuGet par projet via gestion centralisée (`Directory.Packages.props`) : EF Core 10 + SQL Server (Infrastructure) ; FluentValidation (Application) ; Serilog + JWT Bearer + Swashbuckle (Api) ; xUnit + FluentAssertions + NSubstitute + Sqlite (tests)
- [x] T004 [P] Ajouter `.editorconfig` et les analyzers .NET à la racine du dépôt pour le lint/format
- [x] T005 [P] Configurer Serilog (journalisation structurée) et un middleware d'ID de corrélation dans `src/Lumineux.Api/Program.cs` et `src/Lumineux.Api/Middleware/CorrelationIdMiddleware.cs`
- [x] T006 [P] Externaliser la configuration (chaîne de connexion SQL Server, paramètres JWT) via `appsettings.json` + user-secrets, sans secret en dur, dans `src/Lumineux.Api`

**Checkpoint**: Solution compilable, dépendances Onion en place. ✅

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: Socle transverse requis par TOUTES les user stories.

**CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [x] T007 Créer `AbstractEntity` (champs d'audit `createdt/by`, `updatedt/by`) dans `src/Lumineux.Domain/Entities/AbstractEntity.cs`
- [x] T008 [P] Définir le port `IClock` (heure UTC) dans `src/Lumineux.Domain/Abstractions/IClock.cs`
- [x] T009 [P] Définir le port `ICurrentUser` (id membre, droits) dans `src/Lumineux.Application/Abstractions/ICurrentUser.cs`
- [x] T010 [P] Implémenter `SystemClock` dans `src/Lumineux.Infrastructure/Time/SystemClock.cs`
- [x] T011 Créer `AppDbContext` (DbSets + colonnes snake_case, UTC) dans `src/Lumineux.Infrastructure/Persistence/AppDbContext.cs`
- [x] T012 [P] Mapper les entités `Members` et `Antennas` (projections minimales réutilisées) dans `src/Lumineux.Infrastructure/Persistence/Configurations/`
- [x] T012a Ajouter la FK `antenna` (antenne d'origine, nullable, indexée) à l'entité/configuration `Members` et générer la migration correspondante (prérequis de FR-011 / US2)
- [x] T013 Mettre en place l'authentification JWT Bearer + la policy `manage_attendance` (dans `Program.cs`) et l'implémentation de `ICurrentUser` (dans `src/Lumineux.Api/Security/CurrentUser.cs`, liée au contexte HTTP)
- [x] T014 [P] Ajouter un fournisseur de jetons de test (émission JWT pour dev/tests uniquement) dans `src/Lumineux.Infrastructure/Security/TestTokenIssuer.cs` (voir research.md §5)
- [x] T015 Middleware global de gestion d'erreurs au format ProblemDetails (RFC 7807) dans `src/Lumineux.Api/Middleware/ExceptionHandlingMiddleware.cs`
- [x] T015a [P] Définir une abstraction/service de journalisation des opérations sensibles et des refus, utilisée par les handlers dès US1 (Constitution VI), dans `src/Lumineux.Application/Abstractions/IAuditLogger.cs` + impl Serilog dans `src/Lumineux.Infrastructure/Observability/AuditLogger.cs`
- [x] T016 [P] Définir les exceptions métier de base (`DomainException`, `NotFoundException`, `ConflictException`, `ForbiddenException`) dans `src/Lumineux.Domain/Abstractions/Exceptions.cs`
- [x] T017 Créer le harnais de tests d'intégration (`WebApplicationFactory` + base SQLite en mémoire) dans `tests/Lumineux.Api.Tests/Infrastructure/ApiTestFixture.cs`
- [x] T018 Configurer l'enregistrement DI de l'Infrastructure et de l'Application (extensions `AddInfrastructure`/`AddApplication`)
- [x] T018a Implémenter l'intercepteur d'audit EF (`SaveChanges`) peuplant `createdt/by` et `updatedt/by` via `ICurrentUser` + `IClock` (FR-019) dans `src/Lumineux.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs`

**Checkpoint**: Socle prêt — les user stories peuvent démarrer. ✅

---

## Phase 3: User Story 1 — Démarrage d'une session par le bureau (Priority: P1) MVP

**Goal**: Un membre du bureau démarre une session rattachée à une antenne/horaire et obtient un code
QR rotatif exploitable.

**Independent Test**: `POST /attendance-sessions` renvoie une session `Open` + `GET .../qr` renvoie
un jeton rotatif ; un doublon de session ouverte renvoie 409.

### Tests for User Story 1 (écrire d'abord, doivent échouer avant implémentation)

- [x] T019 [P] [US1] Tests unitaires Domain des invariants/transitions de `AttendanceSession` dans `tests/Lumineux.Domain.Tests/AttendanceSessionTests.cs`
- [x] T020 [P] [US1] Tests unitaires du service `QrTokenService` (rotation, tolérance ± 1 pas, jeton périmé) dans `tests/Lumineux.Infrastructure.Tests/QrTokenServiceTests.cs` *(placé en Infrastructure.Tests car l'impl. y réside)*
- [x] T021 [P] [US1] Tests unitaires du cas d'usage `StartSession` (droit requis, refus 409 doublon, antenne inconnue) dans `tests/Lumineux.Application.Tests/StartSessionTests.cs`
- [x] T022 [P] [US1] Test d'intégration `POST /api/v1/attendance-sessions` + `GET /{id}/qr` (201/401/403/409, `qrSecret` non exposé) dans `tests/Lumineux.Api.Tests/AttendanceSessionsEndpointsTests.cs`

### Implementation for User Story 1

- [x] T023 [P] [US1] Créer l'enum `SessionStatus` dans `src/Lumineux.Domain/Enums/SessionStatus.cs`
- [x] T024 [US1] Créer l'entité `AttendanceSession` (invariants Open/Closed, `qrSecret`, `qrStepSeconds`) dans `src/Lumineux.Domain/Entities/AttendanceSession.cs`
- [x] T025 [P] [US1] Définir le port `IAttendanceSessionRepository` dans `src/Lumineux.Domain/Abstractions/IAttendanceSessionRepository.cs`
- [x] T026 [P] [US1] Définir le port `IQrTokenService` dans `src/Lumineux.Domain/Abstractions/IQrTokenService.cs`
- [x] T027 [US1] Implémenter `QrTokenService` (dérivation TOTP à partir de `qrSecret`, fenêtre `qrStepSeconds`) dans `src/Lumineux.Infrastructure/Security/QrTokenService.cs`
- [x] T028 [US1] Configuration EF de `AttendanceSession` (FK antenne, index `(antenna, status)`) dans `src/Lumineux.Infrastructure/Persistence/Configurations/AttendanceSessionConfiguration.cs`
- [x] T029 [US1] Implémenter `AttendanceSessionRepository` dans `src/Lumineux.Infrastructure/Repositories/AttendanceSessionRepository.cs`
- [x] T030 [P] [US1] DTO `StartSessionRequest`/`SessionResponse`/`QrTokenResponse` (sans `qrSecret`) dans `src/Lumineux.Application/Contracts/Sessions/`
- [x] T031 [P] [US1] Validator FluentValidation de `StartSessionRequest` dans `src/Lumineux.Application/AttendanceSessions/StartSessionValidator.cs`
- [x] T032 [US1] Cas d'usage `StartSession` (heure serveur via IClock, secret aléatoire, garde anti-doublon) dans `src/Lumineux.Application/AttendanceSessions/StartSessionHandler.cs`
- [x] T033 [P] [US1] Cas d'usage `GetSession` dans `src/Lumineux.Application/AttendanceSessions/GetSessionHandler.cs`
- [x] T034 [P] [US1] Cas d'usage `GetCurrentQrToken` dans `src/Lumineux.Application/AttendanceSessions/GetCurrentQrTokenHandler.cs`
- [x] T035 [US1] `AttendanceSessionsController` : `POST /`, `GET /{id}`, `GET /{id}/qr` (policy `manage_attendance`) dans `src/Lumineux.Api/Controllers/AttendanceSessionsController.cs`
- [x] T036 [US1] Migration EF Core créant la table `attendance_sessions` (index/contraintes) dans `src/Lumineux.Infrastructure/Persistence/Migrations/`

**Checkpoint**: US1 pleinement fonctionnelle et testable indépendamment (MVP). ✅

---

## Phase 4: User Story 2 — Présence par scan du QR + synchro hors ligne (Priority: P1)

**Goal**: Un membre enregistre sa présence en scannant le QR (heure serveur), sans doublon ; les
scans hors ligne sont synchronisés de façon idempotente avec l'heure réelle d'arrivée.

**Independent Test**: `POST /{id}/scan` crée une présence (201) puis renvoie 200 « déjà présent » au
re-scan ; jeton périmé → 410 ; `POST /{id}/scan/batch` est idempotent via `clientOperationId`.

### Tests for User Story 2

- [x] T037 [P] [US2] Tests unitaires Domain de `Attendance` (invariants, anti-doublon `(session, member)`) dans `tests/Lumineux.Domain.Tests/AttendanceTests.cs`
- [x] T038 [P] [US2] Tests unitaires du cas d'usage `ScanAttendance` (jeton valide/périmé, doublon, antenne différente FR-011, **refus si membre non actif FR-025**) dans `tests/Lumineux.Application.Tests/ScanAttendanceTests.cs`
- [x] T039 [P] [US2] Tests unitaires du cas d'usage `SyncOfflineScans` (idempotence, bornage `clientArrivalTime`, rejet post-clôture FR-023b) dans `tests/Lumineux.Application.Tests/SyncOfflineScansTests.cs`
- [x] T040 [P] [US2] Test d'intégration `POST /{id}/scan` (201/200/410/409) et `POST /{id}/scan/batch` dans `tests/Lumineux.Api.Tests/ScanEndpointsTests.cs`
- [x] T041 [P] [US2] Test d'intégration d'unicité concurrente (anti-doublon sous charge, SC-003) dans `tests/Lumineux.Infrastructure.Tests/AttendanceUniquenessTests.cs`

### Implementation for User Story 2

- [x] T042 [P] [US2] Créer les enums `AttendanceSource` et `AttendanceStatus` dans `src/Lumineux.Domain/Enums/AttendanceEnums.cs`
- [x] T043 [US2] Créer l'entité `Attendance` (invariants, `originAntenna` snapshot, `clientOperationId`) dans `src/Lumineux.Domain/Entities/Attendance.cs`
- [x] T044 [P] [US2] Définir le port `IAttendanceRepository` dans `src/Lumineux.Domain/Abstractions/IAttendanceRepository.cs`
- [x] T045 [US2] Configuration EF de `Attendance` (index unique filtré `(session, member)` sur Valid ; unicité `(session, clientOperationId)` ; index `(session, status)`) dans `src/Lumineux.Infrastructure/Persistence/Configurations/AttendanceConfiguration.cs`
- [x] T046 [US2] Implémenter `AttendanceRepository` (gestion de la violation d'unicité → « déjà présent ») dans `src/Lumineux.Infrastructure/Repositories/AttendanceRepository.cs`
- [x] T047 [P] [US2] DTO `ScanRequest`/`AttendanceResponse` dans `src/Lumineux.Application/Contracts/Attendances/`
- [x] T048 [P] [US2] DTO `OfflineScanBatchRequest`/`OfflineScanBatchResponse` (+ validators) dans `src/Lumineux.Application/Contracts/Attendances/`
- [x] T049 [US2] Cas d'usage `ScanAttendance` (validation jeton via IQrTokenService, heure serveur, anti-doublon, snapshot antenne d'origine `Members.antenna`, **contrôle statut membre actif FR-025**, garde session Open, journalisation via IAuditLogger) dans `src/Lumineux.Application/Attendances/ScanAttendanceHandler.cs`
- [x] T050 [US2] Cas d'usage `SyncOfflineScans` (lot idempotent, bornage `clientArrivalTime`, règle post-clôture) dans `src/Lumineux.Application/Attendances/SyncOfflineScansHandler.cs`
- [x] T051 [US2] `AttendancesController` : `POST /{id}/scan` et `POST /{id}/scan/batch` (membre authentifié) dans `src/Lumineux.Api/Controllers/AttendancesController.cs`
- [x] T052 [US2] Migration EF Core créant la table `attendances` (index/contraintes) dans `src/Lumineux.Infrastructure/Persistence/Migrations/`

**Checkpoint**: US1 + US2 fonctionnent indépendamment ; le cœur du pointage est livrable. ✅

---

## Phase 5: User Story 3 — Ajout manuel et consultation des présences (Priority: P2)

**Goal**: Le bureau ajoute manuellement les membres non équipés, consulte la liste en direct et peut
retirer une présence erronée tant que la session est ouverte.

**Independent Test**: `POST /{id}/attendances` crée une présence Manual ; membre inexistant → 404 ;
`GET /{id}/attendances` renvoie le décompte ; `DELETE .../{memberId}` renvoie 204 (Cancelled tracé).

### Tests for User Story 3

- [x] T053 [P] [US3] Tests unitaires `AddManualAttendance` (membre existant requis FR-017, doublon, droit, **refus si membre non actif FR-025**) dans `tests/Lumineux.Application.Tests/AddManualAttendanceTests.cs`
- [x] T054 [P] [US3] Tests unitaires `CancelAttendance` (session Open requise, passage Cancelled tracé) dans `tests/Lumineux.Application.Tests/CancelAttendanceTests.cs`
- [x] T055 [P] [US3] Tests unitaires `ListAttendances` (filtre status, `validCount`) dans `tests/Lumineux.Application.Tests/ListAttendancesTests.cs`
- [x] T056 [P] [US3] Test d'intégration `POST/GET/DELETE /{id}/attendances` (201/200/404/403/204) dans `tests/Lumineux.Api.Tests/ManualAttendanceEndpointsTests.cs`

### Implementation for User Story 3

- [x] T057 [P] [US3] DTO `ManualAttendanceRequest`/`AttendanceListResponse` (+ validator) dans `src/Lumineux.Application/Contracts/Attendances/`
- [x] T058 [US3] Cas d'usage `AddManualAttendance` (vérif. existence membre, **contrôle statut membre actif FR-025**, source Manual, anti-doublon, garde session Open, journalisation via IAuditLogger) dans `src/Lumineux.Application/Attendances/AddManualAttendanceHandler.cs`
- [x] T059 [P] [US3] Cas d'usage `CancelAttendance` (session Open, status→Cancelled, trace) dans `src/Lumineux.Application/Attendances/CancelAttendanceHandler.cs`
- [x] T060 [P] [US3] Cas d'usage `ListAttendances` (direct + post-clôture, filtre) dans `src/Lumineux.Application/Attendances/ListAttendancesHandler.cs`
- [x] T061 [US3] Étendre `AttendancesController` : `POST /{id}/attendances`, `GET /{id}/attendances`, `DELETE /{id}/attendances/{memberId}` (policy `manage_attendance`) dans `src/Lumineux.Api/Controllers/AttendancesController.cs`

**Checkpoint**: US1 + US2 + US3 fonctionnent indépendamment. ✅

---

## Phase 6: User Story 4 — Clôture de la session et heure de fin (Priority: P2)

**Goal**: Le bureau clôture la session ; l'heure de clôture devient l'heure de fin pour toutes les
présences valides ; la session close refuse tout nouveau pointage/ajout/retrait.

**Independent Test**: `POST /{id}/close` renvoie `Closed` + `endTime` ; toutes les présences valides
partagent le même `endTime` ; ensuite scan/ajout/retrait → 409.

### Tests for User Story 4

- [x] T062 [P] [US4] Tests unitaires Domain de la transition `CloseSession` (Open→Closed, refus depuis Closed) dans `tests/Lumineux.Domain.Tests/CloseSessionTests.cs`
- [x] T063 [P] [US4] Tests unitaires `CloseSession` (propagation `endTime` à toutes les présences valides, transaction) dans `tests/Lumineux.Application.Tests/CloseSessionTests.cs`
- [x] T064 [P] [US4] Test d'intégration `POST /{id}/close` (200, même `endTime` partout) + refus 409 après clôture dans `tests/Lumineux.Api.Tests/CloseSessionEndpointsTests.cs`

### Implementation for User Story 4

- [x] T065 [US4] Cas d'usage `CloseSession` (heure serveur, transition, propagation `endTime` en une transaction) dans `src/Lumineux.Application/AttendanceSessions/CloseSessionHandler.cs`
- [x] T066 [US4] Renforcer les gardes « session Closed → 409 » dans les handlers Scan/Sync/Manual/Cancel (FR-007) dans `src/Lumineux.Application/Attendances/`
- [x] T067 [US4] Ajouter `POST /{id}/close` au `AttendanceSessionsController` (policy `manage_attendance`)
- [x] T068 [P] [US4] Clôture automatique de secours (délai + heure de fin par défaut configurables, FR-024) via service hébergé dans `src/Lumineux.Infrastructure/BackgroundJobs/SessionAutoCloseService.cs`

**Checkpoint**: Cycle de vie complet de la réunion opérationnel ; les 4 user stories livrées. ✅

---

## Phase 7: Polish & Cross-Cutting

**Purpose**: Qualité, sécurité, performance et validation finale.

- [x] T069 [P] Revue finale de la journalisation : vérifier que chaque handler journalise ses opérations sensibles et refus via `IAuditLogger` (T015a) et qu'aucun secret/donnée personnelle superflue ne fuite (FR-019, FR-020)
- [x] T070 [P] Aligner Swagger/OpenAPI généré sur `contracts/openapi.yaml` (codes, schémas, ProblemDetails) dans `src/Lumineux.Api`
- [x] T071 [P] Test de charge « ≥ 200 scans en < 2 min » (SC-006) dans `tests/Lumineux.Api.Tests/Performance/ScanLoadTests.cs`
- [x] T072 [P] Revue de sécurité (validation entrées, secrets hors code, `qrSecret` non exposé, EF paramétré, paquets vulnérables) — checklist dans `specs/001-attendance-management/checklists/`
- [x] T073 [P] Documentation développeur (README API, exécution migrations) dans `src/Lumineux.Api/README.md`
- [x] T074 Exécuter le scénario de bout en bout de `quickstart.md` et vérifier les critères SC-001..SC-005

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance — démarre immédiatement.
- **Foundational (Phase 2)** : dépend de Setup — BLOQUE toutes les user stories.
- **User Stories (Phases 3–6)** : dépendent de Foundational.
- **Polish (Phase 7)** : dépend des user stories livrées.

### User Story Dependencies

- **US1** : après Foundational — aucune dépendance sur les autres stories.
- **US2** : après Foundational ; réutilise `AttendanceSession`/`IQrTokenService` (US1). **Dépend de T012a** (FK `Members.antenna`) pour le snapshot d'antenne d'origine (FR-011).
- **US3** : après Foundational ; réutilise `Attendance` (US2) et la session (US1).
- **US4** : après Foundational ; propage l'`endTime` aux présences (US2/US3) et renforce les gardes.

### Within Each User Story

- Les tests sont écrits d'abord et DOIVENT échouer avant l'implémentation (Constitution III).
- Ordre : entités/enums → ports → configuration EF/repositories → cas d'usage → contrôleurs → migration.

### Parallel Opportunities

- Setup : T003, T004, T005, T006 en parallèle.
- Foundational : T008, T009, T010, T012, T014, T016 en parallèle (fichiers distincts).
- Par user story : tous les tests marqués [P] en parallèle ; DTO/enums/ports [P] en parallèle.

---

## Implementation Strategy

### MVP First (US1 uniquement) — LIVRÉ

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → **build + 23 tests verts + migration**.

### Incremental Delivery

1. Setup + Foundational → socle prêt. ✅
2. + US1 → démarrage de session + QR rotatif (MVP démontrable). ✅
3. + US2 → pointage par scan + hors ligne (cœur du produit livrable). ✅
4. + US3 → ajout manuel + consultation. ✅
5. + US4 → clôture et heure de fin (cycle complet). ✅
6. Phase 7 → durcissement, perf, validation quickstart.

### Notes

- [P] = fichiers différents, aucune dépendance non satisfaite.
- Vérifier que les tests échouent avant d'implémenter (Constitution III).
- Committer après chaque tâche ou groupe logique ; s'arrêter aux checkpoints pour valider une story.
