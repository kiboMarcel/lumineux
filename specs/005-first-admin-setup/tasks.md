---
description: "Task list — Installation du premier administrateur"
---

# Tasks: Installation du premier administrateur

**Input**: Design documents from `specs/005-first-admin-setup/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUS — imposés par la Constitution Lumineux v1.0.0, Principe III.
Unitaires Application ; intégration API (harnais SQLite existant).

**Organization**: Tâches regroupées par user story (US1→US3). Feature de composition — **aucune
nouvelle entité de domaine, aucune migration EF**. Extension de la solution Onion existante
(features 001/002/003/004) : `src/Lumineux.Application|Api`, `tests/Lumineux.*.Tests`.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US3 (uniquement pour les phases de user story)
- Chaque tâche indique le chemin de fichier exact

---

## Phase 1: Setup

Aucune tâche Setup requise — cette feature ne modifie ni la configuration, ni le catalogue de
droits, ni les policies. Elle compose les briques existantes.

**Checkpoint**: aucun.

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: DTO d'entrée + validator partagé + enregistrement DI du handler. Le handler lui-même
est implémenté au fil des user stories.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [x] T001 [P] DTO `InstallFirstAdminRequest(LastName, FirstName, Gender, Password, Email?, Mobile?)` dans `src/Lumineux.Application/Contracts/Setup/SetupDtos.cs`
- [x] T002 [P] Validator `InstallFirstAdminValidator` (FluentValidation) : `LastName`/`FirstName` non vides et ≤ 80 caractères, `Gender` ∈ {"M","F"}, `Password` via `PasswordRules.ApplyPolicy` (feature 003), `Email`/`Mobile` bornés + format email. Fichier : `src/Lumineux.Application/Setup/InstallFirstAdminValidator.cs`
- [x] T003 Squelette `InstallFirstAdminHandler` (constructeur + dépendances : `IBureauProfileRepository`, `IMemberRepository`, `IMemberAccountRepository`, `IMemberReferenceGenerator`, `IPasswordHasher`, `IPermissionCatalog`, `IMemberPermissionRepository`, `ITokenIssuer`, `IClock`, `IAuditLogger`, `IValidator<InstallFirstAdminRequest>`) + méthode `HandleAsync` retournant `TokenResponse` (feature 003) — sans logique métier. Fichier : `src/Lumineux.Application/Setup/InstallFirstAdminHandler.cs`
- [x] T003a **Adapter la fabrique `Member.Create`** : la signature actuelle exige `int antennaId` (non-nullable) alors que l'installation initiale ne rattache pas le premier admin à une antenne (FR-013 côté data-model : `AntennaId = null`). Ajouter une **surcharge** `public static Member Create(string reference, DateTime entryDateUtc, string lastName, string firstName, string gender, int? antennaId)` qui délègue à la version existante quand l'antenne est fournie et instancie directement (avec les mêmes invariants nom/prénom/genre) quand elle est nulle. Ajouter un test unitaire Domain de la nouvelle surcharge. Fichiers : `src/Lumineux.Domain/Entities/Member.cs` + `tests/Lumineux.Domain.Tests/MemberTests.cs` (ou fichier de test dédié).
- [x] T004 Enregistrement DI : `services.AddScoped<InstallFirstAdminHandler>();` dans `src/Lumineux.Application/DependencyInjection.cs`

**Checkpoint**: DTO/validator/skeleton prêts — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Installation initiale sur base vierge (Priority: P1) 🎯 MVP

**Goal**: Sur une base sans admin actif, créer atomiquement Member + MemberAccount actif + profil
« Administrateur » (nouveau ou existant) + attribution ; retourner un jeton d'accès portant tous
les droits fonctionnels connus.

**Independent Test**: `POST /api/v1/setup/first-admin` avec payload valide sur base vierge → 201
+ `TokenResponse` ; le jeton permet immédiatement d'appeler `/bureau-profiles`,
`/members`, `/attendance-sessions` (chaque droit du catalogue est exercé).

### Tests for User Story 1 ⚠️

- [x] T005 [P] [US1] Tests unitaires `InstallFirstAdminHandler` — cas nominal : base vierge, création de Member/Account/Profile/Attribution, émission jeton avec les 3 droits. Vérifications : `SaveChangesAsync` appelé UNE seule fois, entités correctement câblées. Fichier : `tests/Lumineux.Application.Tests/InstallFirstAdminTests.cs`
- [x] T006 [P] [US1] Test d'intégration `POST /api/v1/setup/first-admin` sur base vierge (`ApiTestFixture` **sans** admin amorcé) — 201, `TokenResponse` non vide, aucune donnée sensible dans le corps (`raw.Should().NotContain("password").And.NotContain("passwordHash")`), le jeton porte les 3 droits attendus. Fichier : `tests/Lumineux.Api.Tests/SetupEndpointsTests.cs`
- [x] T007 [P] [US1] Test d'intégration : jeton retourné utilisé pour appeler séquentiellement `GET /bureau-profiles` (200), `GET /members` (200) et `POST /attendance-sessions` (201) — chaque droit vérifié bout-en-bout (SC-004). Fichier : `tests/Lumineux.Api.Tests/SetupEndpointsTests.cs`
- [x] T007a [P] [US1] Test d'intégration **collision coordonnée** (FR-014) : sur base vierge côté profils/comptes, seed d'un `Member` actif avec `Email = "used@example.com"` → `POST /setup/first-admin` avec le même email → 409 `contact_in_use`. Vérifier également qu'aucun `Member`/`MemberAccount`/`BureauProfile` supplémentaire n'a été créé (atomicité — proxy SC-003). Fichier : `tests/Lumineux.Api.Tests/SetupEndpointsTests.cs`

### Implementation for User Story 1

- [x] T008 [US1] Implémenter la **branche « happy path »** de `InstallFirstAdminHandler.HandleAsync` : `CountActiveAdministratorsAsync() == 0` → `ValidateAndThrowAsync(request)` → **vérification collision coordonnée (FR-014)** : si `Email` ou `Mobile` fourni, appeler `_members.IsContactUsedByActiveAsync(request.Email, request.Mobile, excludeMemberId: null, ct)` → si vrai, lever `ConflictException("Coordonnée déjà utilisée par un membre actif.", "contact_in_use")` → génération référence via `IMemberReferenceGenerator` → `Member.Create(reference, _clock.UtcNow, LastName, FirstName, Gender, antennaId: null)` (surcharge nullable de T003a) + `AddAsync` → `MemberAccount.Provision(member, hasher.Hash(password))` + `ChangePassword(sameHash)` + `Activate()` → recherche profil « Administrateur » (`GetByNameNormalizedAsync("administrateur")`) → **création si absent** avec `catalog.All().Select(d => d.Code)` → `AddAssignmentAsync(member, profile)` → **un seul** `SaveChangesAsync` → `GetPermissionsAsync(memberId)` → `ITokenIssuer.Issue(...)` → audit `Operation("Setup.FirstAdminCreated", { memberId, reference })` → retourner `TokenResponse`. Fichier : `src/Lumineux.Application/Setup/InstallFirstAdminHandler.cs`
- [x] T009 [US1] Créer `SetupController` avec `POST /api/v1/setup/first-admin` (`[AllowAnonymous]`, annotations `ProducesResponseType` 201/400/409). Fichier : `src/Lumineux.Api/Controllers/SetupController.cs`
- [x] T010 [US1] Adapter le harnais `ApiTestFixture` : ajouter une méthode `CreateWithoutBureauProfilesAdminAsync()` (ou équivalent) qui garantit une base **sans** admin actif au moment du test — le fixture par défaut ne seed pas d'admin déjà en Phase 4-004, à vérifier ; si besoin, ajouter un helper `ResetBureauProfilesAsync()` pour supprimer d'éventuels profils admin résiduels d'un test précédent. Fichier : `tests/Lumineux.Api.Tests/Infrastructure/ApiTestFixture.cs`

**Checkpoint**: US1 fonctionnelle — MVP. Un opérateur peut installer le premier admin et recevoir un jeton immédiatement utilisable.

---

## Phase 4: User Story 2 — Verrouillage automatique après première installation (Priority: P1)

**Goal**: Dès qu'un admin actif existe, toute nouvelle tentative retourne `409 already_installed`,
**prioritairement** aux erreurs de validation (anti-fuite d'information — FR-005).

**Independent Test**: Après une installation réussie (US1), un second `POST /setup/first-admin`
avec payload valide **ET** avec payload invalide retournent tous deux `409 already_installed`.

### Tests for User Story 2 ⚠️

- [x] T011 [P] [US2] Tests unitaires `InstallFirstAdminHandler` — verrou `already_installed` : lorsque `CountActiveAdministratorsAsync()` renvoie ≥ 1, le handler DOIT lever `ConflictException("already_installed")` **avant** toute autre vérification (validation FluentValidation NON invoquée). Simuler cela en passant un payload volontairement invalide ET un compte admin fictif → l'exception doit rester `already_installed`. Fichier : `tests/Lumineux.Application.Tests/InstallFirstAdminTests.cs`
- [x] T012 [P] [US2] Test d'intégration : second `POST /setup/first-admin` avec payload valide après installation → 409 `already_installed`. Fichier : `tests/Lumineux.Api.Tests/SetupEndpointsTests.cs`
- [x] T013 [P] [US2] Test d'intégration anti-fuite : après installation, un `POST /setup/first-admin` avec payload **invalide** (nom vide, mot de passe faible) → 409 `already_installed` (surtout PAS 400). Fichier : `tests/Lumineux.Api.Tests/SetupEndpointsTests.cs`

### Implementation for User Story 2

- [x] T014 [US2] Compléter `InstallFirstAdminHandler.HandleAsync` : **PRIORITÉ 1** avant `_validator.ValidateAndThrowAsync(...)`, invoquer `_profiles.CountActiveAdministratorsAsync()` ; si ≥ 1, journaliser `Refused("Setup.FirstAdmin", "Déjà installé")` puis lever `ConflictException("Le système est déjà installé — un administrateur actif existe.", "already_installed")`. Fichier : `src/Lumineux.Application/Setup/InstallFirstAdminHandler.cs`
- [x] T014a [P] [US2] Test d'intégration **coexistence avec `Auth:Bootstrap:*`** (FR-012) : (a) démarrer le fixture avec `Auth:Bootstrap:MemberReference` + `Auth:Bootstrap:Permissions` configurés et un `Member`/`MemberAccount` bootstrap seed → au démarrage, `PermissionBootstrapper` (feature 003) et `BureauProfilesBootstrapper` (feature 004) créent un admin actif → `POST /setup/first-admin` répond 409 `already_installed` ; (b) sans bootstrap, base vierge → 201 (chemin nominal, déjà couvert par T006, à référencer pour illustrer l'autre branche). Fichier : `tests/Lumineux.Api.Tests/SetupBootstrapCoexistenceTests.cs`

**Checkpoint**: US1 + US2 — un opérateur peut installer une fois, toute nouvelle tentative est verrouillée avec le bon code métier.

---

## Phase 5: User Story 3 — Idempotence sur le profil « Administrateur » (Priority: P2)

**Goal**: Si un profil « Administrateur » existe déjà (ex. créé par `BureauProfilesBootstrapper`
de la feature 004 avec un nom éponyme, ou orphelin après archivage d'un ancien admin), la route
le **réutilise** sans le dupliquer ni modifier sa description/liste de droits.

**Independent Test**: Base avec profil « Administrateur » préexistant **sans** admin actif titulaire →
`POST /setup/first-admin` réussit, aucun nouveau profil créé, description/permissions du profil
existant inchangées.

### Tests for User Story 3 ⚠️

- [x] T015 [P] [US3] Tests unitaires : le handler appelle `GetByNameNormalizedAsync("administrateur")` **avant** de tenter une création ; si un profil est retourné, `AddAsync(profile)` NE DOIT PAS être appelé, mais `AddAssignmentAsync` DOIT l'être avec l'id du profil existant. Fichier : `tests/Lumineux.Application.Tests/InstallFirstAdminTests.cs`
- [x] T016 [P] [US3] Test d'intégration : sur base avec profil « Administrateur » préexistant (créé via seed EF explicite dans le test) portant une description particulière et une liste de droits **partielle** (ex. seul `manage_bureau_profiles`), l'installation réussit ; le profil est réutilisé et sa liste de droits **reste inchangée** (pas d'ajout des autres droits). L'admin est bien attribué au profil et son jeton porte les droits **actuels** du profil (donc uniquement `manage_bureau_profiles` — pas les 3). Fichier : `tests/Lumineux.Api.Tests/SetupEndpointsTests.cs`

### Implementation for User Story 3

- [x] T017 [US3] Vérifier / consolider la logique déjà présente en T008 : la branche « profil existant » NE DOIT PAS toucher aux permissions du profil ni écraser sa description (FR-013). Ajouter un commentaire de garde-fou en tête du bloc et éventuellement un `Debug.Assert`. Fichier : `src/Lumineux.Application/Setup/InstallFirstAdminHandler.cs`
- [x] T017a [P] [US3] Test unitaire **atomicité sur chemin d'erreur** (SC-003) : simuler via mock une `SaveChangesAsync` qui lève `DbUpdateException` → vérifier qu'aucun retour partiel (`TokenResponse`) n'est produit, que l'exception se propage, et que `_tokenIssuer.Issue(...)` n'est **jamais** appelé (l'émission de jeton dépend d'une réussite atomique). Fichier : `tests/Lumineux.Application.Tests/InstallFirstAdminTests.cs`

**Checkpoint**: Les 3 user stories fonctionnent — installation robuste, verrouillage, idempotence.

---

## Phase 6: Polish & Cross-Cutting

- [x] T018 [P] Confirmer la journalisation `IAuditLogger` : `Operation("Setup.FirstAdminCreated", { memberId, reference })` en cas de succès ; `Refused("Setup.FirstAdmin", "Déjà installé")` en cas de verrou ; **jamais** le mot de passe fourni, **jamais** le jeton émis (FR-010, SC-005). Vérifier par revue de code + éventuellement un test d'assertion sur le mock d'`IAuditLogger`. Fichier : revue de `src/Lumineux.Application/Setup/InstallFirstAdminHandler.cs`
- [x] T019 [P] Aligner Swagger/OpenAPI sur `contracts/openapi.yaml` : annotations `[ProducesResponseType]` complètes sur `SetupController.Install` (201/400/409). Rédiger la revue de sécurité dans `specs/005-first-admin-setup/checklists/security.md`.
- [x] T020 [P] Documentation : compléter `src/Lumineux.Api/README.md` (ajouter la ligne `POST /setup/first-admin` — anonyme, verrou naturel « 0 admin ») et lier `contracts/openapi.yaml`.
- [x] T021 Exécuter le scénario `quickstart.md` bout-en-bout et vérifier SC-001..SC-006, dont : (a) installation < 500 ms p95 en local (SC-001) ; (b) les 3 endpoints protégés accessibles avec le jeton (SC-004) ; (c) après installation, tout appel ultérieur → 409 même avec payload invalide (SC-002/FR-005) ; (d) non-régression complète des features 001–004.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)** BLOQUE toutes les user stories.
- **US1 (P1)** : happy path — cœur ; après Foundational.
- **US2 (P1)** : verrou 0-admin + refus prioritaire — DÉPEND d'US1 (même handler et endpoint ; le refus doit précéder la validation, ce qui implique une modification de `HandleAsync` après T008).
- **US3 (P2)** : idempotence profil — indépendant sur le plan des tests, mais partage le même handler (le code T017 vérifie/consolide ce que T008 aura déjà en principe implémenté).
- **Polish (Phase 6)** : après les user stories.

### User Story Dependencies

- **US1** : après Foundational ; introduit `SetupController` + happy path.
- **US2** : après US1 ; modifie **la même méthode** `HandleAsync` (T008 → T014 séquentiel).
- **US3** : après US1 ; principalement des tests + un commentaire de garde-fou dans le handler.

### Within Each User Story

- Tests d'abord (doivent échouer) — Constitution III.
- Ordre : DTO/validator → handler → contrôleur.

### Parallel Opportunities

- Foundational : T001 ∥ T002 ∥ T003a (fichiers distincts — T003a touche `Member.cs`) ; T003 après T001+T002 ; T004 après T003. T008 dépend de T003a (surcharge Member.Create).
- Par story : tests `[P]` en parallèle (T005–T007a en US1 ; T011–T013 + T014a en US2 ; T015–T016 + T017a en US3).
- Un seul fichier partagé côté implémentation : `InstallFirstAdminHandler.cs` (T003 → T008 → T014 → T017 séquentiels) et `SetupEndpointsTests.cs` (tests d'intégration ajoutés séquentiellement au même fichier).
- T014a et T017a sont dans des fichiers distincts → parallélisables avec les autres tests de leur story.
- Polish : T018, T019, T020 en parallèle ; T021 en fin.

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Foundational → US1 (happy path) → US2 (verrou prioritaire). À ce stade, l'API est **sûre** :
   un opérateur installe le premier admin sur base vierge, tout appel ultérieur est verrouillé.
   Parcours démontrable et déployable.

### Incremental Delivery

1. Foundational → DTO/validator/handler squelette.
2. + US1 → happy path complet (MVP fonctionnel).
3. + US2 → verrouillage sécurisé (MVP publiable).
4. + US3 → idempotence robuste (reprise après incident).
5. Polish → journalisation, doc, quickstart, revue de sécurité.

### Notes

- [P] = fichiers différents, aucune dépendance non satisfaite.
- Vérifier que les tests échouent avant d'implémenter (Constitution III).
- **Zéro migration EF** : aucune nouvelle table, aucune nouvelle colonne.
- **Zéro nouveau port** : composition pure de briques existantes (features 001–004).
- Codes RFC 7807 : `already_installed` (nouveau), `contact_in_use` (hérité feature 002),
  `duplicate_reference` (hérité feature 002 — improbable sur base vierge).
- Le harnais de test partage un `ApiTestFixture` : la Phase 4 (feature 004) a introduit un pattern
  où les tests créent leurs propres admins avec des références uniques ; le fixture reste sain
  entre exécutions.
