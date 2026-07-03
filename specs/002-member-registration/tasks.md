---
description: "Task list — Ajout d'un nouveau membre"
---

# Tasks: Ajout d'un nouveau membre

**Input**: Design documents from `specs/002-member-registration/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUS — imposés par la Constitution Lumineux v1.0.0, Principe III (Tests en premier).
Unitaires Domain/Application ; intégration Infrastructure/API (harnais SQLite existant).

**Organization**: Tâches regroupées par user story (US1→US3). Extension de la solution Onion
existante (feature 001) : `src/Lumineux.Domain|Application|Infrastructure|Api`, `tests/Lumineux.*.Tests`.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US3 (uniquement pour les phases de user story)
- Chaque tâche indique le chemin de fichier exact

---

## Phase 1: Setup

**Purpose**: Dépendances et configuration additionnelles (la solution existe déjà).

- [x] T001 Ajouter le package `Microsoft.Extensions.Identity.Core` (hachage de mot de passe) dans `Directory.Packages.props` et `src/Lumineux.Infrastructure/Lumineux.Infrastructure.csproj`
- [x] T002 [P] Ajouter la section de configuration e-mail (`Email:Provider` = `Logging`|`Smtp`, paramètres SMTP en secrets) dans `src/Lumineux.Api/appsettings.json` et `appsettings.Development.json`

**Checkpoint**: Solution compilable avec les nouvelles dépendances.

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: Modèle, ports, persistance, sécurité et e-mail requis par TOUTES les user stories.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [x] T003 Enrichir l'entité `Member` (champs : reference, entryDate, gender, civility, birthDate, birthPlace, birthCity, mobile, email, address, district, nationality, introducer ; fabrique `Create` + invariants FR-003/004) dans `src/Lumineux.Domain/Entities/Member.cs` — **⚠ compatibilité ascendante** : préserver les usages existants de la feature 001 (`ApiTestFixture`, `ScanLoadTests` instancient `Member`, `AttendanceUniquenessTests`) ; si les setters deviennent privés, mettre à jour ces points de seed dans le même changement pour garder la solution/verte.
- [x] T004 [P] Créer les enums `MemberStatus` (Active/Archived) et `AccountActivationState` (PendingActivation/Active) dans `src/Lumineux.Domain/Enums/MemberEnums.cs`
- [x] T005 Créer l'entité `MemberAccount` (1-1 avec Member, fabrique `Provision` : loginId=référence, passwordHash, mustChangePassword, activationState) dans `src/Lumineux.Domain/Entities/MemberAccount.cs`
- [x] T006 [P] Définir les ports `IMemberRepository` et `IMemberAccountRepository` dans `src/Lumineux.Domain/Abstractions/`
- [x] T007 [P] Définir les ports `IMemberReferenceGenerator`, `IPasswordHasher`, `IEmailSender` dans `src/Lumineux.Domain/Abstractions/`
- [x] T008 [P] Définir le port `IReferenceLookupRepository` (existence antenne/civilité/pays/ville/district + introducteur) dans `src/Lumineux.Domain/Abstractions/IReferenceLookupRepository.cs`
- [x] T009 Configuration EF de `Member` (colonnes enrichies, index unique `reference`, index de recherche, **index uniques filtrés** contact actif e-mail/mobile, FK nomenclatures + introducteur) dans `src/Lumineux.Infrastructure/Persistence/Configurations/MemberConfiguration.cs`
- [x] T010 [P] Configuration EF de `MemberAccount` (1-1, `loginId` unique) dans `src/Lumineux.Infrastructure/Persistence/Configurations/MemberAccountConfiguration.cs`
- [x] T011 Ajouter `DbSet<MemberAccount>` à `AppDbContext` et générer la **migration** (enrichissement `members` + création `member_accounts` + index) dans `src/Lumineux.Infrastructure/Persistence/` — **⚠ `reference` unique+requise** : la table `members` est supposée **vide** (aucune gestion de membre avant cette feature) ; si des lignes préexistent, la migration DOIT inclure le backfill de `reference` (voir T042) **dans le même déploiement** pour ne pas échouer.
- [x] T012 [P] Implémenter `MemberRepository` (getById, recherche paginée, add, save ; requête homonymes ; contrôle contact-in-use actif) dans `src/Lumineux.Infrastructure/Repositories/MemberRepository.cs`
- [x] T013 [P] Implémenter `MemberAccountRepository` dans `src/Lumineux.Infrastructure/Repositories/MemberAccountRepository.cs`
- [x] T014 [P] Implémenter `ReferenceLookupRepository` dans `src/Lumineux.Infrastructure/Repositories/ReferenceLookupRepository.cs`
- [x] T015 [P] Implémenter `MemberReferenceGenerator` (format configurable `LUM-{yyyy}-{seq}`, unicité) dans `src/Lumineux.Infrastructure/Security/MemberReferenceGenerator.cs`
- [x] T016 [P] Implémenter `IdentityPasswordHasher` (`PasswordHasher<T>`) + générateur de mot de passe temporaire sécurisé dans `src/Lumineux.Infrastructure/Security/IdentityPasswordHasher.cs`
- [x] T017 [P] Implémenter l'envoi d'e-mail : `LoggingEmailSender` (dev) + `SmtpEmailSender` (System.Net.Mail, config) + `EmailOptions` + sélection par provider dans `src/Lumineux.Infrastructure/Email/`
- [x] T018 Enregistrement DI des nouveaux services/ports (repos, générateur, hasher, e-mail) dans `src/Lumineux.Infrastructure/DependencyInjection.cs`
- [x] T019 Ajouter la policy d'autorisation `manage_members` (claim `permission=manage_members`) dans `src/Lumineux.Api/Program.cs` et la constante dans `src/Lumineux.Application/Abstractions/Permissions.cs`

**Checkpoint**: Socle prêt — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Créer un nouveau membre + compte (Priority: P1) 🎯 MVP

**Goal**: Le bureau crée un membre avec les infos de base ; référence + date d'entrée + statut actif ;
compte provisionné ; identifiants transmis par e-mail ou repli remise-bureau. Atomique.

**Independent Test**: `POST /members` valide → 201 avec référence, compte, `credentialsDelivery`
(EmailSent ou BureauHandout) ; aucun secret exposé ; refus 400/401/403/404 selon le cas.

### Tests for User Story 1 ⚠️ (écrire d'abord)

- [x] T020 [P] [US1] Tests unitaires Domain `Member.Create` (obligatoires, référence/date/statut) et `MemberAccount.Provision` (mustChangePassword, pas de secret en clair) dans `tests/Lumineux.Domain.Tests/MemberTests.cs`
- [x] T021 [P] [US1] Tests unitaires `CreateMember` (droit requis, obligatoires manquants, FK inconnue → NotFound, atomicité, EmailSent vs BureauHandout, **échec d'envoi SMTP → repli BureauHandout** [E1], **compte provisionné sans aucun droit de gestion** [U1, FR-012]) dans `tests/Lumineux.Application.Tests/CreateMemberTests.cs`
- [x] T022 [P] [US1] Test d'intégration `POST /api/v1/members` (201, mot de passe/hash non exposés, repli BureauHandout, 400/401/403/404) dans `tests/Lumineux.Api.Tests/MembersEndpointsTests.cs`

### Implementation for User Story 1

- [x] T023 [P] [US1] DTO `CreateMemberRequest`/`MemberResponse`/`MemberCreatedResponse` dans `src/Lumineux.Application/Contracts/Members/`
- [x] T024 [P] [US1] Validator `CreateMemberValidator` (nom, prénom, gender ∈ {M,F}, mobile OU email, antenne) dans `src/Lumineux.Application/Members/CreateMemberValidator.cs`
- [x] T025 [US1] Cas d'usage `CreateMember` (droit `manage_members`, validation, existence FK, génération référence, provisionnement compte **atomique**, hachage mot de passe temporaire, envoi e-mail ou repli, audit) dans `src/Lumineux.Application/Members/CreateMemberHandler.cs`
- [x] T026 [US1] `MembersController` : `POST /api/v1/members` (policy `manage_members`) dans `src/Lumineux.Api/Controllers/MembersController.cs`

**Checkpoint**: US1 fonctionnelle et testable (MVP). ✅ Implémentée et vérifiée (build .NET 10 OK, 86 tests verts, migration EF MemberRegistration) le 2026-07-03.

---

## Phase 4: User Story 2 — Doublons et unicité des contacts (Priority: P2)

**Goal**: Détecter les homonymes (avertir + confirmation) et refuser une coordonnée déjà utilisée
par un membre actif.

**Independent Test**: recréer un homonyme → 409 `duplicate_name` ; avec `confirmDuplicate=true` → 201 ;
coordonnée d'un membre actif réutilisée → 409 `contact_in_use`.

### Tests for User Story 2 ⚠️

- [x] T027 [P] [US2] Tests unitaires `CreateMember` — homonyme sans confirmation → conflit ; avec `confirmDuplicate` → succès ; contact déjà utilisé (actif) → conflit, dans `tests/Lumineux.Application.Tests/CreateMemberDuplicateTests.cs`
- [x] T028 [P] [US2] Test d'intégration doublon (`duplicate_name` + `duplicateMemberIds`, puis confirmation) et `contact_in_use` dans `tests/Lumineux.Api.Tests/MemberDuplicateEndpointsTests.cs`
- [x] T029 [P] [US2] Test d'intégration de l'unicité filtrée des contacts actifs au niveau base dans `tests/Lumineux.Infrastructure.Tests/MemberContactUniquenessTests.cs`

### Implementation for User Story 2

- [x] T030 [US2] Étendre `CreateMember` : détection homonymes (nom+prénom), gestion `confirmDuplicate`, contrôle contact-in-use (membre actif) dans `src/Lumineux.Application/Members/CreateMemberHandler.cs`
- [x] T031 [P] [US2] DTO `DuplicateProblemDetails` (code `duplicate_name`/`contact_in_use`, `duplicateMemberIds`) + mapping dans le middleware d'erreurs dans `src/Lumineux.Api/Middleware/ExceptionHandlingMiddleware.cs` et `src/Lumineux.Application/Contracts/Members/`

**Checkpoint**: US1 + US2 fonctionnent ; intégrité des identités et des contacts assurée. ✅ (92 tests verts, 2026-07-03)

---

## Phase 5: User Story 3 — Consultation et correction (Priority: P2)

**Goal**: Rechercher/consulter une fiche membre et corriger les champs autorisés (tracé).

**Independent Test**: `GET /members?query=` → résultats paginés ; `GET /members/{id}` → fiche ;
`PUT /members/{id}` → 200 (modification tracée) ; contact déjà utilisé par un autre actif → 409.

### Tests for User Story 3 ⚠️

- [x] T032 [P] [US3] Tests unitaires `SearchMembers`, `GetMember`, `UpdateMember` (droit, contact-in-use hors soi-même, FK, trace) dans `tests/Lumineux.Application.Tests/MemberQueryAndUpdateTests.cs`
- [x] T033 [P] [US3] Test d'intégration `GET /members` (recherche/pagination), `GET /members/{id}`, `PUT /members/{id}` (200/404/403/409) dans `tests/Lumineux.Api.Tests/MemberSearchUpdateEndpointsTests.cs`

### Implementation for User Story 3

- [x] T034 [P] [US3] DTO `UpdateMemberRequest`/`MemberListItem`/`MemberListResponse` dans `src/Lumineux.Application/Contracts/Members/`
- [x] T035 [US3] Cas d'usage `SearchMembers` (recherche nom/prénom/référence, pagination) dans `src/Lumineux.Application/Members/SearchMembersHandler.cs`
- [x] T036 [P] [US3] Cas d'usage `GetMember` dans `src/Lumineux.Application/Members/GetMemberHandler.cs`
- [x] T037 [US3] Cas d'usage `UpdateMember` (validation FK, contact-in-use hors soi-même, audit) dans `src/Lumineux.Application/Members/UpdateMemberHandler.cs`
- [x] T038 [US3] Étendre `MembersController` : `GET /members`, `GET /members/{id}`, `PUT /members/{id}` (policy `manage_members`) dans `src/Lumineux.Api/Controllers/MembersController.cs`

**Checkpoint**: Les 3 user stories fonctionnent indépendamment. ✅ (104 tests verts, 2026-07-03)

---

## Phase 6: Polish & Cross-Cutting

**Purpose**: Sécurité, observabilité, documentation, validation finale.

- [x] T039 [P] Journaliser via `IAuditLogger` la création/correction et le **résultat d'envoi d'e-mail** (succès/échec, canal) **sans** mot de passe ni secret (FR-015/016)
- [x] T040 [P] Aligner Swagger/OpenAPI généré sur `contracts/openapi.yaml` (codes, `DuplicateProblemDetails`, sécurité) dans `src/Lumineux.Api`
- [x] T041 [P] Revue de sécurité (mot de passe haché jamais en clair, aucun secret en logs/réponses, secrets SMTP hors code, moindre privilège du compte) — `specs/002-member-registration/checklists/security.md`
- [x] T042 **Garde-fou backfill `reference`** : si des membres préexistent sans référence, fournir le script/migration de données de backfill à appliquer **avec T011** (pas après). Sinon (table vide), documenter l'hypothèse. Fichier : `src/Lumineux.Infrastructure/Persistence/Migrations/`
- [x] T043 [P] Documentation : compléter `src/Lumineux.Api/README.md` (endpoints membres, config e-mail, migration)
- [x] T044 Exécuter le scénario `quickstart.md` et vérifier SC-001..SC-005

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** BLOQUE toutes les user stories.
- **US1 (P1)** : après Foundational — cœur (création + compte).
- **US2 (P2)** : après Foundational ; **étend `CreateMember`** (US1) → prévoir US1 avant US2 si mono-développeur.
- **US3 (P2)** : après Foundational ; réutilise entité/repos ; indépendante de US2.
- **Polish (Phase 6)** : après les user stories.

### User Story Dependencies

- **US1** : aucune dépendance sur les autres stories (mais dépend du socle Phase 2).
- **US2** : modifie le handler `CreateMember` de US1 (même fichier) → séquentiel avec US1.
- **US3** : indépendante (nouveaux handlers/endpoints), peut être menée en parallèle de US2 après Foundational.

### Within Each User Story

- Tests d'abord (doivent échouer) — Constitution III.
- Ordre : entités/enums → ports → config EF/migration → repositories/services → cas d'usage → contrôleurs.

### Parallel Opportunities

- Setup : T002 en parallèle de T001.
- Foundational : T004, T006, T007, T008, T010, T012, T013, T014, T015, T016, T017 (fichiers distincts) en parallèle après leurs dépendances (T003/T005/T009/T011).
- Par story : tous les tests `[P]` + DTO/validators `[P]` en parallèle.
- US3 peut avancer en parallèle de US2 (fichiers différents), une fois Foundational terminé.

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Setup → 2. Phase 2 Foundational → 3. Phase 3 US1 → **STOP & VALIDATE** (création membre + compte + transmission identifiants).

### Incremental Delivery

1. Setup + Foundational → socle prêt.
2. + US1 → création de membre opérationnelle (MVP).
3. + US2 → doublons/contacts fiabilisés.
4. + US3 → recherche + correction.
5. Polish → sécurité, doc, validation quickstart.

### Notes

- [P] = fichiers différents, aucune dépendance non satisfaite.
- Vérifier que les tests échouent avant d'implémenter (Constitution III).
- Migration : additive sur `members` ; prévoir le backfill de `reference` (T042) si des membres préexistent.
- Le mot de passe temporaire n'est jamais journalisé ; il n'est renvoyé qu'en repli BureauHandout, une seule fois.
