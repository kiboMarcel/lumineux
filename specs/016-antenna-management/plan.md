# Implementation Plan: API de gestion des antennes (CRUD)

**Branch**: `016-antenna-management` | **Date**: 2026-07-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/016-antenna-management/spec.md`

## Summary

Ajouter à l'**API** (architecture Onion, .NET 10) la **gestion des antennes** : créer, modifier
(libellé + district ; **code immuable**), **désactiver/réactiver** (statut logique, **aucune
suppression physique**), et **lister/consulter pour la gestion** (inactives incluses). Réservé à un
**nouveau droit dédié `manage_referentials`** ajouté au catalogue RBAC. Règles clés : **code unique**
(erreur `duplicate_code` + index unique en base), **district existant** (réutilise
`IReferenceLookupRepository.DistrictExistsAsync`), **refus de désactivation** si l'antenne porte des
**sessions ouvertes** (`antenna_has_open_sessions`). La **lecture publique** des antennes actives
(feature 010, `GET /reference/antennas`) reste **inchangée**.

## Technical Context

**Language/Version**: C# 14 / .NET 10.

**Primary Dependencies**: ASP.NET Core (contrôleurs), EF Core 10 (SQL Server ; SQLite in-memory pour
tests), FluentValidation, Serilog. Réutilise : `ICurrentUser` (droits/identité), `IAuditLogger`
(traçabilité), `IReferenceLookupRepository` (existence district), `IClock`.

**Storage**: SQL Server, code-first. Table `antennas` **existante** ; **migration** requise pour un
**index unique** sur `code` (matérialisation de FR-002, Principe II).

**Testing**: xUnit + NSubstitute + FluentAssertions — Domain (invariants d'entité : create, update,
activate/deactivate), Application (handlers : dup code, district invalide, droit manquant, code
immuable, désactivation refusée si sessions ouvertes, liste gestion incluant inactives), Infrastructure
(index unique `code` ; détection de session ouverte par antenne), Api (endpoints + policy 401/403 +
CRUD nominal).

**Target Platform**: API HTTPS consommée par la SPA (module SPA = feature ultérieure).

**Project Type**: Web service (API). Pas de front dans cette feature.

**Performance Goals**: opérations CRUD ponctuelles, volumétrie faible (dizaines d'antennes) — pas
d'enjeu de charge spécifique.

**Constraints**: droit **`manage_referentials`** (policy + catalogue) ; **code unique** (base + métier) ;
**code immuable** ; **désactivation logique** préservant l'intégrité (FK Restrict membres/sessions) ;
**refus** si sessions ouvertes ; DTO dédiés (pas d'exposition d'entités) ; erreurs ProblemDetails +
`code` ; opérations sensibles journalisées.

**Scale/Scope**: 1 entité (Antenne, enrichie de comportements) ; ~6 endpoints (créer, modifier,
désactiver, réactiver, lister gestion, consulter) ; 1 nouveau droit ; 1 migration (index unique).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Comportements d'entité dans **Domain** (`Antenna`), cas d'usage dans **Application** (handlers/validators/ports), persistance en **Infrastructure**, contrôleur **fin**. Dépendances vers l'intérieur. | ✅ PASS |
| II. Code-First & intégrité BD | **Migration** ajoutant un **index unique** sur `antennas.code` ; désactivation logique via `status` (aucune suppression) ; piste d'audit héritée peuplée par l'intercepteur. | ✅ PASS |
| III. Tests en premier | Tests Domain/Application/Infra/Api écrits avant impl. (rouge → vert) ; règles métier isolées (doubles de test). | ✅ PASS |
| IV. Sécurité par défaut | Nouveau droit **`manage_referentials`** (moindre privilège) vérifié **côté serveur** (policy + handler) ; validation d'entrée serveur ; pas d'exposition d'entités ; secrets/-. | ✅ PASS |
| V. Contrats d'API explicites | **DTO dédiés**, REST cohérent (`/api/v1/antennas`), codes HTTP + format d'erreur homogène (`duplicate_code`, `antenna_has_open_sessions`, 404, 403). | ✅ PASS |
| VI. Traçabilité & observabilité | Création/modification/désactivation/réactivation **journalisées** (auteur+horodatage) ; refus (droit manquant, conflit) consignés ; aucun secret/donnée superflue. | ✅ PASS |

**Résultat initial : PASS** — aucune violation, Complexity Tracking non requis.

*Re-check post-conception (Phase 1)* : la conception (entité enrichie, handlers dédiés, port
`IAntennaRepository`, migration d'index unique, policy + catalogue) respecte les principes. **PASS
confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/016-antenna-management/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   └── rest-api.md          # endpoints antennes (gestion) + codes + droit manage_referentials
└── checklists/requirements.md
```

### Source Code (repository root) — extension de l'API .NET

```text
src/
├── Lumineux.Domain/
│   ├── Entities/Antenna.cs                    # + fabrique Create + méthodes Update/Deactivate/Activate (invariants)
│   └── Abstractions/
│       ├── IAntennaRepository.cs              # NOUVEAU port écriture/gestion (GetById, GetByCode, ListAll, Add, HasOpenSessions, Save)
│       └── IAttendanceSessionRepository.cs    # + HasOpenSessionForAntennaAsync(antennaId)
├── Lumineux.Application/
│   ├── Abstractions/Permissions.cs            # + ManageReferentials = "manage_referentials"
│   ├── Contracts/Antennas/AntennaDtos.cs      # AntennaResponse, CreateAntennaRequest, UpdateAntennaRequest
│   └── Antennas/
│       ├── CreateAntennaHandler.cs · UpdateAntennaHandler.cs
│       ├── SetAntennaActiveHandler.cs         # activer/désactiver (avec règle sessions ouvertes)
│       ├── ListAntennasHandler.cs · GetAntennaHandler.cs
│       ├── AntennaValidators.cs · AntennaMapping.cs
│       └── (DI dans Application/DependencyInjection.cs)
├── Lumineux.Infrastructure/
│   ├── Repositories/AntennaRepository.cs      # NOUVEAU (gestion) ; AttendanceSessionRepository += HasOpenSessionForAntenna
│   ├── Security/PermissionCatalog.cs          # + entrée manage_referentials
│   └── Persistence/Migrations/…               # index unique antennas.code
└── Lumineux.Api/
    ├── Controllers/AntennasController.cs       # NOUVEAU, [Authorize(Policy = manage_referentials)]
    └── Program.cs                              # + policy manage_referentials

tests/
├── Lumineux.Domain.Tests/AntennaTests.cs
├── Lumineux.Application.Tests/{CreateAntenna,UpdateAntenna,SetAntennaActive,ListAntennas}Tests.cs
├── Lumineux.Infrastructure.Tests/AntennaCodeUniquenessTests.cs
└── Lumineux.Api.Tests/AntennaEndpointsTests.cs
```

**Structure Decision**: Extension de l'API existante suivant l'Onion. Le **port d'écriture**
`IAntennaRepository` (gestion) s'ajoute au `IAntennaReadRepository` existant (existence, inchangé). La
**lecture publique** (feature 010) n'est pas touchée. Le nouveau droit `manage_referentials` est
déclaré (constante), catalogué (RBAC) et appliqué par **policy**. Une **migration** matérialise
l'unicité du code.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
