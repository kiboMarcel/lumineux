# Feature Specification: Console web — Gestion des membres (SPA, Lot 2)

**Feature Branch**: `009-spa-members-management`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "Lot 2 du SPA : module Gestion des membres — rechercher, consulter, créer
et corriger les membres (droit manage_members), en consommant l'API existante."

## Contexte & motivation

La console web dispose désormais d'un socle et du cycle de vie du compte (feature 008). Ce lot livre
le **premier module métier** : la **gestion des membres** pour le bureau. Un utilisateur disposant du
droit **gestion des membres** peut **rechercher**, **consulter**, **enrôler** et **corriger** les
membres, en s'appuyant sur les endpoints déjà exposés par l'API (feature 002). L'objectif : rendre
l'enrôlement et l'entretien du fichier des membres autonomes depuis le navigateur, tout en respectant
les règles métier de l'API (unicité des contacts, détection d'homonymie, remise sécurisée des
identifiants initiaux).

Ce lot **ne modifie pas l'API** et **ne couvre pas** l'attribution de droits (Lot 3), les présences
(Lot 4), ni l'import de masse.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Rechercher, lister et consulter les membres (Priority: P1) 🎯 MVP

En tant que gestionnaire des membres, je veux rechercher et parcourir la liste des membres puis
ouvrir la fiche de l'un d'eux, afin de retrouver rapidement une personne et consulter ses
informations.

**Why this priority**: C'est la base du module, **réalisable immédiatement** (aucune dépendance à des
données de référence), et la porte d'entrée de toutes les autres actions. Livrée seule, elle apporte
déjà de la valeur (annuaire consultable).

**Independent Test**: Se connecter avec un compte disposant du droit de gestion des membres, ouvrir
le module, rechercher par nom/référence, naviguer entre les pages de résultats, ouvrir une fiche et
vérifier l'affichage complet sans aucune donnée secrète.

**Acceptance Scenarios**:

1. **Given** un utilisateur avec le droit de gestion des membres, **When** il ouvre le module,
   **Then** une liste **paginée** de membres s'affiche (nom, prénom, référence, contact, statut).
2. **Given** la liste, **When** il saisit un terme de recherche (nom, référence ou contact),
   **Then** les résultats correspondants s'affichent, avec le nombre total et la navigation entre
   pages.
3. **Given** un résultat, **When** il ouvre la fiche, **Then** l'identité complète, la référence, le
   statut et l'état d'activation du compte s'affichent, **sans** mot de passe ni empreinte.
4. **Given** un utilisateur **sans** le droit de gestion des membres, **When** la console s'affiche,
   **Then** l'entrée « Membres » **n'est pas visible** et une tentative d'accès direct à l'URL est
   refusée (l'API répond 403, l'UI l'affiche proprement).
5. **Given** une fiche demandée pour un identifiant inexistant, **When** la page se charge, **Then**
   un message « membre introuvable » s'affiche.

---

### User Story 2 - Enrôler un nouveau membre (Priority: P2)

En tant que gestionnaire des membres, je veux créer un nouveau membre en saisissant ses informations,
gérer un éventuel homonyme, et obtenir le mode de remise de ses identifiants initiaux, afin
d'intégrer une nouvelle personne dans la communauté.

**Why this priority**: Valeur métier forte (enrôlement), mais dépend de la manière de renseigner
l'**antenne d'origine** (champ requis) et des autres champs à clé étrangère (voir Dépendances).

**Independent Test**: Créer un membre avec les informations obligatoires ; provoquer une alerte
d'homonymie et la confirmer ; provoquer un conflit de contact et vérifier le blocage ; vérifier
l'affichage du mode de remise des identifiants (email **ou** remise bureau avec mot de passe
temporaire affiché **une seule fois**).

**Acceptance Scenarios**:

