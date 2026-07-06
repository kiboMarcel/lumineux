# Contrat — Routes & navigation du module Rapports (SPA)

Route Angular ajoutée à `web/src/app/app.routes.ts`, gardée par `permissionGuard` avec
`data.permission = 'manage_attendance'` (en plus de l'authentification du socle).

## Routes

| Route | Écran | Garde | Rôle |
|-------|-------|-------|------|
| `/reports` | `reports-dashboard` | `authGuard` + `permissionGuard('manage_attendance')` | Tableau de bord : période, filtre antenne, synthèse (tableau + barres), export CSV, panneau taux membre |

- Route inconnue sous `/reports/**` → redirection vers `/reports`.
- Le panneau **taux membre** (`member-rate`) est un composant enfant intégré au tableau de bord (pas de
  route dédiée).

## Navigation (shell)

- Nouvelle entrée **« Rapports »** (lien réel) vers `/reports`, visible seulement si le droit
  `manage_attendance` est présent (`NavItem.permission = 'manage_attendance'`).
- Cohérent avec le masquage existant (`visibleModules` / `canSee()`).

## Comportement de garde

- Sans authentification → redirection connexion (socle).
- Authentifié sans `manage_attendance` → refus d'accès, l'API restant l'autorité (403 également géré si
  atteinte directe).
