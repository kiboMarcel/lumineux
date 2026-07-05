# Contrat — Routes du module Membres

Routes ajoutées sous la **coquille console protégée** (feature 008). Toutes exigent une session
(`authGuard`) **et** le droit **`manage_members`** (`permissionGuard`). Le RBAC d'affichage est un
confort ; l'API reste l'autorité (403 géré).

| Chemin | Écran | Garde | Notes |
|--------|-------|-------|-------|
| `/members` | Liste + recherche paginée | `authGuard` + `permissionGuard('manage_members')` | US1. État « aucun résultat » géré. |
| `/members/new` | Création | idem | US2. Homonymie confirmable ; remise identifiants (une fois). |
| `/members/:id` | Fiche (consultation) | idem | US1. 404 → « membre introuvable ». |
| `/members/:id/edit` | Correction | idem | US3. Référence en lecture seule ; conflit contact bloquant. |

## Navigation

- L'entrée **« Membres »** de la coquille (feature 008, aujourd'hui un placeholder masqué selon le
  droit) devient un **lien réel** vers `/members`, **visible uniquement** pour les porteurs de
  `manage_members` (FR-001, SC-006).

## Comportements transverses (hérités du socle)

- **Sans le droit** : l'entrée n'apparaît pas ; l'accès direct à une URL du module est **refusé/redirigé**
  (garde) et, en cas d'appel API direct, l'API répond **403** (message « accès refusé »).
- **Session expirée** (401 sur un appel) : purge + retour connexion, en conservant l'URL visée.
- **Quitter un formulaire non enregistré** : la saisie est perdue (pas de brouillon persistant) ; le
  mot de passe temporaire éventuel n'est **pas** ré-affiché.
