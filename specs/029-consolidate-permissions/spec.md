# Feature Specification: Consolidation du RBAC sur les profils du bureau (retrait du mécanisme hérité)

**Feature Branch**: `029-consolidate-permissions`

**Created**: 2026-07-10

**Status**: Draft

**Input**: User description : « Consolider le système de droits sur l'unique modèle des profils du
bureau et supprimer le mécanisme hérité "permissions directes" (dettes M3/M4). Projet en développement,
aucune donnée de production à préserver : retrait franc, sans migration transitoire. Retirer la table
member_permissions, HasPermissionAsync + écritures directes, PermissionBootstrapper et
BureauProfilesBootstrapper ; les droits du jeton continuent de provenir uniquement des profils
(GetPermissionsAsync inchangé). Préserver l'amorçage de l'admin initial (setup 005) via un profil.
Résultat : une seule source de vérité (profils), aucun code/table mort, autorisation inchangée, tests verts. »

## User Scenarios & Testing *(mandatory)*

Refonte **technique interne** (nettoyage de dette M3/M4) **sans changement de comportement observable** :
les droits sont déjà, en pratique, portés par les **profils du bureau** (features 004/011) ; le jeton
d'accès dérive ses droits des profils. Cette évolution **retire** le mécanisme hérité « permissions
directes » (table `member_permissions` et amorçages de migration) devenu redondant et source de confusion,
pour ne laisser **qu'une seule source de vérité**. Aucune décision d'exploitation n'est requise (projet en
développement, pas de données à migrer).

### User Story 1 - Autorisation strictement inchangée après nettoyage (Priority: P1)

En tant que **membre du bureau** (et administrateur), je veux que **mes droits et accès restent identiques**
après le retrait du mécanisme hérité, afin qu'aucune fonctionnalité ne régresse.

**Why this priority** : c'est la garantie centrale — un nettoyage de dette **ne doit rien casser**. Les
mêmes droits, les mêmes accès autorisés/refusés qu'avant.

**Independent Test** : rejouer les parcours protégés (connexion → jeton, accès aux modules selon droits,
refus sans droit) avant/après le retrait → **comportement identique** (mêmes droits dans le jeton, mêmes
autorisations et refus).

**Acceptance Scenarios** :

1. **Given** un membre disposant de droits via un ou plusieurs **profils du bureau**, **When** il se
   connecte (ou active son compte), **Then** son jeton porte **exactement les mêmes droits** qu'avant le
   nettoyage (droits **issus des profils**).
2. **Given** un membre **sans** droit de gestion, **When** il tente une action protégée, **Then** l'accès
   est **refusé** (comme avant), et la tentative reste **tracée**.
3. **Given** l'attribution/révocation d'un profil à un membre, **When** elle est appliquée, **Then** les
   droits effectifs du membre changent **comme avant** (aucune régression du module Profils, features 004/011).

---

### User Story 2 - Amorçage de l'administrateur initial préservé (Priority: P1)

En tant que **responsable installant l'instance**, je veux que la **première installation** continue de
créer un administrateur pleinement doté, afin de pouvoir démarrer la gestion.

**Why this priority** : l'installation initiale est le seul chemin d'obtention des premiers droits ; elle ne
doit pas dépendre du mécanisme retiré.

