# Implementation Plan: Console web — Gestion des membres (SPA, Lot 2)

**Branch**: `009-spa-members-management` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/009-spa-members-management/spec.md`

## Summary

Ajouter le **module « Membres »** à la console web **Angular** existante (`web/`, feature 008) :
**rechercher/lister** (paginé), **consulter**, **enrôler** (avec homonymie confirmable + remise
d'identifiants) et **corriger** les membres. Le module consomme les endpoints **existants** :

- **Membres** (feature 002) : `GET /members` (recherche paginée), `GET /members/{id}`,
  `POST /members`, `PUT /members/{id}` — tous protégés par le droit **`manage_members`**.
- **Référentiels** (feature 010) : `GET /reference/{antennas|civilities|cities|districts|countries}`
  pour peupler les **listes déroulantes** (l'antenne d'origine, requise, est **sélectionnée** dans une
  liste — FR-016/017).

Points de conception clés (fidèles à l'API réelle) :
- **Homonymie** (création) : un `409 code="duplicate_name"` déclenche une **confirmation** ; le réessai
  renvoie `ConfirmDuplicate=true`. **Conflit de contact** : `409 code="contact_in_use"` = **erreur
  bloquante** non confirmable. La **correction** (`PUT`) n'accepte **pas** de confirmation et ne
  contrôle **que** l'unicité du contact (`contact_in_use`) — l'homonymie est donc **propre à la
  création** (le formulaire d'édition ne déclenche pas de confirmation d'homonyme).
- **Remise des identifiants** (création) : la réponse expose `CredentialsDelivery` (EmailSent |
  BureauHandout) ; le **mot de passe temporaire** (BureauHandout) est **affiché une seule fois** en
  mémoire, **jamais persisté ni ré-affiché** (FR-010).
- **RBAC** : entrée de navigation et routes du module gardées par **`permissionGuard('manage_members')`**
  (feature 008) ; l'API reste l'autorité (403 géré).

Aucune modification de l'API. Aucun état persistant nouveau côté client.

## Technical Context

**Language/Version**: TypeScript ; **Angular 20** (application `web/` existante — standalone, signals,
control flow).

**Primary Dependencies**: socle feature 008 (`SessionStore`, intercepteurs Bearer/erreurs, gardes,
`messageForError`, notifications), Angular Router, `HttpClient`, Reactive Forms. **Réutilise** les
patterns `core/api` (services typés) et `features/` (composants d'écran).

**Storage**: aucune persistance client. Données de formulaire transitoires ; mot de passe temporaire
**en mémoire, une seule fois**.

**Testing**: **Vitest** (unitaires : `MembersApi`, `ReferenceApi`, composants liste/détail/formulaire,
flux homonymie, conflit contact, affichage unique du mot de passe temporaire) + **Playwright** (e2e :
recherche→fiche, création avec confirmation d'homonyme, conflit contact bloquant, correction).

**Target Platform**: navigateurs modernes (bureau + tablette), HTTPS ; consomme l'API `/api/v1`.

**Project Type**: Web (extension de l'application front `web/`). L'API n'est pas modifiée.

**Performance Goals**: recherche paginée réactive ; un chargement de référentiels par formulaire
(listes mises en cache le temps de la session de l'écran).

**Constraints**: droit **`manage_members`** requis (affichage + garde) ; **aucun secret persisté**
(mot de passe temporaire une fois — FR-010, SC-005) ; erreurs mappées (400 par champ, 404, 409
`duplicate_name`/`contact_in_use`) ; antenne **sélectionnée dans une liste** (FR-017) ; **français** +
**responsive**.

**Scale/Scope**: 3 user stories (P1 recherche/consultation, P2 création, P3 correction) ; ~4 écrans
(liste, fiche, création, édition) + 2 services API + intégration navigation.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

> Principes I/VI appliqués dans leur **esprit** au front (séparation composants/services, aucune règle
> métier côté client, aucun secret journalisé) — cadre déjà établi en feature 008.

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Aucune règle métier côté client (l'API tranche) ; **accès API encapsulé** dans `core/api` (`MembersApi`, `ReferenceApi`), composants de présentation dans `features/members`, gardes transverses. | ✅ PASS (esprit) |
| II. Code-First & intégrité BD | **Sans objet** (pas de base côté SPA ; API inchangée). | ✅ N/A |
| III. Tests en premier | Unitaires (services, composants, flux homonymie/conflit, secret unique) + e2e (parcours). Rouge → vert. | ✅ PASS |
| IV. Sécurité par défaut | Garde `manage_members` + **autorité serveur** (403 géré) ; **aucun secret persisté** (mot de passe temporaire une seule fois) ; validation client **indicative** ; anti-fuite (pas de secret en URL/journaux). | ✅ PASS |
| V. Contrats d'API explicites | Consomme les **contrats versionnés existants** (membres 002, référentiels 010) via **modèles typés** ; erreurs ProblemDetails + `code` mappées. | ✅ PASS |
| VI. Traçabilité & observabilité | Opérations sensibles journalisées **côté API** ; côté client, aucun secret loggé. | ✅ PASS (esprit) |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (services `core/api` typés, composants
`features/members`, gardes de droits réutilisées, mot de passe temporaire éphémère) respecte les
principes applicables. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/009-spa-members-management/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── api-consumption.md   # endpoints membres + référentiels (vue client, req/rép, codes 409)
│   └── routes.md            # routes du module (gardes manage_members)
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de l'app front `web/`

```text
web/src/app/
├── core/api/
│   ├── members-api.ts        # search(query,page,pageSize) · get(id) · create(body) · update(id,body)
│   └── reference-api.ts      # antennas() · civilities() · cities() · districts() · countries()
├── features/members/
│   ├── member-list/          # recherche + liste paginée (US1)
│   ├── member-detail/        # fiche (US1)
│   └── member-form/          # formulaire partagé création/édition (US2/US3)
│                             #   + panneau « remise identifiants » (une seule fois)
├── shell/                    # remplacer le placeholder « Membres » par un lien réel (garde droit)
└── app.routes.ts             # routes protégées /members (authGuard + permissionGuard)

# API (src/) et le reste : INCHANGÉS
```

**Structure Decision**: Extension de l'application Angular existante (feature 008), **sans impact
API**. L'accès réseau reste **encapsulé dans `core/api`** (deux services typés : membres et
référentiels), les écrans dans `features/members`, la navigation gardée par
`permissionGuard('manage_members')`. Un **formulaire partagé** sert création et édition (édition :
référence en lecture seule, pas de confirmation d'homonyme — cf. Summary). Le mot de passe temporaire
de la remise bureau est un **état de vue éphémère** (jamais persisté).

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
