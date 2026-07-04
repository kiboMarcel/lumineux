# Implementation Plan: Profils du bureau

**Branch**: `004-bureau-profiles` | **Date**: 2026-07-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-bureau-profiles/spec.md`

## Summary

Doter le bureau d'un **modèle de profils** (groupes nommés de droits fonctionnels) pour attribuer,
révoquer et faire évoluer les responsabilités des membres. Le token d'accès émis à la connexion
(feature 003) portera comme claims l'**union** des droits des profils attribués au membre. Un
nouveau droit `manage_bureau_profiles` régit les opérations d'administration ; un **garde-fou** empêche
tout état sans administrateur (triple protection : révocation, retrait de droit, suppression). La
**migration au déploiement** crée un profil système « Amorçage » qui reçoit les droits déjà accordés
directement (table `member_permissions` issue de la feature 003 F1), l'assigne au membre bootstrap,
puis fait des profils la **source unique de vérité** (le repli `Auth:Bootstrap:*` reste disponible
comme filet d'urgence idempotent au démarrage).

Approche technique : extension de la solution **.NET 10 / Onion / SQL Server code-first**. Ajout
d'entités **`BureauProfile`** et **`MemberBureauProfile`** ; nouveau port
**`IPermissionCatalog`** (référentiel figé des droits fonctionnels) ; **repository** dédié aux profils ;
**cas d'usage** dans `Application/BureauProfiles/*` ; **contrôleur REST** `/api/v1/bureau-profiles/*` ;
**refactoring de l'implémentation de `IMemberPermissionRepository`** (contrat inchangé, source
basculée vers la jointure profils ↔ droits — `LoginHandler`/`ActivateAccountHandler` restent
inchangés). La table `member_permissions` devient obsolète en écriture (préservée en lecture pour la
migration au déploiement). Une **migration EF** introduit les nouvelles tables ; un service de
migration au démarrage crée le profil « Amorçage » de manière idempotente.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (solution existante)

**Primary Dependencies**: ASP.NET Core Web API, EF Core 10 (SQL Server), FluentValidation, Serilog,
`IAuditLogger`/`ICurrentUser`/`IClock` (existants). Réutilise `Permissions` (constantes) et
`JwtTokenIssuer` (feature 003).

**Storage**: SQL Server — migration additive : nouvelles tables `bureau_profiles`,
`bureau_profile_permissions` (association profil ↔ droit) et `member_bureau_profiles`
(association membre ↔ profil). Table `member_permissions` **conservée** en lecture (backward compat
+ fallback bootstrap au démarrage).

**Testing**: xUnit — unitaires Domain/Application sans base ; intégration API sur SQLite via
`ApiTestFixture` (features 001/002/003).

**Target Platform**: API .NET, consommée par la SPA Angular (interface d'administration) et l'app
mobile Flutter (module bureau réduit).

**Project Type**: Web service (API) — extension de la solution existante.

**Performance Goals**: Émission de jeton (avec agrégation des droits via profils) < 200 ms p95 ;
opérations d'administration < 500 ms p95 (volume faible : dizaines de membres, poignée de profils).

**Constraints**: Unicité de nom de profil insensible à la casse ; référentiel figé des droits ;
garde-fou anti-verrouillage (SC-004) ; audit systématique ; jetons non rafraîchis (les changements
prennent effet à la prochaine authentification, cohérent avec feature 003 FR-006).

**Scale/Scope**: Toute la communauté (chaque membre du bureau peut se voir attribuer des profils) ;
4 user stories ; 3 nouvelles tables + 1 migration au démarrage (profil « Amorçage »).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion | Entités `BureauProfile` + `MemberBureauProfile` avec invariants dans le Domain ; cas d'usage dans Application ; catalogue de droits et repositories comme ports ; contrôleur REST dans API | ✅ PASS |
| II. Code-First & intégrité BD | Migration EF additive (3 tables) ; contraintes de FK, unicités (nom profil ci ; couple `(member, profile)` ; couple `(profile, permission)`) ; piste d'audit héritée | ✅ PASS — `data-model.md` |
| III. Tests en premier (NON-NÉGOCIABLE) | Unitaires Domain (invariants de profil, garde-fou dernier admin), Application (create/update/delete, assign/revoke, effective permissions, migration idempotente), intégration API (`bureau-profiles/*`) | ✅ PASS |
| IV. Sécurité par défaut | `manage_bureau_profiles` requis pour toute écriture ; lecture partagée avec `manage_members` (FR-009) ; garde-fou triple anti-verrouillage (FR-012) ; audit systématique (FR-010) ; référentiel figé (FR-008/015) | ✅ PASS — `research.md` §sécurité |
| V. Contrats d'API explicites | DTO dédiés (aucune entité EF exposée) ; REST `/api/v1/bureau-profiles/*`, `/members/{id}/bureau-profiles` ; ProblemDetails ; OpenAPI | ✅ PASS — `contracts/openapi.yaml` |
| VI. Traçabilité & observabilité | Journalisation via `IAuditLogger` de toutes les mutations (profil, attribution, révocation, migration au déploiement) sans PII inutile ; auteur via `ICurrentUser` | ✅ PASS |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (entités, contrats, migration, garde-fous)
respecte les principes ; les tables héritées (`member_permissions`) restent en lecture pour la seule
étape de migration, sans introduire d'ambiguïté d'écriture. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/004-bureau-profiles/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/openapi.yaml
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de la solution existante

```text
src/
├── Lumineux.Domain/
│   ├── Entities/            # BureauProfile (nom, description, droits, invariants — garde-fou de composition)
│   │                        # + MemberBureauProfile (attribution, unicité (membre, profil))
│   └── Abstractions/        # IBureauProfileRepository (CRUD + requêtes garde-fou)
├── Lumineux.Application/
│   ├── Abstractions/        # IPermissionCatalog (référentiel figé) + constante `Permissions.ManageBureauProfiles`
│   ├── BureauProfiles/      # cas d'usage : Create/Update/Delete/List/Get + AssignProfile/RevokeProfile
│   │                        # + validators FluentValidation
│   └── Contracts/BureauProfiles/  # DTO (requête/réponse) — aucun secret exposé
├── Lumineux.Infrastructure/
│   ├── Persistence/         # config EF `BureauProfile`, `BureauProfilePermission`, `MemberBureauProfile` ;
│   │                        # migration `BureauProfiles`
│   ├── Repositories/        # BureauProfileRepository (implémente IBureauProfileRepository)
│   │                        # + MemberPermissionRepository refactoré : lit désormais l'union des droits via profils
│   └── Security/            # PermissionCatalog (constantes) ; BureauProfilesBootstrapper
│                            #   (migration idempotente au démarrage : crée le profil « Amorçage »)
└── Lumineux.Api/
    └── Controllers/         # BureauProfilesController (/api/v1/bureau-profiles/*)
                             # + endpoints d'attribution sous /api/v1/members/{id}/bureau-profiles

tests/
├── Lumineux.Domain.Tests/          # BureauProfile invariants (nom, droits, garde-fou)
├── Lumineux.Application.Tests/     # Create/Update/Delete/Assign/Revoke + garde-fou dernier admin
│                                    # + tests de l'union effective des droits (IMemberPermissionRepository refactoré)
└── Lumineux.Api.Tests/             # endpoints CRUD, attribution/révocation, autorisation (403), migration au démarrage
```

**Structure Decision**: Extension de la solution Onion existante. Le catalogue des droits
(`IPermissionCatalog`) et l'accès aux profils (`IBureauProfileRepository`) sont des **ports** ;
les règles d'invariant (nom non vide, droits reconnus, cohérence de composition) résident **dans le
Domain** (`BureauProfile`) et les contraintes multi-entités (« il doit rester au moins un admin »)
dans les **cas d'usage** au moment de la révocation/modification/suppression. L'implémentation de
`IMemberPermissionRepository` (contrat introduit par la feature 003) est **refactorée** pour
retourner l'union des droits issus des profils du membre, laissant `LoginHandler`/`ActivateAccountHandler`
inchangés.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
