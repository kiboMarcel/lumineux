# Contrat — Routes & navigation du module Présences (SPA)

Routes Angular ajoutées à `web/src/app/app.routes.ts`, toutes **gardées** par
`permissionGuard('manage_attendance')` (en plus de l'authentification du socle).

## Routes

| Route | Écran | Garde | Rôle |
|-------|-------|-------|------|
| `/attendance` | `session-start` | `authGuard` + `permissionGuard('manage_attendance')` | Démarrer une session (antenne + date + pas de rotation) |
| `/attendance/sessions/:id` | `session-run` | idem | Animation : QR rotatif, liste temps réel, ajout manuel, annulation, clôture |

- Après démarrage réussi → navigation vers `/attendance/sessions/:id`.
- `session-run` **recharge** l'état par `:id` (`GET /attendance-sessions/{id}`) — rechargement de page
  et partage d'URL supportés (état serveur, aucun état client persistant requis).
- Route inconnue sous `/attendance/**` → redirection vers `/attendance`.

## Navigation (shell)

- L'entrée **« Présences »** (placeholder feature 008) devient un **lien réel** vers `/attendance`,
  visible seulement si le droit `manage_attendance` est présent (`NavItem.permission = 'manage_attendance'`).
- Cohérent avec le masquage existant (`visibleModules` / `canSee()`).

## Comportement de garde

- Sans authentification → redirection connexion (socle).
- Authentifié sans `manage_attendance` → refus d'accès (redirection/accueil), l'API restant l'autorité
  (403 également géré si atteinte directe).
