# Implementation Plan: Recherche membre allégée (member lookup)

**Branch**: `015-member-lookup-endpoint` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/015-member-lookup-endpoint/spec.md`

## Summary

Ajouter un endpoint de **recherche membre allégée** `GET /api/v1/members/lookup?query=…` renvoyant une
**liste courte** d'entrées **minimales** (identifiant, référence, nom complet, statut), accessible aux
utilisateurs disposant du droit de **gestion des présences** **OU** de **gestion des membres**. Sert de
prérequis à l'ajout manuel de présence (feature 014) pour un opérateur qui n'a **pas** forcément le
droit de gestion des membres.

Extension **minimale** de la solution **.NET 10 / Onion**, **sans migration** :
- **Réutilise** `IMemberRepository.SearchAsync(query, page, pageSize)` (feature 002) avec **page 1** et
  une **taille plafonnée** ; **aucune** nouvelle méthode de persistance.
- **Nouveau cas d'usage** `LookupMembersHandler` (Application/Members) : contrôle d'accès **any-of**
  (`manage_attendance` OU `manage_members`, via `ICurrentUser` — idiome `ReadAccess` de la feature
  004), **exige un terme** (sinon 400), projette vers un **DTO minimal**.
- **Nouveau DTO** `MemberLookupResponse(Id, Reference, FullName, Status)`.
- **Nouveau contrôleur mince** `MemberLookupController` (`[Authorize]`, route `api/v1/members/lookup`),
  distinct de `MembersController` (dont la politique de classe `manage_members` exclurait les opérateurs
  de présence).

Aucune donnée sensible exposée (champs minimaux). La recherche complète (`GET /api/v1/members`,
`manage_members`) reste **inchangée**.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (solution existante)

**Primary Dependencies**: ASP.NET Core Web API ; ports existants `IMemberRepository` (`SearchAsync`) et
`ICurrentUser` (`HasPermission`). **Pas** de FluentValidation nécessaire (validation triviale du terme
dans le handler) ; **pas** de nouvelle dépendance.

**Storage**: SQL Server — **lecture seule** (recherche existante réutilisée). **Aucune migration**.

**Testing**: xUnit — unitaire Application (`LookupMembersHandler` : refus sans droit any-of ; refus si
terme vide ; projection minimale ; plafonnement de la taille) sur doubles ; intégration API (`GET
members/lookup` : 200 avec jeton `manage_attendance` renvoyant des champs **minimaux** ; **403** sans
l'un des droits ; **400** si terme absent).

**Target Platform**: API .NET consommée par la **SPA** (sélecteur d'ajout manuel de présence, feature
014).

**Project Type**: Web service (API) — extension de la solution existante.

**Performance Goals**: résultats **plafonnés** (liste courte) ; recherche existante réutilisée.

**Constraints**: **terme requis** (FR-002/SC-003) ; **champs minimaux** (FR-003/SC-002) ; **accès
any-of** `manage_attendance` OU `manage_members` (FR-005/SC-004) ; **liste plafonnée** (FR-004/SC-005) ;
lecture seule (FR-006) ; **n'affaiblit pas** la recherche complète (FR-007).

**Scale/Scope**: 1 endpoint de lecture ; 1 user story (P1) ; 1 handler + 1 DTO + 1 contrôleur. **0**
entité, **0** migration, **0** nouvelle méthode de repo.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion | Cas d'usage en **Application** (`LookupMembersHandler`) ; accès données via le **port existant** `IMemberRepository` ; **DTO dédié** ; contrôleur mince. Dépendances vers l'intérieur. | ✅ PASS |
| II. Code-First & intégrité BD | **Aucune** évolution de schéma, **aucune** migration (recherche existante réutilisée). | ✅ N/A |
| III. Tests en premier | Unitaire Application (any-of, terme requis, projection, plafond) + intégration API (200/403/400, champs minimaux). Écrits avant/avec l'implémentation. | ✅ PASS |
| IV. Sécurité par défaut | Accès **authentifié** + **any-of** (`manage_attendance`/`manage_members`) vérifié côté serveur ; **champs minimaux** (limite l'exposition de données personnelles) ; **terme requis** + **plafond** (anti-aspiration) ; lecture seule. | ✅ PASS |
| V. Contrats d'API explicites | **DTO dédié** `MemberLookupResponse` (aucune entité exposée, aucune coordonnée) ; REST `GET /api/v1/members/lookup` ; ProblemDetails 400/403 ; OpenAPI. | ✅ PASS — `contracts/openapi.yaml` |
| VI. Traçabilité & observabilité | Lecture **sans effet de bord** ; journalisation HTTP existante ; refus (droit manquant) journalisable via `IAuditLogger` sans secret. | ✅ PASS |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (handler de lecture réutilisant `SearchAsync`,
DTO minimal, contrôleur dédié à l'accès any-of, aucun schéma) respecte l'ensemble des principes.
**PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/015-member-lookup-endpoint/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/openapi.yaml
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de la solution existante

```text
src/
├── Lumineux.Application/
│   ├── Members/                 # LookupMembersHandler (any-of + terme requis + projection minimale)
│   ├── Contracts/Members/       # MemberLookupResponse(Id, Reference, FullName, Status)  (MemberQueryDtos.cs)
│   └── DependencyInjection.cs   # enregistrement du handler
└── Lumineux.Api/
    └── Controllers/             # MemberLookupController ([Authorize], GET api/v1/members/lookup)

# IMemberRepository.SearchAsync / MembersController / recherche complète : RÉUTILISÉS/INCHANGÉS

tests/
├── Lumineux.Application.Tests/   # LookupMembersTests (refus sans droit ; terme requis ; projection ; plafond)
└── Lumineux.Api.Tests/           # MemberLookupEndpointTests (200 manage_attendance ; 403 ; 400 terme absent ; champs minimaux)
```

**Structure Decision**: Extension de la solution Onion existante, **lecture seule**. Le handler
`LookupMembersHandler` **réutilise** `SearchAsync` (page 1, taille plafonnée) et **projette** vers un
**DTO minimal** (aucune coordonnée). Le contrôle d'accès **any-of** est porté par le handler (idiome
`ReadAccess`, feature 004) sur un **contrôleur dédié** (`[Authorize]`), distinct de `MembersController`
dont la politique de classe `manage_members` exclurait les opérateurs de présence. La recherche
complète reste inchangée.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
