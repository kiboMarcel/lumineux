# Feature Specification: API de gestion des antennes (CRUD)

**Feature Branch**: `016-antenna-management`

**Created**: 2026-07-06

**Status**: Draft

**Input**: User description: "Ajouter une API de gestion des antennes (CRUD) permettant au bureau de
créer, modifier et activer/désactiver les antennes (lieux de réunion) sans passer par du SQL manuel.
Prérequis d'un futur module SPA. Nouveau droit dédié `manage_referentials`. Désactivation logique
(pas de suppression physique)."

## Contexte & motivation

La console couvre membres, profils/droits et présences. Toutes ces fonctions s'appuient sur les
**antennes** (lieux de réunion) : une antenne est requise pour **créer un membre** et pour **démarrer
une session de présence**. Or il n'existe aujourd'hui qu'une **lecture** des antennes actives
(`GET /api/v1/reference/antennas`, feature 010) : aucune écriture. Les antennes ne peuvent être
créées/modifiées que par **script SQL manuel**, ce qui est inacceptable en exploitation.

Cette feature ajoute une **API de gestion des antennes** (créer, modifier, activer/désactiver, lister
pour la gestion), réservée à un **nouveau droit dédié** `manage_referentials`. Elle prépare un
**module SPA** de gestion (feature suivante, hors périmètre). La **lecture publique** des antennes
actives (010) reste **inchangée**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Créer une antenne (Priority: P1) 🎯 MVP

En tant que gestionnaire des référentiels, je veux créer une antenne (code, libellé, district de
rattachement), afin qu'elle devienne disponible pour l'enrôlement des membres et l'ouverture de
sessions de présence.

**Why this priority**: Sans création d'antenne en libre-service, l'onboarding d'une nouvelle antenne
reste bloqué au SQL manuel. C'est le cœur du besoin.

**Independent Test**: Avec le droit `manage_referentials`, créer une antenne avec un code inédit et un
district existant ; vérifier qu'elle est enregistrée (statut actif) et qu'elle apparaît ensuite dans
la liste de sélection des antennes actives (010).

**Acceptance Scenarios**:

1. **Given** un utilisateur avec le droit `manage_referentials`, **When** il crée une antenne avec un
   **code inédit**, un **libellé** et un **district existant**, **Then** l'antenne est créée au statut
   **active** et renvoyée avec son identifiant.
2. **Given** une antenne existante de code `X`, **When** on tente de créer une autre antenne de code
   `X`, **Then** l'opération est **refusée** (code `duplicate_code`) sans rien créer.
3. **Given** une création référençant un **district inexistant**, **When** elle est soumise, **Then**
   elle est **refusée** avec un message clair (validation).
4. **Given** un utilisateur **sans** le droit `manage_referentials`, **When** il tente une création,
   **Then** l'accès est **refusé** (401 si non authentifié, 403 sinon).

---

### User Story 2 - Modifier une antenne (Priority: P2)

En tant que gestionnaire des référentiels, je veux corriger le **libellé** et le **district** d'une
antenne, afin de tenir le référentiel à jour sans toucher à son identité (code).

**Why this priority**: La correction est fréquente mais moins critique que la création ; le **code**
reste stable (identifiant métier).

**Independent Test**: Modifier le libellé et le district d'une antenne existante ; vérifier la
persistance et que le **code n'est pas modifiable**.

**Acceptance Scenarios**:

1. **Given** une antenne existante, **When** on met à jour son **libellé** et son **district**
   (existant), **Then** les changements sont enregistrés et l'antenne mise à jour est renvoyée.
2. **Given** une antenne existante, **When** la requête tente de **changer le code**, **Then** le code
   **reste inchangé** (champ non modifiable — ignoré ou rejeté).
3. **Given** une modification vers un **district inexistant**, **When** elle est soumise, **Then** elle
   est **refusée** (validation).
4. **Given** une modification d'une antenne **inexistante**, **When** elle est soumise, **Then** la
   réponse est **introuvable** (404).

---

### User Story 3 - Activer / désactiver une antenne (Priority: P2)

En tant que gestionnaire des référentiels, je veux **désactiver** une antenne qui ne tient plus de
réunions (et pouvoir la **réactiver**), afin qu'elle disparaisse des listes de sélection sans perdre
l'historique des membres et des présences qui y sont rattachés.

