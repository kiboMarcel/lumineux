# Tasks: Profession du membre

**Input**: Design documents from `/specs/030-member-profession/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/members-api.md, quickstart.md

**Tests**: INCLUS — la Constitution (Principe III, NON-NÉGOCIABLE) impose des tests unitaires
sur Domain/Application au même changement, écrits avant l'implémentation (rouge → vert).

**Organization**: tâches groupées par user story. La fondation (Phase 2) est bloquante et
partagée ; chaque story reste ensuite testable indépendamment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]**: US1 / US2 / US3 (voir spec.md)

## Path Conventions

Web app (.NET Onion + SPA Angular) : API sous `src/`, tests sous `tests/`, SPA sous `web/src/`.

---

## Phase 1: Setup

**Purpose**: point de départ propre avant toute modification.

- [X] T001 Vérifier la base verte avant modification : `dotnet test` (4 projets) et `npm test` dans `web/` passent tels quels (référence de non-régression).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: attribut de domaine, colonne, migration et lecture partagée — prérequis de TOUTES les stories.

**⚠️ CRITICAL**: aucune story ne peut être implémentée avant la fin de cette phase.

- [X] T002 Ajouter la propriété `public string? Profession { get; set; }` à l'entité dans `src/Lumineux.Domain/Entities/Member.cs` (nullable, aucun invariant obligatoire, calquée sur `Address`).
- [X] T003 Configurer la colonne dans `src/Lumineux.Infrastructure/Persistence/Configurations/MemberConfiguration.cs` : `builder.Property(x => x.Profession).HasColumnName("profession").HasMaxLength(150);`
- [X] T004 Générer la migration additive : `dotnet ef migrations add MemberProfession` (projet Infrastructure, démarrage API) → vérifier qu'elle ne contient qu'un `AddColumn` `profession nvarchar(150) NULL` sur `members`, sans backfill ; commiter le fichier généré sous `src/Lumineux.Infrastructure/Persistence/Migrations/`.
- [X] T005 [P] Ajouter `string? Profession` au DTO `MemberResponse` dans `src/Lumineux.Application/Contracts/Members/MemberDtos.cs` (dernier avant `Status`/`AccountActivationState`, ou en fin de bloc identité — cohérent avec `Address`).
- [X] T006 [P] Test de mapping (écrire AVANT l'impl T008, doit échouer) : `MemberResponse.Profession` reflète `Member.Profession` dans les deux cas — **renseignée** ET **null** (le cas null couvre SC-004 au niveau mapping) — dans `tests/Lumineux.Application.Tests/MemberProfessionMappingTests.cs`.
- [X] T007 [P] Ajouter `profession?: string | null;` à l'interface `MemberResponse` dans `web/src/app/features/members/member.models.ts`.
- [X] T008 Mapper la profession dans `src/Lumineux.Application/Members/MemberMapping.cs` (`ToResponse` : passer `m.Profession`) — fait passer T006.

**Checkpoint**: le champ existe de bout en bout en lecture ; les stories peuvent démarrer.

---

## Phase 3: User Story 1 - Renseigner la profession à la création (Priority: P1) 🎯 MVP

**Goal**: le bureau peut saisir (ou omettre) une profession à la création d'un membre, nettoyée et bornée.

**Independent Test**: créer un membre avec profession → la fiche l'affiche ; créer sans → champ vide ; espaces seuls → null ; 151 caractères → refus.

### Tests for User Story 1 ⚠️ (écrire AVANT, doivent échouer)

- [X] T009 [P] [US1] Test handler : `CreateMemberHandler` normalise la profession (trim, « espaces seuls » → null) et l'affecte au membre ; inclure un intitulé accentué/avec apostrophe (ex. « Chargé d'affaires ») conservé tel quel après trim (couvre U1), dans `tests/Lumineux.Application.Tests/CreateMemberTests.cs`.
- [X] T010 [P] [US1] Test validation : `CreateMemberValidator` accepte 150 caractères et rejette 151 (message longueur max), dans `tests/Lumineux.Application.Tests/CreateMemberTests.cs`.
- [X] T011 [P] [US1] Test endpoint : `POST /api/v1/members` avec profession → 201 et `member.profession` renseignée ; sans profession → 201 et `null` ; **deux membres avec la même profession sont acceptés** (pas d'unicité, couvre FR-010/C1), dans `tests/Lumineux.Api.Tests/MembersEndpointsTests.cs`.

### Implementation for User Story 1

- [X] T012 [US1] Ajouter `string? Profession` au DTO `CreateMemberRequest` dans `src/Lumineux.Application/Contracts/Members/MemberDtos.cs` (paramètre optionnel, ex. après `Address`).
- [X] T013 [US1] Ajouter la règle `RuleFor(x => x.Profession).MaximumLength(150).When(x => x.Profession is not null);` (message clair) dans `src/Lumineux.Application/Members/CreateMemberValidator.cs`.
- [X] T014 [US1] Dans `src/Lumineux.Application/Members/CreateMemberHandler.cs`, affecter `member.Profession = string.IsNullOrWhiteSpace(request.Profession) ? null : request.Profession.Trim();` (près des autres affectations, après `member.Address`). Ne pas journaliser la valeur.
- [X] T015 [P] [US1] Ajouter `profession?: string | null;` à l'interface `CreateMemberRequest` dans `web/src/app/features/members/member.models.ts`.
- [X] T016 [US1] Ajouter le contrôle et le champ « Profession » (facultatif) au formulaire de création dans `web/src/app/features/members/member-form/member-form.component.ts` (contrôle `profession: ['']`, input à côté de « Adresse », envoi `profession: v.profession || null`).
- [X] T017 [P] [US1] Test SPA : le formulaire de création expose le champ profession et envoie `null` si vide, dans `web/src/app/features/members/member-form/member-form.component.spec.ts`.

**Checkpoint**: création avec/sans profession fonctionnelle et testée de bout en bout.

---

## Phase 4: User Story 2 - Corriger ou compléter la profession (Priority: P2)

**Goal**: ajouter, remplacer ou effacer la profession d'un membre existant via la correction.

**Independent Test**: sur un membre sans profession, en ajouter une ; la remplacer ; la vider → chaque état persisté et relu.

### Tests for User Story 2 ⚠️ (écrire AVANT, doivent échouer)

- [X] T018 [P] [US2] Test handler : `UpdateMemberHandler` ajoute, remplace et efface (vide→null, trim) la profession, dans `tests/Lumineux.Application.Tests/MemberQueryAndUpdateTests.cs`.
- [X] T019 [P] [US2] Test validation : `UpdateMemberValidator` borne la profession à 150 (accepté à la limite, refusé au-delà), dans `tests/Lumineux.Application.Tests/MemberQueryAndUpdateTests.cs`.
- [X] T020 [P] [US2] Test endpoint : `PUT /api/v1/members/{id}` renseigne puis efface la profession (200, valeur reflétée), dans `tests/Lumineux.Api.Tests/MemberSearchUpdateEndpointsTests.cs`.

### Implementation for User Story 2

- [X] T021 [US2] Ajouter `string? Profession` au DTO `UpdateMemberRequest` dans `src/Lumineux.Application/Contracts/Members/MemberQueryDtos.cs`.
- [X] T022 [US2] Ajouter la règle `MaximumLength(150)` sur `Profession` dans `src/Lumineux.Application/Members/UpdateMemberValidator.cs`.
- [X] T023 [US2] Dans `src/Lumineux.Application/Members/UpdateMemberHandler.cs`, affecter `member.Profession = string.IsNullOrWhiteSpace(request.Profession) ? null : request.Profession.Trim();` (avec les autres affectations).
- [X] T024 [US2] Dans `web/src/app/features/members/member-form/member-form.component.ts` : pré-remplir le contrôle profession en édition (`profession: m.profession ?? ''`) et l'inclure dans la requête de correction.
- [X] T025 [P] [US2] Test SPA : en mode édition, le champ profession se pré-remplit et l'effacement envoie `null`, dans `web/src/app/features/members/member-form/member-form.component.spec.ts`.

**Checkpoint**: US1 et US2 fonctionnent indépendamment.

---

## Phase 5: User Story 3 - Consulter la profession dans la fiche (Priority: P3)

**Goal**: la fiche membre affiche la profession si renseignée, sans valeur fictive sinon.

**Independent Test**: ouvrir la fiche d'un membre avec profession (affichée) et d'un membre sans (absence propre).

### Tests for User Story 3 ⚠️ (écrire AVANT, doivent échouer)

- [X] T026 [P] [US3] Test SPA : la fiche affiche la profession quand présente et n'affiche pas de valeur fictive quand absente, dans `web/src/app/features/members/member-detail/member-detail.component.spec.ts`.

### Implementation for User Story 3

- [X] T027 [US3] Afficher la profession dans la fiche membre `web/src/app/features/members/member-detail/member-detail.component.ts` (parmi les infos d'identité ; masquer/mention « non renseignée » si null).

**Checkpoint**: les trois stories sont fonctionnelles indépendamment.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T028 [P] Régression **SC-004** (automatisée) : un membre existant/seedé sans profession se lit via `GET /api/v1/members/{id}` avec `profession == null`, sans erreur — dans `tests/Lumineux.Api.Tests/MembersEndpointsTests.cs`.
- [X] T029 [P] Mettre à jour la doc du modèle de données `docs/audit/03-modele-donnees.md` : ajouter le champ `profession` à l'entité MEMBERS.
- [X] T030 Exécuter la suite complète : `dotnet test` (Domain/Application/Infrastructure/Api) et `npm test` (`web/`) tous verts.
- [ ] T031 Dérouler `specs/030-member-profession/quickstart.md` (vérifications API + SPA), y compris l'application de la migration `dotnet ef database update` sur la base de dev.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: aucune dépendance.
- **Foundational (Phase 2)**: après Setup — **BLOQUE** toutes les stories (T002→T003→T004 séquentiels ; T004 dépend de T002+T003 ; T005 avant T006 pour compiler ; **T006 test écrit et en échec avant T008 impl**).
- **US1 / US2 / US3 (Phases 3–5)**: après Phase 2. Indépendantes entre elles ; peuvent se faire en parallèle si équipe.
- **Polish (Phase 6)**: après les stories souhaitées.

### Within Each User Story

- Tests écrits et **en échec** avant l'implémentation (Principe III).
- DTO → validator → handler → SPA.
- US1, US2, US3 partagent le fichier `member-form.component.ts` (US1/US2) et les modèles — coordonner les éditions sur fichiers communs (non `[P]` entre elles).

### Parallel Opportunities

- T005, T006 (test) et T007 en parallèle (fichiers différents) ; T008 (mapping impl) après T005/T006.
- Dans chaque story, les tâches de test `[P]` s'écrivent en parallèle.
- Attention : T014 (CreateMemberHandler) et T023 (UpdateMemberHandler) sont des fichiers distincts → parallélisables entre stories ; mais T016 et T024 touchent le **même** `member-form.component.ts` → séquentiels.

---

## Implementation Strategy

### MVP (User Story 1 seule)

1. Phase 1 Setup → 2. Phase 2 Foundational (critique) → 3. Phase 3 US1 → **STOP & VALIDATE** (créer un membre avec profession, la relire) → livrable.

### Livraison incrémentale

Foundational → US1 (MVP, création) → US2 (correction) → US3 (affichage fiche). Chaque story ajoute de la valeur sans casser la précédente.

---

## Notes

- Migration : additive, nullable, sans backfill — membres existants → `profession` null (SC-004).
- Sécurité : validation serveur bornée + normalisation (Principe IV) ; valeur non journalisée (Principe VI) ; pas d'unicité (FR-010).
- Déploiement : appliquer la migration (`dotnet ef database update`) aux bases dev/prod — dette de déploiement habituelle, tracée en T030.
- Commiter après chaque groupe logique ; ne pas pousser sans accord explicite.
