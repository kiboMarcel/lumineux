# Feature Specification: Console web — Export PDF des rapports (impression navigateur)

**Feature Branch**: `022-spa-reports-pdf-print`

**Created**: 2026-07-06

**Status**: Draft

**Input**: User description: "Export PDF des rapports par impression navigateur (window.print + mise en
page @media print) du tableau de bord (019/021). Contenu : en-tête (période, antenne, date), synthèse
par antenne (tableau + barres), courbe d'évolution, taux membre si affiché. Sans dépendance, sans modif
API. Droit manage_attendance."

## Contexte & motivation

Le tableau de bord des rapports (features 019/021) présente la **synthèse par antenne** (tableau +
barres), le **taux d'assiduité d'un membre** et la **courbe d'évolution**. L'export **CSV** existe déjà
(données de la synthèse), mais il manque un **document imprimable/partageable** reprenant l'ensemble mis
en forme.

Cette feature ajoute un **export PDF** obtenu par **impression navigateur** : un bouton **« Exporter en
PDF »** déclenche l'**impression** d'une **mise en page dédiée** (via `@media print`) du tableau de bord.
L'utilisateur choisit **« Enregistrer en PDF »** dans le dialogue d'impression. **Aucune dépendance,
aucun appel réseau supplémentaire, aucune modification d'API.** Réservé au droit **`manage_attendance`**
(le module Rapports l'est déjà).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Exporter le rapport en PDF (Priority: P1) 🎯 MVP

En tant que responsable de présence, je veux **exporter en PDF** le rapport affiché (synthèse, courbe,
et taux du membre le cas échéant), afin de l'archiver ou le partager.

**Why this priority**: C'est le cœur de la feature ; sans le déclencheur et la mise en page imprimable,
aucun document n'est produit. Livrée seule, elle rend le rapport partageable.

**Independent Test**: Sur `/reports`, cliquer **« Exporter en PDF »** et vérifier que le **dialogue
d'impression** s'ouvre avec une **mise en page** contenant l'**en-tête** (période, antenne, date), la
**synthèse par antenne** (tableau + barres) et la **courbe d'évolution**.

**Acceptance Scenarios**:

1. **Given** un rapport affiché (période choisie), **When** l'utilisateur clique **« Exporter en
   PDF »**, **Then** le **dialogue d'impression** du navigateur s'ouvre (permettant « Enregistrer en
   PDF »).
2. **Given** l'aperçu d'impression, **When** on l'examine, **Then** il présente un **en-tête** (période,
   **filtre d'antenne** courant, **date de génération**), la **synthèse par antenne** (tableau + barres)
   et la **courbe d'évolution** telles qu'affichées.
3. **Given** un **membre** dont le **taux** est affiché, **When** on exporte, **Then** le **bloc taux du
   membre** figure dans le document ; **sinon** ce bloc **n'apparaît pas**.
4. **Given** un utilisateur **sans** le droit `manage_attendance`, **When** il accède aux rapports,
   **Then** le module (et donc l'export) est **inaccessible** (entrée masquée / 403).

---

### User Story 2 - Mise en page imprimable propre (Priority: P2)

En tant que responsable de présence, je veux que le document imprimé soit **lisible** et **épuré**
(sans la navigation ni les commandes interactives), afin d'obtenir un PDF présentable.

**Why this priority**: Améliore la qualité du livrable ; important mais secondaire par rapport à
l'existence de l'export.

**Independent Test**: En mode impression, vérifier que la **navigation**, les **boutons** et les
**champs de formulaire** ne sont **pas** rendus, et que le contenu (en-tête + rapports) reste lisible.

**Acceptance Scenarios**:

1. **Given** l'aperçu d'impression, **When** on l'examine, **Then** la **barre de navigation**, les
   **boutons** (Afficher, Exporter, granularité…) et les **champs de saisie** sont **masqués**.
2. **Given** le document imprimé, **When** on le lit, **Then** le contenu est **lisible** (contraste
   suffisant, pas de troncature ; les tableaux/graphiques ne débordent pas de la page).

### Edge Cases

- **Sans droit `manage_attendance`** : module Rapports déjà masqué / accès refusé — l'export n'est pas
  atteignable.
- **Aucune donnée** sur la période : le document reflète l'**état vide** (message explicite) sans
  planter l'impression.
- **Aucun membre sélectionné** : le bloc **taux membre** est **absent** du document (pas de section
  vide).
- **Blocage du dialogue d'impression** par le navigateur (rare) : le bouton reste sans effet visible
  sans erreur bloquante.
- **Impression multi-pages** : le contenu se répartit proprement (pas de coupe illisible d'un tableau
  entre deux pages, dans la mesure du possible).

## Requirements *(mandatory)*

### Export & contenu (US1)

- **FR-001**: Le tableau de bord Rapports MUST comporter un bouton **« Exporter en PDF »**, réservé
  (comme le module) au droit **`manage_attendance`** ; l'accès reste sous l'autorité de l'API (403 géré
  pour les données).
- **FR-002**: Le bouton MUST **déclencher l'impression** du navigateur (dialogue permettant « Enregistrer
  en PDF »).
