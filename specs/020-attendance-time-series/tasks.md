# Tasks: API de série temporelle des présences

**Input**: Design documents from `specs/020-attendance-time-series/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — Constitution Lumineux, Principe III (NON-NÉGOCIABLE) : tests Domain/Application avant impl. + tests d'intégration Infrastructure et Api.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- Chemins relatifs à la racine du dépôt

## Path Conventions

- API .NET (Onion) : `src/Lumineux.{Domain,Application,Infrastructure,Api}/` ; tests : `tests/Lumineux.{Application,Infrastructure,Api}.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: DTO de sortie.

- [X] T001 [P] Étendre `src/Lumineux.Application/Contracts/Reports/ReportDtos.cs` : enum `TimeSeriesGranularity` (`Week`, `Month`), `TimeSeriesPoint(periodStart, label, validAttendanceCount, sessionCount)`, `AttendanceTimeSeriesResponse(from, to, granularity, points[])` (voir `contracts/rest-api.md`)

**Checkpoint**: contrat de sortie prêt.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extension du port de lecture (018) et son implémentation — requis par toutes les stories.

**⚠️ CRITICAL**: aucune story ne démarre avant la fin de cette phase.

- [X] T002 [P] Étendre `src/Lumineux.Domain/Abstractions/IAttendanceReportRepository.cs` : record `SessionValidCount(DateTime MeetingDate, int ValidAttendanceCount)` + méthode `GetSessionValidCountsAsync(DateTime from, DateTime to, int? antennaId, CancellationToken)`
- [X] T003 [P] Test Infrastructure dans `tests/Lumineux.Infrastructure.Tests/AttendanceReportRepositoryTests.cs` : `GetSessionValidCountsAsync` renvoie une ligne par session de la période (date + présences **valides**), **annulées exclues**, **filtre antenne** (doit ÉCHOUER)
- [X] T004 Implémenter `GetSessionValidCountsAsync` dans `src/Lumineux.Infrastructure/Repositories/AttendanceReportRepository.cs` (EF, requêtes simples `Where`/`Contains`/`Count`, lecture seule)

**Checkpoint**: port étendu et testé au vert.

---

## Phase 3: User Story 1 - Évolution par intervalle (Priority: P1) 🎯 MVP

**Goal**: Série temporelle continue (semaine ISO / mois) des présences valides sur une période.

**Independent Test**: `GET .../time-series?from=&to=&granularity=Month|Week` → suite ordonnée
d'intervalles avec présences valides + sessions ; intervalle sans donnée à 0 ; granularité invalide → 400.

### Tests (US1)

- [X] T005 [P] [US1] Test Application `tests/Lumineux.Application.Tests/TimeBucketsTests.cs` : génération des intervalles **mois** (`AAAA-MM`) et **semaine ISO** (`AAAA-Sww`, lundi), série **continue** (tous les intervalles de la plage), affectation d'une date à son intervalle (doit ÉCHOUER)
- [X] T006 [P] [US1] Test Application `tests/Lumineux.Application.Tests/AttendanceTimeSeriesTests.cs` : bucketisation via handler (mois/semaine), **zéros** remplis, **annulées exclues** (via comptes du dépôt), **granularité invalide** (400), **plage invalide** (validation), droit manquant (Forbidden) (doit ÉCHOUER)
- [X] T007 [P] [US1] Test Api dans `tests/Lumineux.Api.Tests/ReportsEndpointsTests.cs` : `GET time-series` 200 (Month) ; 400 granularité non supportée (`Day`) ; 400 plage invalide ; 401 sans jeton ; 403 sans `manage_attendance` (doit ÉCHOUER)

### Implémentation (US1)