**Independent Test** : sur une instance vierge, exécuter la **première installation** → l'administrateur créé
possède **tous les droits attendus**, **via un profil du bureau** (et non l'ancienne table).

**Acceptance Scenarios** :

1. **Given** une instance **non installée**, **When** la première installation est effectuée, **Then** un
   administrateur est créé avec un **profil « Administrateur »** portant l'ensemble des droits, et ses droits
   effectifs sont ceux de ce profil.
2. **Given** l'administrateur initial fraîchement créé, **When** il se connecte, **Then** son jeton porte
   l'ensemble des droits — **sans** aucune dépendance au mécanisme hérité.

---

### User Story 3 - Une seule source de vérité, aucun code/table mort (Priority: P2)

En tant que **développeur/mainteneur**, je veux que les droits n'aient **qu'une seule source** (les profils)
et qu'aucun vestige du mécanisme hérité ne subsiste, afin d'éviter les erreurs (« droit ajouté au mauvais
endroit, sans effet »).

**Why this priority** : c'est l'objectif de maintenabilité du nettoyage ; important mais subordonné à la
non-régression (US1/US2).

**Independent Test** : rechercher dans le code toute référence au mécanisme hérité (table/entité de
permissions directes, amorçages de migration) → **plus aucune** ; ajouter un droit **ne peut se faire que**
via un profil.

**Acceptance Scenarios** :

1. **Given** le code après nettoyage, **When** on recherche l'ancien stockage « permissions directes » et
   ses amorçages, **Then** **aucune** référence ne subsiste (entité, table, écritures, services d'amorçage).
2. **Given** un développeur souhaitant octroyer un droit, **When** il cherche où le faire, **Then** **seul**
   le chemin par profils existe (aucune voie parallèle sans effet).
3. **Given** la base de données après nettoyage, **When** on inspecte le schéma, **Then** la table héritée
   des permissions directes **n'existe plus**.

---

### Edge Cases

- **Instance déjà amorcée par l'ancien mécanisme** (droits présents seulement dans l'ancienne table) :
  **hors périmètre** — projet en développement, aucune donnée héritée à préserver ; l'octroi des droits se
  refait via profils si besoin.
- **Configuration d'amorçage héritée présente** (paramètre `Auth:Bootstrap`) : devenue **sans effet** et
  **retirée** ; sa présence résiduelle ne doit rien déclencher.
- **Aucun profil attribué** à un membre : ses droits effectifs sont **vides** (comme aujourd'hui) — l'accès
  aux modules de gestion est refusé.
- **Nom d'interface/opération de lecture** : l'opération qui fournit les droits effectifs (issus des profils)
  reste **fonctionnellement identique** ; tout renommage éventuel est interne et sans effet observable.

## Clarifications

### Session 2026-07-10

- Q: Renommer l'interface/opération de lecture des droits (nom devenu trompeur), ou la garder telle quelle ? → A: **Renommer** pour refléter « droits effectifs issus des profils » (ex. `IEffectivePermissionsReader.GetEffectivePermissionsAsync`) et mettre à jour les appelants — supprime le dernier nom hérité ambigu (objectif US3), sans changement de comportement observable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001** : Les droits portés par le **jeton d'accès** MUST provenir **exclusivement** des **profils du
  bureau** attribués au membre (source unique) ; le comportement actuel de dérivation des droits est
  **conservé à l'identique**.
- **FR-002** : Le mécanisme hérité de **permissions directes** (stockage « un droit ↔ un membre » hors
  profils) MUST être **entièrement retiré** : entité, table, opérations d'écriture et de lecture dédiées.
- **FR-003** : Les **amorçages de migration au démarrage** (service peuplant l'ancienne table ; service
  migrant l'ancienne table vers les profils) MUST être **retirés** — aucune logique de migration de droits
  ne s'exécute plus au démarrage de l'application.
- **FR-004** : La **table** des permissions directes MUST être **supprimée du schéma** (évolution de schéma
  versionnée), sans impact sur les autres tables.
- **FR-005** : La **première installation** (setup) MUST continuer de créer un **administrateur doté de tous
  les droits via un profil du bureau** ; elle ne MUST **pas** dépendre du mécanisme retiré.
- **FR-006** : Le **comportement d'autorisation** MUST rester **inchangé** du point de vue des clients :
  mêmes droits dans le jeton, mêmes accès **autorisés** et **refusés (403)**, mêmes règles de visibilité de
  la navigation selon les droits.
- **FR-007** : Le **paramètre de configuration d'amorçage hérité** (qui alimentait l'ancienne table) MUST
  être **retiré** ; sa disparition ne MUST provoquer **aucune** régression.
- **FR-008** : Après nettoyage, **aucune référence** au mécanisme hérité ne MUST subsister dans le code, la
  configuration ou le schéma (pas de code/table mort). L'**opération de lecture des droits effectifs** MUST
  être **renommée** pour refléter sa source (les profils) et lever tout nom hérité ambigu ; ce renommage est
  **sans effet observable** côté clients (mêmes droits, mêmes réponses).
- **FR-009** : La **couverture de tests** MUST rester **verte** et refléter la source unique : les tests qui
  s'appuyaient sur l'ancien mécanisme sont **adaptés** pour attribuer les droits **via des profils**.
- **FR-010** : Les **tentatives d'accès refusées** MUST continuer d'être **tracées** (audit), comme
  aujourd'hui.

### Key Entities *(include if data involved)*

- **Profil du bureau** : ensemble nommé de droits, attribuable à des membres (features 004/011). **Seule
  source de vérité** des droits après ce lot.
- **Attribution membre ↔ profil** : lien qui confère à un membre les droits d'un profil. Détermine les
  droits effectifs (et donc les droits du jeton).
- **~~Permission directe (membre ↔ droit)~~** : entité/table **héritée**, **supprimée** par ce lot (n'était
  plus la source des droits du jeton).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : **100 %** des droits présents dans le jeton d'un membre proviennent des **profils** ; pour un
  jeu de comptes de test, les droits du jeton sont **identiques** avant et après le nettoyage.
- **SC-002** : **100 %** des accès **autorisés** et **refusés (403)** des parcours protégés donnent le
  **même résultat** qu'avant le nettoyage (aucune régression d'autorisation).
- **SC-003** : La **première installation** produit un administrateur doté de **tous** les droits via un
  profil, dans **100 %** des cas testés.
- **SC-004** : **Zéro** référence au mécanisme hérité subsiste (recherche code/config/schéma = 0 occurrence) ;
  la table héritée **n'existe plus** dans le schéma.
- **SC-005** : La suite de tests est **verte** (aucun test rouge introduit par le retrait) ; **aucune** logique
  de migration de droits ne s'exécute au démarrage.

## Assumptions

- **Projet en développement** : aucune donnée de production à préserver ; le retrait est **franc** (pas
  d'étape transitoire, pas de migration de données existantes). *(Précisé par l'utilisateur.)*
- **Source de vérité déjà en place** : les droits du jeton dérivent **déjà** des profils ; ce lot **retire**
  le mécanisme parallèle devenu inutile, sans introduire de nouveau modèle.
- **Setup admin déjà par profils** : la première installation crée déjà un profil « Administrateur » et
  l'attribue ; ce chemin est **préservé** et ne dépend pas de l'ancien mécanisme.
- **Opération de lecture des droits conservée fonctionnellement, mais renommée** (décision verrouillée, cf.
  Clarifications) : la lecture des droits effectifs (issus des profils) reste identique dans son
  comportement, mais l'interface/opération est **renommée** (ex. `IEffectivePermissionsReader.
  GetEffectivePermissionsAsync`) pour lever le nom hérité ambigu — **sans effet observable** côté clients.
- **Config héritée** : le paramètre d'amorçage de l'ancienne table est retiré ; s'il reste dans un fichier
  d'exemple, il est ignoré.

## Out of Scope

- Toute **migration de données** existantes de l'ancienne table vers les profils (aucune donnée à migrer).
- Toute **évolution fonctionnelle** du modèle de profils (catalogue de droits, UI des profils) — inchangé.
- La **rotation** de secrets ou d'autres dettes non liées au RBAC.
- Tout changement du **contrat d'API** d'autorisation (mêmes droits, mêmes codes) — aucune évolution de
  contrat n'est visée.
