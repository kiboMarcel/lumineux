# Implementation Plan: Console web — Présences (SPA, Lot 4)

**Branch**: `014-spa-attendance` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/014-spa-attendance/spec.md`

## Summary

Ajouter le module **« Présences »** à la console web **Angular** (`web/`) : démarrer une session,
**projeter un QR rotatif**, **suivre les présences en temps réel** (rafraîchissement périodique),
**ajouter/retirer** manuellement une présence, et **clôturer**. Réservé au droit **`manage_attendance`**.
Consomme les endpoints **existants** (sessions + présences), le **référentiel des antennes** (feature
010) et la **recherche membre allégée** (feature 015). L'API **n'est pas modifiée**.

Décisions structurantes (spec) :
- **QR** : l'API ne renvoie qu'un **jeton** ; le SPA **génère l'image QR côté client** via une
  **bibliothèque cliente de génération de QR** (dépendance npm — approbation réseau à l'implémentation,
  comme feature 008). Le jeton alimente **uniquement** le rendu (jamais affiché en clair ni persisté).
- **Rotation** : le SPA **ré-interroge** `GET /attendance-sessions/{id}/qr` et **regénère** l'image
  **avant expiration** (au rythme `stepSeconds`).
- **Temps réel** : **polling** de `GET /attendance-sessions/{id}/attendances` (l'API n'a pas de flux
  temps réel) — liste + décompte rafraîchis à intervalle régulier.
- **Ajout manuel** : sélecteur de membre via **`GET /members/lookup`** (feature 015, accessible à
  `manage_attendance`) → `POST …/attendances { memberId }` (idempotent).
- **Actions destructrices** (annulation, clôture) : **confirmation** obligatoire ; après clôture, QR et
  écritures masqués ; toute écriture sur session close → **409** géré.

Évolution du socle : l'entrée de navigation « Présences » (placeholder feature 008) devient un **lien
réel** (gardée `manage_attendance`).

## Technical Context

**Language/Version**: TypeScript ; **Angular 20** (app `web/` existante — standalone, signals).

**Primary Dependencies**: socle feature 008 (`SessionStore`, intercepteurs, gardes, `messageForError`,
notifications) ; `ReferenceApi` (010, antennes) ; recherche membre allégée (015) ; Angular Router,
`HttpClient`, Reactive Forms, RxJS (polling) ; **bibliothèque cliente de génération de QR** (nouvelle
dépendance npm).

**Storage**: aucune persistance client. État de vue transitoire ; **le jeton QR n'est jamais persisté**
(mémoire éphémère servant au rendu).

**Testing**: **Vitest** (unitaires : services API ; composants démarrage / animation ; logique de
rotation QR et de polling — horloges simulées ; sélecteur d'ajout manuel via lookup ; confirmations
annulation/clôture ; masquage après clôture) + **Playwright** (e2e : démarrer, QR affiché, liste qui
se met à jour, ajout manuel, annulation, clôture).

**Target Platform**: navigateurs modernes (bureau + tablette ; **QR lisible en grand** pour
projection), HTTPS.

**Project Type**: Web (extension de l'app `web/`). L'API n'est pas modifiée.

**Performance Goals**: un **rafraîchissement périodique** raisonnable (liste ~ toutes les 5 s ; QR au
rythme `stepSeconds`) ; arrêt des cycles à la destruction du composant / clôture.

**Constraints**: droit **`manage_attendance`** (garde + masquage) ; **jeton QR jamais affiché/persisté**
(FR-005/SC-005) ; **polling** (pas de websocket) ; **confirmations** destructrices (FR-016/SC-007) ;
idempotence de l'ajout manuel (FR-009/SC-003) ; écriture sur session close refusée (FR-012/SC-004) ;
**français** + responsive.

**Scale/Scope**: 4 user stories ; ~2 écrans (démarrage ; animation de session) + 3 services API +
bibliothèque QR + intégration navigation.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

> Principes I/VI appliqués dans leur **esprit** au front (séparation composants/services, aucune règle
> métier client, aucun secret journalisé).

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Aucune règle métier client ; **accès API encapsulé** (`core/api`) ; composants de présentation (`features/attendance`) ; gardes transverses. | ✅ PASS (esprit) |
| II. Code-First & intégrité BD | **Sans objet** (pas de base côté SPA ; API inchangée). | ✅ N/A |
| III. Tests en premier | Unitaires (services, rotation QR, polling, ajout manuel, confirmations) + e2e. Rouge → vert. | ✅ PASS |
| IV. Sécurité par défaut | Garde `manage_attendance` + **autorité serveur** (403 géré) ; **jeton QR jamais affiché/persisté** ; confirmations destructrices ; recherche membre **minimale** (feature 015). | ✅ PASS |
| V. Contrats d'API explicites | Consomme les **contrats versionnés existants** (sessions/présences, référentiels, lookup) via **modèles typés** ; erreurs ProblemDetails/ `code` mappées. | ✅ PASS |
| VI. Traçabilité & observabilité | Opérations sensibles journalisées **côté API** ; côté client, aucun secret loggé (jeton QR non journalisé). | ✅ PASS (esprit) |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (services `core/api`, composants animation/
démarrage, bibliothèque QR de rendu, polling borné au cycle de vie du composant) respecte les
principes applicables. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/014-spa-attendance/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── api-consumption.md   # endpoints sessions/présences + lookup + antennes (vue client, codes)
│   └── routes.md            # routes du module + garde manage_attendance
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de l'app front `web/`

```text
web/src/app/
├── core/api/
│   ├── attendance-sessions-api.ts   # start · get · qr · close
│   ├── attendances-api.ts           # list(status) · addManual · cancel
│   └── member-lookup-api.ts         # lookup(query) → MemberLookupItem[] (feature 015)
├── features/attendance/
│   ├── session-start/               # US1 : démarrer (antenne + date + pas de rotation)
│   └── session-run/                 # US1..US4 : animation (QR rotatif, liste temps réel, manuel, clôture)
│       ├── qr-panel/                #   panneau QR (rendu via bibliothèque, rotation)
│       └── manual-add/              #   sélecteur de membre (lookup) + ajout
├── shell/                           # nav « Présences » : lien réel (garde manage_attendance)
└── app.routes.ts                    # routes protégées /attendance* (permissionGuard)

# package.json : + bibliothèque de génération de QR (installation npm à approuver)
# API (src/) : INCHANGÉE
```

**Structure Decision**: Extension de l'app Angular existante. L'accès réseau reste **encapsulé** dans
`core/api` (sessions, présences, lookup). L'écran d'**animation** (`session-run`) orchestre trois
préoccupations isolées : **panneau QR** (rendu + rotation), **liste temps réel** (polling + filtre +
décompte) et **ajout manuel** (sélecteur via lookup 015). Les cycles de rafraîchissement sont **bornés
au cycle de vie du composant** et arrêtés à la clôture. La navigation est gardée par
`permissionGuard('manage_attendance')`. Le **jeton QR** ne sert qu'au rendu (jamais affiché/persisté).

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
