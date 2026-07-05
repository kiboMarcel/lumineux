# Tasks: Console web — Présences (SPA, Lot 4)

**Input**: Design documents from `specs/014-spa-attendance/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — la Constitution Lumineux (Principe III, NON-NÉGOCIABLE) impose les tests d'abord.
Unitaires **Vitest** (services, rotation QR, polling, lookup, confirmations, masquage) + e2e **Playwright**.

**Organization**: tâches groupées par user story pour livraison incrémentale (MVP = US1).

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- Chemins relatifs à la racine du dépôt (app front sous `web/`)

## Path Conventions

- App Angular : `web/src/app/` — services `core/api/`, écrans `features/attendance/`
- Tests unitaires : `*.spec.ts` colocalisés ; e2e : `web/e2e/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Dépendance QR et squelette du module.

- [X] T001 Installer une **bibliothèque cliente de génération de QR** dans `web/` (⚠️ `npm install` = appel réseau → **approbation explicite requise** ; indiquer paquet + URL avant), puis vérifier le build `web/package.json`
- [X] T002 [P] Créer l'arborescence du module `web/src/app/features/attendance/` (sous-dossiers `session-start/`, `session-run/`, `session-run/qr-panel/`, `session-run/manual-add/`)
- [X] T003 [P] Créer le fichier de modèles de vue `web/src/app/features/attendance/attendance.models.ts` (SessionResponse, QrTokenResponse, AttendanceResponse, AttendanceListResponse, StartSessionRequest, ManualAttendanceRequest, AttendanceStatus, MemberLookupItem) d'après `data-model.md`

**Checkpoint**: dépendance QR disponible, dossiers + modèles prêts.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Services d'accès API, routes gardées et navigation — requis par TOUTES les stories.

**⚠️ CRITICAL**: aucune story ne démarre avant la fin de cette phase.

- [X] T004 [P] Test unitaire `web/src/app/core/api/attendance-sessions-api.spec.ts` : `start` (POST), `get` (GET), `qr` (GET), `close` (POST) — URLs/verbes/erreurs (doit ÉCHOUER avant impl.)
- [X] T005 [P] Test unitaire `web/src/app/core/api/attendances-api.spec.ts` : `list(status)`, `addManual`, `cancel` — statuts 200/201/204/409 (doit ÉCHOUER)
- [X] T006 [P] Test unitaire `web/src/app/core/api/member-lookup-api.spec.ts` : `lookup(query)` → MemberLookupItem[] (doit ÉCHOUER)
- [X] T007 [P] Implémenter `web/src/app/core/api/attendance-sessions-api.ts` : `start`, `get`, `qr`, `close` (base `environment.apiBaseUrl` + `/api/v1/attendance-sessions`)
- [X] T008 [P] Implémenter `web/src/app/core/api/attendances-api.ts` : `list(sessionId, status)`, `addManual(sessionId, req)`, `cancel(sessionId, memberId)`
- [X] T009 [P] Implémenter `web/src/app/core/api/member-lookup-api.ts` : `lookup(query)` → `GET /api/v1/members/lookup?query=`
- [X] T010 Ajouter les routes `/attendance` (`session-start`) et `/attendance/sessions/:id` (`session-run`) dans `web/src/app/app.routes.ts`, gardées `authGuard` + `permissionGuard('manage_attendance')` (voir `contracts/routes.md`)
- [X] T011 Transformer l'entrée « Présences » (placeholder) en **lien réel** vers `/attendance` dans `web/src/app/shell/shell.component.ts` (`NavItem.permission = 'manage_attendance'`) et mettre à jour `web/src/app/shell/shell.component.spec.ts`

**Checkpoint**: services testés au vert, navigation et routes gardées opérationnelles.

---

## Phase 3: User Story 1 - Démarrer une session + QR rotatif (Priority: P1) 🎯 MVP

**Goal**: Démarrer une session (antenne + date + pas) et projeter un QR qui se renouvelle seul.

**Independent Test**: Se connecter (droit présences), démarrer une session, voir un QR **en grand** qui
se **régénère** avant expiration ; le jeton n'apparaît jamais en clair.

### Tests (US1)

