# Implementation Plan: Console web — Export PDF des rapports (impression navigateur)

**Branch**: `022-spa-reports-pdf-print` | **Date**: 2026-07-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/022-spa-reports-pdf-print/spec.md`

## Summary

Ajouter au tableau de bord Rapports (features 019/021, `web/`) un **export PDF par impression
navigateur** : un bouton **« Exporter en PDF »** déclenche `window.print()` ; une **feuille de style
`@media print`** produit une mise en page épurée du document (en-tête période/antenne/date, synthèse
par antenne, courbe d'évolution, taux membre si affiché) en **masquant** la navigation, les boutons et
les champs. **Aucune dépendance npm, aucun appel réseau, aucune modification d'API.** Réservé au droit
**`manage_attendance`** (module déjà gardé).

Décisions structurantes (spec) :
- **Impression navigateur** (`window.print` + `@media print`) — l'utilisateur choisit « Enregistrer en
  PDF » dans le dialogue.
- **En-tête d'impression** (période, filtre d'antenne, date de génération) ajouté au tableau de bord,
  **visible seulement à l'impression**.
- **Masquage** en impression des éléments interactifs (nav du shell, boutons `.lx-btn`, champs de
  formulaire) via les **styles globaux**.
- **Bloc taux membre** présent seulement si un membre est affiché (déjà conditionnel côté 019).

## Technical Context

**Language/Version**: TypeScript ; **Angular 20** (app `web/` existante) ; **CSS** (`@media print`).

**Primary Dependencies**: tableau de bord Rapports (019/021 : synthèse, courbe, taux membre, période,
filtre d'antenne) ; socle 008 (session, garde de route). **Aucune dépendance npm nouvelle.**

**Storage**: aucune persistance ; aucune donnée nouvelle (réutilise l'état déjà chargé).

**Testing**: **Vitest** (unitaire : `exportPdf()` déclenche `window.print` ; présence et contenu de
l'en-tête d'impression — période/antenne/date ; bloc taux conditionnel) + **Playwright** (e2e :
`emulateMedia({ media: 'print' })` — en-tête visible, navigation/boutons masqués).

**Target Platform**: navigateurs modernes (bureau ; impression → PDF), HTTPS.

**Project Type**: Web (extension de l'app `web/`). L'API n'est pas modifiée.

**Performance Goals**: instantané (impression locale ; aucun calcul/réseau).

**Constraints**: droit **`manage_attendance`** (module déjà gardé) ; **aucune dépendance / appel réseau /
modif API** ; **français** ; document lisible et épuré à l'impression ; aucun secret journalisé.

**Scale/Scope**: 2 user stories ; 1 bouton + 1 en-tête d'impression dans le tableau de bord + règles
`@media print` globales.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

> Principes I/VI appliqués dans leur **esprit** au front (présentation seule, aucun secret journalisé).

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Simple **présentation** (bouton + styles d'impression) ; aucune règle métier ; aucune donnée nouvelle. | ✅ PASS (esprit) |
| II. Code-First & intégrité BD | **Sans objet** (pas de base ; API inchangée). | ✅ N/A |
| III. Tests en premier | Unitaire (`window.print`, en-tête, bloc conditionnel) + e2e (media print). Rouge → vert. | ✅ PASS |
| IV. Sécurité par défaut | Module gardé `manage_attendance` (existant) ; l'API reste l'autorité (403) ; aucune fuite. | ✅ PASS |
| V. Contrats d'API explicites | **Aucun appel API** (impression du contenu déjà chargé). | ✅ N/A |
| VI. Traçabilité & observabilité | Aucun secret loggé ; opération purement locale. | ✅ PASS (esprit) |

**Résultat initial : PASS** — aucune violation, Complexity Tracking non requis.

*Re-check post-conception (Phase 1)* : la conception (bouton `window.print`, en-tête print-only, styles
`@media print` de masquage) respecte les principes applicables. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/022-spa-reports-pdf-print/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   └── print-layout.md      # contrat de la mise en page imprimable (contenu, éléments masqués)
└── checklists/requirements.md
```

### Source Code (repository root) — extension de l'app front `web/`

```text
web/src/
├── styles.css                       # + bloc @media print : masquer nav/boutons/champs, montrer print-only
└── app/features/reports/
    └── reports-dashboard/
        └── reports-dashboard.component.ts   # + bouton « Exporter en PDF » (window.print) + en-tête print-only

# Aucune nouvelle route / entrée de nav ; API (src/) INCHANGÉE
```

**Structure Decision**: Extension **minimale** du tableau de bord existant. Le bouton **« Exporter en
PDF »** appelle `window.print()`. Un **en-tête d'impression** (période, filtre d'antenne, date de
génération) est ajouté au `reports-dashboard`, **masqué à l'écran** et **visible à l'impression** (classe
utilitaire dédiée). Les règles **`@media print`** globales (`styles.css`) **masquent** la barre de
navigation (shell), les **boutons** (`.lx-btn`) et les **champs de formulaire**, ne laissant que le
contenu des rapports (tableau + barres + courbe + taux membre conditionnel). Aucune donnée n'est
rechargée ; le document reflète l'état affiché (WYSIWYG).

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
