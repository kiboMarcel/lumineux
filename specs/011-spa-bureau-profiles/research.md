# Research — Console web : Profils du bureau & droits (Lot 3)

Extension de l'app Angular (feature 008) consommant les API profils (004), membres (002) et catalogue
de permissions. Décisions figées avant conception.

## 1. Autorisation duale : garde et navigation « any-of »

- **Décision** : introduire un contrôle **any-of** pour la **lecture** (accès module + entrée nav) =
  `manage_bureau_profiles` **OU** `manage_members`. On **étend `permissionGuard`** (feature 008) pour
  lire `route.data.anyPermissions: string[]` (autorise si la session détient **au moins un** de ces
  droits), en conservant `route.data.permission` (single) pour les **routes d'écriture**. La
  navigation du shell est étendue de la même façon (`NavItem.anyPermissions`).
- **Rationale** : reflète exactement le `ReadAccess` de l'API (lecture élargie, écriture restreinte).
  Extension **additive** et rétrocompatible du socle.
- **Alternatives écartées** : nouvelle garde dédiée (duplication) ; élargir le droit unique (perdrait
  la distinction lecture/écriture).

## 2. Distinction lecture/écriture dans l'UI

- **Décision** : les **routes d'écriture** (`/bureau-profiles/new`, `/bureau-profiles/:id/edit`) sont
  gardées par `permissionGuard` **single** = `manage_bureau_profiles`. Dans les composants, les
  **actions d'écriture** (créer, modifier, supprimer, attribuer, révoquer) ne sont **affichées** que
  si `SessionStore.hasPermission('manage_bureau_profiles')`. L'API reste l'autorité (403 géré).
- **Rationale** : FR-004/SC-004 ; un lecteur « gestion des membres » consulte sans écrire.
- **Alternatives écartées** : contrôle uniquement serveur (l'UI proposerait des actions vouées au 403).

## 3. Codes de conflit (409) — restitution fidèle

- **Décision** : traiter les 409 selon `code` :
  - `duplicate_name` → « nom déjà utilisé » (création/modification).
  - `last_administrator` → garde-fou dernier administrateur (modification retirant l'admin,
    suppression du dernier profil admin, révocation du dernier admin).
  - `profile_in_use` → suppression d'un profil encore attribué refusée.
  - `member_inactive` → attribution à un membre inactif refusée.
  Tous **bloquants** (non contournables côté client).
- **Rationale** : correspond aux `ConflictException(code=…)` des handlers (features 004). FR-014,
  SC-003.
- **Alternatives écartées** : message générique unique (perte d'information actionnable).

## 4. Sélection des droits depuis le catalogue figé

- **Décision** : le formulaire de profil charge le **catalogue** (`GET /permissions` → `{ code, label }`)
  et présente une **sélection multiple** (cases à cocher). Le corps envoyé contient les **codes**
  choisis. Un code hors catalogue est **refusé** par l'API (400) et restitué comme validation.
- **Rationale** : FR-005/FR-006 ; garantit des droits valides et lisibles.
- **Alternatives écartées** : saisie libre de codes (source d'erreurs).

## 5. Attribution idempotente + garde-fou à la révocation

- **Décision** : l'**attribution** est **idempotente** (réattribuer un profil déjà présent ne produit
  pas d'erreur — comportement serveur). La **révocation** peut être refusée par `last_administrator`
  (bloquant). Les deux rafraîchissent les **droits effectifs** affichés.
- **Rationale** : FR-011/FR-012, SC-005/SC-006.
- **Alternatives écartées** : bloquer côté client la réattribution (inutile, l'API est idempotente).

## 6. Écran des profils d'un membre — point d'entrée

- **Décision** : écran dédié `/members/:id/profiles` (features/bureau-profiles/member-profiles),
  **accessible depuis la fiche membre** (Lot 2) via un lien « Profils & droits » (visible en lecture).
  Affiche profils attribués + **droits effectifs** ; actions attribuer/révoquer si droit d'écriture.
- **Rationale** : FR-013 ; rattache la gouvernance au contexte du membre sans dupliquer la fiche.
- **Alternatives écartées** : intégrer dans la page détail du profil (perd la vue centrée membre).

## 7. Confirmations destructrices & mapping d'erreurs

- **Décision** : **suppression de profil** et **révocation** demandent une **confirmation** explicite
  (FR-015). Mapping via `messageForError` (socle) pour 400/404/5xx, avec traitement des 409 par `code`
  (§3). Notifications/erreurs en contexte.
- **Rationale** : SC-007 ; cohérence avec le socle (features 008/009).
- **Alternatives écartées** : suppression directe sans confirmation (risqué).