- [X] T012 [P] [US1] Test `web/src/app/features/attendance/session-start/session-start.component.spec.ts` : chargement des antennes (référentiel), validation (antenne+date requis), **démarrage empêché si aucune antenne**, navigation vers `/attendance/sessions/:id` au succès (doit ÉCHOUER)
- [X] T013 [P] [US1] Test `web/src/app/features/attendance/session-run/qr-panel/qr-panel.component.spec.ts` : rendu image depuis le jeton, **ré-appel `qr` + regénération avant expiration** (horloge simulée), **arrêt du timer à la destruction**, jeton **jamais** rendu en texte (doit ÉCHOUER)
- [X] T014 [P] [US1] Test `web/src/app/features/attendance/session-run/session-run.component.spec.ts` : chargement session par `:id`, affichage du panneau QR quand ouverte (doit ÉCHOUER)

### Implémentation (US1)

- [X] T015 [US1] Implémenter `web/src/app/features/attendance/session-start/session-start.component.ts` (+ template) : formulaire réactif (antenne via `ReferenceApi`, date, pas de rotation optionnel), appel `AttendanceSessionsApi.start`, mapping erreurs (`error-messages`), garde « aucune antenne active »
- [X] T016 [P] [US1] Implémenter `web/src/app/features/attendance/session-run/qr-panel/qr-panel.component.ts` (+ template) : génération image via la bibliothèque QR, **rotation** (ré-appel `qr` avant `expiresAt`/au rythme `stepSeconds`), timer borné au cycle de vie, jeton **en mémoire uniquement**
- [X] T017 [US1] Implémenter `web/src/app/features/attendance/session-run/session-run.component.ts` (+ template) : charge la session par `:id` (`AttendanceSessionsApi.get`), compose `qr-panel` si session ouverte, gère état transitoire/erreur QR (réessai)

**Checkpoint**: US1 fonctionnelle et testable seule (MVP livrable).

---

## Phase 4: User Story 2 - Suivre les présences en temps réel (Priority: P1)

**Goal**: Liste des présences + décompte des valides, rafraîchis périodiquement, filtrables par statut.

**Independent Test**: Sur session ouverte, la liste se met à jour sans action manuelle ; le filtre
valides/annulées/toutes restreint l'affichage ; décompte des valides affiché.

### Tests (US2)

- [X] T018 [P] [US2] Test dans `web/src/app/features/attendance/session-run/session-run.component.spec.ts` : **polling** de `attendances` (horloge simulée) met à jour liste + décompte ; changement de **filtre** re-requête ; arrêt du polling à la destruction/clôture (doit ÉCHOUER)

### Implémentation (US2)