- **FR-003**: La mise en page imprimable MUST comporter un **en-tête** indiquant la **période** (début/
  fin), le **filtre d'antenne** courant (ou « Toutes ») et la **date de génération**.
- **FR-004**: Le document MUST inclure la **synthèse par antenne** (tableau : sessions, présences
  valides, moyenne ; + barres comparatives) pour le contexte courant.
- **FR-005**: Le document MUST inclure la **courbe d'évolution** telle qu'affichée (granularité
  courante).
- **FR-006**: Le document MUST inclure le **bloc taux d'un membre** **uniquement si** un membre est
  affiché ; sinon ce bloc MUST être **omis**.

### Mise en page (US2)

- **FR-007**: En impression, les éléments **interactifs non pertinents** (barre de navigation, boutons,
  champs de formulaire) MUST être **masqués**.
- **FR-008**: Le contenu imprimé MUST rester **lisible** (contraste, absence de troncature évitable ;
  tableaux/graphiques adaptés à la largeur de page).

### Transverses

- **FR-009**: La feature MUST **n'ajouter aucune dépendance** ni **appel réseau** supplémentaire, et MUST
  **ne pas modifier l'API** (données déjà chargées par le tableau de bord).
- **FR-010**: Le document et l'interface MUST être en **français** ; aucun secret ne MUST être
  journalisé.

### Key Entities *(include if feature involves data)*

- **Contexte de rapport (vue)** : période (début/fin), filtre d'antenne, granularité, membre affiché le
  cas échéant — repris de l'état du tableau de bord (features 019/021). Aucune nouvelle donnée.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un responsable déclenche l'export en **1 clic** (ouverture du dialogue d'impression).
- **SC-002**: Le document produit **reflète fidèlement** les chiffres et graphiques **affichés** (mêmes
  valeurs que le tableau de bord — WYSIWYG).
- **SC-003**: L'**en-tête** (période, antenne, date de génération) est présent dans **100 %** des
  exports.
- **SC-004**: En impression, **100 %** des éléments interactifs (navigation, boutons, champs) sont
  **masqués**.
- **SC-005**: Le **bloc taux membre** est présent **si et seulement si** un membre est affiché.
- **SC-006**: L'export est **inaccessible** à un utilisateur sans le droit `manage_attendance` (module
  déjà gardé) dans **100 %** des cas.

## Assumptions

- **Tableau de bord réutilisé** (features 019/021) : synthèse, courbe, taux membre, période et filtre
  d'antenne déjà présents et chargés. **API inchangée** (aucune requête supplémentaire pour l'export).
- **Impression navigateur** : `window.print` + feuille de style `@media print` ; l'utilisateur choisit
  « Enregistrer en PDF » dans le dialogue (comportement standard des navigateurs). **Aucune dépendance
  npm.**
- **Format** : format/orientation par défaut du navigateur (A4 portrait usuel) ; l'utilisateur peut
  ajuster dans le dialogue.
- **Navigation** : aucune nouvelle route ni entrée de menu ; le bouton vit dans le tableau de bord.
- **Hors périmètre** : génération PDF **côté serveur** (API + bibliothèque) ; bibliothèque JS de
  génération PDF (jsPDF/html2canvas) ; personnalisation avancée de la mise en page (en-têtes/pieds de
  page récurrents, pagination fine) au-delà d'une impression propre ; tout nouveau calcul ou statistique.
