# Research — API de série temporelle des présences

Extension analytique **lecture seule** de l'API rapports (018). Réutilise l'infrastructure existante
(Onion, EF Core, RBAC, `ReportPeriodValidator`). **Aucune migration.**

## 1. Bucketisation en mémoire (portable)

- **Décision** : le dépôt renvoie des **comptes par session** (date de réunion + présences valides) sur
  la période/antenne ; le **handler** range chaque session dans son **intervalle** (semaine ISO / mois)
  et **agrège**. La génération des intervalles et l'affectation se font **en mémoire**.
- **Rationale** : les fonctions de troncature de date (semaine ISO, début de mois) **diffèrent** entre
  SQL Server et SQLite (tests) ; agréger en mémoire garantit un comportement **identique** et testable.
  Volumétrie modérée (sessions d'une période bornée).
- **Alternatives écartées** : `GROUP BY` sur expression de date SQL (non portable SQLite/SQL Server) ;
  vues/colonnes calculées (migration inutile).

## 2. Granularités semaine (ISO) & mois

- **Décision** : deux granularités — `Week` au sens **ISO 8601** (lundi→dimanche, libellé `AAAA-Sww`) et
  `Month` **calendaire** (libellé `AAAA-MM`). Toute autre valeur (« jour ») → **400** (message clair).
- **Rationale** : décision PO 2026-07-06 ; ISO 8601 = standard non ambigu pour la semaine.
- **Alternatives écartées** : granularité « jour » (reportée) ; semaine débutant le dimanche (non ISO).

## 3. Série continue (zéros remplis)

- **Décision** : générer **tous** les intervalles de `[from, to]` (bornes incluses) et affecter les
  comptes ; les intervalles sans donnée valent **0**. Ordre chronologique croissant.
- **Rationale** : FR-003/SC-002 ; une courbe d'évolution exige une série **sans trou**.
- **Alternatives écartées** : ne renvoyer que les intervalles avec données (courbe discontinue).

## 4. Décomptes : seules les présences valides

- **Décision** : le dépôt ne compte que `AttendanceStatus.Valid` (comme la synthèse 018) ; les
  `Cancelled` sont exclues.
- **Rationale** : FR-004/SC-003 ; cohérence avec 018 (SC-006 : mêmes totaux sur la même période).
- **Alternatives écartées** : compter toutes les lignes (fausse la tendance).

## 5. Validation (plage + granularité)

- **Décision** : réutiliser `ReportPeriodValidator` (bornes présentes, `to ≥ from`, plafond ~366 j) ; la
  **granularité** est validée dans le handler (doit être `Week`/`Month`, sinon `DomainException` → 400).
- **Rationale** : FR-006/SC-004 ; cohérence avec les rapports 018.
- **Alternatives écartées** : accepter une granularité libre (contrat flou).

## 6. Extension du port de lecture (018)

- **Décision** : ajouter à `IAttendanceReportRepository` la méthode
  `GetSessionValidCountsAsync(from, to, antennaId?)` → liste de `SessionValidCount(DateTime MeetingDate,
  int ValidAttendanceCount)`. Implémentée par `AttendanceReportRepository` (EF, requêtes simples
  `Where`/`Contains`/`Count`, comme la synthèse).
- **Rationale** : réutilise le dépôt de rapports existant ; garde les requêtes SQL simples et portables.
- **Alternatives écartées** : nouveau port dédié (redondant) ; renvoyer déjà bucketisé (couple le SQL à
  la granularité, non portable).

## 7. Endpoint & DTO

- **Décision** : ajouter `GET /api/v1/reports/attendance/time-series?from=&to=&granularity=&antennaId=`
  au `ReportsController` (droit `manage_attendance`). DTO dédiés : `TimeSeriesPoint(periodStart, label,
  validAttendanceCount, sessionCount)`, `AttendanceTimeSeriesResponse(from, to, granularity, points[])`.
- **Rationale** : Principe V ; cohérent avec les autres endpoints de rapports.
- **Alternatives écartées** : surcharger `antenna-summary` (mélange des préoccupations).
