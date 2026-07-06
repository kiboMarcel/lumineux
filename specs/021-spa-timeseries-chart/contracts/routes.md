# Contrat — Routes & navigation (panneau « Évolution »)

**Aucune nouvelle route.** Le panneau « Évolution » est **intégré** au tableau de bord existant
`reports-dashboard` (route `/reports`, feature 019), déjà gardée `permissionGuard('manage_attendance')`.

## Routes

| Route | Écran | Garde | Nouveauté |
|-------|-------|-------|-----------|
| `/reports` | `reports-dashboard` (+ panneau `time-series-chart`) | `authGuard` + `permissionGuard('manage_attendance')` | **inchangée** — le panneau courbe est ajouté au tableau de bord |

## Navigation (shell)

- Entrée **« Rapports »** (feature 019) **inchangée** (déjà `manage_attendance`). Aucune entrée
  supplémentaire.

## Comportement de garde

- Identique à la feature 019 : sans droit `manage_attendance`, l'entrée est masquée et l'accès direct à
  `/reports` refusé ; l'API reste l'autorité (403).
