---
description: "Task list — Installation découvrable du premier administrateur (SPA)"
---

# Tasks: Console web — Installation découvrable du premier administrateur (SPA)

**Input**: Design documents from `specs/013-spa-discoverable-setup/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-behavior.md

**Tests**: INCLUS — Constitution III (esprit). Unitaires **Vitest** ; e2e **Playwright**.

**Organization**: 2 user stories (P1). **Extension minimale** de l'app Angular `web/` (features 008/012)
— un lien conditionnel sur l'écran de connexion. **Réutilise** l'écran d'installation (008) et le
statut (012) ; **API non modifiée**.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US2 (uniquement pour les phases de user story)
- Chemins relatifs à la racine du dépôt (dossier `web/`)

---

## Phase 1: Setup

**Aucune tâche** : app `web/` déjà en place ; aucune dépendance ni configuration.

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: Modèle et accès au statut d'installation, consommés par l'écran de connexion.

**⚠️ CRITICAL**: Les user stories dépendent de cette phase.

- [X] T001 [P] Ajouter le modèle `SetupStatus { installed: boolean }` dans `web/src/app/core/api/models.ts`
- [X] T002 Ajouter `status()` (→ `GET /api/v1/setup/status`, retourne `SetupStatus`) à `SetupApi` dans `web/src/app/core/api/setup-api.ts`
- [X] T003 [P] Tests unitaires **Vitest** `SetupApi.status` (URL `/api/v1/setup/status`, méthode GET) dans `web/src/app/core/api/setup-api.spec.ts`

**Checkpoint**: Le statut d'installation est consommable — l'écran de connexion peut décider l'affichage.

---

## Phase 3: User Story 1 — Découvrir l'installation sur une instance vierge (Priority: P1) 🎯 MVP

**Goal**: Sur une instance non initialisée, l'écran de connexion affiche un lien « Première
installation » menant à l'écran d'installation existant.

**Independent Test**: Statut `installed=false` → lien visible sur la connexion ; le suivre mène à
`/setup/first-admin` (écran existant).

### Tests for User Story 1 ⚠️ (écrire d'abord)

- [X] T004 [P] [US1] Tests unitaires `LoginComponent` : appel de `SetupApi.status` au chargement ; `showSetupLink` **vrai** quand `installed=false` (lien affiché) dans `web/src/app/features/login/login.component.spec.ts`
- [X] T005 [P] [US1] Test e2e **Playwright** (instance vierge : lien « Première installation » visible sur la connexion et menant à l'écran d'installation) dans `web/e2e/discoverable-setup.spec.ts`

### Implementation for User Story 1

- [X] T006 [US1] `LoginComponent` : au chargement, appeler `SetupApi.status()` ; exposer un signal `showSetupLink` (= `installed === false`) ; afficher **conditionnellement** un lien « Première installation » vers `/setup/first-admin` dans le gabarit dans `web/src/app/features/login/login.component.ts`

**Checkpoint**: US1 fonctionnelle — l'installation est découvrable sur une instance vierge (MVP).

---

## Phase 4: User Story 2 — Ne rien proposer sur une instance déjà installée (Priority: P1)

**Goal**: Aucun point d'entrée d'installation quand l'instance est installée, et **défaut sûr = masqué**
si le statut est indisponible (sans bloquer la connexion).

**Independent Test**: Statut `installed=true` → **aucun** lien ; échec du statut → **aucun** lien et
connexion utilisable.

> L'implémentation repose sur le **même** signal `showSetupLink` que l'US1 (T006) : `false` si
> `installed=true` **ou** en cas d'erreur (défaut sûr). US2 ajoute la **couverture de test** de ces cas.

### Tests for User Story 2 ⚠️ (écrire d'abord)

- [X] T007 [US2] Ajouter à `login.component.spec.ts` : `showSetupLink` **faux** quand `installed=true` ; **faux** quand `SetupApi.status` échoue (défaut sûr) ; la connexion reste opérante dans les deux cas dans `web/src/app/features/login/login.component.spec.ts`
- [X] T008 [US2] Ajouter à `discoverable-setup.spec.ts` le cas **instance installée → aucun lien** dans `web/e2e/discoverable-setup.spec.ts`

### Implementation for User Story 2

- [X] T009 [US2] Garantir le **défaut sûr** dans `LoginComponent` : sur erreur de `SetupApi.status`, `showSetupLink=false` sans notification bloquante ni blocage de la connexion (complète T006) dans `web/src/app/features/login/login.component.ts`

**Checkpoint**: US1 + US2 — lien strictement conditionnel, défaut sûr en cas d'incertitude.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [ ] T010 Exécuter la validation `quickstart.md` (scénarios A→D) et confirmer SC-001..SC-004 — **EN ATTENTE** : la validation e2e complète nécessite l'API démarrée + CORS + un état d'instance contrôlable (vierge/installée). Les comportements A/B/C (lien visible / masqué / défaut sûr) sont **couverts par les tests unitaires (72 verts)** ; les e2e Playwright sont **écrits** mais non exécutés contre une API en ligne dans cette session.
- [X] T011 [P] Revue sécurité : parcours **anonyme** ; **aucune** donnée sensible affichée/persistée (SC-004) ; **défaut sûr = masqué** (SC-003) ; verrou d'installation serveur intact (409)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup** : aucune tâche.
- **Foundational (Phase 2)** : **bloque** les user stories. T001 [P] ; T002 (status) ; T003 [P].
- **US1 (Phase 3)** : après la Phase 2. T004/T005 avant T006.
- **US2 (Phase 4)** : après US1 (même composant/fichiers). T007/T008 avant T009.
- **Polish (Phase 5)** : après US1 et US2.

### Parallel Opportunities

- **Foundational** : T001 ∥ T003 (T002 entre les deux).
- **US1** : T004 ∥ T005.
- ⚠️ `login.component.ts` (T006/T009), `login.component.spec.ts` (T004/T007) et
  `discoverable-setup.spec.ts` (T005/T008) sont partagés US1/US2 → **séquencer US1 → US2**.

---

## Implementation Strategy

### MVP (US1)

1. Phase 2 : Foundational (modèle + `status()`).
2. Phase 3 : US1 (lien visible sur instance vierge) → **STOP & VALIDATE**.

### Livraison incrémentale

1. Socle + US1 → découvrabilité sur instance vierge (MVP).
2. US2 → masquage strict (installée / statut indisponible).
3. Polish → quickstart, revue sécurité.

### Notes

- [P] = fichiers différents, aucune dépendance ; [Story] = traçabilité US.
- **Réutilisation** : écran d'installation (008) et statut (012) inchangés ; API non modifiée.
- Les e2e nécessitent l'API + CORS + un état d'instance contrôlable ; **écrits** dans ce lot.
- Vérifier que les tests échouent avant d'implémenter ; commit après chaque tâche ou groupe logique.
