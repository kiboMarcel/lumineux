---
description: "Task list — Profils du bureau"
---

# Tasks: Profils du bureau

**Input**: Design documents from `specs/004-bureau-profiles/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUS — imposés par la Constitution Lumineux v1.0.0, Principe III.
Unitaires Domain/Application ; intégration API (harnais SQLite existant).

**Organization**: Tâches regroupées par user story (US1→US4). Extension de la solution Onion
existante (features 001/002/003) : `src/Lumineux.Domain|Application|Infrastructure|Api`,
`tests/Lumineux.*.Tests`.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US4 (uniquement pour les phases de user story)
- Chaque tâche indique le chemin de fichier exact

---

## Phase 1: Setup

- [x] T001 [P] Ajouter la constante `Permissions.ManageBureauProfiles` (valeur `"manage_bureau_profiles"`) dans `src/Lumineux.Application/Abstractions/Permissions.cs`
- [x] T002 [P] Enregistrer la policy `manage_bureau_profiles` (RequireClaim) dans `src/Lumineux.Api/Program.cs`

**Checkpoint**: Droit d'administration et policy d'autorisation disponibles.

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: Entités, ports, migration EF, refactor du repository de droits, DI, bootstrap.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [x] T003 [P] Créer l'entité `BureauProfile` (nom + nom normalisé CI, description, méthodes `Rename`/`UpdateDescription`/`SetPermissions` s'appuyant sur `IPermissionCatalog`) dans `src/Lumineux.Domain/Entities/BureauProfile.cs`
- [x] T004 [P] Créer l'entité `BureauProfilePermission` (association profil ↔ droit, unicité `(profil, droit)`) dans `src/Lumineux.Domain/Entities/BureauProfilePermission.cs`
- [x] T005 [P] Créer l'entité `MemberBureauProfile` (association membre ↔ profil, unicité `(membre, profil)`) dans `src/Lumineux.Domain/Entities/MemberBureauProfile.cs`
- [x] T006 [P] Définir le port `IPermissionCatalog` (`Contains(code)`, `All()` retourne descripteurs code + libellé) dans `src/Lumineux.Application/Abstractions/IPermissionCatalog.cs`
- [x] T007 [P] Définir le port `IBureauProfileRepository` (Add/Get/List/Delete + `IsAssignedAsync(profileId)` + `CountActiveAdministrators(excludeProfileId?, excludeMemberId?)` + `GetByNameNormalizedAsync`) dans `src/Lumineux.Domain/Abstractions/IBureauProfileRepository.cs`
- [x] T008 [P] Enrichir `IMemberAccountRepository`/introduire helper si nécessaire pour vérifier `Member.IsActive` par id — sinon réutiliser `IMemberRepository.GetByIdAsync` dans les handlers (documenter dans `IBureauProfileRepository.cs`)
- [x] T009 Configuration EF : `BureauProfileConfiguration`, `BureauProfilePermissionConfiguration`, `MemberBureauProfileConfiguration` (unicités, FK, longueurs) + `DbSet<BureauProfile>`, `DbSet<BureauProfilePermission>`, `DbSet<MemberBureauProfile>` dans `src/Lumineux.Infrastructure/Persistence/Configurations/` et `AppDbContext.cs`
- [x] T010 Générer la **migration** EF `BureauProfiles` (création des 3 tables + index unique CI sur `bureau_profiles.name_normalized`, unicités composées) dans `src/Lumineux.Infrastructure/Persistence/Migrations/`
- [x] T011 [P] Implémenter `PermissionCatalog` (référentiel figé : `manage_attendance`, `manage_members`, `manage_bureau_profiles`) dans `src/Lumineux.Infrastructure/Security/PermissionCatalog.cs`
- [x] T012 [P] Implémenter `BureauProfileRepository` (EF) dans `src/Lumineux.Infrastructure/Repositories/BureauProfileRepository.cs`
- [x] T013 **Refactor** `MemberPermissionRepository.GetPermissionsAsync(memberId)` : lit désormais l'union des droits via `member_bureau_profiles` × `bureau_profile_permissions` (contrat `IMemberPermissionRepository` inchangé) dans `src/Lumineux.Infrastructure/Repositories/MemberPermissionRepository.cs`
- [x] T014 [P] Implémenter `BureauProfilesBootstrapper` (au démarrage, après migrations : crée le profil « Amorçage » avec l'union des droits présents dans `member_permissions` et l'assigne au membre `Auth:Bootstrap:MemberReference` ; si la référence est vide ou introuvable, assigne le profil à tous les membres présents dans `member_permissions` — cf. FR-013 précisé ; idempotent, audité) dans `src/Lumineux.Infrastructure/Security/BureauProfilesBootstrapper.cs`
- [x] T014a [P] Test d'intégration `BureauProfilesBootstrapperTests` (SQLite via `ApiTestFixture`) couvrant : (1) premier démarrage, `member_permissions` peuplée + `Auth:Bootstrap:MemberReference` valide → profil « Amorçage » créé avec l'union des droits, attribué au membre référencé ; (2) `Auth:Bootstrap:MemberReference` vide/absente → attribution à tous les membres ayant des lignes dans `member_permissions` ; (3) relance à l'identique → aucune duplication (idempotence sur nom de profil + attributions) ; (4) `member_permissions` vide → aucune action. Fichier : `tests/Lumineux.Api.Tests/BureauProfilesBootstrapperTests.cs`
- [x] T015 Enregistrement DI : `IPermissionCatalog`→`PermissionCatalog`, `IBureauProfileRepository`→`BureauProfileRepository`, `BureauProfilesBootstrapper` en `IHostedService` (ou service invoqué au démarrage après `PermissionBootstrapper`) dans `src/Lumineux.Infrastructure/DependencyInjection.cs`
- [x] T015a [P] **Adapter le harnais de test** : `ApiTestFixture.SeedActiveMemberAccountAsync(reference, password, ...permissions)` NE DOIT PLUS écrire dans `member_permissions` (source obsolète après T013). Elle DOIT désormais, pour chaque permission demandée, obtenir-ou-créer un profil de test dédié (nom stable ex. `"Test/<permission>"`) contenant cette permission et **attribuer** ce profil au membre via `MemberBureauProfile`. Sans cette adaptation, tous les tests d'intégration des features 001/002/003 utilisant `/auth/login` avec des droits perdent leurs claims → régression massive. Fichier : `tests/Lumineux.Api.Tests/Infrastructure/ApiTestFixture.cs`
- [x] T015b [P] Test d'intégration `BureauProfileRepositoryTests.CountActiveAdministrators` (SQLite via `ApiTestFixture`) couvrant : 0 admin, 1 admin, N admins ; `excludeProfileId` (simule le retrait de `manage_bureau_profiles` sur un profil) ; `excludeMemberId` (simule la révocation d'une attribution) ; membre inactif exclu du décompte. Ce test verrouille la brique du garde-fou triple FR-012 (SC-004). Fichier : `tests/Lumineux.Api.Tests/BureauProfileRepositoryTests.cs`

**Checkpoint**: Socle prêt — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Définir un profil du bureau (Priority: P1) 🎯 MVP

**Goal**: Créer / modifier / supprimer un profil du bureau (nom, description, liste de droits) ;
écritures réservées à `manage_bureau_profiles`.

**Independent Test**: Un administrateur crée `« Gestion des présences »` avec `manage_attendance`,
ajoute `manage_members`, puis supprime le profil non attribué — via `POST/PUT/DELETE /api/v1/bureau-profiles`.

### Tests for User Story 1 ⚠️

- [x] T016 [P] [US1] Tests unitaires Domain `BureauProfile` (nom vide→invariant, nom trop long, `SetPermissions` refuse un droit hors catalogue, déduplication) dans `tests/Lumineux.Domain.Tests/BureauProfileTests.cs`
- [x] T017 [P] [US1] Tests unitaires `CreateBureauProfileHandler` (succès, nom dupliqué→409 `duplicate_name`, droit inconnu→400, sans droit `manage_bureau_profiles`→403) dans `tests/Lumineux.Application.Tests/CreateBureauProfileTests.cs`
- [x] T018 [P] [US1] Tests unitaires `UpdateBureauProfileHandler` (succès, nom dupliqué, droit inconnu, garde-fou dernier admin→409 `last_administrator`) dans `tests/Lumineux.Application.Tests/UpdateBureauProfileTests.cs`
- [x] T019 [P] [US1] Tests unitaires `DeleteBureauProfileHandler` (succès non attribué, attribué→409 `profile_in_use`, garde-fou→409 `last_administrator`) dans `tests/Lumineux.Application.Tests/DeleteBureauProfileTests.cs`
- [x] T020 [P] [US1] Test d'intégration `POST/PUT/DELETE /api/v1/bureau-profiles` (201/200/204 + 400/401/403/404/409) dans `tests/Lumineux.Api.Tests/BureauProfilesCrudEndpointsTests.cs`

### Implementation for User Story 1

- [x] T021 [P] [US1] DTO `BureauProfileWriteRequest`, `BureauProfileSummary`, `BureauProfileDetail`, `MemberRef` dans `src/Lumineux.Application/Contracts/BureauProfiles/BureauProfileDtos.cs`
- [x] T022 [P] [US1] Validator `BureauProfileWriteValidator` (nom non vide/borné, description bornée, `permissions` uniqueItems) dans `src/Lumineux.Application/BureauProfiles/BureauProfileValidators.cs`
- [x] T023 [US1] Cas d'usage `CreateBureauProfileHandler` (vérif. droit, validation catalogue, unicité nom CI, audit) dans `src/Lumineux.Application/BureauProfiles/CreateBureauProfileHandler.cs`
- [x] T024 [US1] Cas d'usage `UpdateBureauProfileHandler` (vérif. droit, unicité nom CI, catalogue, **garde-fou** si retrait de `manage_bureau_profiles`, audit) dans `src/Lumineux.Application/BureauProfiles/UpdateBureauProfileHandler.cs`
- [x] T025 [US1] Cas d'usage `DeleteBureauProfileHandler` (vérif. droit, refus si attribué, **garde-fou** dernier admin, audit) dans `src/Lumineux.Application/BureauProfiles/DeleteBureauProfileHandler.cs`
- [x] T026 [US1] `BureauProfilesController` : `POST /api/v1/bureau-profiles`, `PUT /{id}`, `DELETE /{id}` (`[Authorize(Policy = "manage_bureau_profiles")]`) dans `src/Lumineux.Api/Controllers/BureauProfilesController.cs`
- [x] T027 [US1] Enregistrement DI des 3 handlers dans `src/Lumineux.Application/DependencyInjection.cs`

**Checkpoint**: US1 fonctionnelle (CRUD des profils) — MVP.

---

## Phase 4: User Story 2 — Assigner un profil à un membre (Priority: P1)

**Goal**: Attribuer un profil à un membre actif ; effet sur le jeton à la prochaine connexion
(union des droits).

**Independent Test**: L'administrateur crée un profil `manage_attendance`, l'assigne à un membre
sans droit, celui-ci se reconnecte et peut démarrer une session (endpoint `manage_attendance`).

### Tests for User Story 2 ⚠️

- [x] T028 [P] [US2] Tests unitaires `AssignProfileHandler` (succès, idempotence, membre inactif→409 `member_inactive`, profil inexistant→404, sans droit `manage_bureau_profiles`→403) dans `tests/Lumineux.Application.Tests/AssignProfileTests.cs`
- [x] T029 [P] [US2] Test unitaire du refactor `MemberPermissionRepository` : union sans doublon depuis 2 profils (test d'intégration Infrastructure sur SQLite) dans `tests/Lumineux.Api.Tests/MemberPermissionRepositoryUnionTests.cs`
- [x] T030 [P] [US2] Test d'intégration `POST /api/v1/members/{id}/bureau-profiles` (204 + 401/403/404/409 `member_inactive`) et effet sur la prochaine émission de jeton (nouveau `/auth/login` porte les droits attendus) dans `tests/Lumineux.Api.Tests/AssignBureauProfileEndpointsTests.cs`

### Implementation for User Story 2

- [x] T031 [P] [US2] DTO `AssignProfileRequest` dans `src/Lumineux.Application/Contracts/BureauProfiles/BureauProfileDtos.cs`
- [x] T032 [US2] Cas d'usage `AssignProfileHandler` (vérif. droit, existence membre/profil, statut membre actif → sinon `ConflictException` code `member_inactive` (409, FR-014), idempotence — insertion « if not exists », audit) dans `src/Lumineux.Application/BureauProfiles/AssignProfileHandler.cs`
- [x] T033 [US2] Ajouter `POST /api/v1/members/{memberId}/bureau-profiles` à un nouveau `MemberBureauProfilesController` dans `src/Lumineux.Api/Controllers/MemberBureauProfilesController.cs`
- [x] T034 [US2] Enregistrement DI du handler dans `src/Lumineux.Application/DependencyInjection.cs`

**Checkpoint**: US1 + US2 — l'attribution rend les droits vivants (via prochain jeton).

---

## Phase 5: User Story 3 — Révoquer / faire évoluer les droits (Priority: P2)

**Goal**: Révoquer une attribution ; les changements prennent effet à la prochaine émission de
jeton ; garde-fou anti-verrouillage.

**Independent Test**: Un membre avec `manage_attendance` via profil voit son attribution révoquée ;
son nouveau jeton n'a plus le droit et le démarrage d'une session est refusé (403).

### Tests for User Story 3 ⚠️

- [x] T035 [P] [US3] Tests unitaires `RevokeProfileHandler` (succès, attribution inexistante→404, garde-fou dernier admin→409 `last_administrator`, sans droit→403) dans `tests/Lumineux.Application.Tests/RevokeProfileTests.cs`
- [x] T036 [P] [US3] Test d'intégration `DELETE /api/v1/members/{id}/bureau-profiles/{profileId}` (204/401/403/404/409) + effet sur la prochaine émission de jeton (droit perdu) dans `tests/Lumineux.Api.Tests/RevokeBureauProfileEndpointsTests.cs`
- [x] T037 [P] [US3] Tests d'intégration du garde-fou dernier admin (les 3 portes : révocation, retrait de droit sur le profil, suppression du profil) dans `tests/Lumineux.Api.Tests/BureauProfilesLastAdministratorTests.cs`

### Implementation for User Story 3

- [x] T038 [US3] Cas d'usage `RevokeProfileHandler` (vérif. droit, existence attribution, **garde-fou** dernier admin via `IBureauProfileRepository.CountActiveAdministrators`, audit) dans `src/Lumineux.Application/BureauProfiles/RevokeProfileHandler.cs`
- [x] T039 [US3] Ajouter `DELETE /api/v1/members/{memberId}/bureau-profiles/{profileId}` à `MemberBureauProfilesController`
- [x] T040 [US3] Enregistrement DI du handler dans `src/Lumineux.Application/DependencyInjection.cs`

**Checkpoint**: US1 + US2 + US3 fonctionnent ; garde-fou opérationnel.

---

## Phase 6: User Story 4 — Consulter les profils et leurs titulaires (Priority: P2)

**Goal**: Lecture du catalogue (profils + titulaires) et vue par membre (profils + droits
effectifs) ; accès `manage_bureau_profiles` OU `manage_members`.

**Independent Test**: Après création de 2 profils et 2 attributions, `GET /api/v1/bureau-profiles`
retourne les profils avec `memberCount`, `GET /members/{id}/bureau-profiles` retourne les profils
et l'union `effectivePermissions`.

### Tests for User Story 4 ⚠️

- [x] T041 [P] [US4] Tests unitaires `ListBureauProfilesHandler` / `GetBureauProfileHandler` / `GetMemberProfilesHandler` (données correctes, droit `manage_bureau_profiles` OU `manage_members`→OK, sinon 403) dans `tests/Lumineux.Application.Tests/BureauProfileQueryTests.cs`
- [x] T042 [P] [US4] Test d'intégration `GET /api/v1/bureau-profiles`, `GET /{id}`, `GET /api/v1/members/{id}/bureau-profiles`, `GET /api/v1/permissions` (200 + 401/403/404). DOIT en outre asserter : (a) **FR-016** — la réponse brute NE CONTIENT NI `passwordHash`, NI `email`, NI `mobile` (`raw.Should().NotContain(...)`) ; (b) **FR-008** — `GET /permissions` retourne exactement `manage_attendance`, `manage_members`, `manage_bureau_profiles`. Fichier : `tests/Lumineux.Api.Tests/BureauProfilesQueryEndpointsTests.cs`

### Implementation for User Story 4

- [x] T043 [P] [US4] DTO `MemberProfilesResponse`, `PermissionDescriptor` dans `src/Lumineux.Application/Contracts/BureauProfiles/BureauProfileDtos.cs`
- [x] T044 [US4] Cas d'usage `ListBureauProfilesHandler`, `GetBureauProfileHandler`, `GetMemberProfilesHandler`, `ListPermissionsHandler` (autorisation via `ICurrentUser.HasPermission` — union des deux droits) dans `src/Lumineux.Application/BureauProfiles/`
- [x] T045 [US4] Ajouter `GET /api/v1/bureau-profiles`, `GET /api/v1/bureau-profiles/{id}`, `GET /api/v1/permissions` à `BureauProfilesController` (avec `[Authorize]` sans policy — vérif. dans le handler) et `GET /api/v1/members/{memberId}/bureau-profiles` à `MemberBureauProfilesController`
- [x] T046 [US4] Enregistrement DI des handlers de requête dans `src/Lumineux.Application/DependencyInjection.cs`

**Checkpoint**: Les 4 user stories fonctionnent ; gouvernance des profils complète.

---

## Phase 7: Polish & Cross-Cutting

- [x] T047 [P] Confirmer la journalisation via `IAuditLogger` de tous les événements (create/update/delete profile, assign/revoke, migration Bootstrapper) — sans PII — et compléter là où manquant (FR-010, SC-006)
- [x] T048 [P] Aligner Swagger/OpenAPI sur `contracts/openapi.yaml` (annotations `ProducesResponseType` complètes) et rédiger la revue de sécurité dans `specs/004-bureau-profiles/checklists/security.md`
- [x] T049 [P] Documentation : compléter `src/Lumineux.Api/README.md` (endpoints `/bureau-profiles`, `/members/{id}/bureau-profiles`, `/permissions` ; nouvelle policy `manage_bureau_profiles` ; migration `BureauProfiles` ; note sur le repli `Auth:Bootstrap:*`)
- [x] T050 Exécuter le scénario `quickstart.md` et vérifier SC-001..SC-006, dont la **migration idempotente au démarrage** (profil « Amorçage ») et la **non-régression** des tests d'auth existants (features 001/002/003)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** BLOQUE toutes les user stories.
- **US1 (P1)** : CRUD profil — cœur ; après Foundational.
- **US2 (P1)** : attribution — dépend d'US1 (existence d'un profil) pour les tests ; peut être développée en parallèle mais validée après US1.
- **US3 (P2)** : révocation + garde-fou — dépend d'US2 (existence d'attributions).
- **US4 (P2)** : lectures — indépendant des mutations, peut avancer en parallèle d'US2/US3.
- **Polish (Phase 7)** : après les user stories.

### User Story Dependencies

- **US1** : après Foundational ; introduit `BureauProfilesController` (CRUD).
- **US2** : après Foundational + une base d'US1 (au moins le CRUD minimal) pour les tests d'intégration.
- **US3** : dépend d'US2 (attributions à révoquer) et de la logique garde-fou d'US1 (couvre les 3 portes).
- **US4** : après Foundational ; indépendant des mutations pour la logique.

### Within Each User Story

- Tests d'abord (doivent échouer) — Constitution III.
- Ordre : entité/ports → config EF/migration → services/repos → cas d'usage → contrôleur.

### Parallel Opportunities

- Foundational : T003, T004, T005, T006, T007, T011, T012, T014 en parallèle (fichiers distincts) — T009/T010 après T003–T005 ; T013 après T009 ; T015 après tous ; T015a/T015b en parallèle après T013 (T015a) et T012 (T015b).
- Par story : tests `[P]` + DTO/validators `[P]` en parallèle.
- Contrôleurs partagés (`BureauProfilesController`, `MemberBureauProfilesController`) : endpoints ajoutés séquentiellement (T026 → T033/T039 → T045).

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Setup → Foundational → US1 (CRUD) → US2 (attribution). À ce stade, un administrateur peut créer
   un profil, l'attribuer, et le membre bénéficie des droits à sa prochaine connexion — parcours
   démontrable.

### Incremental Delivery

1. Setup + Foundational → socle prêt (dont refactor `IMemberPermissionRepository` : union via profils).
2. + US1 → CRUD des profils (MVP admin).
3. + US2 → attribution + effet sur les jetons (MVP fonctionnel).
4. + US3 → révocation + garde-fou anti-verrouillage.
5. + US4 → lecture (catalogue + vue par membre).
6. Polish → journalisation, doc, quickstart, revue de sécurité.

### Notes

- [P] = fichiers différents, aucune dépendance non satisfaite.
- Vérifier que les tests échouent avant d'implémenter (Constitution III).
- Refactor de `MemberPermissionRepository` (T013) : le **contrat** `IMemberPermissionRepository`
  reste inchangé côté callers ; la source de lecture bascule sur les profils. La **non-régression**
  des tests d'auth (features 001/002/003) est garantie par T015a (adaptation du harnais qui attribue
  désormais les permissions via un profil de test).
- La migration idempotente `BureauProfilesBootstrapper` (T014) préserve les droits amorcés issus de
  la feature 003 ; le repli `PermissionBootstrapper` reste en place comme filet d'urgence.
- Codes d'erreur métiers documentés : `duplicate_name`, `profile_in_use`, `last_administrator`
  (RFC 7807, extension `code`).