1. **Given** le formulaire de création, **When** l'utilisateur renseigne les champs **obligatoires**
   (nom, prénom, sexe, antenne d'origine) et valide, **Then** le membre est créé et sa référence est
   affichée.
2. **Given** un membre potentiellement **homonyme** d'un existant, **When** l'utilisateur valide,
   **Then** un **avertissement** s'affiche l'invitant à **confirmer** la création malgré tout **ou** à
   annuler ; en cas de confirmation, le membre est créé.
3. **Given** un **contact (mobile ou email) déjà utilisé par un membre actif**, **When** l'utilisateur
   valide, **Then** une **erreur bloquante** s'affiche (pas de confirmation possible) et le membre
   n'est pas créé.
4. **Given** une création réussie **avec** email, **When** la confirmation s'affiche, **Then** elle
   indique qu'une **invitation a été envoyée par email** (aucun mot de passe affiché).
5. **Given** une création réussie **sans** email (repli « remise bureau »), **When** la confirmation
   s'affiche, **Then** elle présente l'**identifiant de connexion** et le **mot de passe temporaire**
   **une seule fois**, avec un rappel de le transmettre en main propre ; ce secret n'est **jamais**
   ré-affiché ni conservé.
6. **Given** un champ obligatoire manquant ou invalide, **When** l'utilisateur valide, **Then** un
   message de validation par champ s'affiche et la création n'est pas tentée.

---

### User Story 3 - Corriger la fiche d'un membre (Priority: P3)

En tant que gestionnaire des membres, je veux corriger les informations d'un membre existant, afin de
maintenir le fichier à jour.

**Why this priority**: Complète le cycle, mais moins fréquent que la recherche/création ; partage les
mêmes règles de conflit.

**Independent Test**: Ouvrir un membre, modifier des champs (hors référence), enregistrer ; vérifier
la prise en compte ; provoquer un conflit de contact et vérifier le blocage.

**Acceptance Scenarios**:

1. **Given** la fiche d'un membre en édition, **When** l'utilisateur modifie des champs (autres que la
   **référence**, non modifiable) et enregistre, **Then** les modifications sont prises en compte et
   confirmées.
2. **Given** une correction introduisant un **contact déjà utilisé par un autre membre actif**,
   **When** l'utilisateur enregistre, **Then** une **erreur bloquante** s'affiche et la modification
   est refusée.
3. **Given** la fiche en édition, **When** l'utilisateur consulte le champ **référence**, **Then**
   celui-ci est affiché en **lecture seule**.

### Edge Cases

- **Droit révoqué en cours de session** : une action de gestion refusée par l'API (403) affiche un
  message « action non autorisée » sans planter ; l'affichage RBAC ayant pu être permissif, l'API
  reste l'autorité.
- **Session expirée pendant la saisie** : un 401 ramène à la connexion (socle feature 008) ; la
  saisie non enregistrée est perdue (comportement assumé, pas de brouillon persistant).
- **Recherche sans résultat** : un état « aucun membre trouvé » s'affiche.
- **Pagination hors limites** : une page vide au-delà du total ramène à un état cohérent (page valide
  ou message).
- **Mot de passe temporaire (remise bureau)** : affiché une seule fois ; un rafraîchissement de page
  ou un retour arrière ne le ré-affiche pas.
- **Homonymie déjà confirmée** : après confirmation, une nouvelle validation identique ne redéclenche
  pas l'avertissement (l'intention de confirmer est conservée le temps de l'opération).

## Requirements *(mandatory)*

### Consultation (US1)

- **FR-001**: L'entrée de navigation « Membres » et l'accès au module MUST être réservés aux
  utilisateurs disposant du droit de **gestion des membres** (affichage) ; l'API reste l'autorité
  (accès direct refusé = 403 géré proprement).
- **FR-002**: Le module MUST proposer une **recherche** des membres (par nom, référence ou contact)
  et afficher les résultats sous forme de **liste paginée** (page, taille de page, total).
- **FR-003**: Chaque élément de liste MUST présenter au minimum nom, prénom, référence, contact et
  statut, et permettre d'ouvrir la **fiche** correspondante.
- **FR-004**: La **fiche** d'un membre MUST afficher son identité complète, sa référence, son statut
  et l'état d'activation de son compte, **sans exposer** de secret (mot de passe, empreinte).
- **FR-005**: Une fiche demandée pour un identifiant **inexistant** MUST afficher un message
  « introuvable » exploitable.

### Enrôlement (US2)

- **FR-006**: Le module MUST permettre de **créer un membre** en saisissant les champs
  **obligatoires** (nom, prénom, sexe, antenne d'origine) et des champs **optionnels** (mobile, email,
  adresse, date de naissance, et autres attributs).
- **FR-007**: En cas d'**homonymie** signalée par l'API, l'UI MUST afficher un **avertissement** et
  offrir un choix explicite **Confirmer / Annuler** ; la confirmation MUST relancer la création en
  **acceptant le doublon**.
- **FR-008**: En cas de **contact déjà utilisé par un membre actif**, l'UI MUST afficher une **erreur
  bloquante** (non confirmable) et **ne pas** créer le membre.
- **FR-009**: À la création réussie, l'UI MUST indiquer le **mode de remise des identifiants** :
  soit **email envoyé**, soit **remise bureau** exposant l'**identifiant de connexion** et le **mot de
  passe temporaire**.
- **FR-010**: Le mot de passe temporaire (remise bureau) MUST être affiché **une seule fois**, **jamais
  persisté, journalisé, ni ré-affiché** ; l'utilisateur est invité à le transmettre en main propre.

### Correction (US3)

- **FR-011**: Le module MUST permettre de **corriger** les informations d'un membre existant, la
  **référence** restant **non modifiable** (lecture seule).
- **FR-012**: La règle de **conflit de contact** (FR-008) MUST s'appliquer également à la
  **correction** (erreur bloquante non confirmable). En revanche, la **confirmation d'homonymie**
  (FR-007) est **propre à la création** : l'API de correction (`PUT`) n'accepte pas de confirmation de
  doublon et ne réalise pas le contrôle nom+prénom — l'écran de correction ne déclenche donc **pas**
  d'avertissement d'homonymie (alignement avec le comportement réel de l'API, cf. `plan.md` / `research.md`).

### Transverses

- **FR-013**: L'application MUST **mapper** les réponses d'erreur de l'API en messages exploitables :
  **validation** (par champ), **introuvable**, **conflit** (contact/homonymie), **non autorisé**,
  sans exposer de détail technique.
- **FR-014**: La validation côté client (champs obligatoires, formats) MUST être **indicative** ;
  l'API reste l'**autorité** (ses refus font foi).
- **FR-015**: Les écrans MUST être en **français** et **responsive** (poste de bureau et tablette).
- **FR-016**: Le module MUST peupler les champs à **clé étrangère** du membre à partir de **listes de
  données de référence fournies par l'API** — l'**antenne d'origine** (obligatoire, sélectionnée dans
  une liste) au minimum, et de façon optionnelle civilité, lieu/ville de naissance, district,
  nationalité, membre introducteur. Ces listes proviennent d'une **feature API de référentiels
  préalable** (cf. Dépendances) ; la saisie manuelle d'identifiants numériques n'est **pas** retenue.
- **FR-017**: L'utilisateur MUST sélectionner l'antenne d'origine dans une **liste** (jamais un
  identifiant saisi à la main) ; en l'absence de la donnée de référence requise, la création MUST être
  empêchée avec un message explicite plutôt qu'une soumission erronée.

