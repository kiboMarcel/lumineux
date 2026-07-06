# Tasks: Console web — Tableau de bord des rapports de présence (SPA)

**Input**: Design documents from `specs/019-spa-attendance-reports/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — Constitution Lumineux, Principe III (NON-NÉGOCIABLE). Unitaires **Vitest** (service
dont Blob, synthèse+barres, export, taux membre) + e2e **Playwright**.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- Chemins relatifs à la racine du dépôt (app front sous `web/`)

## Path Conventions

- App Angular : `web/src/app/` — service `core/api/`, écrans `features/reports/`
- Tests unitaires : `*.spec.ts` colocalisés ; e2e : `web/e2e/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Modèles et squelette du module.

- [X] T001 [P] Créer les modèles `web/src/app/features/reports/report.models.ts` (`AntennaAttendanceSummaryItem`, `AntennaAttendanceSummaryResponse`, `MemberAttendanceRateResponse`) d'après `data-model.md`
- [X] T002 [P] Créer l'arborescence `web/src/app/features/reports/` (sous-dossiers `reports-dashboard/`, `member-rate/`)

**Checkpoint**: modèles et dossiers prêts.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Service d'accès API, route gardée et navigation — requis par TOUTES les stories.

**⚠️ CRITICAL**: aucune story ne démarre avant la fin de cette phase.

- [X] T003 [P] Test unitaire `web/src/app/core/api/reports-api.spec.ts` : `antennaSummary` (GET + query from/to/antennaId), `antennaSummaryCsv` (GET **responseType blob**), `memberRate` (GET) (doit ÉCHOUER)
- [X] T004 [P] Implémenter `web/src/app/core/api/reports-api.ts` : `antennaSummary(from,to,antennaId?)`, `antennaSummaryCsv(from,to,antennaId?)` → `Observable<Blob>`, `memberRate(memberId,from,to)` (base `environment.apiBaseUrl` + `/api/v1/reports/attendance`)
- [X] T005 Ajouter la route `/reports` (`reports-dashboard`) dans `web/src/app/app.routes.ts`, gardée `permissionGuard` + `data: { permission: 'manage_attendance' }` (voir `contracts/routes.md`)
- [X] T006 Ajouter l'entrée de nav **« Rapports »** (lien réel `/reports`, `permission: 'manage_attendance'`) dans `web/src/app/shell/shell.component.ts` et mettre à jour `web/src/app/shell/shell.component.spec.ts`

**Checkpoint**: service testé au vert, navigation et route gardées opérationnelles.

---

## Phase 3: User Story 1 - Synthèse par antenne (Priority: P1) 🎯 MVP

**Goal**: Afficher la synthèse par antenne d'une période (tableau + barres CSS/SVG proportionnelles).

**Independent Test**: Se connecter (droit présences), choisir une plage, voir tableau + barres ; filtre
antenne ; état vide ; plage invalide signalée.

### Tests (US1)

- [X] T007 [P] [US1] Test `web/src/app/features/reports/reports-dashboard/reports-dashboard.component.spec.ts` : chargement `ReportsApi.antennaSummary`, **barres proportionnelles** (hauteur = valeur/max), **filtre antenne** (via `ReferenceApi.antennas`), **état vide**, **plage invalide** (validation locale, aucun appel) (doit ÉCHOUER)

### Implémentation (US1)

- [X] T008 [US1] Implémenter `web/src/app/features/reports/reports-dashboard/reports-dashboard.component.ts` (+ template) : formulaire de **période** (from/to) + **filtre antenne** (`ReferenceApi.antennas`), appel `ReportsApi.antennaSummary`, **tableau** (sessions, présences valides, moyenne) + **barres CSS/SVG** (proportion `valeur/max`), états chargement/vide, mapping erreurs (`messageForError`)

**Checkpoint**: US1 fonctionnelle et testable seule (MVP livrable).

---

## Phase 4: User Story 2 - Export CSV (Priority: P2)

**Goal**: Télécharger la synthèse de la période en CSV (requête authentifiée + fichier navigateur).

**Independent Test**: Cliquer « Exporter (CSV) » → téléchargement d'un fichier au nom reflétant la
période, chiffres cohérents avec le tableau.

### Tests (US2)