- [X] T008 [US1] Implémenter `src/Lumineux.Application/Reports/TimeBuckets.cs` (pur) : génération des intervalles `[from, to]` par granularité (mois calendaire ; semaine ISO 8601 lundi), `periodStart` + `label`, et affectation d'une date à son intervalle
- [X] T009 [US1] Implémenter `src/Lumineux.Application/Reports/GetAttendanceTimeSeriesHandler.cs` (droit `manage_attendance`, `ReportPeriodValidator`, validation granularité `Week`/`Month`→sinon `DomainException`, `GetSessionValidCountsAsync`, bucketisation via `TimeBuckets`, remplissage des zéros, ordre chronologique) + DI dans `src/Lumineux.Application/DependencyInjection.cs`
- [X] T010 [US1] Ajouter `GET /api/v1/reports/attendance/time-series` (from/to/granularity/antennaId?) à `src/Lumineux.Api/Controllers/ReportsController.cs`

**Checkpoint**: US1 fonctionnelle et testable seule (MVP livrable).

---

## Phase 4: User Story 2 - Filtrer par antenne (Priority: P2)

**Goal**: Restreindre la série à une antenne.

**Independent Test**: `GET .../time-series?...&antennaId=<id>` → valeurs de cette antenne ; antenne sans
présence → tous les points à 0.

### Tests (US2)

- [X] T011 [P] [US2] Test dans `AttendanceTimeSeriesTests.cs` (+ `ReportsEndpointsTests.cs`) : `antennaId` propagé au dépôt ; antenne sans donnée → série continue à **0** (doit ÉCHOUER)

### Implémentation (US2)

- [X] T012 [US2] Vérifier/compléter la propagation de `antennaId` de bout en bout (`ReportsController` → `GetAttendanceTimeSeriesHandler` → `GetSessionValidCountsAsync`) ; ajuster si nécessaire

**Checkpoint**: US1 + US2 opérationnelles.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T013 [P] Test Api de **cohérence 018** : sans filtre d'antenne, la **somme** des `validAttendanceCount` de la série égale le total de `antenna-summary` pour la **même période** (SC-006)
- [X] T014 [P] Annotations `ProducesResponseType` (200/400/401/403) sur l'endpoint `time-series` pour Swagger (Principe V)
- [X] T015 Exécuter `dotnet test` (toute la solution verte) puis dérouler `specs/020-attendance-time-series/quickstart.md` (A→D, SC-001..006) ; marquer les tâches `[X]`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** : aucune dépendance.
- **Foundational (P2)** : dépend de Setup — **BLOQUE** les stories (port + comptes par session).
- **US1 (P3)** → **US2 (P4)** : US2 réutilise l'endpoint/handler d'US1 (le filtre `antennaId` est déjà
  dans la signature). Séquentiel sur `ReportsController`/handler ; indépendamment testables.
- **Polish (P5)** : après les stories.

### User Story Dependencies

- **US1 (P1)** : port étendu + `TimeBuckets` + validateur. **MVP**.
- **US2 (P2)** : réutilise US1 (propagation du filtre `antennaId`).

### Parallel Opportunities

- Foundational : T002 (port) et T003 (test) en parallèle ; puis T004.
- US1 : tests T005–T007 en parallèle avant impl. ; `TimeBuckets` (T008) parallèle au reste jusqu'à son intégration.
- Polish : T013, T014 en parallèle.

---

## Parallel Example: US1 (Phase 3)

```text
# Rédiger ensemble (fichiers distincts) :
T005 TimeBucketsTests.cs
T006 AttendanceTimeSeriesTests.cs
T007 ReportsEndpointsTests.cs (cas time-series)
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → **VALIDER** (série mois/semaine) → démo.

### Incremental Delivery

Setup + Foundational → US1 (série, MVP) → US2 (filtre antenne) → Polish (cohérence 018).

---

## Notes

- **Aucune migration** (lecture seule) ; **aucune dépendance nouvelle**.
- **Bucketisation en mémoire** (portable SQLite/SQL Server) ; **semaine ISO 8601** (lundi) / **mois**
  calendaire ; **série continue** (zéros) ; **annulées exclues** ; **cohérence 018** (SC-006).
- Droit **`manage_attendance`** réutilisé.
- Commits après chaque tâche ou groupe logique ; s'arrêter aux checkpoints pour valider une story.
