# Feature Specification: Console web — Courbe d'évolution des présences (SPA)

**Feature Branch**: `021-spa-timeseries-chart`

**Created**: 2026-07-06

**Status**: Draft

**Input**: User description: "Courbe d'évolution temporelle au tableau de bord des rapports (019),
consommant l'API série temporelle (020). Panneau « Évolution » avec sélecteur de granularité
(semaine/mois), réutilise période + filtre d'antenne, courbe/aire SVG maison sans dépendance. Droit
manage_attendance."

## Contexte & motivation

L'API de série temporelle (feature 020) fournit l'**évolution** des présences valides par **semaine** ou
**mois** sur une période. Le tableau de bord des rapports (feature 019) présente déjà la **synthèse par
antenne**, le **taux par membre** et l'**export CSV**, mais pas la **tendance dans le temps**.

Ce module ajoute au tableau de bord un **panneau « Évolution »** affichant une **courbe** (tracé SVG
maison, **sans bibliothèque de graphiques**) de l'affluence dans le temps. Il **réutilise** la période
et le filtre d'antenne déjà choisis, ajoute un **sélecteur de granularité** (semaine/mois), et
**présente** les points fournis par l'API 020 (aucun calcul statistique client). Réservé au droit
**`manage_attendance`** (le module Rapports l'est déjà). **L'API n'est pas modifiée.**

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visualiser l'évolution de l'affluence (Priority: P1) 🎯 MVP

En tant que responsable de présence, je veux voir une **courbe** de l'évolution des présences valides
sur la période (par **semaine** ou **mois**), afin d'identifier les tendances d'affluence.

**Why this priority**: C'est le cœur de la feature ; elle transforme la série de l'API 020 en
**visualisation** exploitable, et suffit à elle seule à apporter de la valeur.

**Independent Test**: Se connecter avec le droit `manage_attendance`, ouvrir « Rapports », choisir une
période et une granularité, et vérifier qu'une **courbe** relie les points (présences valides par
intervalle) avec les **libellés d'intervalle** en repères.

**Acceptance Scenarios**:

1. **Given** un utilisateur avec le droit `manage_attendance` sur `/reports`, **When** il sélectionne
   une **granularité** (semaine ou mois), **Then** une **courbe** (tracé reliant les points) de
   l'évolution des **présences valides** par intervalle s'affiche, avec les **libellés d'intervalle**
   (`AAAA-Sww` ou `AAAA-MM`) en repères temporels.
2. **Given** la courbe affichée, **When** l'utilisateur pointe/consulte un **point**, **Then** il peut
   **lire la valeur** (nombre de présences valides) de cet intervalle.
3. **Given** la série **continue** (fournie par l'API), **When** un intervalle est **sans donnée**,
   **Then** la courbe **redescend à 0** pour cet intervalle (pas de rupture).
4. **Given** un utilisateur **sans** le droit `manage_attendance`, **When** il tente d'accéder au module
   Rapports, **Then** l'accès est **refusé** (entrée masquée / 403), donc le panneau n'est pas
   accessible.

---

### User Story 2 - Réagir aux changements de contexte (Priority: P2)

En tant que responsable de présence, je veux que la courbe se **mette à jour** quand je change la
**période**, le **filtre d'antenne** ou la **granularité**, afin d'explorer les tendances sans
recharger.

**Why this priority**: Rend l'exploration fluide ; important mais secondaire par rapport à l'affichage
initial.

**Independent Test**: Modifier la période, l'antenne ou la granularité et vérifier que la courbe se
recalcule (nouvel appel) et reflète le nouveau contexte.

**Acceptance Scenarios**:

1. **Given** une courbe affichée, **When** l'utilisateur change la **période** (ou l'**antenne**) du
   tableau de bord, **Then** la courbe se **met à jour** avec les données de la nouvelle période/antenne.
2. **Given** une courbe en granularité **mois**, **When** l'utilisateur bascule en **semaine**, **Then**
   la courbe se recalcule avec des **intervalles hebdomadaires**.
3. **Given** une **plage invalide** ou un contexte refusé par l'API, **When** la courbe est demandée,
   **Then** un **message clair** est affiché (l'API 020 reste l'autorité de validation).

### Edge Cases

- **Sans droit `manage_attendance`** : le module Rapports est déjà masqué / l'accès direct refusé
  (403 géré) — le panneau n'est pas atteignable.
