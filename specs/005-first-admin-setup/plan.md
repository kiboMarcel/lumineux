# Implementation Plan: Installation du premier administrateur

**Branch**: `005-first-admin-setup` | **Date**: 2026-07-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-first-admin-setup/spec.md`

## Summary

Ajouter une **route anonyme unique** `POST /api/v1/setup/first-admin` qui, tant qu'aucun membre
actif ne dispose du droit `manage_bureau_profiles`, crée atomiquement un membre actif + son compte
de connexion activé + le profil « Administrateur » (portant tous les droits fonctionnels connus) +
l'attribution, puis retourne un jeton d'accès JWT immédiat. Dès qu'un admin actif existe, la route
refuse tout appel avec `409 already_installed`, refus prioritaire sur les erreurs de validation
pour ne pas divulguer d'information. Idempotence sur le nom de profil (réutilisation d'un profil
« Administrateur » existant sans redéfinir sa liste de droits).

Approche technique : extension de la solution **.NET 10 / Onion / SQL Server code-first** — **zéro
nouvelle entité de domaine**, zéro migration EF. Le cas d'usage `InstallFirstAdminHandler` compose
les briques existantes : `IMemberRepository.AddAsync` + `IMemberReferenceGenerator` (feature 002),
`MemberAccount.Provision` + `Activate` + `ChangePassword` (features 002/003), `IBureauProfileRepository`
+ `IPermissionCatalog` (feature 004), `ITokenIssuer` (feature 003). Un contrôleur dédié
`SetupController` héberge l'endpoint (`[AllowAnonymous]`). Validation FluentValidation + réutilisation
de `PasswordPolicy.ApplyPolicy` (feature 003).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (solution existante)

**Primary Dependencies**: ASP.NET Core Web API, EF Core 10 (SQL Server), FluentValidation, Serilog,
`IAuditLogger`/`IClock` (existants). Réutilise `IMemberRepository`, `IMemberAccountRepository`,
`IMemberReferenceGenerator`, `IPasswordHasher`, `IBureauProfileRepository`, `IPermissionCatalog`,
`ITokenIssuer` — tous en place.

**Storage**: SQL Server — **aucune migration** (utilise les tables `members`, `member_accounts`,
`bureau_profiles`, `bureau_profile_permissions`, `member_bureau_profiles` déjà présentes).

**Testing**: xUnit — unitaires Application (`InstallFirstAdminHandler`) ; intégration API sur
SQLite via `ApiTestFixture` (features 001/002/003/004).

**Target Platform**: API .NET, appelée par un opérateur technique via curl/Postman ou par un écran
d'installation dédié dans la SPA Angular (hors périmètre backend).

**Project Type**: Web service (API) — extension de la solution existante.

**Performance Goals**: Installation complète (validation + création + émission jeton) < 500 ms p95
(volume : 1 appel unique en pratique).

**Constraints**: Route **anonyme** avec verrou naturel (« 0 admin actif ») ; refus
`already_installed` prioritaire sur toute erreur de validation (FR-005) ; opération **atomique**
(une seule `SaveChangesAsync`) ; jeton signé/expirant réutilisant `JwtOptions` existants ; aucun
secret dans les logs.

**Scale/Scope**: 1 appel réel par déploiement ; 3 user stories ; 0 nouvelle table ; 1 contrôleur,
1 handler, 1 validator, 2 DTO.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion | Cas d'usage dans Application ; composition d'agrégats existants (Member, MemberAccount, BureauProfile) ; contrôleur en API ; réutilise ports existants (aucun nouveau port) | ✅ PASS |
| II. Code-First & intégrité BD | **Aucune migration** ; réutilise les contraintes existantes (unicité `LoginId`, unicité `(member, profile)`, unicité CI `NameNormalized`) | ✅ PASS |
| III. Tests en premier (NON-NÉGOCIABLE) | Unitaires Application (installation valide, verrou 0-admin, refus prioritaire, idempotence profil, atomicité rollback) ; intégration API (matrice 201/400/409) | ✅ PASS |
| IV. Sécurité par défaut | Verrou naturel « aucun admin » ; refus prioritaire (anti-fuite) ; politique de mot de passe héritée (feature 003) ; hash via `IPasswordHasher` ; audit ; jeton signé/expirant | ✅ PASS — `research.md` §sécurité |
| V. Contrats d'API explicites | DTO dédiés (`InstallFirstAdminRequest`, réutilisation `TokenResponse`) ; REST `/api/v1/setup/first-admin` ; ProblemDetails ; OpenAPI | ✅ PASS — `contracts/openapi.yaml` |
| VI. Traçabilité & observabilité | `IAuditLogger.Operation("Setup.FirstAdminCreated", ...)` ; horodatage serveur ; jamais le mot de passe ni le jeton | ✅ PASS |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (handler composé de briques existantes, aucune
migration, aucun nouveau port) préserve les principes. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/005-first-admin-setup/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/openapi.yaml
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de la solution existante

```text
src/
├── Lumineux.Domain/               # AUCUN changement (composition d'entités existantes)
├── Lumineux.Application/
│   ├── Setup/                     # InstallFirstAdminHandler + validator
│   │                              #   compose Member/MemberAccount/BureauProfile via ports existants
│   └── Contracts/Setup/           # DTO InstallFirstAdminRequest ; réutilise TokenResponse (feature 003)
├── Lumineux.Infrastructure/       # AUCUN changement (repos déjà en place ; pas de migration)
└── Lumineux.Api/
    └── Controllers/
        └── SetupController.cs     # POST /api/v1/setup/first-admin (anonyme)

tests/
├── Lumineux.Application.Tests/    # InstallFirstAdminTests (5+ scénarios : succès, verrou 0-admin,
│                                  #   refus prioritaire, idempotence profil, mot de passe faible)
└── Lumineux.Api.Tests/            # SetupEndpointsTests (201/400/409 + jeton utilisable sur endpoint protégé)
```

**Structure Decision**: Extension pure de la solution Onion existante. Le cas d'usage vit dans
`Application/Setup/`, expose un contrat REST dédié dans `SetupController`, et **compose** les
briques déjà en place sans introduire de nouvelle abstraction ni de nouvelle table. La séquence
d'écriture (Member → MemberAccount → BureauProfile → MemberBureauProfile) reste dans une seule
transaction EF (SaveChanges unique).

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
