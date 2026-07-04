# Implementation Plan: Authentification et connexion des membres

**Branch**: `003-authentication-login` | **Date**: 2026-07-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-authentication-login/spec.md`

## Summary

Implémenter le parcours de connexion et l'**émission** des jetons d'accès : connexion par référence +
mot de passe → **jeton JWT signé et expirant** portant les droits du membre ; **endpoint dédié de
première connexion** (référence + mot de passe temporaire + nouveau mot de passe) qui active le compte
et lève l'obligation de changement ; changement de mot de passe pour un utilisateur connecté ;
**verrouillage temporaire** anti-force brute (N échecs / durée D) ; messages génériques anti-énumération ;
mots de passe hachés (jamais en clair).

Approche technique : réutilise la solution **.NET 10 / Onion / SQL Server code-first**. **Enrichit
`MemberAccount`** (compteur d'échecs, fin de verrouillage, dernière connexion + méthodes de domaine) ;
ajoute une table minimale **`member_permissions`** (source des droits portés par le jeton — leur
attribution reste hors périmètre) ; ajoute un port **`ITokenIssuer`** (émission JWT) réutilisant les
`JwtOptions` existants et l'`IPasswordHasher` existant. Endpoints REST `/api/v1/auth/*`. Pas de jeton
de rafraîchissement (jeton d'accès expirant, reconnexion à l'expiration).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (solution existante)

**Primary Dependencies**: ASP.NET Core Web API + JWT Bearer (validation, existant), EF Core 10
(SQL Server), FluentValidation, Serilog (existant). **Émission JWT** via `System.IdentityModel.Tokens.Jwt`
(déjà référencé). **Hachage** via `IPasswordHasher` (existant, feature 002).

**Storage**: SQL Server — migration additive : colonnes de sécurité sur `member_accounts` + table
`member_permissions`.

**Testing**: xUnit — unitaires (Domain/Application) sans base ; intégration (Infrastructure/API) sur
SQLite via le harnais existant.

**Target Platform**: API .NET, consommée par la SPA Angular et l'app mobile Flutter (connexion).

**Project Type**: Web service (API) — extension de la solution existante.

**Performance Goals**: Connexion et émission du jeton en < 2 s (SC-001).

**Constraints**: Mots de passe **hachés** (jamais en clair/loggés) ; **messages génériques**
(anti-énumération, SC-002) ; jetons **signés et expirants** (source de temps serveur) ; **verrouillage
temporaire** configurable ; pas d'état serveur pour les jetons (pas de refresh).

**Scale/Scope**: Toute la communauté (chaque membre se connecte) ; 4 user stories ; enrichissement
d'1 entité + 1 table + 1 port.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion | Cas d'usage d'auth dans Application ; émission JWT derrière `ITokenIssuer` (Infra) ; hachage via port existant ; règles de verrouillage dans le Domain (`MemberAccount`) | ✅ PASS |
| II. Code-First & intégrité BD | Migration additive (`member_accounts` + `member_permissions`) ; FK, unicités ; audit hérité | ✅ PASS — `data-model.md` |
| III. Tests en premier (NON-NÉGOCIABLE) | Unitaires Domain (verrouillage, transitions compte) + Application (login/activate/change) ; intégration API | ✅ PASS |
| IV. Sécurité par défaut | Hachage, messages génériques anti-énumération, verrouillage, jetons signés/expirants, secrets hors code, pas de secret en journal | ✅ PASS — `research.md` §sécurité |
| V. Contrats d'API explicites | DTO dédiés (aucun secret exposé) ; REST `/api/v1/auth/*` ; ProblemDetails ; OpenAPI | ✅ PASS — `contracts/openapi.yaml` |
| VI. Traçabilité & observabilité | Journalisation des événements d'auth (succès/échec/verrouillage/changement) sans secret ; temps serveur | ✅ PASS |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (ports, migration, DTO, verrouillage domaine)
respecte les principes. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/003-authentication-login/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/openapi.yaml
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de la solution existante

```text
src/
├── Lumineux.Domain/
│   ├── Entities/            # MemberAccount (enrichi : échecs, verrouillage, dernière connexion,
│   │                        #   méthodes RegisterFailedLogin/RegisterSuccessfulLogin/ChangePassword/Activate)
│   │                        # + MemberPermission (nouvelle)
│   └── Abstractions/        # ITokenIssuer, IMemberPermissionRepository ; MemberAccount repo étendu
├── Lumineux.Application/
│   ├── Auth/                # Login, ActivateAccount (1re connexion), ChangePassword (+ validators)
│   └── Contracts/Auth/      # DTO (LoginRequest/Response, ActivateRequest, ChangePasswordRequest)
├── Lumineux.Infrastructure/
│   ├── Security/            # JwtTokenIssuer (ITokenIssuer) ; AuthOptions ; PasswordPolicy
│   ├── Persistence/         # config MemberPermission + enrichissement MemberAccount ; migration
│   └── Repositories/        # MemberAccountRepository étendu ; MemberPermissionRepository
└── Lumineux.Api/
    └── Controllers/         # AuthController (/login, /activate, /change-password)

tests/
├── Lumineux.Domain.Tests/          # verrouillage, transitions MemberAccount
├── Lumineux.Application.Tests/     # login (générique, verrouillage, must-change), activate, change
└── Lumineux.Api.Tests/             # endpoints /auth/* (200/401/403, verrouillage, activation)
```

**Structure Decision**: Extension de la solution Onion existante. L'émission de jeton (`ITokenIssuer`)
et la source des permissions (`IMemberPermissionRepository`) sont des ports ; les règles de sécurité
du compte (verrouillage, transitions) sont **dans le Domaine** (`MemberAccount`), conformément à la
règle de dépendance (Constitution I).

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
