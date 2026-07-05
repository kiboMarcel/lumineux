# Feature Specification: Endpoints de données de référence (nomenclatures)

**Feature Branch**: `010-reference-data-endpoints`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "Exposer en lecture les référentiels (antennes, civilités, villes,
districts, pays/nationalités) nécessaires pour peupler les listes déroulantes du formulaire membre du
SPA (prérequis du Lot 2, feature 009)."

## Contexte & motivation

Le module « Gestion des membres » du SPA (feature 009) doit renseigner des champs à **clé étrangère**
— l'**antenne d'origine** (obligatoire), et de façon optionnelle la **civilité**, la **ville**
(lieu/ville de naissance), le **district** de résidence et le **pays/nationalité**. Or l'API n'expose
aujourd'hui **aucun moyen de lister** ces nomenclatures : le client ne peut donc pas proposer de
listes de sélection ni garantir des valeurs valides.

Cette fonctionnalité ajoute des **endpoints de lecture** exposant ces référentiels, afin que les
clients (SPA d'abord, mobile ensuite) affichent de vraies listes de choix. Les nomenclatures existent
déjà en base (cibles de clé étrangère de la fiche membre) ; **leur gestion/CRUD relève d'autres
fonctionnalités** et n'entre pas dans ce périmètre — ici, **lecture seule**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Lister les antennes pour l'enrôlement (Priority: P1) 🎯 MVP

En tant que client (SPA) préparant la création d'un membre, je veux obtenir la **liste des antennes**
actives, afin de proposer la sélection de l'**antenne d'origine** (champ obligatoire) sans saisie
d'identifiant à la main.

**Why this priority**: L'antenne est **obligatoire** à la création d'un membre ; sans cette liste,
l'enrôlement depuis le SPA est impossible. C'est le déblocage minimal du Lot 2.

**Independent Test**: Un utilisateur authentifié demande la liste des antennes et obtient les
antennes **actives** avec, pour chacune, un identifiant et un libellé exploitables pour une liste de
sélection.

**Acceptance Scenarios**:

1. **Given** un utilisateur authentifié, **When** il demande la liste des antennes, **Then** il reçoit
   les antennes **actives** (identifiant + code + libellé), triées de façon **stable** (par libellé).
2. **Given** une demande **sans authentification**, **When** la liste est demandée, **Then** l'accès
   est **refusé** (non authentifié).
3. **Given** qu'aucune antenne active n'existe, **When** la liste est demandée, **Then** une liste
   **vide** est renvoyée (pas d'erreur).

---

### User Story 2 - Lister les autres nomenclatures de la fiche membre (Priority: P2)

En tant que client (SPA), je veux obtenir les listes de **civilités**, **villes**, **districts** et
**pays/nationalités** actifs, afin de proposer la sélection des champs optionnels correspondants de la
fiche membre.

**Why this priority**: Améliore la qualité de saisie des champs optionnels, mais non bloquant pour
l'enrôlement minimal (US1 suffit à créer un membre).

**Independent Test**: Un utilisateur authentifié demande chacune de ces listes et obtient les entrées
**actives** avec identifiant et libellé(s) exploitables ; pour les pays, le **libellé de nationalité**
est distinct du libellé de pays.

**Acceptance Scenarios**:

1. **Given** un utilisateur authentifié, **When** il demande les civilités / villes / districts,
   **Then** il reçoit les entrées **actives** (identifiant + code + libellé), triées de façon stable.
2. **Given** un utilisateur authentifié, **When** il demande les pays, **Then** chaque entrée fournit
   un identifiant, un **libellé de pays** et un **libellé de nationalité** distincts.
3. **Given** une demande **sans authentification**, **When** une de ces listes est demandée, **Then**
   l'accès est **refusé**.

### Edge Cases

- **Référentiel vide** : liste vide renvoyée (jamais une erreur).
- **Entrées inactives** : les entrées non actives sont **exclues** des listes (destinées à la saisie).
- **Grand volume** (ex. villes) : les listes sont **triées** de façon stable ; la pagination éventuelle
  est un point de conception (voir Assumptions) mais l'usage attendu est le chargement d'une liste de
  sélection.
- **Non authentifié** : refus uniforme, cohérent avec les autres ressources protégées.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système MUST exposer, pour un utilisateur **authentifié**, la **liste des antennes**
  utilisables pour la sélection de l'antenne d'origine d'un membre.
- **FR-002**: Le système MUST exposer, pour un utilisateur **authentifié**, les listes des
  **civilités**, **villes**, **districts** et **pays/nationalités**.
- **FR-003**: Chaque entrée de liste MUST fournir au minimum un **identifiant** (utilisable comme clé
  étrangère) et un **libellé** d'affichage ; le **code** est également fourni. Pour les **pays**, un
  **libellé de pays** et un **libellé de nationalité** distincts MUST être fournis.
- **FR-004**: Les listes MUST ne contenir que les entrées **actives** (destinées à la saisie), à
  l'exclusion des entrées désactivées.
- **FR-005**: Les listes MUST être triées de façon **stable et prévisible** (par libellé) pour un
  affichage cohérent.
- **FR-006**: L'accès MUST être **réservé aux utilisateurs authentifiés** ; aucune donnée sensible ou
  secrète n'est exposée (nomenclatures publiques au sein de l'application). Une demande non
  authentifiée MUST être **refusée**.
- **FR-007**: Ces endpoints MUST être en **lecture seule** (aucune création/modification de
  nomenclature), sans effet de bord.

### Key Entities *(include if feature involves data)*

- **Antenne (lecture)** : identifiant, code, libellé (état actif requis). Cible de l'antenne d'origine.
- **Civilité (lecture)** : identifiant, code, libellé.
- **Ville (lecture)** : identifiant, code, libellé. Lieu/ville de naissance.
- **District (lecture)** : identifiant, code, libellé. District de résidence.
- **Pays (lecture)** : identifiant, code, **libellé de pays**, **libellé de nationalité**. Cible de la
  nationalité.

Aucune nouvelle donnée persistée : ces entités **existent déjà** (cibles de clé étrangère de la fiche
membre). La fonctionnalité n'ajoute que des **projections de lecture**.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Le SPA peut proposer la sélection de l'**antenne d'origine** à partir d'un **seul appel**
  (plus de saisie d'identifiant à la main), débloquant l'enrôlement du Lot 2.
- **SC-002**: **100 %** des entrées renvoyées sont **actives** (aucune entrée désactivée n'apparaît
  dans une liste de saisie).
- **SC-003**: **100 %** des demandes **non authentifiées** sont refusées.
- **SC-004**: Pour chaque nomenclature, une liste demandée deux fois de suite renvoie le **même ordre**
  (tri stable) — vérifiable sans connaître l'implémentation.
- **SC-005**: **Aucune** donnée secrète n'apparaît dans les réponses (nomenclatures uniquement).

## Assumptions

- **Nomenclatures existantes** : antennes, civilités, villes, districts, pays existent déjà en base
  (cibles de clé étrangère de la fiche membre) ; **aucune migration** ni nouvelle table. Leur
  **CRUD/gestion** est hors périmètre (lecture seule ici).
- **Filtre d'activité** : seules les entrées au statut **actif** sont exposées (destinées à la saisie).
- **Authentification réutilisée** : accès réservé aux utilisateurs authentifiés (mécanisme existant) ;
  aucun droit de gestion particulier requis — ces référentiels ne sont pas sensibles au sein de
  l'application.
- **Pagination** : l'usage attendu est le chargement d'une liste de sélection complète ; une pagination
  ou une recherche serveur pourra être ajoutée ultérieurement si un volume (ex. villes) le justifie
  (décision reportée au plan, hors périmètre fonctionnel de cette version).
- **Consommateur** : le SPA (feature 009) en premier ; l'application mobile pourra consommer les mêmes
  listes.
- **Hors périmètre** : création/modification/suppression de nomenclatures ; gestion des antennes ;
  hiérarchies (ex. ville ↔ district) au-delà des listes simples.
