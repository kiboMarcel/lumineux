# Implementation Plan: Console web — Installation découvrable du premier administrateur (SPA)

**Branch**: `013-spa-discoverable-setup` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/013-spa-discoverable-setup/spec.md`

## Summary

Rendre l'installation du premier administrateur **découvrable** depuis la console web **Angular**
(`web/`) : à l'ouverture de l'écran de **connexion**, consulter le **statut d'installation**
(`GET /api/v1/setup/status`, feature 012, anonyme) et afficher un lien **« Première installation »**
(vers l'écran existant `/setup/first-admin`) **uniquement si** l'instance n'est **pas** initialisée.

Approche **minimale** — réutilisation maximale, **aucune modification d'API** :
- **Étendre `SetupApi`** (core/api, feature 008) avec `status()` → `{ installed: boolean }`, et ajouter
  un modèle `SetupStatus`.
- **Modifier `LoginComponent`** (feature 008) : au chargement, appeler `status()` ; exposer un signal
  `showSetupLink` = `installed === false`. En **erreur** (statut indisponible), **masquer** le lien
  (défaut sûr) sans bloquer la connexion. Afficher le lien conditionnellement dans le gabarit.
- **Réutiliser tel quel** : `SetupComponent` et `SetupApi.installFirstAdmin` (feature 008), qui gèrent
  déjà le refus **409 already_installed** ; la route `/setup/first-admin` reste accessible directement.

Aucun autre écran modifié ; aucun état persistant nouveau.

## Technical Context

**Language/Version**: TypeScript ; **Angular 20** (app `web/` existante — standalone, signals).

**Primary Dependencies**: socle feature 008 (`SetupApi`, `LoginComponent`, notifications) ; endpoint de
statut feature 012. Pas de nouvelle dépendance.

**Storage**: aucune persistance client. Statut consulté à l'ouverture de la connexion (volatil).

**Testing**: **Vitest** (unitaires : `SetupApi.status` URL/méthode ; `LoginComponent` — lien **affiché**
si `installed=false`, **masqué** si `installed=true` **ou** si le statut échoue) + **Playwright** (e2e :
lien présent/absent selon l'état, accès à l'écran d'installation).

**Target Platform**: navigateurs modernes (bureau + tablette), HTTPS.

**Project Type**: Web (extension de l'app `web/`). L'API n'est pas modifiée.

**Performance Goals**: **un seul** appel de statut à l'ouverture de la connexion ; sans impact sur la
connexion.

**Constraints**: parcours **anonyme** ; lien **conditionnel** (installed=false) — FR-002/003 ; **défaut
sûr = masqué** si statut indisponible (FR-005) ; **aucune** donnée sensible ; **français** + responsive ;
l'écran d'installation reste accessible par URL (FR-006) et auto-bloqué côté API (FR-007).

**Scale/Scope**: 2 user stories (P1) ; 1 méthode d'API + 1 modèle + modification de l'écran de
connexion. Réutilisation de l'écran d'installation existant.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Accès API encapsulé dans `SetupApi` (core/api) ; logique d'affichage dans le composant ; aucune règle métier côté client. | ✅ PASS (esprit) |
| II. Code-First & intégrité BD | **Sans objet** (pas de base côté SPA ; API inchangée). | ✅ N/A |
| III. Tests en premier | Unitaires (`SetupApi.status`, `LoginComponent` conditionnel + défaut sûr) + e2e. Rouge → vert. | ✅ PASS |
| IV. Sécurité par défaut | Parcours **anonyme** ; **défaut sûr = masqué** ; aucune donnée sensible ; le verrou d'installation (409) reste l'autorité serveur. | ✅ PASS |
| V. Contrats d'API explicites | Consomme le contrat existant `GET /setup/status` (feature 012) via un **modèle typé**. | ✅ PASS |
| VI. Traçabilité & observabilité | Sans objet côté client (lecture d'un booléen) ; aucun secret loggé. | ✅ PASS (esprit) |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (extension d'un service existant + affichage
conditionnel dans la connexion, réutilisation de l'écran d'installation) respecte les principes.
**PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/013-spa-discoverable-setup/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   └── ui-behavior.md   # comportement du lien conditionnel (connexion) + statut consommé
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de l'app front `web/`

```text
web/src/app/
├── core/api/
│   ├── setup-api.ts     # + status() → { installed }
│   └── models.ts        # + SetupStatus { installed: boolean }
└── features/login/
    └── login.component.ts   # au chargement : status() → showSetupLink (installed===false) ;
                             #   erreur → masqué ; gabarit : lien « Première installation » conditionnel

# SetupComponent / route /setup/first-admin / SetupApi.installFirstAdmin (feature 008) : RÉUTILISÉS
# API (src/) : INCHANGÉE
```

**Structure Decision**: Extension **minimale** de l'app existante. L'accès au statut passe par le
service `SetupApi` (core/api) ; l'**écran de connexion** décide de l'affichage du lien via un signal
dérivé du statut, avec **défaut sûr = masqué** en cas d'échec. L'écran d'installation et sa logique
(feature 008) sont **réutilisés sans modification** ; l'API n'est pas touchée.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
