# Implementation Plan: Gestion de la présence aux réunions

**Branch**: `001-attendance-management` | **Date**: 2026-07-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-attendance-management/spec.md`

## Summary

Première fonctionnalité de la plateforme Lumineux : permettre au bureau de démarrer une session de
présence rattachée à une antenne, aux membres d'enregistrer leur arrivée en scannant un **code QR à
jeton rotatif**, au bureau d'ajouter manuellement les membres non équipés, puis de clôturer la
session en propageant l'heure de fin à toutes les présences. Le scan hors ligne est mis en file
côté mobile puis synchronisé avec conservation de l'heure réelle d'arrivée et anti-doublon.

Approche technique : API **.NET** en architecture **Onion** (Domain / Application / Infrastructure /
API) sur **SQL Server** en **code-first (EF Core)**, avec tests unitaires sur le cœur métier. Le
jeton QR est dérivé d'un secret de session façon TOTP (fenêtre courte) ; l'anti-doublon repose sur
une contrainte d'unicité `(session, membre)` et une synchronisation idempotente. Cette fonctionnalité
pose aussi le socle transverse (authentification JWT, autorisation par droit, journalisation).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (LTS)

**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 10 (provider SQL Server),
FluentValidation, Serilog (journalisation structurée), JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer),
Swashbuckle (OpenAPI). Tests : xUnit, FluentAssertions, Moq/NSubstitute, Testcontainers (SQL Server)
ou LocalDB pour l'intégration.

**Storage**: SQL Server (code-first, migrations EF Core versionnées)

**Testing**: xUnit — tests unitaires (Domain, Application) sans dépendance base ; tests
d'intégration (Infrastructure, API) sur base réelle éphémère.

**Target Platform**: Serveur Windows/Linux exécutant l'API .NET ; clients consommateurs = SPA
Angular (bureau) et application mobile Flutter (scan + ajout). Cette itération livre l'**API**.

**Project Type**: Web service (API) — premier maillon d'une architecture web + mobile.

**Performance Goals**: Absorber une affluence d'arrivée d'au moins 200 scans en < 2 min (SC-006) ;
confirmation de scan perçue en < 5 s (SC-002) ; démarrage de session + QR en < 30 s (SC-001).

**Constraints**: Heures métier (arrivée, fin) fondées sur une **source de temps serveur** (jamais
l'horloge client) ; jeton QR rotatif à fenêtre courte ; scan hors ligne mis en file puis synchronisé
avec anti-doublon idempotent ; secrets hors code source ; données personnelles protégées.

**Scale/Scope**: Communauté à l'échelle de plusieurs antennes ; réunions jusqu'à quelques centaines
de participants ; 4 user stories, ~2 nouvelles entités persistées + réutilisation Membres/Antennes.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Évaluation vis-à-vis de la Constitution Lumineux v1.0.0 :

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion | Couches Domain/Application/Infrastructure/API en assemblies séparés ; dépendances vers l'intérieur ; logique métier hors contrôleurs | ✅ PASS — voir Project Structure |
| II. Code-First & intégrité BD | Schéma via migrations EF Core ; FK/unicité/index déclarés ; champs d'audit hérités | ✅ PASS — `data-model.md` |
| III. Tests en premier (NON-NÉGOCIABLE) | Projets de tests unitaires Domain/Application ; règles métier testables sans base ; CI bloquante | ✅ PASS — projets `tests/` prévus |
| IV. Sécurité par défaut | Validation serveur (FluentValidation) ; EF paramétré ; authz par droit `manage_attendance` ; secrets hors code ; anti-fraude QR | ✅ PASS — `research.md` §sécurité |
| V. Contrats d'API explicites | DTO dédiés (jamais d'entités exposées) ; REST + codes HTTP homogènes ; versioning `/api/v1` ; OpenAPI | ✅ PASS — `contracts/openapi.yaml` |
| VI. Traçabilité & observabilité | Journalisation structurée Serilog ; audit sur sessions/présences ; refus consignés ; temps serveur | ✅ PASS — `data-model.md` + `research.md` |

**Résultat initial : PASS — aucune violation, section Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (data-model, contrats, quickstart) respecte les
mêmes principes ; aucune dérogation introduite. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/001-attendance-management/
├── plan.md              # Ce fichier (/speckit-plan)
├── research.md          # Phase 0 — décisions techniques
├── data-model.md        # Phase 1 — entités, relations, transitions
├── quickstart.md        # Phase 1 — guide de validation bout-en-bout
├── contracts/
│   └── openapi.yaml     # Phase 1 — contrat REST de l'API
├── checklists/
│   └── requirements.md  # Checklist qualité de la spec (déjà produite)
└── tasks.md             # Phase 2 (/speckit-tasks — NON créé ici)
```

### Source Code (repository root)

Architecture Onion — un assembly par couche, dépendances orientées vers le Domain :

```text
src/
├── Lumineux.Domain/            # Cœur : entités (AttendanceSession, Attendance), value objects,
│   │                           #   invariants, interfaces (ports) : IAttendanceSessionRepository,
│   │                           #   IAttendanceRepository, IQrTokenService, IClock
│   ├── Entities/
│   ├── Enums/                  # SessionStatus, AttendanceSource, AttendanceStatus
│   └── Abstractions/           # interfaces de ports, exceptions métier
├── Lumineux.Application/       # Cas d'usage / services applicatifs, DTO, validators, mapping
│   ├── AttendanceSessions/     # StartSession, CloseSession, GetSession, ListAttendances
│   ├── Attendances/            # ScanAttendance, SyncOfflineScans, AddManualAttendance, CancelAttendance
│   ├── Contracts/              # DTO d'entrée/sortie (jamais d'entités exposées)
│   └── Abstractions/           # ports applicatifs additionnels (ex. ICurrentUser)
├── Lumineux.Infrastructure/    # EF Core (DbContext, configurations, migrations), repositories,
│   │                           #   QrTokenService (TOTP), SystemClock, journalisation
│   ├── Persistence/            # DbContext, EntityTypeConfigurations, Migrations/
│   ├── Repositories/
│   └── Security/               # génération/validation jeton QR, résolution des droits
└── Lumineux.Api/               # ASP.NET Core Web API — contrôleurs, DI, middleware, auth, Swagger
    ├── Controllers/            # AttendanceSessionsController, AttendancesController
    ├── Middleware/             # gestion d'erreurs homogène, corrélation/log
    └── Program.cs

tests/
├── Lumineux.Domain.Tests/          # unitaires : invariants d'entités, transitions d'état
├── Lumineux.Application.Tests/     # unitaires : cas d'usage (ports mockés)
├── Lumineux.Infrastructure.Tests/  # intégration : repositories + EF sur base éphémère
└── Lumineux.Api.Tests/             # intégration/contrat : endpoints via WebApplicationFactory
```

**Structure Decision**: Projet **web service** en architecture Onion. Les répertoires ci-dessus
matérialisent la règle de dépendance (Constitution I) : `Domain` sans dépendance sortante,
`Application` dépend de `Domain`, `Infrastructure` et `Api` dépendent de `Application`/`Domain`. Les
clients Angular et Flutter sont hors périmètre de cette itération et consommeront `contracts/openapi.yaml`.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
