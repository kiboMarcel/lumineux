# Implementation Plan: Endpoints de données de référence (nomenclatures)

**Branch**: `010-reference-data-endpoints` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/010-reference-data-endpoints/spec.md`

## Summary

Ajouter des **endpoints de lecture** exposant les nomenclatures nécessaires au formulaire membre du
SPA (feature 009) : **antennes** (US1, débloquant l'antenne d'origine obligatoire), puis
**civilités, villes, districts, pays/nationalités** (US2). Extension **minimale** de la solution
**.NET 10 / Onion**, **sans migration** (les entités existent déjà comme cibles de clé étrangère).

- **Nouveau port** `IReferenceDataRepository` (Domain) : lecture des entrées **actives**, **triées**
  par libellé, pour chaque nomenclature.
- **Implémentation Infrastructure** lisant `AppDbContext` en **lecture seule** (`AsNoTracking`,
  `Where(Status actif)`, `OrderBy(Label)`).
- **Cas d'usage** `GetReferenceDataHandler` (Application) : projette les entités vers des **DTO dédiés**.
- **Endpoints** `GET /api/v1/reference/{antennas|civilities|cities|districts|countries}` sur un
  nouveau `ReferenceController`, **`[Authorize]`** (tout utilisateur authentifié ; aucun droit de
  gestion requis — nomenclatures non sensibles).

`GET /api/v1/reference/antennas` couvre le MVP (US1). L'API n'ajoute **aucune** table ni migration ;
le CRUD des nomenclatures reste hors périmètre.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (solution existante)

**Primary Dependencies**: ASP.NET Core Web API, EF Core 10 (lecture `AppDbContext`), authentification
JWT existante. **Pas** de FluentValidation (aucune entrée), **pas** de nouvelle dépendance.

**Storage**: SQL Server — **lecture seule** des tables existantes (`antennas`, `civilities`, `cities`,
`districts`, `countries`). **Aucune migration**, aucune colonne ajoutée.

**Testing**: xUnit — unitaires Application (`GetReferenceDataHandler` : projection entité→DTO, y
compris nationalité distincte pour les pays) sur doubles ; intégration API (harnais SQLite existant :
200 listes **actives** et **triées**, 401 sans authentification).

**Target Platform**: API .NET consommée par la **SPA** (feature 009), et à terme le mobile.

**Project Type**: Web service (API) — extension de la solution existante.

**Performance Goals**: listes de sélection de taille modérée ; lecture `AsNoTracking` sans suivi.

**Constraints**: entrées **actives** uniquement (FR-004) ; accès **authentifié** (FR-006) ; tri
**stable** par libellé (FR-005) ; **lecture seule** sans effet de bord (FR-007) ; aucun secret exposé.

**Scale/Scope**: 5 endpoints de lecture ; 2 user stories (P1 antennes, P2 autres) ; 1 port + 1 repo
+ 1 handler + DTOs + 1 contrôleur. **0** entité nouvelle, **0** migration.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion | Cas d'usage en **Application** (`GetReferenceDataHandler`) ; accès données derrière un **port** `IReferenceDataRepository` (Domain) implémenté en **Infrastructure** ; **DTO dédiés** ; contrôleur mince. Dépendances vers l'intérieur. | ✅ PASS |
| II. Code-First & intégrité BD | **Aucune** évolution de schéma, **aucune** migration (nomenclatures déjà en base). Lecture seule. | ✅ N/A |
| III. Tests en premier | Unitaires Application (projection, nationalité distincte) + intégration API (200 actif/trié, 401). Écrits avant/avec l'implémentation. | ✅ PASS |
| IV. Sécurité par défaut | `[Authorize]` impose l'authentification **côté serveur** ; **aucun secret** exposé ; **entrées actives** uniquement (pas de fuite de données désactivées destinées à d'autres usages) ; lecture seule. | ✅ PASS |
| V. Contrats d'API explicites | **DTO dédiés** (aucune entité exposée) ; REST `GET /api/v1/reference/*` ; ProblemDetails pour 401 ; OpenAPI. | ✅ PASS — `contracts/openapi.yaml` |
| VI. Traçabilité & observabilité | Lecture **sans effet de bord** ; journalisation HTTP existante ; aucune donnée sensible. | ✅ PASS |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (un port de lecture, un repo Infra, un handler de
projection, DTO dédiés, aucun accès en écriture, aucune migration) respecte l'ensemble des principes.
**PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/010-reference-data-endpoints/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/openapi.yaml
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de la solution existante

```text
src/
├── Lumineux.Domain/
│   └── Abstractions/        # IReferenceDataRepository (nouveau : lecture des nomenclatures actives)
│                            # (entités Antenna/Civility/City/District/Country : INCHANGÉES)
├── Lumineux.Application/
│   ├── Reference/           # GetReferenceDataHandler (projette entités → DTO)
│   └── Contracts/Reference/ # ReferenceItemResponse, CountryResponse (DTO)
├── Lumineux.Infrastructure/
│   ├── Repositories/        # ReferenceDataRepository (AsNoTracking, Status actif, OrderBy Label)
│   └── DependencyInjection.cs   # enregistrement du repo
└── Lumineux.Api/
    └── Controllers/         # ReferenceController ([Authorize], 5 GET)

tests/
├── Lumineux.Application.Tests/   # GetReferenceDataTests (projection, nationalité distincte)
└── Lumineux.Api.Tests/           # ReferenceEndpointsTests (200 actif/trié, 401 sans jeton)
```

**Structure Decision**: Extension de la solution Onion existante, **lecture seule**. Le port
`IReferenceDataRepository` (Domain) découple la couche Application de l'accès EF ; l'implémentation
Infrastructure lit `AppDbContext` sans suivi et **filtre les entrées actives** + **trie par libellé**.
Le handler **projette** les entités vers des **DTO dédiés** (aucune entité exposée). Le contrôleur,
mince, expose cinq lectures `[Authorize]`. Aucune couche d'écriture, aucune migration.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
