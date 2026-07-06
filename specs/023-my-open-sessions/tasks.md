# Tasks: API — Récupérer mes sessions de présence ouvertes

**Input**: Design documents from `specs/023-my-open-sessions/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — Constitution Lumineux, Principe III (NON-NÉGOCIABLE) : tests Application avant impl. + tests d'intégration Infrastructure et Api.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- Chemins relatifs à la racine du dépôt

## Path Conventions

- API .NET (Onion) : `src/Lumineux.{Domain,Application,Infrastructure,Api}/` ; tests : `tests/Lumineux.{Application,Infrastructure,Api}.Tests/`

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Extension du port de session et son implémentation — requis par la story.

**⚠️ CRITICAL**: la story ne démarre pas avant la fin de cette phase.

- [X] T001 [P] Étendre `src/Lumineux.Domain/Abstractions/IAttendanceSessionRepository.cs` : `ListOpenByOpenerAsync(int openedByMemberId, CancellationToken)` → `IReadOnlyList<AttendanceSession>` (sessions `Status == Open` de cet initiateur)
- [X] T002 [P] Test Infrastructure `tests/Lumineux.Infrastructure.Tests/OpenSessionsByOpenerTests.cs` (SQLite in-memory) : `ListOpenByOpenerAsync` ne renvoie que les sessions **ouvertes** de l'**initiateur** demandé (exclut clôturées et sessions d'autres membres) (doit ÉCHOUER)
- [X] T003 Implémenter `ListOpenByOpenerAsync` dans `src/Lumineux.Infrastructure/Repositories/AttendanceSessionRepository.cs` (EF `Where(Status == Open && OpenedByMemberId == id)`, `AsNoTracking`, lecture seule)

**Checkpoint**: port étendu et testé au vert.

---

## Phase 2: User Story 1 - Retrouver ma session ouverte (Priority: P1) 🎯 MVP

**Goal**: Endpoint renvoyant les sessions ouvertes de l'utilisateur courant, pour la reprise.

**Independent Test**: `GET /api/v1/attendance-sessions/mine/open` → renvoie la session ouverte que
l'utilisateur a démarrée ; exclut les clôturées et celles d'autres membres ; 401/403 sans droit.

### Tests (US1)

- [X] T004 [P] [US1] Test Application `tests/Lumineux.Application.Tests/ListMyOpenSessionsTests.cs` : renvoie **les sessions ouvertes de l'utilisateur** (via `ICurrentUser`), **exclut** clôturées et autres membres (repo filtré), **liste vide** si aucune, **droit manquant** (Forbidden) (doit ÉCHOUER)
- [X] T005 [P] [US1] Test Api dans `tests/Lumineux.Api.Tests/` (ex. `MyOpenSessionsEndpointTests.cs`) : `GET mine/open` renvoie **200** avec la session ouverte de l'utilisateur ; **401** sans jeton ; **403** sans `manage_attendance` (doit ÉCHOUER)

### Implémentation (US1)

- [X] T006 [US1] Implémenter `src/Lumineux.Application/AttendanceSessions/ListMyOpenSessionsHandler.cs` (droit `manage_attendance` ; identité via `ICurrentUser.MemberId` — liste vide si absente ; `ListOpenByOpenerAsync` ; projection `SessionMapping.ToResponse`) + DI dans `src/Lumineux.Application/DependencyInjection.cs`
- [X] T007 [US1] Ajouter `GET /api/v1/attendance-sessions/mine/open` au `src/Lumineux.Api/Controllers/AttendanceSessionsController.cs` (renvoie `SessionResponse[]`)

**Checkpoint**: US1 fonctionnelle et testable seule (MVP livrable — prérequis de la reprise SPA).

---

## Phase 3: Polish & Cross-Cutting Concerns

- [X] T008 Exécuter `dotnet test` (toute la solution verte) puis dérouler `specs/023-my-open-sessions/quickstart.md` (A→D, SC-001..005) ; marquer les tâches `[X]`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (P1)** : port + dépôt — **BLOQUE** la story.
- **US1 (P2)** : après Foundational (handler + endpoint).
- **Polish (P3)** : après la story.

### User Story Dependencies

- **US1 (P1)** : port étendu + `ICurrentUser`. **MVP** ; prérequis de la reprise SPA (feature 024).

### Parallel Opportunities

- Foundational : T001 (port) et T002 (test) en parallèle ; puis T003.
- US1 : tests T004/T005 en parallèle avant impl.

---

## Parallel Example: US1 (Phase 2)

```text
T004 ListMyOpenSessionsTests.cs (Application)
T005 MyOpenSessionsEndpointTests.cs (Api)
# puis :
T006 ListMyOpenSessionsHandler.cs + DI
T007 AttendanceSessionsController : GET mine/open
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Foundational → 2. Phase 2 US1 → **VALIDER** (récupération de mes sessions ouvertes) → prêt
   pour la reprise SPA.

---

## Notes

- **Aucune migration** (lecture seule) ; **aucune dépendance nouvelle** ; réutilise le DTO
  `SessionResponse` et le contrôleur existants.
- **Identité par le jeton** (jamais un paramètre client) ; résultat **limité à l'utilisateur courant**.
- Ne modifie NI la règle de conflit au démarrage NI la clôture.
- Commits après chaque tâche ou groupe logique.
