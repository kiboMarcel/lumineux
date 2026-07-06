# Tasks: API de gestion des antennes (CRUD)

**Input**: Design documents from `specs/016-antenna-management/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — Constitution Lumineux, Principe III (NON-NÉGOCIABLE) : tests Domain/Application avant impl. + tests d'intégration Api et Infrastructure.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- Chemins relatifs à la racine du dépôt

## Path Conventions

- API .NET (Onion) : `src/Lumineux.{Domain,Application,Infrastructure,Api}/` ; tests : `tests/Lumineux.{Domain,Application,Infrastructure,Api}.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Squelette partagé (droit, DTO, comportements d'entité).

- [X] T001 Déclarer la constante `ManageReferentials = "manage_referentials"` dans `src/Lumineux.Application/Abstractions/Permissions.cs`
- [X] T002 [P] Créer les DTO `AntennaResponse`, `CreateAntennaRequest`, `UpdateAntennaRequest` dans `src/Lumineux.Application/Contracts/Antennas/AntennaDtos.cs` (voir `contracts/rest-api.md`)
- [X] T003 [P] Enrichir `src/Lumineux.Domain/Entities/Antenna.cs` : fabrique `Create(code,label,districtId)` (statut Active), `UpdateDetails(label,districtId)` (code inchangé), `Deactivate()`, `Activate()` avec invariants (code/label non vides, districtId>0)

**Checkpoint**: droit déclaré, DTO et comportements d'entité prêts.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Ports, persistance, RBAC, migration — requis par TOUTES les stories.

**⚠️ CRITICAL**: aucune story ne démarre avant la fin de cette phase.

- [X] T004 [P] Test Domain `tests/Lumineux.Domain.Tests/AntennaTests.cs` : invariants `Create`/`UpdateDetails` (code/label requis, district>0), `Deactivate`/`Activate` (idempotents, code inchangé) (doit ÉCHOUER)
- [X] T005 [P] Créer le port `src/Lumineux.Domain/Abstractions/IAntennaRepository.cs` : `GetByIdAsync`, `GetByCodeAsync`, `ListAllAsync`, `AddAsync`, `SaveChangesAsync`
- [X] T006 [P] Ajouter `HasOpenSessionForAntennaAsync(int antennaId, CancellationToken)` à `src/Lumineux.Domain/Abstractions/IAttendanceSessionRepository.cs` et l'implémenter dans `src/Lumineux.Infrastructure/Repositories/AttendanceSessionRepository.cs` (Status==Open && AntennaId==id)
- [X] T007 Implémenter `src/Lumineux.Infrastructure/Repositories/AntennaRepository.cs` (IAntennaRepository ; `GetByCodeAsync` insensible aux espaces) et l'enregistrer dans `src/Lumineux.Infrastructure/DependencyInjection.cs`
- [X] T008 [P] Ajouter l'entrée `manage_referentials` (« Gérer les référentiels ») au catalogue `src/Lumineux.Infrastructure/Security/PermissionCatalog.cs`
- [X] T009 [P] Déclarer la policy `Permissions.ManageReferentials` dans `src/Lumineux.Api/Program.cs` (RequireClaim "permission")
- [X] T010 [P] Créer `AntennaValidators` (Create/Update : code non vide, libellé requis, districtId>0) et `AntennaMapping` (entité→`AntennaResponse`) dans `src/Lumineux.Application/Antennas/`
- [X] T011 Générer la migration EF Core d'**index unique** `IX_antennas_code` sur `antennas.code` (⚠️ `dotnet ef migrations add AntennaCodeUnique` — build requis) dans `src/Lumineux.Infrastructure/Persistence/Migrations/`
- [X] T012 [P] Test Infrastructure `tests/Lumineux.Infrastructure.Tests/AntennaCodeUniquenessTests.cs` : l'index unique rejette deux antennes de même code (SQLite in-memory) (doit ÉCHOUER avant migration)

**Checkpoint**: ports, dépôt, RBAC, migration et validators prêts ; entité testée au vert.

---

## Phase 3: User Story 1 - Créer une antenne (Priority: P1) 🎯 MVP

**Goal**: Créer une antenne (code unique, libellé, district existant), exploitable ensuite pour membres/sessions.

**Independent Test**: `POST /api/v1/antennas` avec code inédit + district existant → 201 (Active), visible dans la lecture publique 010 ; code dupliqué → 409 `duplicate_code`.

### Tests (US1)

- [X] T013 [P] [US1] Test Application `tests/Lumineux.Application.Tests/CreateAntennaTests.cs` : succès (Active), `duplicate_code` (409), district inexistant (validation), droit manquant (Forbidden) (doit ÉCHOUER)
- [X] T014 [P] [US1] Test Api `tests/Lumineux.Api.Tests/AntennaEndpointsTests.cs` : `POST` 201 + `Location` ; 409 code dupliqué ; 401 sans jeton ; 403 sans `manage_referentials` (doit ÉCHOUER)

### Implémentation (US1)

- [X] T015 [US1] Implémenter `src/Lumineux.Application/Antennas/CreateAntennaHandler.cs` (vérifie `manage_referentials`, unicité via `GetByCodeAsync`→`duplicate_code`, district via `IReferenceLookupRepository.DistrictExistsAsync`, audit) + DI dans `src/Lumineux.Application/DependencyInjection.cs`
- [X] T016 [US1] Implémenter `src/Lumineux.Application/Antennas/GetAntennaHandler.cs` (404 si introuvable) + DI (nécessaire au `CreatedAtAction`)
- [X] T017 [US1] Créer `src/Lumineux.Api/Controllers/AntennasController.cs` `[Authorize(Policy = Permissions.ManageReferentials)]` avec `POST` (CreatedAtAction) et `GET /{id}`

**Checkpoint**: US1 fonctionnelle et testable seule (MVP livrable).

---

## Phase 4: User Story 2 - Modifier une antenne (Priority: P2)

**Goal**: Corriger libellé + district ; le code reste immuable.

**Independent Test**: `PUT /api/v1/antennas/{id}` met à jour libellé/district ; code inchangé ; 404 si introuvable.

### Tests (US2)

- [X] T018 [P] [US2] Test Application `tests/Lumineux.Application.Tests/UpdateAntennaTests.cs` : mise à jour libellé+district, **code non modifié**, district inexistant (validation), 404, droit manquant (doit ÉCHOUER)

### Implémentation (US2)

- [X] T019 [US2] Implémenter `src/Lumineux.Application/Antennas/UpdateAntennaHandler.cs` (charge par id→404, `UpdateDetails`, district validé, audit) + DI
- [X] T020 [US2] Ajouter `PUT /{id}` à `src/Lumineux.Api/Controllers/AntennasController.cs`

**Checkpoint**: US1 + US2 opérationnelles.

---

## Phase 5: User Story 3 - Activer / désactiver (Priority: P2)

**Goal**: Désactiver (logique) / réactiver ; refus si sessions ouvertes ; intégrité préservée.

**Independent Test**: `deactivate` → Inactive (disparaît de 010) ; refus 409 `antenna_has_open_sessions` si session ouverte ; `activate` → Active.

### Tests (US3)

- [X] T021 [P] [US3] Test Application `tests/Lumineux.Application.Tests/SetAntennaActiveTests.cs` : désactivation OK, **refus `antenna_has_open_sessions`** (session ouverte), réactivation, idempotence, 404, droit manquant (doit ÉCHOUER)

### Implémentation (US3)

- [X] T022 [US3] Implémenter `src/Lumineux.Application/Antennas/SetAntennaActiveHandler.cs` (désactiver : `HasOpenSessionForAntennaAsync`→`ConflictException` `antenna_has_open_sessions` ; réactiver ; audit) + DI
- [X] T023 [US3] Ajouter `POST /{id}/deactivate` et `POST /{id}/activate` à `src/Lumineux.Api/Controllers/AntennasController.cs`

**Checkpoint**: US1 + US2 + US3 opérationnelles.

---

## Phase 6: User Story 4 - Lister / consulter en gestion (Priority: P3)

**Goal**: Lister toutes les antennes (inactives incluses) avec statut.

**Independent Test**: `GET /api/v1/antennas` renvoie actives **et** inactives (contrairement à 010).

### Tests (US4)

- [X] T024 [P] [US4] Test Application `tests/Lumineux.Application.Tests/ListAntennasTests.cs` : renvoie actives+inactives avec statut, droit manquant (Forbidden) (doit ÉCHOUER)

### Implémentation (US4)

- [X] T025 [US4] Implémenter `src/Lumineux.Application/Antennas/ListAntennasHandler.cs` (`ListAllAsync`→`AntennaResponse[]`, vérifie `manage_referentials`) + DI
- [X] T026 [US4] Ajouter `GET /` (liste gestion) à `src/Lumineux.Api/Controllers/AntennasController.cs`

**Checkpoint**: les 4 stories opérationnelles.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T027 [P] Compléter `tests/Lumineux.Api.Tests/AntennaEndpointsTests.cs` : parcours CRUD complet (create→update→deactivate refusé si session ouverte→activate→list inactives) et 401/403 sur chaque endpoint
- [X] T028 [P] Test Api : la lecture publique `GET /api/v1/reference/antennas` (010) reste **inchangée** (n'expose que les actives)
- [X] T029 [P] Annotations `ProducesResponseType` (201/200/400/404/409/401/403) sur `AntennasController` pour Swagger (Principe V)
- [X] T030 Exécuter `dotnet test` (toute la solution verte) puis dérouler `specs/016-antenna-management/quickstart.md` (A→F, SC-001..006) ; marquer les tâches `[X]`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** : aucune dépendance.
- **Foundational (P2)** : dépend de Setup — **BLOQUE** toutes les stories (ports, dépôt, RBAC, migration).
- **US1 (P3)** → **US2 (P4)** → **US3 (P5)** → **US4 (P6)** : toutes après Foundational. US2/US3/US4 étendent le **même contrôleur** (`AntennasController`) → séquentiel sur ce fichier ; handlers indépendants et testables isolément.
- **Polish (P7)** : après les stories visées.

### User Story Dependencies

- **US1 (P1)** : socle foundational. **MVP**.
- **US2 (P2)** : foundational ; enrichit le contrôleur.
- **US3 (P2)** : dépend de `HasOpenSessionForAntennaAsync` (T006) ; enrichit le contrôleur.
- **US4 (P3)** : dépend de `ListAllAsync` (T005/T007) ; enrichit le contrôleur.

### Parallel Opportunities

- Setup : T002, T003 en parallèle (après T001).
- Foundational : T004, T005, T008, T009, T010, T012 en parallèle ; puis T006/T007/T011 (dépôt/migration).
- Tests de story (T013/T014, T018, T021, T024) rédigés en parallèle avant impl.
- Polish : T027–T029 en parallèle.

---

## Parallel Example: Foundational (Phase 2)

```text
# Rédiger ensemble (fichiers distincts) :
T004 AntennaTests.cs (Domain)
T005 IAntennaRepository.cs
T008 PermissionCatalog += manage_referentials
T009 Program.cs policy
T010 AntennaValidators + AntennaMapping
T012 AntennaCodeUniquenessTests.cs
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → **VALIDER** (créer une antenne exploitable sans SQL) → démo.

### Incremental Delivery

Setup + Foundational → US1 (créer, MVP) → US2 (modifier) → US3 (activer/désactiver + règle sessions ouvertes) → US4 (liste gestion) → Polish.

---

## Notes

- **T011 = migration EF** : nécessite un build réussi ; index unique déterministe et rejouable.
- **duplicate_code** / **antenna_has_open_sessions** : `ConflictException` (409) mappée par le middleware ; validations (district/champ) via FluentValidation (400).
- **Lecture publique 010 inchangée** (T028).
- Commits après chaque tâche ou groupe logique ; s'arrêter aux checkpoints pour valider une story.
