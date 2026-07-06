# Contrat — Routes & navigation du module Antennes (SPA)

Routes Angular ajoutées à `web/src/app/app.routes.ts`, toutes **gardées** par
`permissionGuard` avec `data.permission = 'manage_referentials'` (en plus de l'authentification du
socle).

## Routes

| Route | Écran | Garde | Rôle |
|-------|-------|-------|------|
| `/antennas` | `antenna-list` | `authGuard` + `permissionGuard('manage_referentials')` | Liste de gestion (actives + inactives) + actions activer/désactiver |
| `/antennas/new` | `antenna-form` | idem | Créer une antenne (code + libellé + district) |
| `/antennas/:id/edit` | `antenna-form` | idem | Modifier (libellé + district ; **code lecture seule**) |

- Après création/modification réussie → retour à `/antennas` (liste rafraîchie).
- Route inconnue sous `/antennas/**` → redirection vers `/antennas`.

## Navigation (shell)

- Nouvelle entrée **« Antennes »** (lien réel) vers `/antennas`, visible seulement si le droit
  `manage_referentials` est présent (`NavItem.permission = 'manage_referentials'`).
- Cohérent avec le masquage existant (`visibleModules` / `canSee()`).

## Comportement de garde

- Sans authentification → redirection connexion (socle).
- Authentifié sans `manage_referentials` → refus d'accès (redirection/accueil), l'API restant
  l'autorité (403 également géré si atteinte directe).
