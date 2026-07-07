# Contrat — Routes & navigation (reprise de session)

**Aucune nouvelle route.** La reprise est intégrée à l'écran de **démarrage** existant `session-start`
(route `/attendance`, feature 014), déjà gardée `permissionGuard('manage_attendance')`.

## Routes

| Route | Écran | Garde | Nouveauté |
|-------|-------|-------|-----------|
| `/attendance` | `session-start` | `authGuard` + `permissionGuard('manage_attendance')` | **inchangée** — encart de reprise + gestion du conflit ajoutés |
| `/attendance/sessions/:id` | `session-run` | idem | **inchangée** — cible du bouton « Reprendre » |

- Le bouton **« Reprendre »** navigue vers `/attendance/sessions/:id` (écran d'animation existant).

## Navigation (shell)

- Entrée **« Présences »** (feature 014) **inchangée** (déjà `manage_attendance`). Aucune entrée
  supplémentaire.

## Comportement de garde

- Identique à la feature 014 : sans droit `manage_attendance`, l'entrée est masquée et l'accès direct à
  `/attendance` refusé ; l'API reste l'autorité (403).
