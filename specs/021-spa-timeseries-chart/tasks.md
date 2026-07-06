# Tasks: Console web — Courbe d'évolution des présences (SPA)

**Input**: Design documents from `specs/021-spa-timeseries-chart/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — Constitution Lumineux, Principe III (NON-NÉGOCIABLE). Unitaires **Vitest** (service,
tracé proportionnel, série continue, granularité, réactivité, erreurs) + e2e **Playwright**.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- Chemins relatifs à la racine du dépôt (app front sous `web/`)

## Path Conventions

- App Angular : `web/src/app/` — service `core/api/`, écrans `features/reports/`
- Tests unitaires : `*.spec.ts` colocalisés ; e2e : `web/e2e/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Modèles et squelette du panneau.

- [X] T001 [P] Étendre `web/src/app/features/reports/report.models.ts` : `TimeSeriesGranularity` (`'Week' | 'Month'`), `TimeSeriesPoint` (`periodStart`, `label`, `validAttendanceCount`, `sessionCount`), `AttendanceTimeSeriesResponse` (`from`, `to`, `granularity`, `points[]`) d'après `data-model.md`
- [X] T002 [P] Créer l'arborescence `web/src/app/features/reports/time-series-chart/`

**Checkpoint**: modèles et dossier prêts.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extension du service d'accès API — requise par les stories.

**⚠️ CRITICAL**: aucune story ne démarre avant la fin de cette phase.

- [X] T003 [P] Test unitaire dans `web/src/app/core/api/reports-api.spec.ts` : `timeSeries(from, to, granularity, antennaId?)` (GET + query `from`/`to`/`granularity`/`antennaId`) (doit ÉCHOUER)
- [X] T004 [P] Étendre `web/src/app/core/api/reports-api.ts` : `timeSeries(from, to, granularity, antennaId?)` → `Observable<AttendanceTimeSeriesResponse>` (`/api/v1/reports/attendance/time-series`)

**Checkpoint**: service `timeSeries` testé au vert.

---

## Phase 3: User Story 1 - Visualiser l'évolution (Priority: P1) 🎯 MVP

**Goal**: Courbe/aire SVG des présences valides par intervalle (semaine/mois), série continue, lecture
de valeur.

**Independent Test**: Sur `/reports`, choisir une période et une granularité → courbe reliant les points
avec libellés d'intervalle ; hauteur proportionnelle aux valeurs ; intervalle vide à 0 ; valeur lisible.

### Tests (US1)

- [X] T005 [P] [US1] Test `web/src/app/features/reports/time-series-chart/time-series-chart.component.spec.ts` : chargement via `ReportsApi.timeSeries`, **coordonnées Y proportionnelles** (`y ∝ value/max`), **série continue** (point à 0), sélecteur de granularité (défaut Month), **état vide**, lecture de valeur d'un point (doit ÉCHOUER)

### Implémentation (US1)

- [X] T006 [US1] Implémenter `web/src/app/features/reports/time-series-chart/time-series-chart.component.ts` (+ template SVG) : entrées `[from]`/`[to]`/`[antennaId]`, **sélecteur de granularité** (Week/Month), appel `ReportsApi.timeSeries`, calcul des points **polyline** (x uniforme, `y = hauteur − (valeur/max)·hauteur`), **aire** optionnelle, **points** avec info-bulle (`label` + valeur), repères de libellés, états chargement/vide, mapping erreurs (`messageForError`)
- [X] T007 [US1] Intégrer le panneau dans `web/src/app/features/reports/reports-dashboard/reports-dashboard.component.ts` : exposer la **période appliquée** + le **filtre d'antenne** (signaux mis à jour à la validation `Afficher`) et insérer `<app-time-series-chart [from]="…" [to]="…" [antennaId]="…" />`

**Checkpoint**: US1 fonctionnelle et testable seule (MVP livrable).

---

## Phase 4: User Story 2 - Réactivité au contexte (Priority: P2)

**Goal**: La courbe se met à jour au changement de période, d'antenne ou de granularité.

**Independent Test**: Basculer mois↔semaine, changer période/antenne → nouvel appel et courbe à jour.

### Tests (US2)

- [X] T008 [P] [US2] Test dans `time-series-chart.component.spec.ts` : changement de **granularité** → nouvel appel `timeSeries` ; changement des **entrées** (`from`/`to`/`antennaId`) → nouvel appel ; erreur de plage/granularité mappée en message clair (doit ÉCHOUER)

### Implémentation (US2)

- [X] T009 [US2] Implémenter la **réactivité** dans `time-series-chart.component.ts` (effet sur entrées + signal de granularité → rechargement) et s'assurer que `reports-dashboard` **applique** le contexte à la validation (période/antenne)

**Checkpoint**: US1 + US2 opérationnelles.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T010 [P] [US1] E2e Playwright `web/e2e/reports-evolution.spec.ts` : période → courbe affichée ; bascule semaine/mois met à jour la courbe
- [X] T011 [P] Vérifier **responsive** (bureau + tablette), libellés **français**, lisibilité des repères d'intervalle
- [X] T012 Exécuter `ng test --no-watch` (unitaires au vert) et dérouler `specs/021-spa-timeseries-chart/quickstart.md` (A→C, SC-001..006) ; marquer les tâches `[X]`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** : aucune dépendance.
- **Foundational (P2)** : dépend de Setup — **BLOQUE** les stories (service `timeSeries`).
- **US1 (P3)** → **US2 (P4)** : US2 étend le composant courbe (même fichier) → séquentiel ; la réactivité
  s'appuie sur l'intégration d'US1. Indépendamment testables.
- **Polish (P5)** : après les stories.

### User Story Dependencies

- **US1 (P1)** : socle + service + tableau de bord 019. **MVP**.
- **US2 (P2)** : étend `time-series-chart` (réactivité) + contexte appliqué du tableau de bord.

### Parallel Opportunities

- Setup : T001, T002 en parallèle.
- Foundational : T003 (test) et T004 (impl) en parallèle.
- Polish : T010, T011 en parallèle.

---

## Parallel Example: Foundational (Phase 2)

```text
T003 reports-api.spec.ts (cas timeSeries)   # test service
T004 reports-api.ts (méthode timeSeries)    # impl service
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → **VALIDER** (courbe mois/semaine) → démo.

### Incremental Delivery

Setup + Foundational → US1 (courbe, MVP) → US2 (réactivité) → Polish.

---

## Notes

- **Aucune dépendance npm** ; courbe/aire en **SVG maison** ; réutilise le tableau de bord 019 (période +
  filtre d'antenne) et `ReportsApi`.
- **Aucun calcul statistique client** : les points viennent de l'API 020 ; le client met à l'échelle et
  trace ; **série continue** (zéros) respectée.
- Le panneau réagit au **contexte appliqué** (pas à chaque frappe de date).
- Commits après chaque tâche ou groupe logique ; s'arrêter aux checkpoints pour valider une story.
