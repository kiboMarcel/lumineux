---
description: "Task list — Console web : Profils du bureau & droits (SPA, Lot 3)"
---

# Tasks: Console web — Profils du bureau & droits (SPA, Lot 3)

**Input**: Design documents from `specs/011-spa-bureau-profiles/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (api-consumption, routes)

**Tests**: INCLUS — Constitution III (esprit) + emphase sécurité de la spec. Unitaires **Vitest** ;
e2e **Playwright**.

**Organization**: Tâches regroupées par user story (US1→US3). **Extension** de l'application Angular
`web/` (features 008/009), consommant l'API profils (004), membres (002) et catalogue. L'API n'est
pas modifiée.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US3 (uniquement pour les phases de user story)
- Chemins relatifs à la racine du dépôt (dossier `web/`)

---

## Phase 1: Setup

**Aucune tâche** : l'application `web/` est déjà échafaudée ; aucune nouvelle dépendance ni config.

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: Modèles, services d'accès API (profils, attribution, catalogue) et **garde any-of**
(lecture élargie) partagés par toutes les user stories.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [X] T001 [P] Modèles de vue profils (`BureauProfileSummary`, `MemberRef`, `BureauProfileDetail`, `BureauProfileWriteRequest`, `MemberProfilesResponse`) dans `web/src/app/features/bureau-profiles/bureau-profile.models.ts`
- [X] T002 [P] Modèle `PermissionDescriptor` (`{ code, label }`) dans `web/src/app/core/api/permission.models.ts`
- [X] T003 [P] `BureauProfilesApi` (`list()`, `get(id)`, `create(body)`, `update(id,body)`, `remove(id)`) dans `web/src/app/core/api/bureau-profiles-api.ts`
- [X] T004 [P] `MemberProfilesApi` (`get(memberId)`, `assign(memberId,profileId)`, `revoke(memberId,profileId)`) dans `web/src/app/core/api/member-profiles-api.ts`
- [X] T005 [P] `PermissionsApi` (`list()`) dans `web/src/app/core/api/permissions-api.ts`
- [X] T006 Étendre `permissionGuard` pour un contrôle **any-of** (`route.data.anyPermissions: string[]` — autorise si ≥1 droit détenu ; conserve `route.data.permission` single) dans `web/src/app/core/guards/guards.ts`
- [X] T007 [P] Tests unitaires **Vitest** du socle (`BureauProfilesApi`/`MemberProfilesApi`/`PermissionsApi` : URLs/méthodes ; `permissionGuard` any-of : autorisé avec un des droits, refusé sans aucun) dans `web/src/app/core/api/*.spec.ts` et `web/src/app/core/guards/guards.spec.ts`

**Checkpoint**: Services, modèles et garde any-of prêts — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Consulter les profils et leurs droits (Priority: P1) 🎯 MVP

**Goal**: Liste et détail des profils accessibles en **lecture** (admin profils **ou** gestion
membres) ; actions d'écriture masquées sans droit d'administration. Intègre navigation et gardes.

**Independent Test**: Connecté en lecture, ouvrir Profils du bureau → liste ; ouvrir un profil →
droits + titulaires ; un lecteur « gestion membres » ne voit **aucune** action d'écriture. Sans droit
de lecture → entrée absente, accès direct refusé.

### Tests for User Story 1 ⚠️ (écrire d'abord)

- [X] T008 [P] [US1] Tests unitaires `ProfileListComponent` (liste) et `ProfileDetailComponent` (droits + titulaires ; actions d'écriture **masquées** sans `manage_bureau_profiles`) dans `web/src/app/features/bureau-profiles/profile-list/profile-list.component.spec.ts` et `web/src/app/features/bureau-profiles/profile-detail/profile-detail.component.spec.ts`
- [X] T009 [P] [US1] Test e2e **Playwright** (liste → détail ; entrée visible en lecture ; accès direct refusé sans droit) dans `web/e2e/bureau-profiles-read.spec.ts`

### Implementation for User Story 1

- [X] T010 [US1] `ProfileListComponent` (via `BureauProfilesApi.list`) dans `web/src/app/features/bureau-profiles/profile-list/profile-list.component.ts`
- [X] T011 [US1] `ProfileDetailComponent` (via `BureauProfilesApi.get` ; droits + titulaires ; boutons Modifier/Supprimer **conditionnés** à `manage_bureau_profiles`) dans `web/src/app/features/bureau-profiles/profile-detail/profile-detail.component.ts`
- [X] T012 [US1] Routes `/bureau-profiles` et `/bureau-profiles/:id` (`authGuard` + `permissionGuard` **any-of** [admin profils, gestion membres]) dans `web/src/app/app.routes.ts` ; entrée « Profils du bureau » (any-of) en **lien réel** vers `/bureau-profiles` dans `web/src/app/shell/shell.component.ts`

**Checkpoint**: US1 fonctionnelle — visibilité de la gouvernance (MVP).

---

## Phase 4: User Story 2 — Créer, modifier et supprimer un profil (Priority: P2)

**Goal**: Administration des profils (droit `manage_bureau_profiles`) : formulaire avec sélection des
droits du catalogue ; gestion des conflits `duplicate_name`, permission inconnue, `last_administrator`,
`profile_in_use` ; suppression avec confirmation.

**Independent Test**: Créer un profil (droits du catalogue) ; nom déjà utilisé → bloquant ; droit
inconnu → refus ; retrait admin du dernier profil admin → bloquant ; supprimer avec confirmation +
garde-fous.

### Tests for User Story 2 ⚠️ (écrire d'abord)

- [X] T013 [P] [US2] Tests unitaires `ProfileFormComponent` (création/édition ; **sélection des droits** depuis le catalogue ; **catalogue vide → message + soumission empêchée** (G1) ; `409 duplicate_name` bloquant ; `409 last_administrator` bloquant ; `400` permission inconnue) et **suppression** (confirmation + `409 profile_in_use`/`last_administrator`) dans `web/src/app/features/bureau-profiles/profile-form/profile-form.component.spec.ts`
- [X] T014 [P] [US2] Test e2e **Playwright** (création ; suppression avec confirmation ; garde-fou dernier administrateur) dans `web/e2e/bureau-profiles-admin.spec.ts`

### Implementation for User Story 2

- [X] T015 [US2] `ProfileFormComponent` (mode création/édition : `PermissionsApi.list` pour le catalogue → cases à cocher ; **si catalogue vide → message explicite + soumission empêchée** (G1) ; `BureauProfilesApi.create`/`update` ; mapping des 409 par `code`) dans `web/src/app/features/bureau-profiles/profile-form/profile-form.component.ts`
- [X] T016 [US2] Routes `/bureau-profiles/new` et `/bureau-profiles/:id/edit` (`authGuard` + `permissionGuard('manage_bureau_profiles')`) + action **Supprimer** avec **confirmation** (gestion `profile_in_use`/`last_administrator`) dans `web/src/app/app.routes.ts` et `web/src/app/features/bureau-profiles/profile-detail/profile-detail.component.ts`

**Checkpoint**: US1 + US2 — administration complète des profils.

---

## Phase 5: User Story 3 — Attribuer et révoquer les profils d'un membre (Priority: P2)

**Goal**: Écran des profils/droits effectifs d'un membre (accessible depuis la fiche membre) ;
attribution idempotente ; révocation avec garde-fou dernier administrateur.

**Independent Test**: Depuis la fiche membre, ouvrir Profils & droits → profils + droits effectifs ;
attribuer (idempotent) ; révoquer (confirmation ; dernier admin → bloquant) ; un lecteur ne voit pas
les actions d'écriture.

### Tests for User Story 3 ⚠️ (écrire d'abord)

- [X] T017 [P] [US3] Tests unitaires `MemberProfilesComponent` (droits effectifs = union ; **attribution idempotente** ; **révocation** avec confirmation et `409 last_administrator` bloquant ; actions masquées sans `manage_bureau_profiles`) dans `web/src/app/features/bureau-profiles/member-profiles/member-profiles.component.spec.ts`
- [X] T018 [P] [US3] Test e2e **Playwright** (attribuer puis révoquer depuis la fiche membre) dans `web/e2e/member-profiles.spec.ts`

### Implementation for User Story 3

- [X] T019 [US3] `MemberProfilesComponent` (via `MemberProfilesApi.get` ; attribuer/révoquer avec confirmation ; rafraîchit les droits effectifs) + route `/members/:id/profiles` (`authGuard` + `permissionGuard` **any-of**) dans `web/src/app/features/bureau-profiles/member-profiles/member-profiles.component.ts` et `web/src/app/app.routes.ts`
- [X] T020 [US3] Ajouter le lien **« Profils & droits »** vers `/members/:id/profiles` sur la fiche membre (Lot 2) dans `web/src/app/features/members/member-detail/member-detail.component.ts`

**Checkpoint**: US1→US3 — gouvernance des droits complète (profils + attribution).

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T021 [P] Passe **responsive & accessibilité** (liste/détail/formulaire/profils-membre, bureau + tablette) sur `web/src/app/features/bureau-profiles/**`
- [ ] T022 Exécuter la validation `quickstart.md` (scénarios A→D) et confirmer SC-001..SC-007 — **EN ATTENTE** : nécessite l'API démarrée + CORS + comptes de test (admin profils, gestion membres). Validé au niveau **unitaire (68 tests Vitest verts)** et **build applicative** ; les e2e Playwright (T009/T014/T018) sont **écrits** mais non exécutés contre une API en ligne dans cette session.
- [X] T023 [P] **Revue sécurité** : aucune donnée sensible exposée (titulaires = référence/nom/statut) ; actions d'écriture masquées sans `manage_bureau_profiles` (SC-004) ; confirmations destructrices (SC-007) ; garde-fous serveur restitués

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune tâche.
- **Foundational (Phase 2)** : **bloque** les user stories. T001–T005 en parallèle ; T006 (garde) ; T007 (tests).
- **US1 (Phase 3)** : après la Phase 2. T008/T009 avant T010–T012.
- **US2 (Phase 4)** : après US1 (réutilise détail pour l'action Supprimer). T013/T014 avant T015/T016.
- **US3 (Phase 5)** : après la Phase 2 (indépendant d'US2). T017/T018 avant T019/T020.
- **Polish (Phase 6)** : après les user stories visées.

### Within Each User Story

- Tests écrits avant/avec l'implémentation (rouge → vert).
- Accès API **toujours** via `core/api` ; aucune règle métier client.

### Parallel Opportunities

- **Foundational** : T001–T005 en parallèle ; puis T006 ; T007 [P].
- **US1** : T008 ∥ T009.
- ⚠️ `app.routes.ts` est modifié par T012/T016/T019 → **séquencer**. `profile-detail.component.ts`
  par T011/T016 → séquencer US1 → US2. `member-detail.component.ts` (T020) partagé avec le Lot 2 —
  simple ajout de lien.

---

## Implementation Strategy

### MVP (US1)

1. Phase 2 : Foundational (modèles, services, garde any-of) — **bloquant**.
2. Phase 3 : US1 (liste/détail + navigation + gardes any-of) → **STOP & VALIDATE** : gouvernance
   consultable, distinction lecture/écriture vérifiée.

### Livraison incrémentale

1. Socle + US1 → consulter (MVP).
2. US2 → administrer les profils.
3. US3 → attribuer/révoquer au niveau du membre.
4. Polish → responsive/a11y, quickstart, revue sécurité.

### Notes

- [P] = fichiers différents, aucune dépendance ; [Story] = traçabilité US.
- Distinction **lecture (any-of) / écriture (admin profils)** ; l'API reste l'autorité (403 géré).
- Les e2e Playwright nécessitent l'API + CORS + comptes de test (admin profils, gestion membres) ;
  **écrits** dans ce lot, exécutables contre une API en ligne.
- Vérifier que les tests échouent avant d'implémenter ; commit après chaque tâche ou groupe logique.
