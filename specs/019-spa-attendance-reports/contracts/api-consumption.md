# Contrat — Endpoints consommés par le tableau de bord Rapports (vue client)

Endpoints **existants** (rapports 018, référentiels 010, recherche membre 015). **Aucune modification
d'API.** Tous requièrent un jeton Bearer. Les rapports exigent le droit **`manage_attendance`**.

## Rapports (`manage_attendance`, feature 018)

| # | Méthode & chemin | Requête | Réponse | Statuts notables |
|---|------------------|---------|---------|------------------|
| 1 | `GET /api/v1/reports/attendance/antenna-summary?from=&to=&antennaId=` | — | `200 AntennaAttendanceSummaryResponse` | `400` (plage), `401`, `403` |
| 2 | `GET /api/v1/reports/attendance/antenna-summary.csv?from=&to=&antennaId=` | — | `200 text/csv` (**Blob**) | `400`, `401`, `403` |
| 3 | `GET /api/v1/reports/attendance/member-rate?memberId=&from=&to=` | — | `200 MemberAttendanceRateResponse` | `400`, `404` (membre), `401`, `403` |

## Référentiel & recherche (authentifié)

| # | Méthode & chemin | Réponse |
|---|------------------|---------|
| 4 | `GET /api/v1/reference/antennas` (010) | `200 ReferenceItem[]` (filtre d'antenne) |
| 5 | `GET /api/v1/members/lookup?query=…` (015) | `200 MemberLookupItem[]` (sélecteur de membre) |

## Notes

- **Synthèse (1)** : agrégats par antenne pour la période ; `antennaId` filtre. Le client **présente**
  (tableau + barres proportionnelles) sans recalcul.
- **Export CSV (2)** : consommé en **`Blob`** via `HttpClient` (jeton porté par l'intercepteur) puis
  téléchargé côté navigateur ; nom de fichier reflétant la période.
- **Taux membre (3)** : `rate` est une fraction 0..1 → affichée en **pourcentage** ; `memberId` obtenu
  via le lookup (5) ; membre introuvable → `404` mappé.
- Mapping via le socle (`messageForError`) ; 401 gérés globalement (purge + reconnexion).
