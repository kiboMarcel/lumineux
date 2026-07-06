# Feature Specification: Console web — Gestion des antennes (SPA)

**Feature Branch**: `017-spa-antenna-management`

**Created**: 2026-07-06

**Status**: Draft

**Input**: User description: "Module SPA de gestion des antennes consommant l'API 016 (CRUD antennes),
réservé au droit manage_referentials. Liste (inactives incluses), création, modification (code lecture
seule), activation/désactivation confirmée. Réutilise le socle 008 et le référentiel des districts (010)."

## Contexte & motivation

L'API de gestion des antennes (feature 016) est livrée : créer, modifier, activer/désactiver, lister
(inactives incluses). Il manque la **brique front** pour que le bureau administre les **antennes**
(lieux de réunion) depuis la console web, **sans SQL manuel**. Ce module ajoute à la SPA Angular
(`web/`) un écran de gestion des antennes, réservé au droit **gestion des référentiels**
(`manage_referentials`).

Le module réutilise le **socle** (feature 008 : session, gardes RBAC, mapping d'erreurs,
notifications) et le **référentiel des districts** (feature 010) pour le sélecteur de district. **L'API
n'est pas modifiée** ; la lecture publique des antennes actives (010) reste inchangée et utilisée
ailleurs (fiche membre, démarrage de session).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Lister et consulter les antennes (Priority: P1) 🎯 MVP

En tant que gestionnaire des référentiels, je veux voir la liste de **toutes** les antennes (actives
et inactives) avec leur statut, afin d'avoir une vue d'ensemble et d'accéder aux actions de gestion.

**Why this priority**: Sans la liste de gestion, aucune administration n'est possible ; c'est le point
d'entrée du module et la base des autres actions.

**Independent Test**: Se connecter avec le droit `manage_referentials`, ouvrir « Antennes » et vérifier
que la liste affiche les antennes **actives et inactives** avec leur **statut**.

**Acceptance Scenarios**:

1. **Given** un utilisateur avec le droit `manage_referentials`, **When** il ouvre le module Antennes,
   **Then** la liste affiche **toutes** les antennes (code, libellé, district, statut), inactives
   incluses.
2. **Given** la liste affichée, **When** l'utilisateur consulte une antenne, **Then** son détail (code,
   libellé, district, statut) est présenté.
3. **Given** un utilisateur **sans** le droit `manage_referentials`, **When** il regarde la navigation,
   **Then** l'entrée « Antennes » **n'est pas visible** et l'accès direct à l'URL est **refusé**.

---

### User Story 2 - Créer une antenne (Priority: P1)

En tant que gestionnaire des référentiels, je veux créer une antenne (code, libellé, district), afin
qu'elle devienne disponible pour l'enrôlement des membres et l'ouverture de sessions.

**Why this priority**: La création en libre-service est la valeur principale du module (fin du SQL
manuel).

**Independent Test**: Depuis le module, créer une antenne avec un code inédit et un district choisi
dans la liste ; vérifier qu'elle apparaît ensuite dans la liste de gestion ; un code déjà utilisé est
refusé avec un message clair.

**Acceptance Scenarios**:

1. **Given** le formulaire de création, **When** l'utilisateur saisit un **code inédit**, un
   **libellé** et choisit un **district** (liste déroulante), **Then** l'antenne est créée et
   apparaît dans la liste.
2. **Given** un code **déjà utilisé**, **When** l'utilisateur soumet, **Then** un message clair
   « code déjà utilisé » est affiché, sans détail technique, et rien n'est créé.
3. **Given** un formulaire incomplet (code ou libellé manquant, district non choisi), **When**
   l'utilisateur soumet, **Then** la validation empêche l'envoi et signale les champs requis.

---

### User Story 3 - Modifier une antenne (Priority: P2)

En tant que gestionnaire des référentiels, je veux corriger le **libellé** et le **district** d'une
antenne, sans pouvoir changer son **code** (identifiant stable).

**Why this priority**: Correction courante mais secondaire par rapport à la création.

**Independent Test**: Modifier le libellé et le district d'une antenne ; vérifier la persistance ; le
champ **code** est présenté en **lecture seule**.

**Acceptance Scenarios**:

1. **Given** le formulaire de modification, **When** l'utilisateur change le **libellé** et le
   **district**, **Then** les modifications sont enregistrées et reflétées dans la liste.
2. **Given** le formulaire de modification, **When** l'utilisateur consulte le **code**, **Then** il
   est affiché en **lecture seule** (non modifiable).
3. **Given** une modification vers un district invalide ou un libellé vide, **When** l'utilisateur
   soumet, **Then** la validation/erreur est signalée sans détail technique.

---

### User Story 4 - Activer / désactiver une antenne (Priority: P2)

En tant que gestionnaire des référentiels, je veux **désactiver** une antenne qui ne tient plus de
réunions (et pouvoir la **réactiver**), avec une **confirmation**, afin de la retirer proprement des
listes de sélection sans perdre l'historique.

**Why this priority**: Retrait/rétablissement propre d'une antenne ; vient après création/correction.

**Independent Test**: Désactiver une antenne (avec confirmation) → son statut passe à inactif dans la
liste ; la réactiver → statut actif. La désactivation d'une antenne avec session ouverte est refusée
avec un message clair.

**Acceptance Scenarios**:

1. **Given** une antenne active, **When** l'utilisateur demande sa **désactivation**, **Then** une
   **confirmation** est requise ; à la confirmation, son statut passe à **inactif** et la liste
   reflète l'état à jour.
2. **Given** une antenne inactive, **When** l'utilisateur la **réactive**, **Then** son statut repasse
   à **actif**.
