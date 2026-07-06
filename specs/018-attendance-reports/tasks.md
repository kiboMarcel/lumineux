# Tasks: API de rapports & statistiques de présence

**Input**: Design documents from `specs/018-attendance-reports/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — Constitution Lumineux, Principe III (NON-NÉGOCIABLE) : tests Domain/Application avant impl. + tests d'intégration Infrastructure et Api.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- Chemins relatifs à la racine du dépôt

## Path Conventions

- API .NET (Onion) : `src/Lumineux.{Domain,Application,Infrastructure,Api}/` ; tests : `tests/Lumineux.{Application,Infrastructure,Api}.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: DTO et validation partagés.

- [X] T001 [P] Créer les DTO dans `src/Lumineux.Application/Contracts/Reports/ReportDtos.cs` : `AntennaAttendanceSummaryItem`, `AntennaAttendanceSummaryResponse`, `MemberAttendanceRateResponse` (voir `contracts/rest-api.md`)
- [X] T002 [P] Créer `src/Lumineux.Application/Reports/ReportPeriodValidator.cs` (FluentValidation) : `from`/`to` requis, `to >= from`, plafond de période (ex. 366 jours)

**Checkpoint**: DTO et validateur prêts.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Port d'agrégation et son implémentation — requis par TOUTES les stories.

**⚠️ CRITICAL**: aucune story ne démarre avant la fin de cette phase.

- [X] T003 [P] Créer le port `src/Lumineux.Domain/Abstractions/IAttendanceReportRepository.cs` : `GetAntennaSummaryAsync(from, to, antennaId?)` et `GetMemberRateDataAsync(memberId, from, to)` (lecture/agrégation)
- [X] T004 [P] Test Infrastructure `tests/Lumineux.Infrastructure.Tests/AttendanceReportRepositoryTests.cs` (SQLite in-memory) : synthèse par antenne (sessionCount/validCount), **présences annulées exclues**, **dénominateur du taux = sessions de l'antenne d'origine**, période vide (doit ÉCHOUER)
- [X] T005 Implémenter `src/Lumineux.Infrastructure/Repositories/AttendanceReportRepository.cs` (EF `GroupBy`/`Count`, jointures `antennas`/`members`) et l'enregistrer dans `src/Lumineux.Infrastructure/DependencyInjection.cs`

**Checkpoint**: port testé au vert (agrégations exactes).

---

## Phase 3: User Story 1 - Synthèse par antenne + période (Priority: P1) 🎯 MVP

**Goal**: Synthèse d'affluence par antenne sur une période (sessions, présences valides, moyenne).

**Independent Test**: `GET /reports/attendance/antenna-summary?from=&to=` → par antenne : sessionCount, validAttendanceCount, averageValidPerSession ; annulées exclues ; filtre antenne ; période vide OK.

### Tests (US1)

- [X] T006 [P] [US1] Test Application `tests/Lumineux.Application.Tests/AntennaAttendanceSummaryTests.cs` : moyenne calculée, **annulées exclues**, filtre antenne, période vide, **plage invalide** (validation), droit manquant (Forbidden) (doit ÉCHOUER)
- [X] T007 [P] [US1] Test Api `tests/Lumineux.Api.Tests/ReportsEndpointsTests.cs` : `GET antenna-summary` 200 ; 400 plage invalide ; 401 sans jeton ; 403 sans `manage_attendance` (doit ÉCHOUER)

### Implémentation (US1)

- [X] T008 [US1] Implémenter `src/Lumineux.Application/Reports/GetAntennaAttendanceSummaryHandler.cs` (droit `manage_attendance`, `ReportPeriodValidator`, agrégation via port, calcul moyenne/arrondi) + DI dans `src/Lumineux.Application/DependencyInjection.cs`
- [X] T009 [US1] Créer `src/Lumineux.Api/Controllers/ReportsController.cs` `[Authorize(Policy = Permissions.ManageAttendance)]` avec `GET /api/v1/reports/attendance/antenna-summary` (from/to/antennaId?)

**Checkpoint**: US1 fonctionnelle et testable seule (MVP livrable).

---

## Phase 4: User Story 2 - Taux de présence par membre (Priority: P2)

