# Tasks: Console web — Gestion des antennes (SPA)

**Input**: Design documents from `specs/017-spa-antenna-management/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — Constitution Lumineux, Principe III (NON-NÉGOCIABLE). Unitaires **Vitest** (service,
liste, formulaire, garde, mapping d'erreurs, confirmation) + e2e **Playwright**.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- Chemins relatifs à la racine du dépôt (app front sous `web/`)

## Path Conventions

- App Angular : `web/src/app/` — service `core/api/`, écrans `features/antennas/`
- Tests unitaires : `*.spec.ts` colocalisés ; e2e : `web/e2e/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Modèles et squelette du module.

- [X] T001 [P] Créer les modèles `web/src/app/features/antennas/antenna.models.ts` (`AntennaResponse`, `CreateAntennaRequest`, `UpdateAntennaRequest`, `AntennaStatus`) d'après `data-model.md`
- [X] T002 [P] Créer l'arborescence `web/src/app/features/antennas/` (sous-dossiers `antenna-list/`, `antenna-form/`)

**Checkpoint**: modèles et dossiers prêts.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Service d'accès API, routes gardées et navigation — requis par TOUTES les stories.

**⚠️ CRITICAL**: aucune story ne démarre avant la fin de cette phase.

- [X] T003 [P] Test unitaire `web/src/app/core/api/antennas-api.spec.ts` : `list`, `get`, `create` (POST), `update` (PUT), `deactivate`, `activate` (POST) — URLs/verbes (doit ÉCHOUER)
- [X] T004 [P] Implémenter `web/src/app/core/api/antennas-api.ts` : `list()`, `get(id)`, `create(body)`, `update(id, body)`, `deactivate(id)`, `activate(id)` (base `environment.apiBaseUrl` + `/api/v1/antennas`)
- [X] T005 Ajouter les routes `/antennas` (`antenna-list`), `/antennas/new` et `/antennas/:id/edit` (`antenna-form`) dans `web/src/app/app.routes.ts`, gardées `permissionGuard` + `data: { permission: 'manage_referentials' }` (voir `contracts/routes.md`)
- [X] T006 Ajouter l'entrée de nav **« Antennes »** (lien réel `/antennas`, `permission: 'manage_referentials'`) dans `web/src/app/shell/shell.component.ts` et mettre à jour `web/src/app/shell/shell.component.spec.ts`

**Checkpoint**: service testé au vert, navigation et routes gardées opérationnelles.

---

## Phase 3: User Story 1 - Lister et consulter (Priority: P1) 🎯 MVP

**Goal**: Afficher toutes les antennes (actives + inactives) avec statut, points d'entrée vers les actions.

**Independent Test**: Se connecter (droit référentiels), ouvrir « Antennes » et voir la liste complète
avec statut ; l'entrée est masquée / l'accès refusé sans le droit.

### Tests (US1)

- [X] T007 [P] [US1] Test `web/src/app/features/antennas/antenna-list/antenna-list.component.spec.ts` : chargement via `AntennasApi.list`, affichage actives **et** inactives avec statut, état vide, liens vers édition/nouvelle (doit ÉCHOUER)

### Implémentation (US1)

- [X] T008 [US1] Implémenter `web/src/app/features/antennas/antenna-list/antenna-list.component.ts` (+ template) : charge `AntennasApi.list`, tableau (code, libellé, district, statut), lien « Nouvelle antenne » et lien d'édition par ligne, état de chargement/vide (français, responsive)

**Checkpoint**: US1 fonctionnelle et testable seule (MVP livrable).

---

## Phase 4: User Story 2 - Créer une antenne (Priority: P1)

**Goal**: Créer une antenne (code + libellé + district), erreurs mappées.

**Independent Test**: Créer avec code inédit + district → apparaît dans la liste ; code dupliqué →
message clair ; formulaire incomplet → validation.

### Tests (US2)

- [X] T009 [P] [US2] Test `web/src/app/features/antennas/antenna-form/antenna-form.component.spec.ts` : chargement des districts (`ReferenceApi.districts`), validation (code+libellé+district requis), création `AntennasApi.create`, mapping `duplicate_code`, navigation vers `/antennas` au succès (doit ÉCHOUER)

### Implémentation (US2)

- [X] T010 [US2] Implémenter `web/src/app/features/antennas/antenna-form/antenna-form.component.ts` (+ template) : formulaire réactif (code, libellé, district via `ReferenceApi.districts`), appel `AntennasApi.create`, mapping erreurs (`messageForError` + libellé dédié `duplicate_code`), navigation retour liste

**Checkpoint**: US1 + US2 opérationnelles.

---

## Phase 5: User Story 3 - Modifier une antenne (Priority: P2)

**Goal**: Corriger libellé + district ; code en lecture seule.

