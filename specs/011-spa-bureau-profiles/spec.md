# Feature Specification: Console web — Profils du bureau & droits (SPA, Lot 3)

**Feature Branch**: `011-spa-bureau-profiles`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "Lot 3 du SPA : module Profils du bureau & droits — gérer les profils
(regroupements de droits) et leur attribution aux membres, en consommant l'API existante."

## Contexte & motivation

La console web permet déjà de gérer les membres (Lot 2). Ce lot livre la **gouvernance des droits** :
un profil du bureau est un **regroupement nommé de droits fonctionnels** (permissions) que l'on
**attribue** à des membres. Le module permet de **consulter** les profils, de les **créer/modifier/
supprimer**, et d'**attribuer/révoquer** un profil à un membre, en respectant les **garde-fous**
métier de l'API (unicité du nom, permission inconnue, protection du **dernier administrateur**).

Le module distingue **lecture** et **écriture** : un gestionnaire des membres peut **consulter** les
profils et les droits d'un membre ; seul un administrateur des profils peut **modifier**. L'API reste
l'autorité (toute action non autorisée est refusée côté serveur).

Ce lot **ne modifie pas l'API** et **ne couvre pas** la modification du catalogue de droits (figé
côté serveur), la gestion des membres (Lot 2) ni les présences (Lot 4).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consulter les profils du bureau et leurs droits (Priority: P1) 🎯 MVP

En tant qu'utilisateur disposant d'un droit de lecture (administration des profils **ou** gestion des
membres), je veux lister les profils du bureau et ouvrir le détail de l'un d'eux, afin de comprendre
quels droits il confère et qui en est titulaire.

**Why this priority**: Base du module, **lecture seule**, accessible au plus grand nombre (deux droits
de lecture), et prérequis des actions d'écriture. Livrée seule, elle apporte de la visibilité sur la
gouvernance.

**Independent Test**: Se connecter avec un compte disposant d'un droit de lecture, ouvrir le module,
lister les profils (nom, description, droits, nombre de titulaires), ouvrir un détail (droits + liste
des titulaires) sans donnée sensible.

**Acceptance Scenarios**:

1. **Given** un utilisateur avec administration des profils **ou** gestion des membres, **When** il
   ouvre le module, **Then** la liste des profils s'affiche (nom, description, droits, nombre de
   titulaires).
2. **Given** la liste, **When** il ouvre un profil, **Then** son détail s'affiche : droits conférés et
   **liste des membres titulaires** (référence, nom, statut), sans donnée sensible.
3. **Given** un utilisateur **sans** aucun droit de lecture, **When** la console s'affiche, **Then**
   l'entrée « Profils du bureau » **n'est pas visible** et l'accès direct est refusé (403 géré).
4. **Given** un utilisateur disposant **uniquement** de la gestion des membres (lecture), **When** il
   consulte un profil, **Then** aucune action d'écriture (créer/modifier/supprimer) ne lui est
   proposée.

---

### User Story 2 - Créer, modifier et supprimer un profil (Priority: P2)

En tant qu'administrateur des profils, je veux créer un profil (nom, description, droits choisis dans
le catalogue), le modifier et le supprimer, afin de faire évoluer la gouvernance des droits.

**Why this priority**: Cœur de l'administration des droits, réservé au droit d'administration des
profils ; s'appuie sur la consultation (US1).

**Independent Test**: Créer un profil en sélectionnant des droits du catalogue ; provoquer un conflit
de nom (refus) et une permission invalide (refus) ; modifier un profil ; tenter de retirer le droit
d'administration du **dernier** profil administrateur (refus bloquant) ; supprimer un profil (avec
confirmation) et vérifier le garde-fou du dernier administrateur.

**Acceptance Scenarios**:

1. **Given** le formulaire de création, **When** l'administrateur saisit un nom et **sélectionne des
   droits** depuis le **catalogue figé**, **Then** le profil est créé et apparaît dans la liste.
2. **Given** un nom **déjà utilisé** par un autre profil, **When** il valide, **Then** une **erreur
   bloquante** « nom déjà utilisé » s'affiche et le profil n'est pas créé/modifié.
3. **Given** une sélection contenant un **droit inconnu** du catalogue, **When** il valide, **Then**
   une **erreur** s'affiche (validation refusée).
4. **Given** la modification qui **retirerait le droit d'administration du dernier** profil
   administrateur, **When** il valide, **Then** une **erreur bloquante** « dernier administrateur »
   s'affiche et la modification est refusée.
