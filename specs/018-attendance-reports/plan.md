# Implementation Plan: API de rapports & statistiques de présence

**Branch**: `018-attendance-reports` | **Date**: 2026-07-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/018-attendance-reports/spec.md`

## Summary

Ajouter à l'**API** (Onion, .NET 10) une **couche analytique en lecture seule** sur les présences
(feature 001) : **synthèse par antenne + période** (sessions, présences valides, moyenne),
**taux de présence par membre** (présences valides ÷ sessions de son **antenne d'origine** sur la
période) et **export CSV** de la synthèse. Réservé au droit **`manage_attendance`**. **Aucune écriture,
aucune migration** (agrégations sur les données existantes). Le **tableau de bord SPA** et la **série
temporelle** relèvent d'incréments ultérieurs.

Décisions structurantes (spec) :
- **Présences annulées exclues** de tous les totaux (seules les `Valid` comptent).
- **Taux membre** : dénominateur = sessions de l'**antenne d'origine** du membre (0 → taux 0 %).
- **Validation** stricte de la plage de dates (fin ≥ début, bornes présentes ; plafond de période).
- **CSV** = format d'export du MVP ; PDF/graphiques hors périmètre (SPA).

## Technical Context

**Language/Version**: C# 14 / .NET 10.

**Primary Dependencies**: ASP.NET Core (contrôleurs), EF Core 10 (agrégations `GroupBy`/`Count` ;
SQLite in-memory pour tests), FluentValidation. Réutilise : `ICurrentUser` (droits), `IClock`. Aucune
dépendance nouvelle (CSV généré à la main, sans lib).

**Storage**: SQL Server, code-first. **Lecture seule** sur `attendance_sessions` / `attendances` /
`members` / `antennas`. **Aucune migration.**

**Testing**: xUnit + NSubstitute + FluentAssertions — Application (agrégation via port substitué :
moyenne, exclusion des annulées, taux 0 %, validation de plage, droit manquant), Infrastructure
(agrégations réelles SQLite : synthèse par antenne, dénominateur = antenne d'origine, annulées
exclues), Api (endpoints + policy 401/403 + en-têtes/Content-Type CSV).

**Target Platform**: API HTTPS consommée par un futur tableau de bord SPA.

**Project Type**: Web service (API). Pas de front dans cette feature.

**Performance Goals**: agrégations ponctuelles sur volumétrie modérée ; plafond de période pour borner
le coût.

**Constraints**: droit **`manage_attendance`** ; **lecture seule** (aucun effet de bord) ; **validation
serveur** de la plage ; **PII minimale** (membre = id + nom) ; DTO dédiés (pas d'entités exposées) ;
erreurs ProblemDetails.

**Scale/Scope**: 3 rapports ; ~3 endpoints (synthèse JSON, synthèse CSV, taux membre) ; 1 port de
lecture + impl ; 0 migration.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Cas d'usage (handlers) en **Application** ; agrégations derrière un **port** implémenté en **Infrastructure** ; contrôleur **fin**. | ✅ PASS |
| II. Code-First & intégrité BD | **Lecture seule**, **aucune migration** ni modification de schéma. | ✅ N/A |
| III. Tests en premier | Tests Application/Infrastructure/Api écrits avant impl. (rouge → vert). | ✅ PASS |
| IV. Sécurité par défaut | Droit **`manage_attendance`** (policy + handler) ; **validation** de la plage ; **PII minimale** ; pas d'écriture. | ✅ PASS |
| V. Contrats d'API explicites | **DTO dédiés**, REST cohérent (`/api/v1/reports/attendance/...`), codes HTTP + format d'erreur homogène ; CSV avec Content-Type/nom de fichier. | ✅ PASS |
| VI. Traçabilité & observabilité | Accès journalisé (request logging) ; aucune donnée sensible superflue ; horodatages métier issus du serveur. | ✅ PASS (esprit) |

**Résultat initial : PASS** — aucune violation, Complexity Tracking non requis.

*Re-check post-conception (Phase 1)* : la conception (port d'agrégation lecture seule, handlers,
validation, CSV sans dépendance) respecte les principes. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/018-attendance-reports/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   └── rest-api.md          # endpoints rapports (synthèse JSON/CSV, taux membre) + droit + codes
└── checklists/requirements.md
```

### Source Code (repository root) — extension de l'API .NET

```text
src/
├── Lumineux.Domain/
│   └── Abstractions/IAttendanceReportRepository.cs   # NOUVEAU port (lecture/agrégation)
├── Lumineux.Application/
│   ├── Contracts/Reports/ReportDtos.cs               # AntennaAttendanceSummaryItem/Response, MemberAttendanceRateResponse
│   └── Reports/
│       ├── GetAntennaAttendanceSummaryHandler.cs     # US1 (+ calcul moyenne)
│       ├── GetMemberAttendanceRateHandler.cs         # US2 (taux, antenne d'origine)
│       ├── ExportAntennaAttendanceCsvHandler.cs      # US3 (rend le CSV depuis la synthèse)
│       ├── ReportPeriodValidator.cs                  # plage de dates (fin ≥ début, plafond)
│       └── (DI dans Application/DependencyInjection.cs)
├── Lumineux.Infrastructure/
│   └── Repositories/AttendanceReportRepository.cs    # NOUVEAU (EF GroupBy/Count) + DI
└── Lumineux.Api/
    └── Controllers/ReportsController.cs               # [Authorize(Policy = manage_attendance)]

tests/
├── Lumineux.Application.Tests/{AntennaAttendanceSummary,MemberAttendanceRate,ReportPeriodValidator}Tests.cs
├── Lumineux.Infrastructure.Tests/AttendanceReportRepositoryTests.cs
└── Lumineux.Api.Tests/ReportsEndpointsTests.cs
```

**Structure Decision**: Extension de l'API existante suivant l'Onion. Un **port de lecture**
`IAttendanceReportRepository` porte les **agrégations** (synthèse par antenne ; éléments du taux d'un
membre) ; les **handlers** appliquent le contrôle de droit, la **validation de plage**, le calcul des
**moyennes/taux** et le rendu **CSV** (généré à la main, sans dépendance). Aucune migration : les
requêtes lisent `attendance_sessions`/`attendances` en joignant `antennas`/`members` pour les libellés.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
