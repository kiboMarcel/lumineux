# Implementation Plan: API de série temporelle des présences

**Branch**: `020-attendance-time-series` | **Date**: 2026-07-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/020-attendance-time-series/spec.md`

## Summary

Étendre l'**API de rapports** (feature 018, Onion, .NET 10) d'une **série temporelle** en **lecture
seule** : présences valides et sessions agrégées par **intervalle** (**semaine ISO** ou **mois
calendaire**) sur une période, éventuellement filtrées par **antenne**. La série est **continue**
(intervalles sans donnée à **0**). Réservé au droit **`manage_attendance`**. **Aucune écriture, aucune
migration.** Alimentera une **courbe SPA** (feature ultérieure).

Décisions structurantes (spec) :
- **Granularités** : `Week` (ISO 8601, lundi) et `Month` (calendaire) ; « jour » **hors périmètre**.
- **Série continue** : tous les intervalles de la plage présents, **zéros** inclus.
- **Présences annulées exclues** ; **cohérence** avec la synthèse 018 (même total sur la même période).
- **Bucketisation en mémoire** (portable SQLite/SQL Server) : le dépôt renvoie des comptes par session,
  le handler range chaque session dans son intervalle et remplit les zéros.

## Technical Context

**Language/Version**: C# 14 / .NET 10.

**Primary Dependencies**: ASP.NET Core (contrôleur `ReportsController` existant, étendu), EF Core 10
(requêtes simples ; SQLite in-memory en test), FluentValidation (`ReportPeriodValidator` existant
réutilisé). Réutilise : `ICurrentUser`, `IAttendanceReportRepository` (feature 018, étendu). Aucune
dépendance nouvelle.

**Storage**: SQL Server, code-first. **Lecture seule** sur `attendance_sessions`/`attendances`.
**Aucune migration.**

**Testing**: xUnit + NSubstitute + FluentAssertions — Application (bucketisation mois/semaine, série
**continue** à zéros, exclusion des annulées via comptes du dépôt, filtre antenne, **granularité
invalide**, plage invalide, droit manquant), Infrastructure (comptes par session réels SQLite : range,
annulées exclues, filtre antenne), Api (endpoint + granularité/période invalides + policy 401/403).

**Target Platform**: API HTTPS consommée par un futur graphique SPA.

**Project Type**: Web service (API). Pas de front dans cette feature.

**Performance Goals**: agrégation ponctuelle sur volumétrie modérée ; plafond de période (réutilisé de
018) pour borner le coût.

**Constraints**: droit **`manage_attendance`** ; **lecture seule** (aucun effet de bord) ; **validation
serveur** (plage + granularité) ; DTO dédiés ; erreurs ProblemDetails.

**Scale/Scope**: 1 rapport supplémentaire ; **1 endpoint** (`time-series`) ; extension du port de
lecture 018 (1 méthode) ; 1 handler + génération d'intervalles ; 0 migration.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Cas d'usage (handler) en **Application** (dont la génération d'intervalles) ; agrégation derrière le **port** 018 (Infrastructure) ; contrôleur **fin**. | ✅ PASS |
| II. Code-First & intégrité BD | **Lecture seule**, **aucune migration** ni modification de schéma. | ✅ N/A |
| III. Tests en premier | Tests Application/Infrastructure/Api écrits avant impl. (rouge → vert). | ✅ PASS |
| IV. Sécurité par défaut | Droit **`manage_attendance`** (policy + handler) ; **validation** plage + granularité ; pas d'écriture. | ✅ PASS |
| V. Contrats d'API explicites | **DTO dédiés**, REST cohérent (`/api/v1/reports/attendance/time-series`), codes HTTP + erreurs homogènes. | ✅ PASS |
| VI. Traçabilité & observabilité | Accès journalisé (request logging) ; aucune donnée sensible superflue ; horodatages métier issus du serveur. | ✅ PASS (esprit) |

**Résultat initial : PASS** — aucune violation, Complexity Tracking non requis.

*Re-check post-conception (Phase 1)* : la conception (extension du port lecture, handler de
bucketisation, validation, DTO dédiés) respecte les principes. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/020-attendance-time-series/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   └── rest-api.md          # endpoint time-series + granularité + droit + codes
└── checklists/requirements.md
```

### Source Code (repository root) — extension de l'API .NET (rapports 018)

```text
src/
├── Lumineux.Domain/
│   └── Abstractions/IAttendanceReportRepository.cs   # + GetSessionValidCountsAsync(from,to,antennaId?) + record SessionValidCount
├── Lumineux.Application/
│   ├── Contracts/Reports/ReportDtos.cs               # + TimeSeriesPoint, AttendanceTimeSeriesResponse, TimeSeriesGranularity
│   └── Reports/
│       ├── GetAttendanceTimeSeriesHandler.cs         # US1/US2 : validation, bucketisation (semaine/mois), zéros
│       ├── TimeBuckets.cs                             # génération d'intervalles ISO-semaine / mois (pur, testable)
│       └── (DI dans Application/DependencyInjection.cs)
├── Lumineux.Infrastructure/
│   └── Repositories/AttendanceReportRepository.cs    # + GetSessionValidCountsAsync (EF, lecture seule)
└── Lumineux.Api/
    └── Controllers/ReportsController.cs               # + GET .../time-series (from/to/granularity/antennaId)

tests/
├── Lumineux.Application.Tests/AttendanceTimeSeriesTests.cs   (+ TimeBucketsTests.cs)
├── Lumineux.Infrastructure.Tests/AttendanceReportRepositoryTests.cs   (cas série)
└── Lumineux.Api.Tests/ReportsEndpointsTests.cs   (cas time-series)
```

**Structure Decision**: Extension de l'API rapports (018) suivant l'Onion. Le **port de lecture**
`IAttendanceReportRepository` reçoit une méthode `GetSessionValidCountsAsync` (comptes de présences
valides par **session** sur la période, filtrable par antenne). Le **handler**
`GetAttendanceTimeSeriesHandler` valide la plage (`ReportPeriodValidator` réutilisé) et la
**granularité**, **génère la suite d'intervalles** (semaine ISO / mois — logique pure `TimeBuckets`,
testable), y range chaque session par sa **date de réunion** et **remplit les zéros**. L'endpoint
s'ajoute au `ReportsController` existant. **Bucketisation en mémoire** → portable SQLite/SQL Server, pas
de fonction de date SQL spécifique. Aucune migration.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
