---
description: "Task list — Console web : Gestion des membres (SPA, Lot 2)"
---

# Tasks: Console web — Gestion des membres (SPA, Lot 2)

**Input**: Design documents from `specs/009-spa-members-management/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (api-consumption, routes)

**Tests**: INCLUS — Constitution III (esprit) + emphase sécurité de la spec. Unitaires **Vitest** ;
e2e **Playwright**.

**Organization**: Tâches regroupées par user story (US1→US3). **Extension** de l'application Angular
`web/` (feature 008), consommant l'API membres (002) et référentiels (010). L'API n'est pas modifiée.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US3 (uniquement pour les phases de user story)
- Chemins relatifs à la racine du dépôt (dossier `web/`)

---

## Phase 1: Setup

**Aucune tâche** : l'application `web/` est déjà échafaudée (feature 008) ; aucune nouvelle
dépendance ni configuration.

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: Modèles typés et services d'accès API (membres + référentiels) partagés par toutes les
user stories.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [X] T001 [P] Modèles de vue membres (`MemberListItem`, `MemberListResponse`, `MemberResponse`, `CreateMemberRequest`, `UpdateMemberRequest`, `MemberCreatedResponse`) dans `web/src/app/features/members/member.models.ts`
- [X] T002 [P] Modèles de référentiels (`ReferenceItem`, `Country`) dans `web/src/app/core/api/reference.models.ts`
- [X] T003 `MembersApi` (`search(query,page,pageSize)`, `get(id)`, `create(body)`, `update(id,body)`) dans `web/src/app/core/api/members-api.ts`
- [X] T004 [P] `ReferenceApi` (`antennas()`, `civilities()`, `cities()`, `districts()`, `countries()`) dans `web/src/app/core/api/reference-api.ts`
- [X] T005 [P] Tests unitaires **Vitest** des services (`MembersApi` : URLs/méthodes/paramètres de pagination ; `ReferenceApi` : URLs) dans `web/src/app/core/api/members-api.spec.ts` et `web/src/app/core/api/reference-api.spec.ts`

**Checkpoint**: Services et modèles prêts — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Rechercher, lister et consulter (Priority: P1) 🎯 MVP

**Goal**: Module « Membres » accessible aux porteurs de `manage_members` : recherche + liste paginée,
et consultation de fiche. Intègre la navigation et les gardes.

**Independent Test**: Connecté avec `manage_members`, ouvrir Membres → liste paginée ; rechercher ;
ouvrir une fiche (sans secret) ; id inexistant → « introuvable ». Sans le droit → entrée masquée,
accès direct refusé.

### Tests for User Story 1 ⚠️ (écrire d'abord)

- [X] T006 [P] [US1] Tests unitaires `MemberListComponent` (recherche, pagination, état « aucun résultat ») et `MemberDetailComponent` (404 → message) dans `web/src/app/features/members/member-list/member-list.component.spec.ts` et `web/src/app/features/members/member-detail/member-detail.component.spec.ts`
- [X] T007 [P] [US1] Test e2e **Playwright** (recherche → fiche ; entrée « Membres » **masquée** sans droit ; accès direct `/members` refusé) dans `web/e2e/members-list.spec.ts`

### Implementation for User Story 1

- [X] T008 [US1] `MemberListComponent` (recherche + liste paginée via `MembersApi.search`) dans `web/src/app/features/members/member-list/member-list.component.ts`
- [X] T009 [US1] `MemberDetailComponent` (fiche via `MembersApi.get` ; gestion 404) dans `web/src/app/features/members/member-detail/member-detail.component.ts`
- [X] T010 [US1] Routes `/members` et `/members/:id` (`authGuard` + `permissionGuard('manage_members')`) dans `web/src/app/app.routes.ts` ; remplacer le placeholder « Membres » par un **lien réel** (masqué selon droit) dans `web/src/app/shell/shell.component.ts`

**Checkpoint**: US1 fonctionnelle — annuaire consultable, module démontrable (MVP).

---

## Phase 4: User Story 2 — Enrôler un nouveau membre (Priority: P2)

**Goal**: Formulaire de création avec listes de référence, homonymie confirmable, conflit de contact
bloquant, et affichage **unique** du mode de remise des identifiants.

**Independent Test**: Créer un membre valide (email → invitation ; sans email → identifiants une
fois) ; homonyme → Confirmer/Annuler ; contact déjà utilisé → erreur bloquante ; antenne indisponible
→ création empêchée.

### Tests for User Story 2 ⚠️ (écrire d'abord)

- [X] T011 [P] [US2] Tests unitaires `MemberFormComponent` (création) : champs requis, antenne **choisie dans la liste**, **homonymie** (`409 duplicate_name` → confirmation → réessai `confirmDuplicate=true`), **`contact_in_use`** bloquant, **remise identifiants** affichée **une seule fois** (mot de passe temporaire non persisté) dans `web/src/app/features/members/member-form/member-form.component.spec.ts`
- [X] T012 [P] [US2] Test e2e **Playwright** (création nominale email/remise bureau une fois ; homonymie confirmée ; conflit contact bloquant ; antenne indisponible) dans `web/e2e/members-create.spec.ts`

### Implementation for User Story 2

- [X] T013 [US2] `MemberFormComponent` (mode **création**) : formulaire réactif + **listes de référence** (`ReferenceApi`), soumission `MembersApi.create`, flux **homonymie** (bannière Confirmer/Annuler → renvoi `confirmDuplicate=true`), gestion **`contact_in_use`**, panneau **remise identifiants** (email vs remise bureau ; `temporaryPassword` en **état de vue éphémère**, jamais persisté), blocage si **aucune antenne active** dans `web/src/app/features/members/member-form/member-form.component.ts`
- [X] T014 [US2] Route `/members/new` (`authGuard` + `permissionGuard('manage_members')`) + action « Nouveau membre » depuis la liste dans `web/src/app/app.routes.ts` et `web/src/app/features/members/member-list/member-list.component.ts`

**Checkpoint**: US1 + US2 — enrôlement complet, secrets non persistés.

---

## Phase 5: User Story 3 — Corriger la fiche d'un membre (Priority: P3)

**Goal**: Réutiliser le formulaire en mode édition (préchargement, référence en lecture seule),
conflit de contact bloquant.

**Independent Test**: Ouvrir une fiche → Modifier ; changer des champs (référence non modifiable) →
enregistrer ; contact en conflit → erreur bloquante.

### Tests for User Story 3 ⚠️ (écrire d'abord)

- [X] T015 [P] [US3] Tests unitaires `MemberFormComponent` (édition) : préchargement de la fiche, **référence en lecture seule**, `MembersApi.update`, `contact_in_use` bloquant (pas de confirmation d'homonyme à l'édition) dans `web/src/app/features/members/member-form/member-form.component.spec.ts`

### Implementation for User Story 3

- [X] T016 [US3] Étendre `MemberFormComponent` (mode **édition** : préremplissage via `MembersApi.get`, `reference` en lecture seule, soumission `MembersApi.update`) dans `web/src/app/features/members/member-form/member-form.component.ts`
- [X] T017 [US3] Route `/members/:id/edit` (gardée) + action « Modifier » depuis la fiche dans `web/src/app/app.routes.ts` et `web/src/app/features/members/member-detail/member-detail.component.ts`

**Checkpoint**: US1→US3 — cycle membre complet (rechercher, consulter, créer, corriger).

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T018 [P] Passe **responsive & accessibilité** (liste/fiche/formulaire sur bureau et tablette, sans défilement horizontal — SC-008 socle) sur `web/src/app/features/members/**`
- [ ] T019 Exécuter la validation `quickstart.md` (scénarios A→G) et confirmer SC-001..SC-007 — **EN ATTENTE** : nécessite l'API démarrée + CORS + données de test. Validé au niveau **unitaire (48 tests Vitest verts)** et **build applicative** ; les e2e Playwright (T007/T012) sont **écrits** mais non exécutés contre une API en ligne dans cette session.
- [X] T020 [P] **Revue sécurité** : le mot de passe temporaire (remise bureau) n'est **jamais** persisté/journalisé ni ré-affiché (SC-005) ; aucun secret en URL/console ; entrée/actions masquées sans droit (SC-006)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune tâche.
- **Foundational (Phase 2)** : **bloque** les user stories. T001/T002/T004 en parallèle ; T003 (MembersApi) ; T005 (tests services).
- **US1 (Phase 3)** : après la Phase 2. T006/T007 (tests) avant T008–T010.
- **US2 (Phase 4)** : après la Phase 2. Réutilise `MembersApi`/`ReferenceApi`. T011/T012 avant T013/T014.
- **US3 (Phase 5)** : après US2 (étend `MemberFormComponent`, **même fichier** que T013). T015 avant T016/T017.
- **Polish (Phase 6)** : après les user stories visées.

### Within Each User Story

- Tests écrits avant/avec l'implémentation (rouge → vert).
- Accès API **toujours** via `core/api` (aucun appel HTTP dans les composants) ; aucune règle métier client.

### Parallel Opportunities

- **Foundational** : T001, T002, T004 en parallèle ; puis T003 ; T005 [P].
- **US1** : T006 ∥ T007 (fichiers distincts) avant l'implémentation.
- ⚠️ T013 (US2) et T016 (US3) modifient le **même** fichier `member-form.component.ts` → séquencer US2 → US3. De même, T010/T014/T017 modifient `app.routes.ts` → séquencer.

---

## Parallel Example: Foundational (Phase 2)

```bash
Task: "Modèles membres dans web/src/app/features/members/member.models.ts"
Task: "Modèles référentiels dans web/src/app/core/api/reference.models.ts"
Task: "ReferenceApi dans web/src/app/core/api/reference-api.ts"
# puis MembersApi (T003), puis tests services (T005)
```

---

## Implementation Strategy

### MVP (US1)

1. Phase 2 : Foundational (modèles + services API) — **bloquant**.
2. Phase 3 : US1 (liste/recherche/fiche + navigation + gardes) → **STOP & VALIDATE** : annuaire
   consultable, RBAC vérifié.

### Livraison incrémentale

1. Socle + US1 → rechercher/consulter (MVP).
2. US2 → enrôler (homonymie, remise identifiants).
3. US3 → corriger (formulaire en édition).
4. Polish → responsive/a11y, quickstart, revue sécurité.

### Notes

- [P] = fichiers différents, aucune dépendance ; [Story] = traçabilité US.
- Les e2e Playwright nécessitent l'API démarrée + CORS + données de test (référentiels 010) ; ils sont
  **écrits** dans ce lot, exécutables contre une API en ligne.
- Aucun secret persisté : le mot de passe temporaire de la remise bureau est éphémère (état de vue).
- Vérifier que les tests échouent avant d'implémenter ; commit après chaque tâche ou groupe logique.
