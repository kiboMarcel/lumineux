# Implementation Plan: Console web — Profils du bureau & droits (SPA, Lot 3)

**Branch**: `011-spa-bureau-profiles` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/011-spa-bureau-profiles/spec.md`

## Summary

Ajouter le module **« Profils du bureau & droits »** à la console web **Angular** (`web/`) :
**consulter** les profils (US1), les **administrer** (créer/modifier/supprimer, US2) et **attribuer/
révoquer** les profils d'un membre avec ses **droits effectifs** (US3). Consomme les endpoints
**existants** (features 004/002) :

- **Profils** : `GET /bureau-profiles`, `GET /bureau-profiles/{id}`, `POST`, `PUT /{id}`,
  `DELETE /{id}`.
- **Attribution** : `GET /members/{memberId}/bureau-profiles`, `POST` (attribuer), `DELETE /{profileId}`
  (révoquer).
- **Catalogue** : `GET /permissions` (droits figés `{ code, label }`).

Autorisation **duale** (fidèle à l'API) :
- **Lecture** (liste/détail profils, profils d'un membre) = **`manage_bureau_profiles` OU
  `manage_members`**.
- **Écriture** (créer/modifier/supprimer, attribuer/révoquer) = **`manage_bureau_profiles`** seul.

Codes de conflit restitués tels quels (erreurs bloquantes) : **`duplicate_name`**,
**`last_administrator`** (retrait admin du dernier / suppression du dernier profil admin / révocation
du dernier admin), **`profile_in_use`** (suppression d'un profil encore attribué), **`member_inactive`**
(attribution à un membre inactif). Attribution **idempotente**. Actions destructrices **confirmées**.

Évolutions du **socle (feature 008)** :
- **Garde/Nav any-of** : le RBAC d'affichage doit gérer « l'un **OU** l'autre » droit (lecture). On
  étend `permissionGuard` et la navigation du shell pour accepter un **ensemble de droits** (any-of).
- **Point d'entrée** : lien « Profils & droits » ajouté sur la **fiche membre** (Lot 2) vers l'écran
  des profils du membre.

L'API n'est pas modifiée. Aucun état persistant nouveau côté client.

## Technical Context

**Language/Version**: TypeScript ; **Angular 20** (app `web/` existante — standalone, signals).

**Primary Dependencies**: socle feature 008 (`SessionStore`, intercepteurs, gardes, `messageForError`,
notifications), Angular Router, `HttpClient`, Reactive Forms. Réutilise les patterns `core/api` et
`features/`.

**Storage**: aucune persistance client. Données de vue transitoires.

**Testing**: **Vitest** (unitaires : `BureauProfilesApi`/`MemberProfilesApi`/`PermissionsApi`,
composants liste/détail/formulaire/profils-membre, garde any-of, gestion des 409
`duplicate_name`/`last_administrator`/`profile_in_use`, idempotence attribution) + **Playwright**
(e2e : consulter, créer/supprimer avec confirmation, attribuer/révoquer, garde-fou dernier admin).

**Target Platform**: navigateurs modernes (bureau + tablette), HTTPS ; consomme l'API `/api/v1`.

**Project Type**: Web (extension de l'app `web/`). L'API n'est pas modifiée.

**Performance Goals**: listes de taille modérée ; un chargement du catalogue de droits par formulaire.

**Constraints**: **lecture** = deux droits, **écriture** = admin profils (masquage + garde), autorité
serveur (403 géré) ; **aucune donnée sensible** exposée ; erreurs mappées (400/404/409 par `code`) ;
**confirmations** destructrices (FR-015) ; validation client **indicative** ; **français** + responsive.

**Scale/Scope**: 3 user stories ; ~5 écrans (liste, détail, formulaire, profils-membre) + 3 services
API + évolution garde/nav + lien fiche membre.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

> Principes I/VI appliqués dans leur **esprit** au front (séparation composants/services, aucune règle
> métier côté client, aucun secret journalisé) — cadre établi en features 008/009.

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Aucune règle métier client ; **accès API encapsulé** (`core/api`), composants de présentation (`features/bureau-profiles`), gardes transverses. | ✅ PASS (esprit) |
| II. Code-First & intégrité BD | **Sans objet** (pas de base côté SPA ; API inchangée). | ✅ N/A |
| III. Tests en premier | Unitaires (services, composants, garde any-of, 409, idempotence) + e2e (parcours). Rouge → vert. | ✅ PASS |
| IV. Sécurité par défaut | **Lecture vs écriture** distinguées + **autorité serveur** (403 géré) ; **aucune donnée sensible** ; confirmations destructrices ; garde-fous serveur (`last_administrator`, etc.) restitués sans contournement. | ✅ PASS |
| V. Contrats d'API explicites | Consomme les **contrats versionnés existants** via **modèles typés** ; erreurs ProblemDetails + `code` mappées. | ✅ PASS |
| VI. Traçabilité & observabilité | Opérations sensibles journalisées **côté API** ; côté client, aucun secret loggé. | ✅ PASS (esprit) |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (services `core/api`, composants
`features/bureau-profiles`, garde/nav any-of, distinction lecture/écriture, confirmations) respecte
les principes applicables. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/011-spa-bureau-profiles/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── api-consumption.md   # endpoints profils + attribution + catalogue (vue client, codes 409)
│   └── routes.md            # routes du module + gardes (lecture any-of / écriture admin)
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de l'app front `web/`

```text
web/src/app/
├── core/
│   ├── api/
│   │   ├── bureau-profiles-api.ts   # list · get · create · update · remove
│   │   ├── member-profiles-api.ts   # get(memberId) · assign · revoke
│   │   └── permissions-api.ts       # list() catalogue { code, label }
│   └── guards/guards.ts             # permissionGuard ÉTENDU (any-of : data.anyPermissions)
├── features/bureau-profiles/
│   ├── profile-list/                # US1 : liste
│   ├── profile-detail/              # US1 : détail (droits + titulaires) + actions écriture (si droit)
│   ├── profile-form/                # US2 : création/édition (droits du catalogue)
│   └── member-profiles/             # US3 : profils + droits effectifs d'un membre (assign/revoke)
├── shell/                           # nav « Profils du bureau » : any-of (lecture) + lien réel
└── app.routes.ts                    # routes /bureau-profiles* et /members/:id/profiles (gardées)

# member-detail (Lot 2) : ajout d'un lien « Profils & droits » vers /members/:id/profiles
# API (src/) : INCHANGÉE
```

**Structure Decision**: Extension de l'app Angular existante. Accès API **encapsulé** dans trois
services `core/api` (profils, attribution, catalogue). La distinction **lecture/écriture** est portée
par : (a) une **garde any-of** (`permissionGuard` étendu à `data.anyPermissions`) pour l'accès en
lecture, (b) une garde single-droit pour les **routes d'écriture**, (c) le **masquage conditionnel**
des actions d'écriture dans les composants selon `manage_bureau_profiles`. L'API reste l'autorité
(403). L'écran des profils d'un membre est accessible depuis la **fiche membre** (Lot 2).

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