**Why this priority**: Le retrait propre d'une antenne est nécessaire, mais vient après la
création/correction.

**Independent Test**: Désactiver une antenne active → elle n'apparaît plus dans la liste des antennes
actives (010) mais reste consultable en gestion (statut inactif) ; la réactiver → elle réapparaît.

**Acceptance Scenarios**:

1. **Given** une antenne **active**, **When** on la **désactive**, **Then** son statut passe à
   **inactif** et elle **n'apparaît plus** dans la lecture publique des antennes actives (010).
2. **Given** une antenne **inactive**, **When** on la **réactive**, **Then** son statut repasse à
   **actif** et elle réapparaît dans les listes de sélection.
3. **Given** une antenne référencée par des **membres** et des **sessions** existants, **When** on la
   désactive, **Then** ces enregistrements **conservent** leur rattachement (intégrité préservée,
   aucune suppression).
4. **Given** une antenne rattachée à une ou plusieurs **sessions de présence encore ouvertes**,
   **When** on tente de la désactiver, **Then** l'opération est **refusée** avec un message clair
   (code `antenna_has_open_sessions`) tant qu'une session ouverte subsiste ; l'antenne reste active.
   L'opérateur doit d'abord **clôturer** les sessions concernées.

---

### User Story 4 - Lister / consulter pour la gestion (Priority: P3)

En tant que gestionnaire des référentiels, je veux lister et consulter **toutes** les antennes, y
compris les **inactives** (avec leur statut), afin de les administrer et de les réactiver au besoin.

**Why this priority**: Support des autres opérations ; la lecture publique (010) existe déjà pour les
listes de sélection actives.

**Independent Test**: Lister les antennes en gestion et constater que les **inactives** sont incluses
avec leur statut (contrairement à la lecture publique 010 qui ne renvoie que les actives).

**Acceptance Scenarios**:

1. **Given** des antennes actives et inactives, **When** le gestionnaire liste les antennes en
   gestion, **Then** **toutes** sont renvoyées avec leur **statut**.
2. **Given** une antenne existante, **When** le gestionnaire la consulte par identifiant, **Then** son
   détail (code, libellé, district, statut) est renvoyé.
3. **Given** un utilisateur **sans** le droit `manage_referentials`, **When** il tente d'accéder à la
   liste de gestion, **Then** l'accès est **refusé** (403).

### Edge Cases

- **Code en doublon** (y compris casse/espaces) : rejeté (`duplicate_code`) ; l'unicité ne doit pas
  être contournable par des variations triviales d'espaces.
- **District inexistant** à la création/modification : rejeté (validation).
- **Antenne introuvable** (modif/désactivation/consultation) : 404.
- **Double désactivation / double réactivation** : idempotent (l'état cible est atteint sans erreur
  incohérente).
- **Désactivation avec session(s) ouverte(s)** : refusée (`antenna_has_open_sessions`) jusqu'à
  clôture ; une fois toutes les sessions clôturées, la désactivation est possible.
- **Accès sans droit** : 401 (non authentifié) / 403 (authentifié sans `manage_referentials`).
- **Lecture publique 010 inchangée** : ne renvoie que les antennes actives.

## Requirements *(mandatory)*

### Gestion des antennes

- **FR-001**: Le système MUST permettre de **créer une antenne** avec un **code**, un **libellé** et un
  **district de rattachement** ; l'antenne créée est au statut **actif**.
- **FR-002**: Le **code d'antenne** MUST être **unique** ; toute création avec un code déjà utilisé
  MUST être refusée avec une erreur métier claire (`duplicate_code`), sans rien créer.
- **FR-003**: Le **district** référencé à la création/modification MUST **exister** ; sinon l'opération
  est refusée (validation).
- **FR-004**: Le système MUST permettre de **modifier** le **libellé** et le **district** d'une
  antenne. Le **code** MUST être **non modifiable** après création.
- **FR-005**: Le système MUST permettre de **désactiver** (statut inactif) et de **réactiver** (statut
  actif) une antenne. La désactivation est **logique** ; **aucune suppression physique** n'est
  proposée.
- **FR-005a**: La désactivation d'une antenne portant une ou plusieurs **sessions de présence
  ouvertes** MUST être **refusée** avec un code métier clair (`antenna_has_open_sessions`) ; l'antenne
  reste active tant que ces sessions ne sont pas clôturées.
