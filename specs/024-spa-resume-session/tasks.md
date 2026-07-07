# Tasks: Console web — Reprendre une session de présence en cours

**Input**: Design documents from `specs/024-spa-resume-session/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — Constitution Lumineux, Principe III (NON-NÉGOCIABLE). Unitaires **Vitest** (service,
encart, reprise, conflit) + e2e **Playwright**.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- Chemins relatifs à la racine du dépôt (app front sous `web/`)

## Path Conventions

- App Angular : `web/src/app/` — service `core/api/`, écran `features/attendance/session-start/`
- Tests unitaires : `*.spec.ts` colocalisés ; e2e : `web/e2e/`

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Extension du service d'accès API — requise par les stories.

**⚠️ CRITICAL**: aucune story ne démarre avant la fin de cette phase.

- [X] T001 [P] Test unitaire dans `web/src/app/core/api/attendance-sessions-api.spec.ts` : `myOpenSessions()` (GET `/api/v1/attendance-sessions/mine/open`) (doit ÉCHOUER)
- [X] T002 [P] Étendre `web/src/app/core/api/attendance-sessions-api.ts` : `myOpenSessions()` → `Observable<SessionResponse[]>`

**Checkpoint**: service `myOpenSessions` testé au vert.

---

## Phase 2: User Story 1 - Reprendre ma session en cours (Priority: P1) 🎯 MVP

**Goal**: Encart « Vous avez une session en cours » au chargement de l'écran de démarrage, avec reprise.

**Independent Test**: Avec une session ouverte, ouvrir `/attendance` → encart (antenne, date, heure) +
« Reprendre » → écran d'animation ; sans session ouverte → aucun encart ; échec non bloquant.

### Tests (US1)

- [X] T003 [P] [US1] Test `web/src/app/features/attendance/session-start/session-start.component.spec.ts` : chargement `AttendanceSessionsApi.myOpenSessions`, **encart** si sessions ouvertes (libellé d'antenne via `ReferenceApi`), **reprise** navigue vers `/attendance/sessions/:id`, **aucun encart** si liste vide, **échec non bloquant** (formulaire utilisable) (doit ÉCHOUER)

### Implémentation (US1)

- [X] T004 [US1] Étendre `web/src/app/features/attendance/session-start/session-start.component.ts` (+ template) : au chargement, appeler `myOpenSessions` (signal + état de chargement) ; **encart** listant chaque session (libellé d'antenne mappé depuis `antennas()`, date, heure de début) avec bouton **« Reprendre »** (`router.navigate(['/attendance/sessions', id])`) ; mapping d'erreur **non bloquant** ; formulaire de démarrage conservé sous l'encart

**Checkpoint**: US1 fonctionnelle et testable seule (MVP livrable — corrige le blocage).

---

## Phase 3: User Story 2 - Proposer la reprise en cas de conflit (Priority: P2)

**Goal**: Sur conflit 409 au démarrage, proposer « Reprendre la session en cours » (antenne+date).

**Independent Test**: Démarrer une session sur une antenne/date déjà occupée → bouton « Reprendre la
session en cours » (au lieu d'un simple message) → écran d'animation ; sans correspondance → message clair.

### Tests (US2)

- [X] T005 [P] [US2] Test dans `session-start.component.spec.ts` : `start()` avec **409** → retrouve la session ouverte correspondant à **antenne + date** (dans `myOpenSessions`) → propose la reprise ; **409 sans correspondance** → message de conflit (pas d'action trompeuse) (doit ÉCHOUER)

### Implémentation (US2)

- [X] T006 [US2] Compléter `start()` dans `session-start.component.ts` : sur erreur **409**, chercher dans les sessions ouvertes celle où `antennaId` = antenne choisie et `meetingDate` (jour) = date choisie → signal de reprise + bouton **« Reprendre la session en cours »** ; sinon `messageForError` (message de conflit)

**Checkpoint**: US1 + US2 opérationnelles.

---

## Phase 4: Polish & Cross-Cutting Concerns

- [X] T007 [P] [US1] E2e Playwright `web/e2e/attendance-resume.spec.ts` : démarrer une session, naviguer ailleurs, revenir sur `/attendance`, **Reprendre** → écran d'animation
- [X] T008 Exécuter `ng test --no-watch` (unitaires au vert) et dérouler `specs/024-spa-resume-session/quickstart.md` (A→C, SC-001..006) ; marquer les tâches `[X]`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (P1)** : service `myOpenSessions` — **BLOQUE** les stories.
- **US1 (P2)** → **US2 (P3)** : US2 complète `session-start` (même fichier) → séquentiel ; indépendamment
  testables.
- **Polish (P4)** : après les stories.

### User Story Dependencies

- **US1 (P1)** : socle + service + `ReferenceApi`. **MVP** (corrige le blocage).
- **US2 (P2)** : étend `session-start` (gestion du conflit) ; réutilise la liste `myOpenSessions`.

### Parallel Opportunities

- Foundational : T001 (test) et T002 (impl) en parallèle.
- US1/US2 : tests T003/T005 rédigés avant impl.

---

## Parallel Example: Foundational (Phase 1)

```text
T001 attendance-sessions-api.spec.ts (cas myOpenSessions)   # test service
T002 attendance-sessions-api.ts (méthode myOpenSessions)    # impl service
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Foundational → 2. Phase 2 US1 → **VALIDER** (encart + reprise) → le blocage est corrigé.

### Incremental Delivery

Foundational → US1 (encart, MVP) → US2 (reprise sur conflit) → Polish.

---

## Notes

- **Aucune dépendance npm** ; réutilise le socle 014, `ReferenceApi` (010), et l'API 023 (`mine/open`).
- **Aucun calcul métier client** : la liste des sessions ouvertes vient de l'API ; le client présente et
  navigue. La vérification est **non bloquante** pour le démarrage.
- Le **409** au démarrage est l'unique conflit → le statut suffit à déclencher la reprise (correspondance
  antenne+date).
- Commits après chaque tâche ou groupe logique.
