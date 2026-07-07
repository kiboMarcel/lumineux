# Implementation Plan: Console web — Reprendre une session de présence en cours

**Branch**: `024-spa-resume-session` | **Date**: 2026-07-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/024-spa-resume-session/spec.md`

## Summary

Ajouter à l'**écran de démarrage** des présences (feature 014, `session-start`) la **reprise** d'une
session en cours : au chargement, récupérer les **sessions ouvertes de l'utilisateur** (API 023,
`mine/open`) et afficher un **encart** « Vous avez une session en cours » avec un bouton **« Reprendre »**
par session (→ écran d'animation) ; et, en cas de **conflit** au démarrage (409 « session déjà ouverte
pour cette antenne/date »), proposer **« Reprendre la session en cours »** au lieu d'un simple message.
Réservé au droit **`manage_attendance`** (module déjà gardé). **L'API n'est pas modifiée. Aucune
dépendance npm.**

Décisions structurantes (spec) :
- **Encart proactif** : `AttendanceSessionsApi.myOpenSessions()` au chargement ; encart si liste non vide.
- **Reprise sur conflit** : sur **409** au démarrage, retrouver (via `mine/open`) la session ouverte
  correspondant à l'**antenne + date** choisies → bouton de reprise ; sinon message de conflit clair.
- **Libellés d'antenne** via le référentiel déjà chargé (010) ; **aucun calcul métier client**.
- **Non-bloquant** : un échec de la vérification n'empêche pas le **démarrage** d'une nouvelle session.

## Technical Context

**Language/Version**: TypeScript ; **Angular 20** (app `web/` existante — standalone, signals).

**Primary Dependencies**: socle feature 008 (`SessionStore`, intercepteurs, `messageForError`,
notifications) ; module Présences 014 (`session-start`, `session-run`) ; `ReferenceApi` (010, antennes) ;
`AttendanceSessionsApi` (014, **étendu** de `myOpenSessions()`) ; Angular Router. **Aucune dépendance npm
nouvelle.**

**Storage**: aucune persistance client (état de vue transitoire).

**Testing**: **Vitest** (unitaires : `AttendanceSessionsApi.myOpenSessions` ; `session-start` — encart
affiché si sessions ouvertes, reprise navigue, absence d'encart si aucune, conflit 409 → reprise
correspondante, 409 sans correspondance → message ; libellé d'antenne) + **Playwright** (e2e : démarrer,
naviguer, revenir, reprendre).

**Target Platform**: navigateurs modernes (bureau + tablette), HTTPS.

**Project Type**: Web (extension de l'app `web/`). L'API n'est pas modifiée.

**Performance Goals**: un appel de vérification au chargement de l'écran ; navigation immédiate à la
reprise.

**Constraints**: droit **`manage_attendance`** (module déjà gardé) ; **aucun calcul métier client** ;
erreurs mappées ; **français** + responsive ; la vérification ne doit **pas** bloquer le démarrage.

**Scale/Scope**: 2 user stories ; extension d'**1 écran** (`session-start`) + 1 méthode de service.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

> Principes I/VI appliqués dans leur **esprit** au front (séparation composant/service, aucune règle
> métier client, aucun secret journalisé).

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Accès API encapsulé (`core/api/attendance-sessions-api.ts`) ; composant de présentation (`session-start`) ; **aucune règle métier client**. | ✅ PASS (esprit) |
| II. Code-First & intégrité BD | **Sans objet** (pas de base côté SPA ; API 023 inchangée). | ✅ N/A |
| III. Tests en premier | Unitaires (service, encart, reprise, conflit) + e2e. Rouge → vert. | ✅ PASS |
| IV. Sécurité par défaut | Module gardé `manage_attendance` (existant) ; l'API 023 ne renvoie **que** mes sessions (autorité serveur) ; 401 → purge/reconnexion. | ✅ PASS |
| V. Contrats d'API explicites | Consomme le **contrat versionné** 023 (`mine/open`) via **modèles typés** existants (`SessionResponse`) ; erreurs mappées. | ✅ PASS |
| VI. Traçabilité & observabilité | Accès journalisé **côté API** ; côté client, aucun secret loggé. | ✅ PASS (esprit) |

**Résultat initial : PASS** — aucune violation, Complexity Tracking non requis.

*Re-check post-conception (Phase 1)* : la conception (service étendu, encart de reprise, gestion du
conflit) respecte les principes applicables. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/024-spa-resume-session/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── api-consumption.md   # endpoint mine/open (023) + antennes (010) consommés
│   └── routes.md            # aucune nouvelle route ; encart intégré à /attendance
└── checklists/requirements.md
```

### Source Code (repository root) — extension de l'app front `web/`

```text
web/src/app/
├── core/api/
│   └── attendance-sessions-api.ts   # + myOpenSessions() → SessionResponse[] (API 023)
├── features/attendance/
│   └── session-start/               # + encart « session en cours » + reprise + reprise sur conflit
└── (route /attendance inchangée ; nav « Présences » inchangée)

# API (src/) : INCHANGÉE
```

**Structure Decision**: Extension de l'écran `session-start` (014). Le service `AttendanceSessionsApi`
reçoit `myOpenSessions()`. Au chargement, le composant récupère mes sessions ouvertes et affiche un
**encart** (libellé d'antenne via `ReferenceApi` déjà chargé, date, heure de début) avec **« Reprendre »**
(→ `/attendance/sessions/:id`). Sur **409** au démarrage, il retrouve la session correspondant à
l'**antenne + date** choisies (dans la liste `mine/open`) et propose la reprise ; sinon le **message de
conflit** est affiché. La vérification est **non bloquante** pour le formulaire de démarrage. Aucune
règle métier client ; aucune nouvelle route.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
