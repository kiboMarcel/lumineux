# Implementation Plan: Console web — Gestion des antennes (SPA)

**Branch**: `017-spa-antenna-management` | **Date**: 2026-07-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/017-spa-antenna-management/spec.md`

## Summary

Ajouter à la console web **Angular** (`web/`) un module **« Antennes »** : **lister** (actives et
inactives, avec statut), **créer** (code + libellé + district), **modifier** (libellé + district ;
**code en lecture seule**), **activer/désactiver** (désactivation **confirmée**). Réservé au droit
**`manage_referentials`**. Consomme l'**API 016** (gestion des antennes) et le **référentiel des
districts** (feature 010) pour le sélecteur. Le **socle 008** fournit session, gardes, intercepteurs,
mapping d'erreurs et notifications. **L'API n'est pas modifiée.**

Décisions structurantes (spec) :
- **Liste de gestion** distincte de la lecture publique 010 : inclut les **inactives** (via
  `GET /api/v1/antennas`).
- **Code immuable** : affiché en lecture seule à la modification (l'API ignore tout changement).
- **Désactivation** : **confirmation** obligatoire ; refus API `antenna_has_open_sessions` → **message
  clair** (via `messageForError`), antenne conservée active.
- **RBAC d'affichage** : entrée de nav « Antennes » et routes gardées `permissionGuard('manage_referentials')` ;
  l'API reste l'autorité (403 géré).

## Technical Context

**Language/Version**: TypeScript ; **Angular 20** (app `web/` existante — standalone, signals).

**Primary Dependencies**: socle feature 008 (`SessionStore`, intercepteurs, `permissionGuard`,
`messageForError`, notifications) ; `ReferenceApi` (010, districts) ; Angular Router, `HttpClient`,
Reactive/Template Forms. Nouveau service `AntennasApi` (consomme l'API 016). Aucune dépendance npm
nouvelle.

**Storage**: aucune persistance client (état de vue transitoire).

**Testing**: **Vitest** (unitaires : service `AntennasApi` ; composants liste et formulaire ; garde de
droit ; mapping des erreurs `duplicate_code` / `antenna_has_open_sessions` ; confirmation de
désactivation ; code en lecture seule) + **Playwright** (e2e : lister, créer, modifier, désactiver/
réactiver).

**Target Platform**: navigateurs modernes (bureau + tablette), HTTPS.

**Project Type**: Web (extension de l'app `web/`). L'API n'est pas modifiée.

**Performance Goals**: interactions CRUD ponctuelles ; volumétrie faible (dizaines d'antennes).

**Constraints**: droit **`manage_referentials`** (garde + masquage) ; **code non modifiable** ;
**confirmation** de désactivation ; erreurs mappées sans détail technique ; **français** + responsive ;
aucune règle métier client ; aucun secret journalisé.

**Scale/Scope**: 4 user stories ; ~2 écrans (liste ; formulaire création/édition) + 1 service API +
intégration navigation.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

> Principes I/VI appliqués dans leur **esprit** au front (séparation composants/services, aucune règle
> métier client, aucun secret journalisé).

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Accès API encapsulé (`core/api/antennas-api.ts`) ; composants de présentation (`features/antennas`) ; gardes transverses. Aucune règle métier client. | ✅ PASS (esprit) |
| II. Code-First & intégrité BD | **Sans objet** (pas de base côté SPA ; API 016 inchangée). | ✅ N/A |
| III. Tests en premier | Unitaires (service, liste, formulaire, garde, mapping d'erreurs, confirmation) + e2e. Rouge → vert. | ✅ PASS |
| IV. Sécurité par défaut | Garde `manage_referentials` + **autorité serveur** (403 géré) ; **confirmation** destructrice ; 401 → purge/reconnexion. | ✅ PASS |
| V. Contrats d'API explicites | Consomme les **contrats versionnés** de l'API 016 via **modèles typés** ; erreurs ProblemDetails/`code` mappées. | ✅ PASS |
| VI. Traçabilité & observabilité | Opérations sensibles journalisées **côté API** ; côté client, aucun secret loggé. | ✅ PASS (esprit) |

**Résultat initial : PASS** — aucune violation, Complexity Tracking non requis.

*Re-check post-conception (Phase 1)* : la conception (service `core/api`, composants liste/formulaire,
garde de droit, mapping d'erreurs) respecte les principes applicables. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/017-spa-antenna-management/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── api-consumption.md   # endpoints antennes (016) + districts (010) consommés (vue client)
│   └── routes.md            # routes du module + garde manage_referentials + nav
└── checklists/requirements.md
```

### Source Code (repository root) — extension de l'app front `web/`

```text
web/src/app/
├── core/api/
│   ├── antennas-api.ts              # list · get · create · update · deactivate · activate (API 016)
│   └── (reference-api.ts existant : districts() pour le sélecteur)
├── features/antennas/
│   ├── antenna.models.ts            # AntennaResponse, CreateAntennaRequest, UpdateAntennaRequest
│   ├── antenna-list/                # US1/US4 : liste (inactives incluses), actions activer/désactiver
│   └── antenna-form/                # US2/US3 : création + édition (code lecture seule en édition)
├── shell/                           # nav « Antennes » : lien réel (garde manage_referentials)
└── app.routes.ts                    # routes protégées /antennas* (permissionGuard)

# API (src/) : INCHANGÉE
```

**Structure Decision**: Extension de l'app Angular existante (mêmes patterns que le module Membres 009).
L'accès réseau est **encapsulé** dans `core/api/antennas-api.ts`. Deux écrans : **liste** de gestion
(`antenna-list`, avec actions activer/désactiver confirmées) et **formulaire** (`antenna-form`) partagé
création/édition (le **code** est éditable en création, **lecture seule** en édition). Le **sélecteur de
district** réutilise `ReferenceApi.districts()` (010). La navigation et les routes sont gardées par
`permissionGuard('manage_referentials')`.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
