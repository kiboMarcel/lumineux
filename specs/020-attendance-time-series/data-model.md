# Data Model — API de série temporelle des présences

**Aucune nouvelle entité persistée, aucune migration.** Vues **calculées** (agrégats temporels) dérivées
en lecture des tables existantes ; la **bucketisation** est faite en mémoire par le handler.

```mermaid
flowchart LR
    S[(attendance_sessions\nantenna, meeting_date)] --> Repo{GetSessionValidCountsAsync}
    A[(attendances\nsession, status=Valid)] --> Repo
    Repo -->|liste (meetingDate, validCount) par session| H[Handler : bucketise semaine/mois + zéros]
    H --> TS["AttendanceTimeSeriesResponse\n(points ordonnés, continus)"]
```

## Sources (lecture seule, inchangées)

| Table | Champs utilisés |
|-------|-----------------|
| `attendance_sessions` | `antenna`, `meeting_date` |
| `attendances` | `session`, `status` (seules les `Valid`) |

## Contrat de sortie (DTO)

| Modèle | Champs |
|--------|--------|
| `TimeSeriesGranularity` (enum) | `Week`, `Month` |
| `TimeSeriesPoint` | `periodStart` (date de début d'intervalle), `label` (`AAAA-MM` ou `AAAA-Sww`), `validAttendanceCount`, `sessionCount` |
| `AttendanceTimeSeriesResponse` | `from`, `to`, `granularity`, `points: TimeSeriesPoint[]` (ordonnés, **continus**) |

## Logique de bucketisation (Application — `TimeBuckets`, pure)

| Granularité | Intervalle | Libellé |
|-------------|-----------|---------|
| `Month` | mois calendaire contenant `meeting_date` ; début = 1er du mois | `AAAA-MM` |
| `Week` | semaine **ISO 8601** (lundi→dimanche) contenant `meeting_date` ; début = lundi | `AAAA-Sww` (semaine ISO) |

- **Génération** : produire **tous** les intervalles de `[from, to]` (bornes incluses), ordre croissant.
- **Affectation** : chaque session (par sa `meeting_date`) tombe dans un intervalle → `sessionCount++`,
  `validAttendanceCount += ValidAttendanceCount(session)`.
- **Zéros** : intervalles sans session → `sessionCount = 0`, `validAttendanceCount = 0`.

## Port de lecture (Domain/Application — extension 018)

- **`IAttendanceReportRepository`** (+ méthode) :
  `GetSessionValidCountsAsync(DateTime from, DateTime to, int? antennaId, ct)` →
  `IReadOnlyList<SessionValidCount>` où `SessionValidCount(DateTime MeetingDate, int ValidAttendanceCount)`.
- **`SessionValidCount`** (record, Domain.Abstractions) : une ligne par **session** de la période
  (filtrée éventuellement par antenne) avec son décompte de présences **valides**.

## Paramètres & validation

| Paramètre | Règles |
|-----------|--------|
| `from`, `to` | requis ; `to ≥ from` ; plafond de période (~366 j) — via `ReportPeriodValidator` |
| `granularity` | `Week` ou `Month` (sinon `400`) |
| `antennaId` | optionnel |

Erreurs : plage/granularité invalide → `400` ; droit manquant → `401/403`.

## Persistance

**Aucune** création/modification. Agrégation calculée à la volée.
