# Implementation Plan: Statut d'installation (setup/status)

**Branch**: `012-setup-status-endpoint` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/012-setup-status-endpoint/spec.md`

## Summary

Ajouter un **endpoint anonyme de lecture** `GET /api/v1/setup/status` renvoyant un **indicateur
booléen** d'installation (`{ installed: true|false }`). `installed = true` **si et seulement si** au
moins un **administrateur actif** existe — **exactement** la règle du verrou de l'installation du
premier administrateur (feature 005), réutilisée via le **même décompte**
`IBureauProfileRepository.CountActiveAdministratorsAsync`.

Extension **minimale** de la solution **.NET 10 / Onion**, **sans persistance ni migration** :
- **Nouveau cas d'usage** `GetSetupStatusHandler` (Application/Setup) : `installed = (count > 0)`.
- **Nouveau DTO** `SetupStatusResponse(bool Installed)`.
- **Nouvel endpoint** `GET setup/status` sur `SetupController`, **`[AllowAnonymous]`**.
- **DI** : enregistrement du handler.

Aucune donnée sensible n'est exposée (un seul booléen). Le verrou d'installation (`POST
setup/first-admin`, 409 `already_installed`) **reste inchangé**.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (solution existante)

**Primary Dependencies**: ASP.NET Core Web API ; port existant `IBureauProfileRepository`
(`CountActiveAdministratorsAsync`, feature 004/005). **Pas** de FluentValidation (aucune entrée),
**pas** de nouvelle dépendance.

**Storage**: SQL Server — **lecture seule** (décompte des administrateurs actifs). **Aucune
migration**, aucune table/colonne ajoutée.

**Testing**: xUnit — unitaire Application (`GetSetupStatusHandler` : `installed` faux si 0 admin, vrai
si ≥1) sur double ; intégration API (harnais SQLite : `GET setup/status` **anonyme** → 200 ; bascule
non installé → installé après provisionnement d'un administrateur actif).

**Target Platform**: API .NET consommée par la **SPA** (affichage conditionnel du lien
« Première installation »).

**Project Type**: Web service (API) — extension de la solution existante.

**Performance Goals**: un décompte léger ; réponse immédiate.

**Constraints**: **anonyme** (FR-001) ; réponse = **un seul booléen**, aucune donnée sensible ni
énumération (FR-003, SC-003) ; **même règle** que le verrou (FR-002, SC-002) ; **lecture seule**
(FR-004) ; **ne modifie pas** le verrou (FR-005, SC-004).

**Scale/Scope**: 1 endpoint de lecture ; 1 user story (P1) ; 1 handler + 1 DTO + 1 route. **0**
entité, **0** migration, **0** modification du verrou.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion | Cas d'usage en **Application** (`GetSetupStatusHandler`) ; décompte derrière le **port existant** `IBureauProfileRepository` ; **DTO dédié** ; contrôleur mince. Dépendances vers l'intérieur. | ✅ PASS |
| II. Code-First & intégrité BD | **Aucune** évolution de schéma, **aucune** migration (lecture seule). | ✅ N/A |
| III. Tests en premier | Unitaire Application (0/≥1 admin) + intégration API (anonyme 200, bascule). Écrits avant/avec l'implémentation. | ✅ PASS |
| IV. Sécurité par défaut | `[AllowAnonymous]` **assumé et justifié** (décision avant session) ; réponse = **un booléen**, **aucune** énumération/donnée sensible ; **n'affaiblit pas** le verrou d'installation. | ✅ PASS |
| V. Contrats d'API explicites | **DTO dédié** `SetupStatusResponse` ; REST `GET /api/v1/setup/status` ; 200 ; OpenAPI. | ✅ PASS — `contracts/openapi.yaml` |
| VI. Traçabilité & observabilité | Lecture **sans effet de bord** ; journalisation HTTP existante ; aucune donnée sensible. | ✅ PASS |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (un handler de lecture réutilisant le port
existant, un DTO booléen, un endpoint anonyme, aucun impact sur le verrou) respecte l'ensemble des
principes. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/012-setup-status-endpoint/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/openapi.yaml
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de la solution existante

```text
src/
├── Lumineux.Application/
│   ├── Setup/                # GetSetupStatusHandler (installed = CountActiveAdministrators > 0)
│   ├── Contracts/Setup/      # SetupStatusResponse(bool Installed)  (SetupDtos.cs)
│   └── DependencyInjection.cs   # enregistrement du handler
└── Lumineux.Api/
    └── Controllers/          # SetupController (+ GET status, [AllowAnonymous])

# IBureauProfileRepository (CountActiveAdministratorsAsync) : RÉUTILISÉ, inchangé
# InstallFirstAdminHandler / verrou : INCHANGÉS

tests/
├── Lumineux.Application.Tests/   # GetSetupStatusTests (0 admin → false ; ≥1 → true)
└── Lumineux.Api.Tests/           # SetupStatusEndpointTests (anonyme 200 ; bascule après install)
```

**Structure Decision**: Extension de la solution Onion existante, **lecture seule**. Le handler
`GetSetupStatusHandler` **réutilise** le décompte `CountActiveAdministratorsAsync` du port
`IBureauProfileRepository` (garantissant la cohérence **exacte** avec le verrou d'installation), et
projette vers un **DTO booléen**. Le contrôleur `SetupController` expose une lecture
`[AllowAnonymous]`. Le verrou et l'installation restent **inchangés**.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
