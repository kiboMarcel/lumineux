# Feature Specification: API de série temporelle des présences

**Feature Branch**: `020-attendance-time-series`

**Created**: 2026-07-06

**Status**: Draft

**Input**: User description: "Série temporelle des présences (évolution) — couche analytique lecture
seule sur 001, complétant les rapports (018). Granularités semaine et mois. API d'agrégation d'abord
(courbe SPA ultérieure). Droit manage_attendance réutilisé."

## Contexte & motivation

Les rapports de présence (feature 018) fournissent une **photographie** par antenne sur une période
(synthèse, taux par membre). Il manque la **dimension temporelle** : voir **l'évolution** de
l'affluence dans le temps (tendance à la hausse/baisse) pour piloter l'activité.

Cette feature ajoute une **API de série temporelle** en **lecture seule** : les présences valides
agrégées par **intervalle de temps** (**semaine** ou **mois**) sur une période, éventuellement filtrées
par **antenne**. Elle **complète** l'API 018 (mêmes principes, mêmes données) et alimentera une
**courbe d'évolution** dans le tableau de bord SPA (feature ultérieure). **Aucune écriture, aucune
migration.** Accès réservé au droit **`manage_attendance`**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Évolution de l'affluence par intervalle (Priority: P1) 🎯 MVP

En tant que responsable de présence, je veux voir l'**évolution** des présences valides **par semaine
ou par mois** sur une période, afin d'observer les tendances d'affluence.

**Why this priority**: C'est le cœur de la feature ; elle transforme la collecte en **tendance
temporelle**, exploitable telle quelle (et par une future courbe).

**Independent Test**: Demander la série temporelle pour une **période** et une **granularité**
(semaine/mois), et vérifier qu'on obtient une **suite d'intervalles** ordonnés, chacun avec son
**nombre de présences valides** (et son nombre de sessions).

**Acceptance Scenarios**:

1. **Given** des présences sur une période, **When** l'utilisateur demande la série en granularité
   **mois**, **Then** il obtient **un point par mois** de la période (début d'intervalle + libellé),
   chacun avec le **nombre de présences valides** et le **nombre de sessions**.
2. **Given** la granularité **semaine**, **When** la série est demandée, **Then** il obtient **un point
   par semaine** (semaines ISO, débutant le lundi).
3. **Given** un intervalle **sans aucune présence**, **When** la série est calculée, **Then** cet
   intervalle apparaît **avec une valeur de 0** (série **continue**, pas de trou).
4. **Given** des présences **annulées**, **When** la série est calculée, **Then** elles **ne sont pas
   comptées** (seules les présences **valides**).
5. **Given** un utilisateur **sans** le droit `manage_attendance`, **When** il demande la série,
   **Then** l'accès est **refusé** (401/403).

---

### User Story 2 - Filtrer l'évolution par antenne (Priority: P2)

En tant que responsable de présence, je veux **filtrer** l'évolution sur une **antenne** précise, afin
de suivre la tendance d'une antenne donnée.

**Why this priority**: Complète la vue globale par une vue ciblée ; utile mais secondaire.

**Independent Test**: Demander la série pour une antenne donnée et vérifier que les valeurs ne
concernent que cette antenne, sur les mêmes intervalles.

**Acceptance Scenarios**:

1. **Given** plusieurs antennes, **When** l'utilisateur demande la série **filtrée par une antenne**,
   **Then** les valeurs par intervalle ne concernent que **cette antenne**.
2. **Given** une antenne **sans présence** sur la période, **When** la série filtrée est demandée,
   **Then** tous les intervalles valent **0** (aucune erreur).

### Edge Cases

- **Granularité non prise en charge** (ex. « jour ») : requête **refusée** avec un message clair
  (seules **semaine** et **mois** sont supportées au MVP).
- **Plage de dates invalide** (fin avant début, bornes manquantes) : requête **refusée** (validation).
- **Plage très large** : plafond de période appliqué (cohérent avec les rapports 018).
- **Période sans aucune donnée** : série **continue** d'intervalles à **0** (aucune erreur).
- **Présences annulées** : exclues de tous les décomptes.
- **Antenne inexistante** en filtre : intervalles à **0** (aucune erreur).
- **Sans droit `manage_attendance`** : 401/403.

## Requirements *(mandatory)*

### Série temporelle (US1)

- **FR-001**: Le système MUST fournir une **série temporelle** des présences **valides** pour une
  **plage de dates** et une **granularité** donnée, sous forme d'une **suite ordonnée d'intervalles** ;
  chaque intervalle porte son **début**, un **libellé**, son **nombre de présences valides** et son
  **nombre de sessions**.
- **FR-002**: Les granularités supportées MUST être **semaine** et **mois** ; toute autre granularité
  (ex. « jour ») MUST être **refusée** avec un message clair. Les **semaines** s'entendent au sens
  **ISO 8601** (débutant le **lundi**) ; les **mois** au sens **calendaire**.
- **FR-003**: La série MUST être **continue** sur la période : **tous** les intervalles de la plage sont
  présents, ceux **sans donnée** apparaissant avec une valeur **0** (pas de trou).
- **FR-004**: Seules les présences **valides** MUST être comptées ; les **annulées** MUST être exclues.

### Filtrage (US2)

- **FR-005**: La série MUST pouvoir être **filtrée par antenne** (optionnel) ; sans filtre, elle agrège
  **toutes** les antennes.

### Transverses & sécurité

- **FR-006**: Les **paramètres** (plage de dates, granularité, antenne) MUST être **validés** côté
  serveur ; une plage invalide (fin < début, bornes manquantes) ou dépassant le **plafond de période**
  MUST être refusée avec un message clair, sans exécuter d'agrégation.
- **FR-007**: La série MUST être en **lecture seule** : aucun effet de bord, aucune écriture, aucune
  migration (réutilise les données de présence existantes).
- **FR-008**: Tous les rapports MUST être réservés au droit **`manage_attendance`** ; toute demande sans
  authentification ou sans ce droit MUST être **refusée** (401/403). L'API reste l'autorité. Les
  contrats d'échange MUST utiliser des **représentations dédiées** (pas d'exposition d'entités de
  persistance) ; erreurs au format homogène.

