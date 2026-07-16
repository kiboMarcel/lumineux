# Implementation Plan: Type de session de présence

**Branch**: `031-session-type` | **Date**: 2026-07-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/031-session-type/spec.md`

## Summary

Ajouter à `AttendanceSession` un discriminant **`SessionType`** (enum applicatif
`AntennaMeeting` par défaut, `Teaching` préparé), calqué sur le champ `Status` existant
(enum persisté en chaîne via `HasConversion<string>`). Le type est fixé à la création
(fabrique `Start`), immuable ensuite, validé côté serveur contre l'ensemble fermé, et exposé
dans `SessionResponse`. Migration additive avec **valeur par défaut `AntennaMeeting`** pour
rétro-remplir toutes les sessions existantes. Aucun domaine cours, aucune FK, aucune logique
métier conditionnelle au type. Livraison **API-only** : le SPA reçoit le champ dans le contrat
de session (non affiché), mais aucun sélecteur de type n'est ajouté à l'écran de démarrage
(différé au futur domaine des enseignements).

## Technical Context

**Language/Version**: C# / .NET 10 (API) ; TypeScript / Angular 20.3 (SPA, contrat seulement)

**Primary Dependencies**: EF Core 10 (code-first, SQL Server / SQLite tests), FluentValidation,
xUnit (.NET), Vitest (SPA)

**Storage**: SQL Server (prod), SQLite (tests d'intégration Infra) ; enum persisté en chaîne
(`HasConversion<string>`, `HasMaxLength(20)`), comme `Status`.

**Testing**: xUnit (Domain/Application/Infrastructure/Api), Vitest (SPA)

**Target Platform**: API web + console web Angular (contrat) ; mobile inchangé

**Project Type**: Web (API .NET Onion + SPA Angular)

**Performance Goals**: aucun — une colonne discriminante, aucun index ni requête supplémentaire.

**Constraints**: additivité stricte (QR rotatif, pointage, clôture, annulation, auto-clôture,
rapports 018/020, scan mobile inchangés) ; type immuable après création ; validation serveur
faisant autorité ; migration déterministe rétro-remplissant l'existant (`default 'AntennaMeeting'`).

**Scale/Scope**: 1 enum, 1 propriété de domaine + 1 paramètre de fabrique, 1 colonne, 1 migration,
2 DTO étendus, 1 mapping, 1 validator, 1 config EF, 1 interface SPA (contrat).

## Constitution Check

*GATE: à valider avant Phase 0, re-vérifié après Phase 1.*

| Principe | Statut | Justification |
|---|---|---|
| **I. Onion & couches** | ✅ PASS | Enum + propriété dans le Domain ; validation dans Application ; persistance en Infrastructure ; exposition via DTO en API. Immuabilité portée par la fabrique du domaine (setter privé). |
| **II. Code-first & intégrité** | ✅ PASS | Colonne via migration EF versionnée, additive, **valeur par défaut** rétro-remplissant l'existant → aucune session sans type (FR-003), rejouable sur base vierge. |
| **III. Tests d'abord (NON-NÉGOCIABLE)** | ✅ PASS | Tests au même changement : défaut `AntennaMeeting`, démarrage avec type explicite, refus d'un type inconnu, immuabilité, mapping DTO, non-régression des parcours de session. |
| **IV. Sécurité par défaut** | ✅ PASS | Type validé contre un ensemble fermé côté serveur (rejet d'une valeur inconnue), comme `Gender`/`Status` ; persistance via ORM (aucune concaténation SQL) ; aucune donnée sensible ajoutée. |
| **V. Contrats d'API explicites** | ✅ PASS | Champ ajouté aux DTO dédiés ; `SessionResponse` gagne un champ (ajout **rétrocompatible**) ; `StartSessionRequest` gagne un champ **optionnel** → pas de rupture pour SPA/mobile. Entités non exposées. |
| **VI. Traçabilité & audit** | ✅ PASS | Démarrage déjà journalisé (`_audit.Operation("StartSession", …)`) et horodaté ; aucun secret ajouté ; le type peut enrichir la charge d'audit sans donnée sensible. |

**Verdict** : aucun écart. Section *Complexity Tracking* sans objet.

## Project Structure

### Documentation (this feature)

```text
specs/031-session-type/
├── plan.md              # Ce fichier
├── research.md          # Phase 0 — décisions (enum, persistance chaîne, défaut en base, API-only)
├── data-model.md        # Phase 1 — enum SessionType, propriété, colonne, contraintes
├── quickstart.md        # Phase 1 — scénarios de validation de bout en bout
├── contracts/
│   └── sessions-api.md  # Phase 1 — deltas de contrat (start/response)
└── checklists/
    └── requirements.md  # (créé au /speckit-specify)
```

### Source Code (repository root)

Fichiers **modifiés / ajoutés** (aucune nouvelle couche, aucun nouveau projet) :

```text
src/
├── Lumineux.Domain/
│   ├── Enums/SessionType.cs                        # NOUVEAU : enum { AntennaMeeting=0, Teaching=1 }
│   └── Entities/AttendanceSession.cs               # + propriété SessionType (setter privé) + param fabrique Start (défaut AntennaMeeting)
├── Lumineux.Application/
│   ├── Contracts/Sessions/SessionDtos.cs           # + SessionType? dans StartSessionRequest ; + SessionType (string) dans SessionResponse
│   ├── AttendanceSessions/SessionMapping.cs        # mappe s.SessionType.ToString()
│   ├── AttendanceSessions/StartSessionHandler.cs   # passe le type (parse/défaut) à la fabrique
│   └── AttendanceSessions/StartSessionValidator.cs # + règle : type reconnu si fourni
└── Lumineux.Infrastructure/
    ├── Persistence/Configurations/AttendanceSessionConfiguration.cs  # colonne "session_type" HasConversion<string> HasMaxLength(20), défaut AntennaMeeting
    └── Persistence/Migrations/<timestamp>_SessionType.cs             # AddColumn NOT NULL default 'AntennaMeeting'

web/src/app/features/attendance/
└── attendance.models.ts                            # + sessionType sur l'interface SessionResponse (contrat, non affiché)

tests :
├── Lumineux.Domain.Tests            # défaut + immuabilité de la fabrique
├── Lumineux.Application.Tests       # démarrage typé, refus type inconnu, mapping
├── Lumineux.Infrastructure.Tests    # persistance/relecture du type (SQLite)
└── Lumineux.Api.Tests               # endpoint start avec/sans type, réponse expose le type
```

**Structure Decision** : application web existante (API Onion + SPA). Enrichissement additif
d'une entité en place, réutilisant intégralement le flux de démarrage (014/023) et le cœur
présence, sans module ni couche nouvelle. Côté SPA, seul le contrat (interface) est étendu ;
aucun composant modifié (API-only).

## Complexity Tracking

Sans objet — la Constitution Check ne relève aucun écart.