5. **Given** un profil, **When** l'administrateur demande sa **suppression**, **Then** une
   **confirmation** est requise ; à la confirmation, le profil est supprimé, **sauf** garde-fou
   (dernier administrateur, ou profil encore attribué le cas échéant) → **erreur bloquante** explicite.

---

### User Story 3 - Attribuer et révoquer les profils d'un membre (Priority: P2)

En tant qu'administrateur des profils, je veux consulter les profils et les **droits effectifs** d'un
membre, lui attribuer un profil et lui en révoquer, afin de gérer précisément ses habilitations.

**Why this priority**: Rend la gouvernance opérationnelle au niveau du membre ; partage la protection
du dernier administrateur.

**Independent Test**: Depuis la fiche d'un membre, ouvrir la gestion de ses profils ; voir ses profils
et ses **droits effectifs** (union) ; attribuer un profil (idempotent) ; révoquer un profil ; tenter
de révoquer le profil administrateur du **dernier** administrateur (refus bloquant).

**Acceptance Scenarios**:

1. **Given** la fiche d'un membre, **When** l'administrateur ouvre la gestion de ses profils, **Then**
   la liste de ses **profils attribués** et ses **droits effectifs** (union des droits) s'affichent.
2. **Given** cet écran, **When** il **attribue** un profil, **Then** le profil est ajouté aux profils
   du membre (opération **idempotente** : réattribuer le même profil n'échoue pas) et les droits
   effectifs sont mis à jour.
3. **Given** cet écran, **When** il **révoque** un profil, **Then** le profil est retiré et les droits
   effectifs sont mis à jour ; **sauf** si cela retirerait le **dernier administrateur** → **erreur
   bloquante** « dernier administrateur ».
4. **Given** un utilisateur disposant **uniquement** de la lecture (gestion des membres), **When** il
   ouvre cet écran, **Then** il **voit** les profils/droits effectifs mais **aucune** action
   d'attribution/révocation ne lui est proposée.

### Edge Cases

- **Droit d'écriture manquant malgré une UI permissive** : une action refusée par l'API (403) affiche
  « action non autorisée » sans planter ; l'API reste l'autorité.
- **Suppression d'un profil attribué** : selon la règle serveur, la suppression peut être refusée
  (409) → message explicite, sans état incohérent côté UI.
- **Dernier administrateur** : toute opération qui laisserait l'instance sans administrateur (retrait
  du droit, suppression du profil, révocation de l'attribution) est **refusée** (409) et clairement
  signalée.
- **Attribution idempotente** : réattribuer un profil déjà présent n'entraîne ni erreur ni doublon.
- **Session expirée** pendant une action : retour à la connexion (socle feature 008).
- **Catalogue de droits vide/indisponible** : le formulaire signale l'impossibilité de sélectionner
  des droits plutôt qu'un envoi invalide.

## Requirements *(mandatory)*

### Consultation (US1)

- **FR-001**: L'entrée de navigation « Profils du bureau » et l'accès au module MUST être visibles pour
  un utilisateur disposant du droit **d'administration des profils** **ou** de **gestion des membres**
  (lecture) ; l'API reste l'autorité (accès direct sans droit = 403 géré).
- **FR-002**: Le module MUST **lister** les profils (nom, description, droits conférés, nombre de
  titulaires).
- **FR-003**: Le module MUST afficher le **détail** d'un profil : droits conférés et **liste des
  membres titulaires** (référence, nom, statut), **sans** donnée sensible.
- **FR-004**: Les **actions d'écriture** (créer/modifier/supprimer, attribuer/révoquer) MUST être
  proposées **uniquement** aux porteurs du droit **d'administration des profils** (masquées pour un
  lecteur « gestion des membres »).

### Administration des profils (US2)

- **FR-005**: Un administrateur des profils MUST pouvoir **créer** un profil : **nom** (requis),
  **description** (optionnelle), **sélection de droits** issus du **catalogue figé** de permissions.
- **FR-006**: Le module MUST récupérer et présenter le **catalogue de droits** (libellés) pour la
  sélection ; un droit **hors catalogue** MUST être refusé (message de validation).
- **FR-007**: Un **nom déjà utilisé** par un autre profil MUST produire une **erreur bloquante**
  (création et modification).
- **FR-008**: Un administrateur MUST pouvoir **modifier** un profil (nom, description, droits) ; toute
  modification retirant le **droit d'administration du dernier** profil administrateur MUST être
  **refusée** avec un message « dernier administrateur ».
