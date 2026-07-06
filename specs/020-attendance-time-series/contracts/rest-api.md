# Contrat REST — Série temporelle des présences (`/api/v1/reports/attendance/time-series`)

Endpoint **ajouté** au `ReportsController` (feature 018). Exige un jeton Bearer **et** le droit
**`manage_attendance`** (`[Authorize(Policy = manage_attendance)]`). **Lecture seule** (aucun effet de
bord). Erreurs au format **ProblemDetails** (RFC 7807).

## Endpoint

| # | Méthode & chemin | Paramètres (query) | Réponse | Statuts |
|---|------------------|--------------------|---------|---------|
| 1 | `GET /api/v1/reports/attendance/time-series` | `from`, `to`, `granularity` (`Week`\|`Month`), `antennaId?` | `200 AttendanceTimeSeriesResponse` | 200, 400 (plage/granularité), 401, 403 |

## Formats

```text
AttendanceTimeSeriesResponse {
  from, to,
  granularity,                 # "Week" | "Month"
  points: TimeSeriesPoint[]    # ordonnés chronologiquement, continus (zéros inclus)
}
TimeSeriesPoint { periodStart, label, validAttendanceCount, sessionCount }
```

- `from`/`to` : dates (jour) ISO `yyyy-MM-dd`. Bornes **inclusives** sur `meeting_date`.
- `granularity` : `Week` (semaine **ISO 8601**, lundi ; `label` = `AAAA-Sww`) ou `Month` (calendaire ;
  `label` = `AAAA-MM`).
- `periodStart` : date de **début** de l'intervalle (lundi de la semaine ISO, ou 1er du mois).
- `validAttendanceCount` / `sessionCount` : décomptes de l'intervalle (**valides** uniquement).

## Règles

- **Série continue** : **tous** les intervalles de `[from, to]` sont présents, ceux sans donnée à **0**.
- **Valides uniquement** : les présences **annulées** sont exclues.
- **Filtre** : `antennaId` restreint à une antenne ; sans filtre, agrège toutes les antennes.
- **Cohérence 018** : sans filtre d'antenne, la **somme** des `validAttendanceCount` de la série égale le
  total de la synthèse (`antenna-summary`) sur la **même période**.

## Validation (400)

- `from`/`to` **requis** ; `to ≥ from` ; **plafond de période** (~366 j) — via `ReportPeriodValidator`.
- `granularity` ∈ { `Week`, `Month` } ; toute autre valeur (ex. `Day`) → **400** (message clair), sans
  agrégation.

## RBAC & sécurité

- Droit **`manage_attendance`** (réutilisé). Aucune donnée personnelle (agrégats). Accès journalisé
  (request logging). **Aucune écriture, aucune migration.**
