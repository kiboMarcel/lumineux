# Research — API de gestion des antennes (CRUD)

Décisions techniques figées avant conception. Réutilise l'infrastructure existante (Onion, EF Core,
RBAC, audit) ; l'entité `Antenna` et la table `antennas` existent déjà (features 001/010).

## 1. Nouveau droit `manage_referentials`

- **Décision** : introduire un droit dédié `manage_referentials`, distinct de `manage_members` et
  `manage_attendance` (moindre privilège). Déclaré dans `Permissions` (Application), ajouté au
  **catalogue** `PermissionCatalog` (Infrastructure) pour être **attribuable** via les profils du
  bureau (feature 011), et appliqué par **policy** dans `Program.cs` (comme les droits existants).
- **Rationale** : FR-010/011 ; sépare la gestion des référentiels de celle des membres.
- **Alternatives écartées** : réutiliser `manage_members` (couple deux responsabilités) ; droit
  hyper-spécifique `manage_antennas` (moins réutilisable pour les futurs référentiels — civilités,
  districts).

## 2. Unicité du code : base + métier

- **Décision** : **index unique** sur `antennas.code` (migration) **+** vérification applicative
  (`GetByCodeAsync`) renvoyant une erreur métier `duplicate_code` avant insertion.
- **Rationale** : Principe II (intégrité matérialisée) et Principe V (erreur claire non technique) ;
  double filet cohérent avec `members.reference` (index unique). Comparaison **insensible aux espaces**
  (trim) à l'écriture.
- **Alternatives écartées** : unicité applicative seule (course possible) ; index seul (erreur
  technique brute renvoyée au client).

## 3. Désactivation logique, jamais physique

- **Décision** : statut `Active`/`Inactive` porté par l'entité ; **aucune suppression**. Les FK
  `Restrict` depuis `members.antenna` et `attendance_sessions.antenna` interdisent de toute façon la
  suppression et garantissent l'intégrité/l'historique.
- **Rationale** : FR-005/006/007 ; cohérent avec la désactivation des membres.
- **Alternatives écartées** : suppression physique (perte d'historique, viole les FK).

## 4. Refus de désactivation si sessions ouvertes

- **Décision** : avant de désactiver, vérifier l'absence de **session de présence ouverte** sur
  l'antenne (`HasOpenSessionForAntennaAsync`) ; sinon **refus** `antenna_has_open_sessions`
  (409/conflit). La réactivation n'a pas cette contrainte.
- **Rationale** : décision produit 2026-07-06 (FR-005a) ; évite qu'une antenne « disparaisse » pendant
  une séance en cours.
- **Alternatives écartées** : autoriser sans impact (retenu en option B, écarté) ; clôturer
  automatiquement les sessions (effet de bord trop fort).

## 5. Validation de l'existence du district

- **Décision** : réutiliser `IReferenceLookupRepository.DistrictExistsAsync` pour valider le district à
  la création et à la modification.
- **Rationale** : port déjà utilisé par la création de membre (FR-005 de 002) ; évite une abstraction
  redondante. `antennas.district` reste un entier de rattachement (pas de refonte de schéma).
- **Alternatives écartées** : nouvel index/relation FK stricte sur `district` (refonte de schéma hors
  périmètre).

## 6. Découpage des cas d'usage (handlers)

- **Décision** : handlers dédiés — `CreateAntennaHandler`, `UpdateAntennaHandler` (libellé+district,
  **code ignoré**), `SetAntennaActiveHandler` (active/désactive, porte la règle sessions ouvertes),
  `ListAntennasHandler` (gestion : inclut inactives), `GetAntennaHandler`. Validation FluentValidation
  (code non vide+unique, libellé requis, district > 0). Chacun vérifie `manage_referentials` via
  `ICurrentUser` et journalise via `IAuditLogger`.
- **Rationale** : Principe I (un cas d'usage = un handler testable) ; cohérent avec les handlers
  Membres/Profils.
- **Alternatives écartées** : un unique handler « CRUD » monolithique (moins testable).

## 7. Contrats & mapping

- **Décision** : DTO dédiés `AntennaResponse(Id, Code, Label, DistrictId, Status)`,
  `CreateAntennaRequest(Code, Label, DistrictId)`, `UpdateAntennaRequest(Label, DistrictId)`. Mapping
  entité→DTO isolé (`AntennaMapping`). Endpoints REST sous `/api/v1/antennas`.
- **Rationale** : Principe V (contrats explicites, pas d'entité exposée).
- **Alternatives écartées** : réutiliser `ReferenceItemResponse` de 010 (n'expose pas district/statut
  et mélange lecture publique/gestion).