**Independent Test**: Éditer une antenne → libellé/district mis à jour ; champ code non modifiable.

### Tests (US3)

- [X] T011 [P] [US3] Test dans `antenna-form.component.spec.ts` : mode édition (`:id`) charge l'antenne (`AntennasApi.get`), **code en lecture seule**, `AntennasApi.update` (sans code), navigation au succès (doit ÉCHOUER)

### Implémentation (US3)

- [X] T012 [US3] Étendre `web/src/app/features/antennas/antenna-form/antenna-form.component.ts` : détecter `:id` (édition) → pré-remplir, **désactiver le champ code** (lecture seule), appeler `AntennasApi.update(id, { label, districtId })`

**Checkpoint**: US1 + US2 + US3 opérationnelles.

---

## Phase 6: User Story 4 - Activer / désactiver (Priority: P2)

**Goal**: Désactiver (confirmé) / réactiver ; refus session ouverte mappé ; liste à jour.

**Independent Test**: Désactiver (confirmation) → statut inactif ; réactiver → actif ; désactivation
d'une antenne à session ouverte → message clair, statut inchangé.

### Tests (US4)

- [X] T013 [P] [US4] Test dans `antenna-list.component.spec.ts` : **désactivation** demande **confirmation** puis `AntennasApi.deactivate` + rafraîchissement ; **réactivation** `AntennasApi.activate` ; mapping `antenna_has_open_sessions` (409) (doit ÉCHOUER)

### Implémentation (US4)

- [X] T014 [US4] Ajouter à `antenna-list.component.ts` les actions **Désactiver** (avec **confirmation**) et **Réactiver** → `AntennasApi.deactivate`/`activate`, rafraîchir la liste, mapper le refus `antenna_has_open_sessions` en message clair (notification)

**Checkpoint**: les 4 stories opérationnelles.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T015 [P] [US1] E2e Playwright `web/e2e/antennas-list.spec.ts` : liste avec actives + inactives, entrée masquée sans droit
- [X] T016 [P] [US2] E2e Playwright `web/e2e/antennas-create.spec.ts` : création + code dupliqué refusé
- [X] T017 [P] [US3] E2e Playwright `web/e2e/antennas-edit.spec.ts` : édition, code en lecture seule
- [X] T018 [P] [US4] E2e Playwright `web/e2e/antennas-status.spec.ts` : désactiver/réactiver + refus session ouverte
- [X] T019 [P] Vérifier **responsive** (bureau + tablette) et libellés en **français**
- [X] T020 Exécuter `ng test --no-watch` (unitaires au vert) et dérouler `specs/017-spa-antenna-management/quickstart.md` (A→E, SC-001..007) ; marquer les tâches `[X]`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)** : aucune dépendance.
- **Foundational (P2)** : dépend de Setup — **BLOQUE** toutes les stories (service, routes, nav).
- **US1 (P3)** → **US2 (P4)** → **US3 (P5)** → **US4 (P6)** : toutes après Foundational. US3 étend
  `antenna-form` (US2) ; US4 étend `antenna-list` (US1) → séquentiel sur ces fichiers ; indépendamment
  testables.
- **Polish (P7)** : après les stories visées.

### User Story Dependencies

- **US1 (P1)** : socle + service. **MVP**.
- **US2 (P1)** : service + `ReferenceApi.districts` ; nouveau composant `antenna-form`.
- **US3 (P2)** : étend `antenna-form` (mode édition).
- **US4 (P2)** : étend `antenna-list` (actions de statut).

### Parallel Opportunities

- Setup : T001, T002 en parallèle.
- Foundational : T003 (test) et T004 (impl) ; puis T005, T006.
- Polish : e2e T015–T018 en parallèle.

---

## Parallel Example: Foundational (Phase 2)

```text
T003 antennas-api.spec.ts   # test service (rouge)
T004 antennas-api.ts        # impl service
# puis :
T005 app.routes.ts          # routes gardées
T006 shell.component.ts     # nav « Antennes »
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → **VALIDER** (liste de gestion) → démo.

### Incremental Delivery

Setup + Foundational → US1 (liste, MVP) → US2 (créer) → US3 (modifier) → US4 (activer/désactiver) →
Polish. Chaque story ajoute de la valeur sans casser les précédentes.

---

## Notes

- Aucune dépendance npm nouvelle ; réutilise le socle 008 et `ReferenceApi` (010).
- **Code immuable** en édition (FR-009/SC-005) ; **confirmation** de désactivation (FR-010/SC-007).
- Erreurs 409 : `duplicate_code` et `antenna_has_open_sessions` → **libellés dédiés** ; le reste via
  `messageForError`.
- **Lecture publique 010 inchangée** (utilisée ailleurs) — ne pas la modifier.
- Commits après chaque tâche ou groupe logique ; s'arrêter aux checkpoints pour valider une story.
