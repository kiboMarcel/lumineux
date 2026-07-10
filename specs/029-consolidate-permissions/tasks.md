---
description: "Task list — Consolidation du RBAC sur les profils (retrait du mécanisme hérité, feature 029)"
---

# Tasks: Consolidation du RBAC sur les profils du bureau (retrait du mécanisme hérité)

**Input**: Design documents from `specs/029-consolidate-permissions/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — Principe III. Ici, les tests sont surtout **adaptés** (mocks au nouveau nom) et
**élagués** (tests de coexistence/migration obsolètes) ; le **filet de non-régression** = la suite existante
qui doit rester **verte** (mêmes droits/claims, mêmes 403).

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, sans dépendance sur une tâche incomplète)
- **[Story]** : US1 / US2 / US3 (voir spec.md) — absent en Setup / Foundational / Polish
- Chemins **exacts** (racine repo). ⚠️ **Refactor séquentiel** : le code ne compile qu'une fois l'ensemble
  cohérent ; la validation build/test est en Polish.

## Path Conventions

API .NET (Onion) sous `src/` + tests sous `tests/`. Aucun client (SPA/mobile) impacté.
**Contrainte** : la migration (T017) et `dotnet test` (T019) exigent l'**API dev arrêtée** (verrou de DLL).

---

## Phase 1: Setup

- [X] T001 Vérifier la baseline verte **avant** modifications (API dev arrêtée) : `dotnet build` et `dotnet test` réussissent — repère de non-régression.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Retirer les **consommateurs** du mécanisme hérité puis **renommer/réduire le port** — pivot dont dépendent toutes les user stories.

**⚠️ CRITICAL**: Aucune user story ne peut être finalisée avant cette phase (le port renommé est partagé).

- [X] T002 Supprimer `src/Lumineux.Infrastructure/Security/PermissionBootstrapper.cs` et son enregistrement `services.AddHostedService<PermissionBootstrapper>();` dans `src/Lumineux.Infrastructure/DependencyInjection.cs` (research.md D3).
- [X] T003 Supprimer `src/Lumineux.Infrastructure/Security/BureauProfilesBootstrapper.cs` et son enregistrement `services.AddHostedService<BureauProfilesBootstrapper>();` dans `src/Lumineux.Infrastructure/DependencyInjection.cs` (research.md D3).
- [X] T004 Retirer la configuration d'amorçage héritée : propriété `Bootstrap` + classe `BootstrapOptions` dans `src/Lumineux.Application/Abstractions/AuthOptions.cs`, **et** la section `"Bootstrap": { … }` sous `Auth` dans `src/Lumineux.Api/appsettings.json` (FR-007).
- [X] T005 Renommer le port `src/Lumineux.Domain/Abstractions/IMemberPermissionRepository.cs` → `IEffectivePermissionsReader` ; ne conserver que la lecture, renommée `GetEffectivePermissionsAsync(int memberId, CancellationToken)` ; **supprimer** `HasPermissionAsync`, `AddAsync`, `SaveChangesAsync` (data-model.md §3).
- [X] T006 Renommer l'implémentation `src/Lumineux.Infrastructure/Repositories/MemberPermissionRepository.cs` → `EffectivePermissionsReader` : conserver **uniquement** la requête sur les **profils** (`MemberBureauProfiles ⋈ BureauProfilePermissions`), méthode `GetEffectivePermissionsAsync` ; mettre à jour l'enregistrement DI (`IEffectivePermissionsReader`) dans `DependencyInjection.cs`.

**Checkpoint**: Le port unique de lecture (profils) est en place ; l'ancien mécanisme n'a plus de consommateur.

---

## Phase 3: User Story 1 — Autorisation strictement inchangée (Priority: P1) 🎯 MVP

**Goal**: Les droits du jeton (connexion/activation) restent **identiques**, issus des profils.

**Independent Test**: rejouer connexion/activation → mêmes droits dans le jeton qu'avant ; accès autorisés/refusés inchangés.

- [X] T007 [US1] Mettre à jour `src/Lumineux.Application/Auth/LoginHandler.cs` et `src/Lumineux.Application/Auth/ActivateAccountHandler.cs` : injecter `IEffectivePermissionsReader` et appeler `GetEffectivePermissionsAsync` (comportement inchangé).
- [X] T008 [P] [US1] Adapter les mocks dans `tests/Lumineux.Application.Tests/LoginTests.cs` et `tests/Lumineux.Application.Tests/ActivateAccountTests.cs` au nouveau type/nom de méthode ; les assertions de droits restent identiques (non-régression, SC-001/SC-002).
- [X] T009 [P] [US1] Adapter `tests/Lumineux.Api.Tests/MemberPermissionRepositoryUnionTests.cs` au nouveau type `IEffectivePermissionsReader` (le test vérifie l'**union de plusieurs profils** avec dédup — conservé tel quel sur le fond).

**Checkpoint**: Connexion/activation compilent et testent avec la source unique.

---

## Phase 4: User Story 2 — Amorçage de l'admin initial préservé (Priority: P1)

**Goal**: La première installation crée toujours un admin doté de tous les droits **via un profil**.

**Independent Test**: installation sur instance vierge → admin avec profil « Administrateur » et tous les droits.

- [X] T010 [US2] Mettre à jour `src/Lumineux.Application/Setup/InstallFirstAdminHandler.cs` : injecter `IEffectivePermissionsReader` + `GetEffectivePermissionsAsync` (l'octroi par profil « Administrateur » reste **inchangé**).
- [X] T011 [P] [US2] Adapter le mock dans `tests/Lumineux.Application.Tests/InstallFirstAdminTests.cs` au nouveau type/nom de méthode ; assertions inchangées (SC-003).

**Checkpoint**: Le setup admin ne dépend plus d'aucun nom hérité.

---

## Phase 5: User Story 3 — Source unique, aucun code/table mort (Priority: P2)

**Goal**: Supprimer l'entité, la table, la config héritées et les tests obsolètes ; **zéro vestige**.

**Independent Test**: recherche du mécanisme hérité = 0 occurrence vivante ; la table `member_permissions` n'existe plus.

- [X] T012 [US3] Supprimer l'entité `src/Lumineux.Domain/Entities/MemberPermission.cs`.
- [X] T013 [US3] Supprimer la config EF `src/Lumineux.Infrastructure/Persistence/Configurations/MemberPermissionConfiguration.cs`.
- [X] T014 [US3] Retirer le `DbSet<MemberPermission> MemberPermissions` de `src/Lumineux.Infrastructure/Persistence/AppDbContext.cs`.
- [X] T015 [US3] Dans `tests/Lumineux.Api.Tests/Infrastructure/ApiTestFixture.cs`, retirer la purge `db.MemberPermissions.RemoveRange(...)` de `ResetInstallationStateAsync` (table supprimée).
- [X] T016 [P] [US3] Supprimer les tests obsolètes `tests/Lumineux.Api.Tests/BureauProfilesBootstrapperTests.cs` et `tests/Lumineux.Api.Tests/SetupBootstrapCoexistenceTests.cs` (scénarios de coexistence/migration disparus).
- [X] T017 [US3] Générer la migration `RemoveMemberPermissions` (**DROP** de la table `member_permissions`) sous `src/Lumineux.Infrastructure/Persistence/Migrations/` (API arrêtée) ; vérifier `Up()` = drop, `Down()` = recréation, rejouable (research.md D5).
- [X] T018 [US3] Vérification **zéro vestige** : `grep -rniE "member_permissions|MemberPermission|IMemberPermissionRepository|PermissionBootstrapper|BureauProfilesBootstrapper|Auth:Bootstrap|BootstrapOptions" src tests` ne renvoie **aucune** occurrence vivante (hors migrations historiques figées) ; le **snapshot** EF régénéré ne contient plus la table (SC-004). **Contrôle de non-régression (F1)** : vérifier qu'**aucun test ni fichier de config résiduel** ne sème des droits via l'ancienne voie — chercher spécifiquement `"Bootstrap"` dans `src/Lumineux.Api/appsettings*.json` et toute écriture `member_permissions` / usage `Auth:Bootstrap` dans `tests/**` ; s'il en reste, **migrer ces amorçages vers des profils** (via `ApiTestFixture.SeedActiveMemberAccountAsync`, qui attribue déjà des profils) avant de conclure.

**Checkpoint**: Une seule source de vérité (profils) ; ni code ni table hérités.

---

## Phase 6: Polish & Validation

- [X] T019 `dotnet build` puis `dotnet test` **verts** (API arrêtée) : Domain + Application + Api + Infrastructure — aucune régression (SC-005).
- [X] T020 Exécuter la validation `specs/029-consolidate-permissions/quickstart.md` : invariant d'autorisation confirmé (mêmes claims / mêmes 403, `contracts/authorization-invariant.md`), aucun `IHostedService` de migration au démarrage, table absente du schéma.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : baseline.
- **Foundational (Phase 2)** : **BLOQUE** US1/US2 (port renommé partagé). T002/T003/T004 avant T005/T006 (retirer les consommateurs des méthodes héritées avant de les supprimer du port).
- **US1 (Phase 3)** & **US2 (Phase 4)** : après la Fondation ; indépendantes entre elles (fichiers de handlers/tests distincts).
- **US3 (Phase 5)** : après la Fondation ; les suppressions d'entité/config/table sont indépendantes des handlers (US1/US2), mais le **build final** (T019) exige que **tout** soit cohérent.
- **Polish (Phase 6)** : après US1 + US2 + US3.

### Parallel Opportunities

- Phase 2 : T002/T003 (fichiers distincts) puis T004 ; T005→T006 séquentiels (port puis impl/DI).
- US1 : T008/T009 [P] (fichiers de tests distincts) ; T007 avant (handlers).
- US2 : T011 [P] après T010.
- US3 : T012/T013/T014/T015/T016 largement [P] (fichiers distincts) ; T017 (migration) après T012–T014 ; T018 en dernier.

---

## Parallel Example: User Story 3

```bash
# Suppressions à fichiers distincts (parallélisables) :
Task: "T012 supprimer MemberPermission.cs"
Task: "T013 supprimer MemberPermissionConfiguration.cs"
Task: "T016 supprimer les 2 tests de bootstrapper/coexistence"
# Puis, séquentiels :
Task: "T014 retirer le DbSet"  ->  "T017 migration RemoveMemberPermissions"  ->  "T018 zéro vestige"
```

---

## Implementation Strategy

### MVP (garantie de non-régression d'abord)

1. Setup → 2. Foundational (retrait consommateurs + port renommé) → 3. US1 (droits inchangés) → **valider** que connexion/activation portent les mêmes droits.

### Incrémental

1. Fondation. 2. US1 (droits inchangés). 3. US2 (setup admin). 4. US3 (suppression + migration + zéro vestige). 5. Build/test verts.

---

## Notes

- **Non-régression** : `GetEffectivePermissions` lit **déjà** uniquement les profils (aucune union avec la
  table héritée) → retirer `member_permissions` ne change pas les droits du jeton.
- **Contrainte d'exécution** : T017 (migration) et T019 (`dotnet test`) nécessitent l'**API dev arrêtée**.
- **Migrations historiques figées** : elles gardent des références au modèle d'alors — ce n'est **pas** un
  vestige vivant (ne pas les modifier).
- Commit après chaque groupe logique ; s'arrêter aux checkpoints pour valider la non-régression.
