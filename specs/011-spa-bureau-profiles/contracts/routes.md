# Contrat — Routes du module Profils & droits

Routes ajoutées sous la **coquille console protégée** (feature 008). **Lecture** = any-of
(`manage_bureau_profiles` OU `manage_members`) ; **écriture** = `manage_bureau_profiles`. Le RBAC
d'affichage est un confort ; l'API reste l'autorité (403 géré).

| Chemin | Écran | Garde | Notes |
|--------|-------|-------|-------|
| `/bureau-profiles` | Liste des profils | `authGuard` + `permissionGuard` **any-of** [admin profils, gestion membres] | US1. |
| `/bureau-profiles/:id` | Détail (droits + titulaires) | idem (any-of) | US1. Actions d'écriture affichées si admin profils. |
| `/bureau-profiles/new` | Création | `authGuard` + `permissionGuard('manage_bureau_profiles')` | US2. |
| `/bureau-profiles/:id/edit` | Modification | idem (admin profils) | US2. |
| `/members/:id/profiles` | Profils & droits d'un membre | `authGuard` + `permissionGuard` **any-of** | US3. Attribuer/révoquer si admin profils. |

## Navigation & points d'entrée

- Entrée **« Profils du bureau »** de la coquille : **any-of** (visible en lecture) → `/bureau-profiles`.
- **Fiche membre** (Lot 2) : lien **« Profils & droits »** → `/members/:id/profiles` (visible en
  lecture).

## Gardes (évolution du socle)

- **`permissionGuard`** étendu : autorise si `route.data.permission` (single) **ou** si
  `route.data.anyPermissions: string[]` contient **au moins un** droit détenu. Rétrocompatible.
- Les **actions d'écriture** dans les composants sont masquées si l'utilisateur n'a pas
  `manage_bureau_profiles` (même si l'écran est accessible en lecture).

## Comportements transverses (hérités du socle)

- **Sans droit de lecture** : entrée masquée ; accès direct refusé/redirigé (garde) ; appel API direct → 403.
- **Action destructrice** : confirmation obligatoire (suppression profil, révocation).
- **Session expirée** : purge + retour connexion.