### Key Entities *(include if feature involves data)*

- **Point de série (vue calculée)** : début d'intervalle, libellé (ex. « 2026-06 » ou « 2026-S23 »),
  nombre de présences valides, nombre de sessions — sur l'intervalle.
- **Série temporelle (vue calculée)** : plage (début/fin), granularité, éventuelle antenne, et la
  **suite ordonnée** de points.
- **Session de présence (source, lecture)** : antenne, date de réunion — base du décompte de sessions
  par intervalle. Non modifiée.
- **Présence (source, lecture)** : session, statut (valide/annulée) — base du décompte de présences
  valides par intervalle. Non modifiée.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un responsable obtient l'**évolution** des présences par mois **ou** par semaine sur une
  période en **une seule demande**, sous forme d'une **suite ordonnée d'intervalles**.
- **SC-002**: La série est **continue** : **100 %** des intervalles de la plage sont présents, ceux sans
  donnée valant **0** (aucun trou).
- **SC-003**: **100 %** des présences **annulées** sont **exclues** des décomptes.
- **SC-004**: **100 %** des granularités non supportées et des plages invalides sont **refusées** avec un
  message clair, sans exécuter d'agrégation.
- **SC-005**: **100 %** des demandes sans le droit `manage_attendance` sont refusées (401/403).
- **SC-006**: La somme des présences valides de la série (sans filtre d'antenne) **correspond** au total
  de la synthèse (feature 018) pour la **même période** (cohérence inter-rapports).

## Assumptions

- **Données source réutilisées** (feature 001) : sessions (antenne, date de réunion) et présences
  (statut valide/annulée). **Aucune modification** ni migration.
- **Bornes d'intervalle** : chaque présence/session est rattachée à l'intervalle contenant sa **date de
  réunion** ; les **semaines** suivent **ISO 8601** (lundi→dimanche), les **mois** le calendrier.
- **Série continue** : les intervalles sans donnée sont **remplis à 0** pour une courbe exploitable.
- **Plafond de période** : réutilise la règle des rapports 018 (ex. 366 jours) pour borner le coût.
- **Droit d'accès** : réutilise **`manage_attendance`** (décidé le 2026-07-06) ; pas de nouveau droit.
- **Hors périmètre** : la **granularité « jour »** (reportée) ; la **courbe/visualisation SPA** (feature
  ultérieure) ; l'**export** (CSV/PDF) de la série ; toute écriture sur les présences.