### Key Entities *(include if feature involves data)*

- **Membre (vue)** : identité (nom, prénom, sexe, civilité, date/lieu de naissance…), coordonnées
  (mobile, email, adresse), rattachements (antenne d'origine, district, nationalité, introducteur),
  **référence** (identifiant unique, non modifiable), **statut**, **état d'activation du compte**.
  Aucune donnée secrète.
- **Élément de liste** : sous-ensemble d'affichage (nom, prénom, référence, contact, statut).
- **Résultat de création** : mode de remise des identifiants (email envoyé **ou** remise bureau),
  identifiant de connexion, et — uniquement en remise bureau — mot de passe temporaire (éphémère,
  affiché une fois).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un gestionnaire retrouve un membre existant (recherche → fiche) en **moins de 30
  secondes** et **moins de 3 actions**.
- **SC-002**: Un gestionnaire crée un membre valide (cas nominal) en **moins de 2 minutes**, et obtient
  un retour clair sur le **mode de remise** des identifiants.
- **SC-003**: Dans **100 %** des cas d'homonymie signalés par l'API, l'utilisateur se voit proposer un
  choix explicite Confirmer/Annuler (jamais de création silencieuse ni de blocage définitif injustifié).
- **SC-004**: Dans **100 %** des cas de contact déjà utilisé par un membre actif, la création/correction
  est **refusée** avec un message bloquant compréhensible.
- **SC-005**: **Aucun** secret (mot de passe temporaire, empreinte) n'est observable de façon
  persistante (stockage, URL, journaux) ; le mot de passe temporaire n'apparaît qu'une fois à l'écran.
- **SC-006**: Dans **100 %** des cas, l'entrée « Membres » et les actions du module sont **absentes**
  pour un utilisateur sans le droit correspondant.
- **SC-007**: Chaque type d'erreur (validation, introuvable, conflit, non autorisé) produit un **message
  distinct et exploitable** guidant l'action suivante.

## Assumptions

- **Socle réutilisé** : session, intercepteurs (jeton, erreurs), gardes et RBAC d'affichage de la
  feature 008 ; droits obtenus via `/auth/me`. L'API (feature 002 — MembersController) **n'est pas
  modifiée** par ce lot.
- **Endpoints consommés (existants)** : liste/recherche paginée, fiche, création (avec confirmation de
  doublon), correction. Requièrent le droit de gestion des membres.
- **Remise des identifiants** : le mode (email vs remise bureau) et le mot de passe temporaire
  proviennent de la réponse de création de l'API ; l'UI ne fait que les restituer (mot de passe une
  seule fois).
- **Suppression de membre hors périmètre** (aucun endpoint API).
- **Données de référence — PRÉREQUIS API (décidé le 2026-07-05)** : les champs à clé étrangère sont
  peuplés depuis des **endpoints de référentiels** exposés par l'API. Une **petite feature API
  « référentiels » préalable** (au minimum la **liste des antennes**, idéalement civilités, villes,
  nationalités) doit être livrée **avant l'enrôlement** (US2) et la correction de ces champs (US3).
  **US1 (recherche/consultation) est indépendante** de cette dépendance et peut être livrée en
  premier. L'antenne étant requise à la création, l'endpoint antennes est le minimum bloquant pour US2.
- **Pagination** : tailles de page raisonnables par défaut (ex. 20), navigation page à page.
- **Hors périmètre** : attribution de profils/droits (Lot 3), présences (Lot 4), import/export de
  masse, internationalisation au-delà du français.
