# Implementation Plan: Profil de l'utilisateur courant (auth/me)

**Branch**: `007-auth-me` | **Date**: 2026-07-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/007-auth-me/spec.md`

## Summary

Ajouter un **unique endpoint de lecture** `GET /api/v1/auth/me` qui restitue, à l'utilisateur
**authentifié**, son **identité de session** et ses **droits effectifs**, afin que la console web
(SPA) pilote sa navigation et son affichage (RBAC côté UI) sans décoder elle-même le jeton.

Approche technique — extension **minimale** de la solution **.NET 10 / Onion** existante, **sans
persistance ni migration** :

- **Tout est dérivé de la session (jeton)**, jamais recalculé en base : l'identité et les droits
  retournés sont **exactement** ceux portés par le jeton courant → cohérence stricte entre ce que la
  SPA affiche et ce que l'API autorise (FR-006).
- **Extension du port existant `ICurrentUser`** (Application) avec la **liste des permissions** de la
  session. `ICurrentUser` expose déjà `MemberId`, `UserName` (= nom complet, claim `ClaimTypes.Name`)
  et `HasPermission(x)` ; il lui manque seulement l'**énumération** des droits. L'implémentation API
  `CurrentUser` énumère les claims `permission`.
- **Nouveau cas d'usage** `GetCurrentUserHandler` (Application/Auth) : mappe `ICurrentUser` vers un
  **DTO dédié** `CurrentUserResponse(MemberId, DisplayName, Permissions)`. Garde défensive : contexte
  authentifié mais sans `member_id` exploitable → refus 401 générique journalisé.
- **Nouvel endpoint** `GET /api/v1/auth/me` sur `AuthController`, `[Authorize]` (tout membre
  connecté). Le refus d'authentification (jeton absent/invalide/expiré) est produit **uniformément**
  par le middleware d'authentification (401), comme pour tous les endpoints protégés existants.

Aucune donnée secrète n'est exposée (ni empreinte, ni jeton). Aucun accès base de données n'est
nécessaire (lecture pure des claims). Aucune modification des endpoints existants.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (solution existante)

**Primary Dependencies**: ASP.NET Core Web API, authentification JWT existante (feature 003),
port `ICurrentUser` (existant). **Pas** d'EF Core, **pas** de FluentValidation (requête `GET` sans
corps), **pas** de nouvelle dépendance.

**Storage**: **N/A** — aucune entité persistée, **aucune migration**. Lecture pure dérivée du jeton.

**Testing**: xUnit — unitaires Application (`GetCurrentUserHandler` : mapping identité + droits,
garde 401 sans contexte) sans base ; intégration API sur le harnais existant (200 avec jeton,
401 sans jeton, correspondance exacte des droits, absence de secret).

**Target Platform**: API .NET consommée par la **SPA Angular** (bootstrap + après connexion), et à
terme l'app mobile Flutter.

**Project Type**: Web service (API) — extension de la solution existante.

**Performance Goals**: réponse en **un seul appel**, sans I/O base (SC-001) ; coût négligeable
(lecture de claims en mémoire).

**Constraints**: droits retournés **strictement égaux** à ceux de la session (SC-002, FR-006) ;
**aucun secret** dans la réponse (SC-004, FR-007) ; refus **uniforme** 401 pour jeton
absent/invalide/expiré (SC-003, FR-003) ; le client n'a **pas** à décoder le jeton (SC-005).

**Scale/Scope**: tout membre authentifié ; 2 user stories (P1 lecture, P2 refus) ;
1 extension de port + 1 DTO + 1 cas d'usage + 1 endpoint. **0** entité, **0** migration.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion | Cas d'usage dans **Application** (`GetCurrentUserHandler`) ; accès au contexte via l'abstraction **`ICurrentUser`** (Application), implémentée en **API** (`CurrentUser`, lié à HTTP) ; contrôleur mince. Dépendances vers l'intérieur. | ✅ PASS |
| II. Code-First & intégrité BD | **Aucune** évolution de schéma, **aucune** migration, aucune table touchée. Principe sans objet — non violé. | ✅ N/A |
| III. Tests en premier (NON-NÉGOCIABLE) | Unitaires Application (mapping identité+droits, garde 401) écrits avant l'implémentation ; intégration API (200/401, égalité des droits, absence de secret). | ✅ PASS |
| IV. Sécurité par défaut | `[Authorize]` impose l'authentification **côté serveur** ; **aucun droit de gestion** requis (moindre privilège : lecture de ses **propres** infos) ; **aucun secret** exposé ; droits = ceux de la session (cohérence avec l'autorisation) ; refus journalisé. | ✅ PASS |
| V. Contrats d'API explicites | **DTO dédié** `CurrentUserResponse` (aucune entité exposée) ; REST `GET /api/v1/auth/me` ; 200/401 ; ProblemDetails pour le 401 ; OpenAPI. | ✅ PASS — `contracts/openapi.yaml` |
| VI. Traçabilité & observabilité | Refus défensif (contexte sans membre) journalisé via `IAuditLogger` **sans** secret ; lecture **sans effet de bord** (FR-008). Le 401 « pas de jeton » est produit uniformément par le middleware, comme les autres endpoints protégés. | ✅ PASS |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (extension additive d'`ICurrentUser`, un cas
d'usage mince, un DTO dédié, aucun accès base, aucun secret) respecte l'ensemble des principes.
**PASS confirmé** — aucune dérogation à justifier.

## Project Structure

### Documentation (this feature)

```text
specs/007-auth-me/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/openapi.yaml
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de la solution existante

```text
src/
├── Lumineux.Application/
│   ├── Abstractions/        # ICurrentUser (ÉTENDU : liste des permissions de la session)
│   ├── Auth/                # GetCurrentUserHandler (nouveau : mappe ICurrentUser → DTO)
│   └── Contracts/Auth/      # CurrentUserResponse (nouveau DTO)
├── Lumineux.Api/
│   ├── Security/            # CurrentUser (implémente la nouvelle propriété : énumère les claims permission)
│   └── Controllers/         # AuthController (+ GET /me, [Authorize])
└── (Domain, Infrastructure : INCHANGÉS — aucune entité, aucun repo, aucune migration)

tests/
├── Lumineux.Application.Tests/   # GetCurrentUserTests (identité+droits mappés, droits vides, garde 401)
└── Lumineux.Api.Tests/           # AuthMeEndpointsTests (200 avec jeton, 401 sans, égalité des droits, no-secret)
```

**Structure Decision**: Extension de la solution Onion existante, **côté périphérie uniquement**.
Le besoin (« qui suis-je + mes droits ») est entièrement satisfait par la **session** : on étend
l'abstraction déjà dédiée à cet usage (`ICurrentUser`) plutôt que d'introduire un nouveau port ou de
lire les claims dans le contrôleur (ce qui violerait la règle de dépendance, Constitution I). Le cas
d'usage reste mince mais **présent** (testabilité, Constitution III) et le contrôleur se contente de
déléguer. Aucune couche Domain/Infrastructure/persistance n'est sollicitée.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
