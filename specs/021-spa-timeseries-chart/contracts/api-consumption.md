# Contrat — Endpoint consommé par le panneau « Évolution » (vue client)

Endpoint **existant** (série temporelle 020). **Aucune modification d'API.** Requiert un jeton Bearer et
le droit **`manage_attendance`** (le module Rapports est déjà gardé).

## Série temporelle (`manage_attendance`, feature 020)

| # | Méthode & chemin | Requête | Réponse | Statuts notables |
|---|------------------|---------|---------|------------------|
| 1 | `GET /api/v1/reports/attendance/time-series?from=&to=&granularity=Week|Month&antennaId=` | — | `200 AttendanceTimeSeriesResponse` | `400` (plage/granularité), `401`, `403` |

## Notes

- **Série continue** : `points` couvre **tous** les intervalles de `[from, to]`, ceux sans donnée à
  **0** → la courbe redescend (pas de rupture).
- **granularity** : `Week` (semaine ISO, libellé `AAAA-Sww`) ou `Month` (`AAAA-MM`) ; « jour » n'est pas
  proposé (non fourni par 020).
- **Filtre** : `antennaId` (repris du tableau de bord) restreint à une antenne ; sinon toutes.
- Le client **présente** seulement : mise à l'échelle des coordonnées SVG (proportionnelles à
  `validAttendanceCount`) et tracé. **Aucun recalcul.**
- Mapping via le socle (`messageForError`) ; 401 gérés globalement (purge + reconnexion).
