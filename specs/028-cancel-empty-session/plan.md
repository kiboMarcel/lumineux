# Implementation Plan: Annulation d'une session de présence vide

**Branch**: `028-cancel-empty-session` | **Date**: 2026-07-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/028-cancel-empty-session/spec.md`

## Summary

Ajouter au module Présences la possibilité d'**annuler** une session **ouverte** **uniquement si elle ne
comporte aucune présence valide** (décompte = 0), pour corriger une ouverture par erreur sans jamais perdre
de présence. L'annulation introduit un **état terminal « annulée »** conservé (soft, traçable) : la session
disparaît des vues actives (reprise 023, listes, rapports) mais reste auditée (auteur/horodatage). Le chemin
API existant (`src/`) est étendu **de façon additive** : nouvel `SessionStatus.Cancelled`, méthode de domaine
`AttendanceSession.Cancel(...)`, `CancelSessionHandler` (mirroir de `CloseSessionHandler`) qui **re-vérifie
atomiquement** le décompte de présences valides (`IAttendanceRepository.CountValidBySessionAsync`, **déjà
existant**) avant de basculer l'état, et un endpoint `POST /attendance-sessions/{id}/cancel`
(`[Authorize] manage_attendance`). Côté SPA (`web/`), un bouton **« Annuler la session »** (confirmé)
apparaît sur le suivi de session **tant qu'aucune présence valide** n'existe ; sur refus serveur (409), un
message clair est affiché. Distinct de la **clôture** (existante) et de l'**annulation d'une présence**
(existante). Tests API (domaine + handler + endpoint) et SPA (Principe III).

## Technical Context

**Language/Version** : C# / .NET (API `src/`, architecture Onion) · TypeScript / Angular 20 (SPA `web/`).

**Primary Dependencies** : existantes — EF Core (SQL Server, code-first + migrations), FluentValidation,
`IAuditLogger`, `ICurrentUser`, `IClock` ; Angular (RxJS, signals). **Aucune** nouvelle dépendance.

**Storage** : SQL Server. Évolution **additive** : `SessionStatus.Cancelled = 2` (colonne `Status` déjà un
`int`) + **2 colonnes nullables** d'audit sur `attendance_sessions` : `CancelledByMemberId`, `CancelledAt`.
**Migration code-first** requise, rejouable sur base vierge.

**Testing** : xUnit (Domain + Application + Api tests, `src/`) ; Vitest (`web/`). Tests-first sur la
logique (garde de domaine, handler, concurrence).

**Target Platform** : API .NET (serveur) + console web Angular (bureau). Pas de mobile.

**Performance Goals** : annulation confirmée en **< 5 s** (SC-001) ; opération O(1) (un décompte + une mise à
jour d'état).

**Constraints** : **serveur autorité** sur la règle « vide » ; **re-vérification atomique** du décompte au
moment de l'annulation (SC-003) ; **aucune** présence supprimée/modifiée (FR-008) ; **traçabilité** de
l'annulation et des refus pour droit manquant (FR-009) ; évolution d'API **additive** (pas de rupture).

**Scale/Scope** : 1 valeur d'enum + 2 colonnes + 1 méthode de domaine + 1 handler + 1 validator + 1 endpoint
consommé ; 1 méthode SPA + 1 bouton/flux sur `session-run` ; 1 persona (bureau).

## Constitution Check

*GATE : doit passer avant Phase 0 ; re-vérifié après Phase 1.*

| Principe | Applicabilité | Verdict |
|----------|---------------|---------|
| **I. Onion & séparation des couches** | Transition d'état dans le **Domain** (`AttendanceSession.Cancel`), règle d'orchestration + décompte dans l'**Application** (`CancelSessionHandler`) via ports (`IAttendanceSessionRepository`, `IAttendanceRepository`), endpoint mince en **Api**. Aucune logique métier dans le contrôleur. | ✅ PASS |
| **II. Code-First & intégrité BD** | Nouvel état + 2 colonnes d'audit via **migration** versionnée, déterministe/rejouable ; piste d'audit héritée (`AbstractEntity`) préservée. | ✅ PASS |
| **III. Tests en premier (NON-NÉGOCIABLE)** | Tests unitaires : garde de domaine `Cancel` (refus si non ouverte), handler (**introuvable/non ouverte/non vide/succès/concurrence**), endpoint API (200/404/409/403), SPA (bouton masqué si présences, confirmation, message 409). Test-first. | ✅ PASS |
| **IV. Sécurité par défaut** | Endpoint `[Authorize] manage_attendance` ; **serveur seul autorité** sur le décompte et l'état ; **audit** de l'annulation et des tentatives refusées ; aucune donnée personnelle exposée. | ✅ PASS |
| **V. Contrats d'API explicites** | Ajout **additif** `POST .../{id}/cancel` (DTO `SessionResponse` réutilisé, `Status` expose « Cancelled ») ; codes cohérents **404** (introuvable) / **409** (non ouverte ou non vide) / **403** (droit) ; documenté (OpenAPI). Pas de rupture → pas de versionnage. | ✅ PASS |
| **VI. Traçabilité & audit** | `IAuditLogger` journalise l'annulation (auteur, horodatage, session) et les refus pour droit manquant ; horodatage via **source serveur** (`IClock`) ; champs `CancelledByMemberId`/`CancelledAt`. | ✅ PASS |

**Résultat** : aucun écart. Section *Complexity Tracking* laissée vide.

## Project Structure

### Documentation (this feature)

```text
specs/028-cancel-empty-session/
├── plan.md              # Ce fichier
├── research.md          # Décisions techniques (Phase 0)
├── data-model.md        # États/entités & transitions (Phase 1)
├── quickstart.md        # Guide de validation (Phase 1)
├── contracts/           # Contrats (Phase 1)
│   ├── cancel-session-api.md   # Endpoint POST /attendance-sessions/{id}/cancel (nouveau)
│   └── cancel-session-ui.md    # Bouton « Annuler la session » (console bureau)
└── tasks.md             # Phase 2 (/speckit-tasks — non créé ici)
```

### Source Code (repository root)

```text
src/                                             # API .NET (Onion) — inchangée hors de ces points
├── Lumineux.Domain/
│   ├── Enums/SessionStatus.cs                   # MODIF : + Cancelled = 2
│   └── Entities/AttendanceSession.cs            # MODIF : + Cancel(cancelledBy, nowUtc) ; + CancelledByMemberId/CancelledAt
├── Lumineux.Application/
│   └── AttendanceSessions/
│       ├── CancelSessionHandler.cs              # NOUVEAU : garde ouverte + décompte 0 (atomique) + Cancel + audit
│       └── SessionMapping.cs                    # (Status déjà mappé ; « Cancelled » exposé tel quel)
├── Lumineux.Infrastructure/
│   └── Persistence/
│       ├── Configurations/AttendanceSessionConfiguration.cs # MODIF : mapping des 2 colonnes d'audit
│       └── Migrations/<horodatage>_CancelSession.cs         # NOUVEAU : + Cancelled + 2 colonnes nullables
└── Lumineux.Api/
    └── Controllers/AttendanceSessionsController.cs # MODIF : + [HttpPost("{id}/cancel")]

tests/                                           # xUnit
├── Lumineux.Domain.Tests/AttendanceSessionTests.cs      # + Cancel (ouverte→annulée ; refus si close/annulée)
├── Lumineux.Application.Tests/CancelSessionTests.cs      # NOUVEAU : introuvable/non ouverte/non vide/succès/concurrence
└── Lumineux.Api.Tests/SessionEndpointsTests.cs          # + cancel 200/404/409/403

web/                                             # SPA Angular
└── src/app/
    ├── core/api/attendance-sessions-api.ts      # MODIF : + cancel(sessionId)
    └── features/attendance/session-run/session-run.component.ts # MODIF : bouton « Annuler la session » (si 0 présence valide) + confirmation + message 409
```

**Structure Decision** : extension **additive** de la verticale Présences existante (features 014/023/024),
sans nouveau module. La règle « vide » réutilise `CountValidBySessionAsync` (**déjà présent**) ; l'exclusion
des vues actives est **acquise** car la reprise (023) et les listes filtrent `Status == Open`. Le seul
changement de schéma est la migration additive (enum + 2 colonnes d'audit).

## Complexity Tracking

*Aucun écart à la constitution — section vide.*
