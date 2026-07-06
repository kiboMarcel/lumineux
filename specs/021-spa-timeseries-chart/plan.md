# Implementation Plan: Console web — Courbe d'évolution des présences (SPA)

**Branch**: `021-spa-timeseries-chart` | **Date**: 2026-07-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/021-spa-timeseries-chart/spec.md`

## Summary

Ajouter au tableau de bord Rapports (feature 019, `web/`) un **panneau « Évolution »** : une **courbe/
aire SVG maison** (polyline, **sans dépendance**) de l'affluence (présences valides) par intervalle
(**semaine ISO** / **mois**), avec **sélecteur de granularité**, **réutilisant** la période et le filtre
d'antenne du tableau de bord. Consomme l'**API 020** (`time-series`). Réservé au droit
**`manage_attendance`** (module déjà gardé). **L'API n'est pas modifiée. Aucune dépendance npm.**

Décisions structurantes (spec) :
- **Courbe/aire SVG** : coordonnées **mises à l'échelle** des valeurs API (x = intervalle, y ∝ présences
  valides) ; libellés d'intervalle en repères ; **lecture de valeur** par point (info-bulle).
- **Aucun calcul statistique client** : les points viennent de l'API 020 ; le client trace seulement.
- **Série continue** : intervalle sans donnée → **0** (la courbe redescend).
- **Mise à jour** sur changement de **période / antenne / granularité**.

## Technical Context

**Language/Version**: TypeScript ; **Angular 20** (app `web/` existante — standalone, signals).

**Primary Dependencies**: socle feature 008 (`SessionStore`, intercepteurs, `permissionGuard`,
`messageForError`, notifications) ; tableau de bord 019 (`reports-dashboard` : période + filtre antenne) ;
`ReportsApi` (019, étendu) ; Angular Router, `HttpClient`, Forms/Signals. **Aucune dépendance npm
nouvelle** (courbe en SVG maison).

**Storage**: aucune persistance client (état de vue transitoire).

**Testing**: **Vitest** (unitaires : `ReportsApi.timeSeries` ; composant courbe — génération des points
SVG **proportionnels** aux valeurs, série continue à 0, sélecteur de granularité, réaction aux
changements de contexte, état vide/erreur mappée) + **Playwright** (e2e : courbe affichée, bascule
semaine/mois).

**Target Platform**: navigateurs modernes (bureau + tablette), HTTPS.

**Project Type**: Web (extension de l'app `web/`). L'API n'est pas modifiée.

**Performance Goals**: rendu immédiat (tracé SVG trivial) ; un appel API par changement de contexte.

**Constraints**: droit **`manage_attendance`** (module déjà gardé) ; **aucun calcul statistique client** ;
**pas de dépendance graphique** ; erreurs mappées ; **français** + responsive ; aucun secret journalisé.

**Scale/Scope**: 2 user stories ; **1 composant** (panneau courbe) intégré au tableau de bord + 1 méthode
de service.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

> Principes I/VI appliqués dans leur **esprit** au front (séparation composant/service, aucun calcul
> métier client, aucun secret journalisé).

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Accès API encapsulé (`core/api/reports-api.ts`) ; composant de présentation (`features/reports`) ; **aucun calcul statistique client** (tracé seul). | ✅ PASS (esprit) |
| II. Code-First & intégrité BD | **Sans objet** (pas de base côté SPA ; API 020 inchangée). | ✅ N/A |
| III. Tests en premier | Unitaires (service, tracé proportionnel, série continue, granularité, réactivité, erreurs) + e2e. Rouge → vert. | ✅ PASS |
| IV. Sécurité par défaut | Module gardé `manage_attendance` (existant) + **autorité serveur** (403 géré) ; 401 → purge/reconnexion. | ✅ PASS |
| V. Contrats d'API explicites | Consomme le **contrat versionné** 020 (`time-series`) via **modèles typés** ; erreurs mappées. | ✅ PASS |
| VI. Traçabilité & observabilité | Accès journalisé **côté API** ; côté client, aucun secret loggé. | ✅ PASS (esprit) |

**Résultat initial : PASS** — aucune violation, Complexity Tracking non requis.

*Re-check post-conception (Phase 1)* : la conception (service étendu, composant de tracé SVG proportionnel,
réactivité au contexte) respecte les principes applicables. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/021-spa-timeseries-chart/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── api-consumption.md   # endpoint time-series (020) consommé (vue client)
│   └── routes.md            # aucune nouvelle route ; panneau intégré à /reports
└── checklists/requirements.md
```

### Source Code (repository root) — extension de l'app front `web/`

```text
web/src/app/
├── core/api/
│   └── reports-api.ts               # + timeSeries(from, to, granularity, antennaId?) (API 020)
├── features/reports/
│   ├── report.models.ts             # + TimeSeriesGranularity, TimeSeriesPoint, AttendanceTimeSeriesResponse
│   ├── reports-dashboard/           # expose le contexte appliqué (période + antenne) au panneau courbe
│   └── time-series-chart/           # NOUVEAU panneau : sélecteur granularité + courbe/aire SVG
└── (route /reports inchangée ; nav « Rapports » inchangée)

# API (src/) : INCHANGÉE
```

**Structure Decision**: Extension de l'app Angular existante. Le service `ReportsApi` (019) reçoit une
méthode `timeSeries`. Un **nouveau composant** `time-series-chart` (panneau « Évolution ») reçoit en
entrées la **période appliquée** et le **filtre d'antenne** du tableau de bord, porte son propre
**sélecteur de granularité**, appelle `ReportsApi.timeSeries` et **trace une courbe/aire SVG** dont les
coordonnées sont **mises à l'échelle** des valeurs (y ∝ présences valides). Il **réagit** aux
changements de période/antenne/granularité (effet sur les entrées/signal). Le tableau de bord
(`reports-dashboard`) intègre ce panneau et lui fournit le contexte (période validée + antenne). Aucune
règle métier ni recomputation statistique côté client ; aucune nouvelle route.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