**Goal**: Taux d'assiduité d'un membre (présences valides ÷ sessions de son antenne d'origine).

**Independent Test**: `GET /reports/attendance/member-rate?memberId=&from=&to=` → validCount, eligibleSessionCount, rate ; taux 0 sans présence ; 404 membre inconnu.

### Tests (US2)

- [X] T010 [P] [US2] Test Application `tests/Lumineux.Application.Tests/MemberAttendanceRateTests.cs` : taux calculé, **pas de division par zéro** (0 %), annulées exclues, **404 membre inconnu**, droit manquant (doit ÉCHOUER)

### Implémentation (US2)

- [X] T011 [US2] Implémenter `src/Lumineux.Application/Reports/GetMemberAttendanceRateHandler.cs` (droit, validation plage, port `GetMemberRateDataAsync` → `NotFoundException` si null, calcul taux sans division par zéro) + DI
- [X] T012 [US2] Ajouter `GET /api/v1/reports/attendance/member-rate` à `ReportsController` + compléter `ReportsEndpointsTests` (200 ; 404 membre)

**Checkpoint**: US1 + US2 opérationnelles.

---

## Phase 5: User Story 3 - Export CSV (Priority: P2)

**Goal**: Export CSV de la synthèse par antenne (mêmes chiffres que le JSON).

**Independent Test**: `GET /reports/attendance/antenna-summary.csv?from=&to=` → text/csv, en-têtes + une ligne par antenne, valeurs identiques à la synthèse.

### Tests (US3)

- [X] T013 [P] [US3] Test Application `tests/Lumineux.Application.Tests/ExportAntennaAttendanceCsvTests.cs` : en-têtes présents, **une ligne par antenne**, valeurs cohérentes avec la synthèse, échappement des champs (doit ÉCHOUER)

### Implémentation (US3)

- [X] T014 [US3] Implémenter `src/Lumineux.Application/Reports/ExportAntennaAttendanceCsvHandler.cs` (réutilise la synthèse US1, rend le CSV : en-têtes, séparateur `;`, UTF-8 BOM, échappement) + DI
- [X] T015 [US3] Ajouter `GET /api/v1/reports/attendance/antenna-summary.csv` à `ReportsController` (réponse `text/csv` + `Content-Disposition`) + compléter `ReportsEndpointsTests` (Content-Type CSV)

**Checkpoint**: les 3 rapports opérationnels.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T016 [P] Compléter `ReportsEndpointsTests` : parcours cohérence (synthèse JSON vs CSV mêmes chiffres) et 401/403 sur les trois endpoints
- [X] T017 [P] Annotations `ProducesResponseType` (200/400/404/401/403) sur `ReportsController` pour Swagger (Principe V)
- [X] T018 Exécuter `dotnet test` (toute la solution verte) puis dérouler `specs/018-attendance-reports/quickstart.md` (A→D, SC-001..006) ; marquer les tâches `[X]`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** : aucune dépendance.
- **Foundational (P2)** : dépend de Setup — **BLOQUE** toutes les stories (port + agrégations).
- **US1 (P3)** → **US2 (P4)** → **US3 (P5)** : toutes après Foundational. US2/US3 étendent le **même
  contrôleur** (`ReportsController`) et le **même test Api** → séquentiel sur ces fichiers ; handlers
  indépendants et testables isolément. **US3 réutilise la synthèse d'US1.**
- **Polish (P7)** : après les stories visées.

### User Story Dependencies

- **US1 (P1)** : port + validateur. **MVP**.
- **US2 (P2)** : port (`GetMemberRateDataAsync`) + validateur.
- **US3 (P2)** : **dépend d'US1** (rend la synthèse en CSV).

### Parallel Opportunities

- Setup : T001, T002 en parallèle.
- Foundational : T003, T004 en parallèle ; puis T005.
- Tests de story (T006/T007, T010, T013) rédigés en parallèle avant impl.
- Polish : T016, T017 en parallèle.

---

## Parallel Example: Foundational (Phase 2)

```text
T003 IAttendanceReportRepository.cs   # port
T004 AttendanceReportRepositoryTests.cs  # test agrégation (rouge)
# puis :
T005 AttendanceReportRepository.cs    # impl EF + DI
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → **VALIDER** (synthèse par antenne) → démo.

### Incremental Delivery

Setup + Foundational → US1 (synthèse, MVP) → US2 (taux membre) → US3 (export CSV) → Polish.

---

## Notes

- **Aucune migration** (lecture seule) ; **aucune dépendance nouvelle** (CSV à la main).
- **Annulées exclues** partout (FR-003/SC-002) ; **taux sans division par zéro** (FR-005/SC-003) ;
  **plage validée** (FR-010/SC-006).
- Droit **`manage_attendance`** réutilisé ; PII minimale (membre = id + nom).
- Commits après chaque tâche ou groupe logique ; s'arrêter aux checkpoints pour valider une story.