- **Période sans aucune donnée** : état **vide explicite** (ou courbe entièrement à 0 selon la série).
- **Un seul intervalle** dans la période : afficher un **point** lisible (pas d'échec de tracé).
- **Plage/granularité invalide** : message clair (validation côté API 020 fait autorité).
- **Session expirée (401)** : purge et retour à la connexion (comportement du socle).
- **Indisponibilité momentanée** : message clair sans casser le reste du tableau de bord.

## Requirements *(mandatory)*

### Panneau & courbe (US1)

- **FR-001**: Le tableau de bord Rapports MUST comporter un **panneau « Évolution »** réservé (comme le
  module) au droit **`manage_attendance`** ; l'API reste l'autorité (403 géré).
- **FR-002**: Le panneau MUST offrir un **sélecteur de granularité** (**semaine** / **mois**) ; la
  granularité **« jour » n'est pas** proposée (non fournie par l'API 020).
- **FR-003**: Le panneau MUST afficher une **courbe** (tracé reliant les points, option **aire**) de
  l'évolution des **présences valides** par intervalle sur la période, avec les **libellés
  d'intervalle** (`AAAA-Sww` / `AAAA-MM`) en repères temporels.
- **FR-004**: L'utilisateur MUST pouvoir **lire la valeur** d'un intervalle (nombre de présences
  valides) via une **info-bulle** ou une **étiquette**.
- **FR-005**: La courbe MUST refléter la **série continue** de l'API : un intervalle sans donnée
  apparaît à **0** (la courbe redescend, pas de rupture).

### Contexte & mise à jour (US2)

- **FR-006**: Le panneau MUST **réutiliser** la **période** et le **filtre d'antenne** déjà choisis dans
  le tableau de bord (pas de nouvelle sélection de dates).
- **FR-007**: La courbe MUST se **mettre à jour** lorsque la **période**, le **filtre d'antenne** ou la
  **granularité** changent.

### Présentation & transverses

- **FR-008**: Aucun **calcul statistique** ne MUST être dupliqué côté client : les points (présences
  valides par intervalle) proviennent de l'API 020 ; le client ne fait que **mettre à l'échelle** les
  coordonnées de tracé (proportionnelles aux valeurs) et **tracer**.
- **FR-009**: L'application MUST **mapper** les erreurs de l'API en messages exploitables (validation de
  plage/granularité, non autorisé) sans détail technique ; états **chargement** et **vide** explicites.
- **FR-010**: Le panneau MUST être en **français** et **responsive** (poste de bureau et tablette) ;
  aucun secret ne MUST être journalisé.

### Key Entities *(include if feature involves data)*

- **Point d'évolution (vue)** : début d'intervalle, libellé (`AAAA-Sww` / `AAAA-MM`), nombre de
  présences valides, nombre de sessions. Fourni par l'API 020.
- **Série d'évolution (vue)** : plage (début/fin), granularité, éventuelle antenne, et la **suite
  ordonnée** de points (continue). Fournie par l'API 020.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un responsable obtient une **courbe** d'évolution (semaine ou mois) pour une période en
  **moins de 1 minute** après connexion.
- **SC-002**: La **géométrie** de la courbe reflète fidèlement les valeurs de l'API : la position
  verticale d'un point est **proportionnelle** à son nombre de présences valides (aucune valeur inventée
  côté client).
- **SC-003**: Un intervalle **sans donnée** apparaît à **0** sur la courbe (série continue respectée)
  dans **100 %** des cas.
- **SC-004**: Un **changement** de période, d'antenne ou de granularité met à jour la courbe dans
  **100 %** des cas.
- **SC-005**: Dans **100 %** des cas, le panneau « Évolution » est **inaccessible** à un utilisateur
  sans le droit `manage_attendance` (module déjà gardé).
- **SC-006**: Chaque **valeur d'intervalle** est **lisible** par l'utilisateur (info-bulle/étiquette).

## Assumptions

- **Socle & tableau de bord réutilisés** (features 008 & 019) : session, intercepteurs, gardes RBAC,
  mapping d'erreurs, notifications ; **période** et **filtre d'antenne** déjà présents ; route `/reports`
  déjà gardée `manage_attendance`. **API 020 inchangée.**
- **Visualisation** : **courbe/aire SVG maison** (polyline), **sans** bibliothèque de graphiques ni
  dépendance npm (décidé le 2026-07-06).
- **Granularités** : **semaine** (ISO 8601) et **mois** — celles fournies par l'API 020 ; « jour »
  hors périmètre.
- **Aucun calcul client** : les points viennent de l'API ; le client met à l'échelle et trace.
- **Hors périmètre** : toute modification d'API ; la granularité « jour » ; l'**export PDF** ; l'usage
  d'une bibliothèque de graphiques externe ; toute statistique non fournie par 020.