- [X] T009 [P] [US2] Test dans `reports-dashboard.component.spec.ts` : le clic **Exporter** appelle `ReportsApi.antennaSummaryCsv` (Blob) pour la période/filtre courants et déclenche un téléchargement (URL d'objet créée puis révoquée) (doit ÉCHOUER)

### Implémentation (US2)

- [X] T010 [US2] Ajouter à `reports-dashboard.component.ts` le bouton **Exporter (CSV)** : `ReportsApi.antennaSummaryCsv` → `URL.createObjectURL(blob)` → ancre `download` (nom `presence-antennes_<from>_<to>.csv`) → `revokeObjectURL` ; gestion d'erreur mappée

**Checkpoint**: US1 + US2 opérationnelles.

---

## Phase 5: User Story 3 - Taux par membre (Priority: P2)

**Goal**: Sélectionner un membre (recherche allégée) et afficher son taux (%) avec jauge.

**Independent Test**: Sélectionner un membre, afficher présences valides / sessions éligibles / taux % ;
0 % sans présence ; 404 membre inconnu ; sans membre → aucun appel.

### Tests (US3)

- [X] T011 [P] [US3] Test `web/src/app/features/reports/member-rate/member-rate.component.spec.ts` : recherche via `MemberLookupApi`, sélection membre, `ReportsApi.memberRate`, affichage **pourcentage** (rate*100), 0 % sans présence, 404 mappé, **aucun appel sans membre sélectionné** (doit ÉCHOUER)

### Implémentation (US3)

- [X] T012 [US3] Implémenter `web/src/app/features/reports/member-rate/member-rate.component.ts` (+ template) : entrées `[from]`/`[to]`, sélecteur membre via `MemberLookupApi.lookup`, appel `ReportsApi.memberRate`, **jauge** de pourcentage (largeur = `rate`), mapping erreurs ; puis l'intégrer dans `reports-dashboard` (période partagée)

**Checkpoint**: les 3 rapports opérationnels.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T013 [P] [US1] E2e Playwright `web/e2e/reports-summary.spec.ts` : période → tableau + barres ; entrée masquée sans droit
- [X] T014 [P] [US2] E2e Playwright `web/e2e/reports-export.spec.ts` : export CSV déclenche un téléchargement
- [X] T015 [P] [US3] E2e Playwright `web/e2e/reports-member-rate.spec.ts` : sélection membre + taux en %
- [X] T016 [P] Vérifier **responsive** (bureau + tablette), libellés **français**, affichage des **libellés** antenne/membre (pas seulement des ids)
- [X] T017 Exécuter `ng test --no-watch` (unitaires au vert) et dérouler `specs/019-spa-attendance-reports/quickstart.md` (A→D, SC-001..006) ; marquer les tâches `[X]`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** : aucune dépendance.
- **Foundational (P2)** : dépend de Setup — **BLOQUE** toutes les stories (service, route, nav).
- **US1 (P3)** → **US2 (P4)** → **US3 (P5)** : toutes après Foundational. US2 étend `reports-dashboard`
  (US1) → séquentiel sur ce fichier ; US3 est un composant enfant puis intégré. Indépendamment testables.
- **Polish (P7)** : après les stories visées.

### User Story Dependencies

- **US1 (P1)** : socle + service + `ReferenceApi`. **MVP**.
- **US2 (P2)** : étend `reports-dashboard` (bouton export, `antennaSummaryCsv`).
- **US3 (P2)** : nouveau composant `member-rate` + `MemberLookupApi` (015), intégré au tableau de bord.

### Parallel Opportunities

- Setup : T001, T002 en parallèle.
- Foundational : T003 (test) et T004 (impl) ; puis T005, T006.
- Polish : e2e T013–T015 en parallèle.

---

## Parallel Example: Foundational (Phase 2)

```text
T003 reports-api.spec.ts   # test service (dont Blob)
T004 reports-api.ts        # impl service
# puis :
T005 app.routes.ts         # route gardée
T006 shell.component.ts    # nav « Rapports »
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → **VALIDER** (synthèse par antenne) → démo.

### Incremental Delivery

Setup + Foundational → US1 (synthèse, MVP) → US2 (export CSV) → US3 (taux membre) → Polish.

---

## Notes

- **Aucune dépendance npm** ; barres/jauges en **CSS/SVG** maison ; réutilise socle 008, `ReferenceApi`
  (010), `MemberLookupApi` (015).
- **Aucun calcul statistique client** : présentation seule (taux → %, barres proportionnelles).
- **Export CSV** via **Blob authentifié** (jeton porté par l'intercepteur), pas un simple lien.
- Commits après chaque tâche ou groupe logique ; s'arrêter aux checkpoints pour valider une story.
