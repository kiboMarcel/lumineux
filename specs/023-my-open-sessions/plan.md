# Implementation Plan: API — Récupérer mes sessions de présence ouvertes

**Branch**: `023-my-open-sessions` | **Date**: 2026-07-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/023-my-open-sessions/spec.md`

## Summary

Ajouter à l'**API** (Onion, .NET 10) un endpoint **lecture seule** renvoyant les **sessions de présence
encore ouvertes** dont l'**initiateur** est l'**utilisateur courant** (membre du jeton), afin de
permettre la **reprise** côté console (feature 014, SPA à venir). Réservé au droit **`manage_attendance`**.
Réutilise le **DTO `SessionResponse`** existant et la persistance de session. **Aucune écriture, aucune
migration** ; la règle de conflit au démarrage et la clôture ne changent pas.

Décisions structurantes (spec) :
- **Filtrage** : `Status = Open` **et** `OpenedByMemberId = utilisateur courant`.
- **Liste** (0, 1 ou plusieurs) ; **clôturées et sessions d'autres membres exclues**.
- **Identité par le jeton** (`ICurrentUser`), jamais par paramètre client.
- Cohérent avec `GetSession` : `AttendanceCount` renvoyé à **0** (le SPA récupère le décompte à part) —
  aucun calcul de compte ajouté.

## Technical Context

**Language/Version**: C# 14 / .NET 10.

**Primary Dependencies**: ASP.NET Core (contrôleur `AttendanceSessionsController` existant, étendu), EF
Core 10 (requête simple ; SQLite in-memory en test). Réutilise : `ICurrentUser` (identité + droit),
`IAttendanceSessionRepository` (étendu), `SessionMapping.ToResponse`, DTO `SessionResponse`.

**Storage**: SQL Server, code-first. **Lecture seule** sur `attendance_sessions`. **Aucune migration.**

**Testing**: xUnit + NSubstitute + FluentAssertions — Application (handler : ne renvoie que les sessions
ouvertes de l'utilisateur, exclut clôturées et autres membres, liste vide, droit manquant),
Infrastructure (requête réelle SQLite : filtre statut ouvert + initiateur), Api (endpoint + policy
401/403 + résultat limité à l'utilisateur).

**Target Platform**: API HTTPS consommée par la console (feature 024 SPA à venir).

**Project Type**: Web service (API). Pas de front dans cette feature.

**Performance Goals**: requête ponctuelle (une par entrée sur l'écran de démarrage) ; volumétrie
minime (0/1 session par utilisateur).

**Constraints**: droit **`manage_attendance`** ; **lecture seule** ; résultat **strictement limité** à
l'utilisateur courant ; DTO existant réutilisé ; erreurs ProblemDetails.

**Scale/Scope**: 1 endpoint de lecture ; 1 méthode de dépôt ; 1 handler ; 0 migration.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Cas d'usage (handler) en **Application** ; requête derrière le **port** (Infrastructure) ; contrôleur **fin**. | ✅ PASS |
| II. Code-First & intégrité BD | **Lecture seule**, **aucune migration** ni modification de schéma. | ✅ N/A |
| III. Tests en premier | Tests Application/Infrastructure/Api écrits avant impl. (rouge → vert). | ✅ PASS |
| IV. Sécurité par défaut | Droit **`manage_attendance`** ; identité **par le jeton** ; résultat **limité à l'utilisateur** (pas de fuite des sessions d'autrui). | ✅ PASS |
| V. Contrats d'API explicites | **DTO existant** `SessionResponse` ; REST cohérent (`/api/v1/attendance-sessions/mine/open`) ; codes HTTP + erreurs homogènes. | ✅ PASS |
| VI. Traçabilité & observabilité | Accès journalisé (request logging) ; aucune donnée sensible superflue. | ✅ PASS (esprit) |

**Résultat initial : PASS** — aucune violation, Complexity Tracking non requis.

*Re-check post-conception (Phase 1)* : la conception (extension du port, handler filtré par jeton, DTO
existant) respecte les principes. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/023-my-open-sessions/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   └── rest-api.md          # endpoint « mes sessions ouvertes » + droit + codes
└── checklists/requirements.md
```

### Source Code (repository root) — extension de l'API .NET

```text
src/
├── Lumineux.Domain/
│   └── Abstractions/IAttendanceSessionRepository.cs   # + ListOpenByOpenerAsync(int openedByMemberId)
├── Lumineux.Application/
│   └── AttendanceSessions/
│       ├── ListMyOpenSessionsHandler.cs               # US1 : droit + identité jeton + filtre + mapping
│       └── (DI dans Application/DependencyInjection.cs)
├── Lumineux.Infrastructure/
│   └── Repositories/AttendanceSessionRepository.cs    # + ListOpenByOpenerAsync (EF, lecture seule)
└── Lumineux.Api/
    └── Controllers/AttendanceSessionsController.cs    # + GET mine/open

tests/
├── Lumineux.Application.Tests/ListMyOpenSessionsTests.cs
├── Lumineux.Infrastructure.Tests/… (cas ListOpenByOpener)
└── Lumineux.Api.Tests/… (cas mine/open sur AttendanceSessions)
```

**Structure Decision**: Extension de l'API de présence (001) suivant l'Onion. Le **port**
`IAttendanceSessionRepository` reçoit `ListOpenByOpenerAsync(openedByMemberId)` (sessions `Open` de cet
initiateur). Le **handler** `ListMyOpenSessionsHandler` vérifie le droit `manage_attendance`, lit
l'**identité via `ICurrentUser`** (jamais un paramètre client), appelle le dépôt et **projette** via
`SessionMapping.ToResponse` (comme `GetSession`, `AttendanceCount = 0`). L'endpoint **`GET
/api/v1/attendance-sessions/mine/open`** s'ajoute au contrôleur existant. Aucune migration.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
