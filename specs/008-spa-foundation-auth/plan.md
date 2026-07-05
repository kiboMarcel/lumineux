# Implementation Plan: Console web Lumineux — socle & cycle de vie du compte (SPA)

**Branch**: `008-spa-foundation-auth` | **Date**: 2026-07-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-spa-foundation-auth/spec.md`

## Summary

Créer la **console web Lumineux** (application monopage **Angular**) — **première application front**
du dépôt — dans un dossier dédié `web/` du mono-dépôt. Cet incrément livre :

1. **Le socle (Lot 0)** : agencement + navigation responsive en français ; **communication
   authentifiée** avec l'API (intercepteur qui porte le jeton) ; **gestion centralisée des erreurs**
   (mapping ProblemDetails + codes métier → messages exploitables) ; **contrôle d'accès à l'affichage**
   selon les droits effectifs (récupérés via `GET /auth/me`) ; **bootstrap de session** et retour à la
   connexion sur refus d'authentification (401).
2. **Le cycle de vie du compte (Lot 1)** : connexion, activation (1re connexion), mot de passe oublié
   + réinitialisation (route publique avec jeton), changement de mot de passe, déconnexion, et
   installation du premier administrateur.

Approche technique :
- **Jeton en mémoire** (service de session, jamais en `localStorage`) → un rechargement complet
  déconnecte (reconnexion simple, **pas de refresh token** dans cet incrément).
- **Après connexion**, appel de `GET /auth/me` pour obtenir **identité + droits effectifs** ; la SPA
  **ne décode jamais** le jeton (cf. feature 007).
- **RBAC d'affichage** via *guards* de routes + masquage conditionnel, l'**API restant l'autorité**
  (toute tentative non autorisée reste refusée côté serveur → 401/403 gérés).
- **Accès API encapsulé** dans des services typés (un service par domaine d'auth), aucune règle métier
  côté client.
- **L'API n'est pas modifiée** : la SPA consomme les endpoints existants
  (`/auth/login|activate|forgot-password|reset-password|change-password|me`, `/setup/first-admin`).

## Technical Context

**Language/Version**: TypeScript ; **Angular** dernière version stable (≥ v19 : composants
**standalone**, **signals**, control flow intégré). Aligné sur la Constitution (« SPA : Angular »).

**Primary Dependencies**: Angular Router (routing + guards), `HttpClient` + `HttpInterceptor`
(Bearer, gestion 401, mapping d'erreurs), Reactive Forms (validation politique de mot de passe). Pas
de librairie d'état lourde : un **service de session** (signals) suffit à cet incrément.

**Storage**: **Aucune persistance** côté client. État de session (identité, droits, jeton) **en
mémoire** uniquement (volatil, purgé au rechargement/déconnexion). Configuration (URL de base de
l'API) via fichiers d'environnement Angular.

**Testing**: **Vitest** (unitaires : services de session/API, mapping d'erreurs, guards, validateurs)
+ **Playwright** (bout en bout : parcours connexion, activation, oublié→reset, changement, garde de
routes, retour connexion sur 401).

**Target Platform**: navigateurs modernes (poste de bureau + tablette), servi en HTTPS ; consomme
l'API .NET Lumineux via `/api/v1`.

**Project Type**: Web (application front distincte du back-end) — **nouveau** projet `web/` dans le
mono-dépôt.

**Performance Goals**: bootstrap et affichage de la console **immédiats** à l'échelle utilisateur ;
un seul appel `GET /auth/me` au démarrage de session pour l'identité/les droits.

**Constraints**: jeton **jamais** dans un stockage exposé XSS (FR-003) ; **aucun secret** affiché ou
persisté (FR-009, SC-005) ; **messages génériques** anti-énumération relayés fidèlement (FR-012/013,
SC-006) ; **RBAC d'affichage = confort**, autorité serveur (FR-005, SC-003) ; validation client
**non autoritaire** (FR-017) ; **français** + responsive (FR-001, SC-008).

**Scale/Scope**: 5 user stories (P1→P3) ; 7 endpoints consommés ; ~6 écrans (connexion, activation,
oublié, réinitialisation, changement, installation) + coquille console. Aucune entité persistée.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

> Les principes I, II et VI sont formulés pour l'**API** ; on applique ici leur **esprit** au front
> (séparation des responsabilités, absence de logique métier côté client, absence de secret journalisé).

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | La SPA **ne porte aucune règle métier** (l'API en est la source). Séparation nette : **composants** (présentation) / **services** (accès API encapsulé, session) / **guards & interceptors** (transverses). Accès API centralisé derrière des services typés. | ✅ PASS (esprit respecté) |
| II. Code-First & intégrité BD | **Sans objet** — la SPA n'a pas de base de données ni de migration. | ✅ N/A |
| III. Tests en premier | Tests unitaires (session, mapping d'erreurs, guards, validateurs) écrits avec l'implémentation ; e2e sur les parcours critiques. Rouge → vert. | ✅ PASS |
| IV. Sécurité par défaut | Jeton **hors `localStorage`** (mémoire) ; **aucun secret** affiché/persisté/loggé ; validation client **indicative** (serveur autoritaire) ; **RBAC d'affichage + autorité serveur** ; anti-énumération relayée ; HTTPS ; entrées utilisateur validées avant envoi. | ✅ PASS |
| V. Contrats d'API explicites & cohérents | Consomme les **contrats versionnés existants** (`/api/v1`) via des **modèles typés** (vues client), sans coupler l'UI aux entités de persistance ; erreurs au format ProblemDetails mappées. | ✅ PASS |
| VI. Traçabilité, audit & observabilité | Les actions sensibles sont journalisées **côté API** (features 003→007). Côté client : journalisation d'erreurs **sans secret** ; corrélation possible via l'API. | ✅ PASS (esprit respecté) |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (dossier `web/`, séparation
composants/services/guards, session en mémoire, RBAC d'affichage + autorité serveur, modèles typés
sur contrats existants) respecte l'ensemble des principes applicables. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/008-spa-foundation-auth/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── api-consumption.md   # endpoints consommés (vue client, req/rép)
│   └── routes.md            # contrat de routage (publiques/protégées, guards)
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — NOUVEAU projet front

```text
web/                                   # application Angular (mono-dépôt)
├── src/
│   ├── app/
│   │   ├── core/                       # transverse
│   │   │   ├── session/                # SessionStore (signals : identité, droits, jeton en mémoire)
│   │   │   ├── http/                   # authTokenInterceptor (Bearer), errorInterceptor (401→login, mapping)
│   │   │   ├── guards/                 # authGuard, permissionGuard, guestOnly, setupGuard
│   │   │   └── api/                    # AuthApi (login/activate/forgot/reset/change/me), SetupApi (first-admin)
│   │   ├── features/
│   │   │   ├── login/                  # écran connexion
│   │   │   ├── activate/               # première connexion / activation
│   │   │   ├── forgot-password/        # demande de réinitialisation (message générique)
│   │   │   ├── reset-password/         # route publique /auth/reset-password?token=…
│   │   │   ├── change-password/        # changement (connecté)
│   │   │   └── setup/                  # installation premier administrateur
│   │   ├── shell/                      # coquille console : layout + navigation adaptée aux droits
│   │   ├── shared/                     # UI communs, validateurs (politique mot de passe), notifications
│   │   └── app.routes.ts               # routes publiques vs protégées (guards)
│   └── environments/                   # apiBaseUrl (dev/prod)
├── e2e/                                # Playwright (parcours critiques)
└── (config Angular / Vitest / Playwright)

# src/ (.NET) et le reste du dépôt : INCHANGÉS (API non modifiée)
```

**Structure Decision**: **Nouvelle application Angular** isolée dans `web/` (mono-dépôt), sans impact
sur la solution .NET (`src/`). L'architecture front sépare **présentation** (features/shell/shared),
**accès API et état** (core/api, core/session) et **transverses** (core/http, core/guards). L'accès
réseau est **encapsulé dans des services typés** (aucun appel HTTP dans les composants), et la SPA ne
contient **aucune règle métier** — cohérent avec l'esprit de la Constitution (I) et sa règle
« autorité serveur » (IV). L'authentification et le RBAC d'affichage reposent sur `GET /auth/me`
(feature 007).

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
