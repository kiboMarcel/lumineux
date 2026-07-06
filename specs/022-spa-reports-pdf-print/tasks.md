# Tasks: Console web — Export PDF des rapports (impression navigateur)

**Input**: Design documents from `specs/022-spa-reports-pdf-print/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — Constitution Lumineux, Principe III (NON-NÉGOCIABLE). Unitaire **Vitest**
(`window.print` déclenché, en-tête d'impression) + e2e **Playwright** (émulation `media: print`).

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- Chemins relatifs à la racine du dépôt (app front sous `web/`)

## Path Conventions

- App Angular : `web/src/app/` ; styles globaux : `web/src/styles.css` ; e2e : `web/e2e/`

---

## Phase 1: Foundational (Blocking Prerequisites)

**Purpose**: Feuille de style d'impression globale — requise par les deux stories.

**⚠️ CRITICAL**: aucune story ne démarre avant la fin de cette phase.

- [X] T001 Ajouter à `web/src/styles.css` un bloc **`@media print`** : masquer la navigation du shell (`.lx-nav`, `.lx-user`), les **boutons** (`.lx-btn`) et les **champs** (`input`, `select`) ; règle utilitaire `.lx-print-only` (masquée à l'écran, **affichée** à l'impression)

**Checkpoint**: styles d'impression en place.

---

## Phase 2: User Story 1 - Exporter le rapport en PDF (Priority: P1) 🎯 MVP

**Goal**: Bouton « Exporter en PDF » déclenchant l'impression, avec en-tête (période/antenne/date) et le
contenu des rapports.

**Independent Test**: Sur `/reports`, cliquer « Exporter en PDF » → `window.print` appelé ; l'en-tête
d'impression contient période, antenne et date ; la synthèse et la courbe sont présentes.

### Tests (US1)

- [X] T002 [P] [US1] Test dans `web/src/app/features/reports/reports-dashboard/reports-dashboard.component.spec.ts` : `exportPdf()` appelle **`window.print`** (spy) ; l'**en-tête d'impression** expose la **période** (from/to), le **libellé d'antenne** (ou « Toutes ») et une **date de génération** (doit ÉCHOUER)

### Implémentation (US1)

- [X] T003 [US1] Étendre `web/src/app/features/reports/reports-dashboard/reports-dashboard.component.ts` : bouton **« Exporter en PDF »** → `exportPdf()` (appelle `window.print()`) ; **en-tête d'impression** `.lx-print-only` (période, antenne filtrée→libellé ou « Toutes », date de génération) ; le bloc **taux membre** reste conditionnel (déjà géré par `member-rate`)

**Checkpoint**: US1 fonctionnelle et testable seule (MVP livrable).

---

## Phase 3: User Story 2 - Mise en page imprimable propre (Priority: P2)

**Goal**: Document lisible et épuré (nav/boutons/champs masqués), sans troncature.

**Independent Test**: En `media: print`, la navigation, les boutons et les champs sont masqués ;
l'en-tête et le contenu des rapports restent visibles et lisibles.

### Tests (US2)

- [X] T004 [P] [US2] E2e Playwright `web/e2e/reports-print.spec.ts` : `page.emulateMedia({ media: 'print' })` → **navigation/boutons masqués**, **en-tête d'impression visible**, contenu des rapports présent (doit ÉCHOUER sans le CSS/impl)

### Implémentation (US2)

- [X] T005 [US2] Compléter `web/src/styles.css` (`@media print`) pour la **lisibilité** : largeur pleine des tableaux/SVG, `page-break-inside: avoid` sur les cartes (`.lx-card`)/lignes de tableau, couleurs lisibles ; retirer les ombres/fonds superflus

**Checkpoint**: US1 + US2 opérationnelles.

---

## Phase 4: Polish & Cross-Cutting Concerns

- [X] T006 Exécuter `ng test --no-watch` (unitaires au vert) et dérouler `specs/022-spa-reports-pdf-print/quickstart.md` (A→C, SC-001..006) ; marquer les tâches `[X]`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (P1)** : styles d'impression — **BLOQUE** les stories.
- **US1 (P2)** → **US2 (P3)** : US2 affine la mise en page introduite par US1 (même styles.css) →
  séquentiel ; indépendamment testables.
- **Polish (P4)** : après les stories.

### User Story Dependencies

- **US1 (P1)** : styles print + tableau de bord 019/021. **MVP**.
- **US2 (P2)** : affine `styles.css` (lisibilité/pagination).

### Parallel Opportunities

- Peu de parallélisme (feature légère) : T002 (test) parallèle à la rédaction ; T004 (e2e) après impl.

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 Foundational → 2. Phase 2 US1 → **VALIDER** (bouton + impression + en-tête) → démo.

### Incremental Delivery

Foundational → US1 (export, MVP) → US2 (mise en page propre) → Polish.

---

## Notes

- **Aucune dépendance npm**, **aucun appel réseau**, **aucune modification d'API** : impression locale du
  DOM courant (WYSIWYG).
- L'export réutilise l'**état déjà chargé** du tableau de bord (synthèse 019, courbe 021, taux membre).
- Réservé `manage_attendance` via la garde existante — aucune nouvelle route/nav.
- Commits après chaque tâche ou groupe logique.