- [X] T019 [US2] Étendre `web/src/app/features/attendance/session-run/session-run.component.ts` : **polling** (~5 s) de `AttendancesApi.list(status)`, signal liste + `validCount`, sélecteur de **filtre** (Valid/Cancelled/All), arrêt borné au cycle de vie et à la clôture
- [X] T020 [US2] Ajouter au template de `session-run` : tableau des présences (membre, heure d'arrivée, source, statut), **décompte des valides**, contrôle de filtre (français, responsive)

**Checkpoint**: US1 + US2 opérationnelles indépendamment.

---

## Phase 5: User Story 3 - Ajouter / retirer manuellement (Priority: P2)

**Goal**: Ajout manuel via lookup (idempotent) et annulation confirmée, refusés si session close.

**Independent Test**: Ajouter un membre trouvé par lookup → apparaît ; réajout sans doublon ; annuler
(avec confirmation) → statut annulé ; sur session close → refus (409) clair.

### Tests (US3)

- [X] T021 [P] [US3] Test `web/src/app/features/attendance/session-run/manual-add/manual-add.component.spec.ts` : recherche via `MemberLookupApi`, sélection membre, `addManual`, **idempotence** (réajout sans doublon), erreur 409 session close (doit ÉCHOUER)
- [X] T022 [P] [US3] Test dans `session-run.component.spec.ts` : **annulation** demande **confirmation** puis `AttendancesApi.cancel` ; refus 409 mappé (doit ÉCHOUER)

### Implémentation (US3)

- [X] T023 [US3] Implémenter `web/src/app/features/attendance/session-run/manual-add/manual-add.component.ts` (+ template) : champ de recherche → `MemberLookupApi.lookup`, liste de résultats minimaux, sélection → `AttendancesApi.addManual`, gestion idempotence + mapping erreurs
- [X] T024 [US3] Intégrer `manual-add` dans `session-run` et l'action **Annuler** (par présence) avec **confirmation** (`FR-016`) → `AttendancesApi.cancel`, refresh liste ; masquer ces actions si session close

**Checkpoint**: US1 + US2 + US3 opérationnelles.

---

## Phase 6: User Story 4 - Clôturer la session (Priority: P2)

**Goal**: Clôture confirmée ; après clôture, QR + écritures masqués ; heure de fin figée.

**Independent Test**: Clôturer (confirmation) → statut close + heure de fin ; QR et actions d'écriture
disparaissent ; écriture ultérieure refusée.

### Tests (US4)

- [X] T025 [P] [US4] Test dans `session-run.component.spec.ts` : **clôture** demande **confirmation** puis `AttendanceSessionsApi.close` ; après clôture, **QR masqué** et ajout/annulation **indisponibles** (`FR-011`, `SC-006`) (doit ÉCHOUER)

### Implémentation (US4)

- [X] T026 [US4] Ajouter l'action **Clôturer** dans `session-run.component.ts` avec **confirmation** → `AttendanceSessionsApi.close`, mise à jour de l'état ; arrêt du polling QR/liste
- [X] T027 [US4] Masquer dans le template de `session-run` le panneau QR, l'ajout manuel et l'annulation lorsque `status = close` ; afficher l'état clôturé (heure de fin)

**Checkpoint**: cycle complet démarrer → animer → clôturer.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T028 [P] [US1] E2e Playwright `web/e2e/attendance-start.spec.ts` : démarrer une session, QR affiché
- [X] T029 [P] [US2] E2e Playwright `web/e2e/attendance-tracking.spec.ts` : liste qui se met à jour + filtre
- [X] T030 [P] [US3] E2e Playwright `web/e2e/attendance-manual.spec.ts` : ajout manuel via lookup + annulation
- [X] T031 [P] [US4] E2e Playwright `web/e2e/attendance-close.spec.ts` : clôture + masquage QR/écritures
- [X] T032 [P] Vérifier **responsive** (bureau + tablette) et **QR lisible en grand** (`FR-017`) ; libellés en **français**
- [X] T033 Exécuter `ng test --no-watch` (unitaires au vert) et dérouler `specs/014-spa-attendance/quickstart.md` (scénarios A→F, SC-001..007) ; marquer les tâches `[X]`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** : aucune dépendance.
- **Foundational (P2)** : dépend de Setup — **BLOQUE** toutes les stories.
- **US1 (P3)** → **US2 (P4)** → **US3 (P5)** → **US4 (P6)** : toutes après Foundational. US2/US3/US4 étendent `session-run` (mêmes fichiers) → séquentiel recommandé ; indépendamment testables.
- **Polish (P7)** : après les stories visées.

### User Story Dependencies

- **US1 (P1)** : socle uniquement. **MVP**.
- **US2 (P1)** : socle ; enrichit `session-run` (après US1 de préférence).
- **US3 (P2)** : dépend du **lookup (T009)** ; enrichit `session-run`.
- **US4 (P2)** : socle ; enrichit `session-run`.

### Within Each User Story

- Tests d'abord (rouge), puis implémentation (vert).
- Services avant composants ; composant conteneur (`session-run`) avant ses enfants intégrés.

### Parallel Opportunities

- Setup : T002/T003 en parallèle (après T001).
- Foundational : tests T004–T006 en parallèle ; impl. T007–T009 en parallèle ; puis T010, T011.
- US1 : tests T012–T014 en parallèle ; T016 (`qr-panel`) parallèle à T015 (`session-start`).
- Polish : e2e T028–T031 en parallèle.

---

## Parallel Example: Foundational (Phase 2)

```text
# Tests services ensemble :
T004 attendance-sessions-api.spec.ts
T005 attendances-api.spec.ts
T006 member-lookup-api.spec.ts

# Puis implémentations ensemble :
T007 attendance-sessions-api.ts
T008 attendances-api.ts
T009 member-lookup-api.ts
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → **VALIDER** (démarrage + QR rotatif) → démo.

### Incremental Delivery

Setup + Foundational → US1 (MVP) → US2 (temps réel) → US3 (manuel/annulation) → US4 (clôture) → Polish.
Chaque story ajoute de la valeur sans casser les précédentes.

---

## Notes

- **T001** = `npm install` (réseau) : **approbation explicite** requise avant exécution (URL + paquet).
- [P] = fichiers distincts, sans dépendance en attente.
- **Jeton QR jamais affiché/persisté** (FR-005/SC-005) — vérifié en test (T013) et e2e.
- Commits après chaque tâche ou groupe logique ; s'arrêter aux checkpoints pour valider une story.