3. **Given** une antenne rattachée à une **session de présence ouverte**, **When** l'utilisateur tente
   de la désactiver, **Then** l'opération est **refusée** par l'API et un **message clair** est
   affiché (« une session est encore ouverte »), l'antenne restant active.

### Edge Cases

- **Sans droit `manage_referentials`** : entrée « Antennes » masquée ; accès direct à une URL du module
  refusé (403 géré).
- **Code en doublon** : message clair « code déjà utilisé », rien n'est créé.
- **Désactivation refusée (session ouverte)** : message explicite ; état inchangé.
- **Antenne introuvable** (modification/consultation d'un identifiant obsolète) : message « introuvable ».
- **Session expirée (401)** pendant l'usage : purge et retour à la connexion (comportement du socle).
- **Aucune antenne** : la liste affiche un état vide explicite, sans erreur.
- **Districts indisponibles** momentanément : le sélecteur signale l'indisponibilité sans bloquer la
  navigation.

## Requirements *(mandatory)*

### Accès & navigation

- **FR-001**: L'entrée de navigation « Antennes » et l'accès au module MUST être réservés aux
  utilisateurs disposant du droit **`manage_referentials`** ; l'entrée MUST être **masquée** sinon, et
  l'accès direct à une URL du module **refusé** (l'API reste l'autorité, 403 géré).

### Liste & consultation (US1)

- **FR-002**: Le module MUST afficher la **liste de gestion** de **toutes** les antennes (actives **et**
  inactives) avec, pour chacune, **code, libellé, district et statut**.
- **FR-003**: Le module MUST permettre de **consulter** le détail d'une antenne.
- **FR-004**: La liste MUST offrir l'accès aux actions de **modification** et d'**activation/
  désactivation** pour chaque antenne.

### Création (US2)

- **FR-005**: L'utilisateur MUST pouvoir **créer** une antenne en saisissant **code**, **libellé** et en
  choisissant un **district** dans une **liste déroulante** (référentiel des districts).
- **FR-006**: Les erreurs de création MUST être **mappées** en messages clairs : **code déjà utilisé**,
  **champ requis manquant / district invalide**, sans détail technique.
- **FR-007**: La création réussie MUST rendre l'antenne visible dans la **liste de gestion**.

### Modification (US3)

- **FR-008**: L'utilisateur MUST pouvoir **modifier** le **libellé** et le **district** d'une antenne.
- **FR-009**: Le **code** MUST être présenté en **lecture seule** (non modifiable).

### Activation / désactivation (US4)

- **FR-010**: L'utilisateur MUST pouvoir **désactiver** et **réactiver** une antenne ; la
  **désactivation** MUST requérir une **confirmation**.
- **FR-011**: Si la désactivation est **refusée** par l'API (antenne avec **session ouverte**), le
  module MUST afficher un **message clair** et **conserver** l'état actif.
- **FR-012**: Après un changement de statut réussi, la **liste** MUST refléter l'**état à jour**.

### Transverses

- **FR-013**: L'application MUST **mapper** les erreurs de l'API en messages exploitables : validation,
  conflit (code / session ouverte), introuvable, non autorisé — sans détail technique.
- **FR-014**: Les écrans MUST être en **français** et **responsive** (poste de bureau et tablette).
- **FR-015**: Aucune **règle métier** ne MUST être dupliquée côté client (l'API reste l'autorité) ;
  aucun secret ne MUST être journalisé.

### Key Entities *(include if feature involves data)*

- **Antenne (vue)** : identifiant, code (stable, lecture seule après création), libellé, district
  (rattachement), statut (active/inactive). Consommée depuis l'API de gestion (016).
- **District (référentiel, lecture)** : identifiant + libellé, pour le sélecteur de rattachement
  (feature 010). Non modifié.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un gestionnaire crée une nouvelle antenne exploitable en **moins de 1 minute**, **sans
  aucune intervention SQL**.
- **SC-002**: La liste de gestion affiche **100 %** des antennes, **inactives incluses**, avec leur
  statut.
- **SC-003**: **100 %** des tentatives de création avec un **code déjà utilisé** sont signalées par un
  message clair, sans doublon créé.
- **SC-004**: **100 %** des désactivations d'une antenne avec **session ouverte** sont refusées avec un
  message explicite, l'antenne restant active.
- **SC-005**: Le **code** d'une antenne est **non modifiable** en modification dans **100 %** des cas.
- **SC-006**: Dans **100 %** des cas, l'entrée « Antennes » et les actions du module sont **absentes**
  pour un utilisateur sans le droit `manage_referentials`.
- **SC-007**: Chaque **désactivation** requiert une **confirmation** avant exécution.

## Assumptions

- **Socle réutilisé** (feature 008) : session (jeton en mémoire), intercepteurs Bearer/erreurs, gardes
  RBAC (`permissionGuard`), mapping d'erreurs (`messageForError`), notifications. **API 016 inchangée**.
- **Référentiel des districts** (feature 010) : `GET /reference/districts` alimente le sélecteur de
  district (lecture, non sensible).
- **Navigation** : une entrée dédiée « Antennes » (le regroupement « Référentiels » pourra venir plus
  tard quand d'autres nomenclatures seront gérées).
- **Statuts** : actif / inactif (cohérent avec l'API 016).
- **Confirmation** : la désactivation (action retirant l'antenne des sélections) est confirmée ; la
  réactivation ne l'est pas nécessairement.
- **Hors périmètre** : toute modification d'API ; le CRUD des autres référentiels (civilités, districts,
  villes, pays) ; la lecture publique des antennes actives (010) qui reste inchangée et utilisée
  ailleurs.