- **FR-009**: Un administrateur MUST pouvoir **supprimer** un profil après **confirmation** ; les
  garde-fous serveur (dernier administrateur, profil encore attribué le cas échéant) MUST être
  restitués comme **erreurs bloquantes** explicites.

### Attribution aux membres (US3)

- **FR-010**: Le module MUST afficher, pour un membre, ses **profils attribués** et ses **droits
  effectifs** (union des droits de ses profils).
- **FR-011**: Un administrateur des profils MUST pouvoir **attribuer** un profil à un membre de façon
  **idempotente** (réattribuer le même profil ne produit pas d'erreur).
- **FR-012**: Un administrateur des profils MUST pouvoir **révoquer** un profil d'un membre ; une
  révocation qui retirerait le **dernier administrateur** MUST être **refusée** (« dernier
  administrateur »).
- **FR-013**: L'écran de gestion des profils d'un membre MUST être **accessible depuis la fiche
  membre** (Lot 2).

### Transverses

- **FR-014**: L'application MUST **mapper** les erreurs de l'API en messages exploitables :
  **validation / permission inconnue** (400), **introuvable** (404), **conflit** (nom déjà utilisé,
  dernier administrateur) (409), **non autorisé** (403), sans détail technique.
- **FR-015**: Les **actions destructrices** (suppression de profil, révocation) MUST demander une
  **confirmation** explicite avant exécution.
- **FR-016**: La validation côté client (nom requis, au moins un droit conseillé) MUST être
  **indicative** ; l'API reste l'**autorité**.
- **FR-017**: Les écrans MUST être en **français** et **responsive** (poste de bureau et tablette).

### Key Entities *(include if feature involves data)*

- **Profil du bureau (vue)** : identifiant, **nom**, description, **droits conférés** (liste de clés de
  permission), **nombre de titulaires**, et (au détail) **liste des titulaires** (référence, nom,
  statut). Aucune donnée sensible.
- **Droit / permission (catalogue, vue)** : clé et libellé d'un droit fonctionnel figé.
- **Attribution (vue au niveau du membre)** : profils attribués à un membre + **droits effectifs**
  (union).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un lecteur (administration des profils **ou** gestion des membres) peut consulter la
  liste et le détail d'un profil (droits + titulaires) sans aucune action d'écriture visible s'il n'a
  pas le droit d'administration.
- **SC-002**: Un administrateur crée un profil valide (nom + droits) en **moins de 2 minutes**, et le
  voit apparaître dans la liste.
- **SC-003**: Dans **100 %** des cas de conflit (nom déjà utilisé, dernier administrateur), l'action
  est **refusée** avec un message bloquant compréhensible (jamais d'état incohérent).
- **SC-004**: Dans **100 %** des cas, aucune **action d'écriture** n'est proposée à un utilisateur qui
  n'a pas le droit d'administration des profils (et toute tentative directe est refusée par l'API).
- **SC-005**: L'attribution d'un profil déjà présent (idempotence) **n'échoue jamais** et ne crée pas
  de doublon.
- **SC-006**: Les droits effectifs affichés pour un membre correspondent à l'**union** des droits de
  ses profils attribués (vérifiable en confrontant profils et droits effectifs).
- **SC-007**: Toute action destructrice (suppression, révocation) requiert une **confirmation** avant
  exécution (aucune suppression/révocation en un seul clic non confirmé).

## Assumptions

- **Socle réutilisé** : session, intercepteurs, gardes et RBAC d'affichage (feature 008) ; droits
  obtenus via `/auth/me`. L'API (features 004 profils, 002 membres) **n'est pas modifiée**.
- **Modèle d'autorisation** : **lecture** = administration des profils **ou** gestion des membres ;
  **écriture** = administration des profils uniquement (aligné sur l'API). L'API tranche (403).
- **Catalogue de droits figé** : fourni en lecture par l'API ; sa **modification est hors périmètre**.
- **Codes de conflit** : nom déjà utilisé et « dernier administrateur » sont restitués par l'API et
  traités comme erreurs bloquantes non contournables côté client.
- **Point d'entrée « profils d'un membre »** : depuis la **fiche membre** (Lot 2) ; ce lot ajoute un
  accès et un écran dédiés, sans modifier la logique de gestion des membres.
- **Hors périmètre** : gestion des membres (Lot 2), présences (Lot 4), modification du catalogue de
  droits, import/export, internationalisation au-delà du français.