- **FR-006**: Une antenne **inactive** MUST **ne plus apparaître** dans la lecture publique des
  antennes actives (feature 010), tout en restant **consultable en gestion**.
- **FR-007**: La désactivation/réactivation MUST **préserver l'intégrité** : les membres et sessions
  déjà rattachés à l'antenne **conservent** leur référence (aucune donnée supprimée).
- **FR-008**: Le système MUST permettre de **lister** (gestion) **toutes** les antennes, actives **et**
  inactives, avec leur **statut**, et de **consulter** une antenne par identifiant.
- **FR-009**: Les opérations d'activation/désactivation et de modification MUST être **idempotentes**
  quant à l'état cible (pas d'erreur incohérente sur une opération répétée menant au même état).

### Contrôle d'accès & sécurité

- **FR-010**: Toutes les opérations d'**écriture et de lecture de gestion** MUST être réservées à un
  **nouveau droit dédié** `manage_referentials`, **distinct** de `manage_members` et
  `manage_attendance`. L'API reste l'autorité (401/403).
- **FR-011**: Le droit `manage_referentials` MUST être **ajouté au catalogue des droits** (RBAC) afin
  d'être **attribuable** via les profils du bureau et **vérifiable** par contrôle d'accès.
- **FR-012**: Toute entrée MUST être **validée côté serveur** (code non vide et unique, libellé requis,
  district existant) ; les erreurs MUST suivre un **format homogène** (détail + code métier), sans
  fuite technique ; les entités de persistance MUST **ne pas** être exposées directement.
- **FR-013**: Les opérations sensibles (création, modification, désactivation, réactivation) MUST être
  **journalisées** avec **auteur** et **horodatage** (traçabilité) ; les refus (droit manquant) MUST
  être consignés.

### Key Entities *(include if feature involves data)*

- **Antenne** : lieu de réunion. Attributs : identifiant, **code** (unique, stable), **libellé**,
  **district** de rattachement, **statut** (actif/inactif), et piste d'audit (auteur/horodatage de
  création et de mise à jour). Référencée par les membres (antenne d'origine) et les sessions de
  présence.
- **District** (référentiel existant) : cible du rattachement d'une antenne ; son existence est
  validée. Non modifié par cette feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un gestionnaire crée une nouvelle antenne exploitable (sélectionnable pour un membre /
  une session) en **moins de 1 minute**, **sans aucune intervention SQL**.
- **SC-002**: **100 %** des tentatives de création avec un **code déjà utilisé** sont refusées, sans
  doublon en base.
- **SC-003**: **100 %** des désactivations **préservent** les rattachements existants (aucun membre ni
  session orphelin ; aucune suppression).
- **SC-004**: Une antenne **désactivée** disparaît des listes de sélection actives dans **100 %** des
  cas, et une **réactivation** la restaure.
- **SC-005**: **100 %** des accès sans le droit `manage_referentials` sont refusés (401/403) ; la
  lecture publique des antennes actives (010) reste inchangée.
- **SC-006**: **100 %** des opérations sensibles (création, modification, désactivation, réactivation)
  génèrent une entrée de journal traçable (auteur + horodatage).

## Assumptions

- **Entité Antenne existante réutilisée** : code, libellé, district (entier de rattachement), statut,
  audit. Le champ `district` reste un rattachement au référentiel des districts (existence validée à
  l'écriture) ; aucune refonte de schéma imposée au-delà de l'ajout du droit.
- **Code non modifiable** après création (identifiant métier stable) — défaut retenu ; une éventuelle
  ré-immatriculation relèverait d'une opération distincte hors périmètre.
- **Nouveau droit `manage_referentials`** ajouté au catalogue RBAC (feature 004) ; attribuable via les
  profils du bureau (feature 011) et vérifié par policy (comme `manage_members`/`manage_attendance`).
- **Lecture publique inchangée** : `GET /api/v1/reference/antennas` (feature 010) continue de ne
  renvoyer que les antennes **actives** ; la liste de **gestion** (inactives incluses) est distincte.
- **Statuts** : `Active` / `Inactive` (cohérent avec le champ statut existant des référentiels).
- **Hors périmètre** : module SPA de gestion des antennes (feature suivante) ; CRUD des autres
  référentiels (civilités, districts, villes, pays) ; suppression physique ; seed de migration des
  villes/nationalités (décidé séparément).
