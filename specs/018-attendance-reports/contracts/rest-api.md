# Contrat REST — Rapports de présence (`/api/v1/reports/attendance`)

Tous les endpoints exigent un jeton Bearer **et** le droit **`manage_attendance`**
(`[Authorize(Policy = manage_attendance)]`). L'API reste l'autorité (401 non authentifié / 403 sans
droit). **Lecture seule** (aucun effet de bord). Erreurs au format **ProblemDetails** (RFC 7807).

## Endpoints

| # | Méthode & chemin | Paramètres (query) | Réponse | Statuts |
|---|------------------|--------------------|---------|---------|
| 1 | `GET /api/v1/reports/attendance/antenna-summary` | `from`, `to`, `antennaId?` | `200 AntennaAttendanceSummaryResponse` | 200, 400 (plage invalide), 401, 403 |
| 2 | `GET /api/v1/reports/attendance/antenna-summary.csv` | `from`, `to`, `antennaId?` | `200 text/csv` (fichier) | 200, 400, 401, 403 |
| 3 | `GET /api/v1/reports/attendance/member-rate` | `memberId`, `from`, `to` | `200 MemberAttendanceRateResponse` | 200, 400, 404 (membre), 401, 403 |

## Formats

```text
AntennaAttendanceSummaryItem { antennaId, antennaLabel, sessionCount, validAttendanceCount, averageValidPerSession }
AntennaAttendanceSummaryResponse { from, to, items: AntennaAttendanceSummaryItem[] }
MemberAttendanceRateResponse { memberId, memberFullName, from, to, validAttendanceCount, eligibleSessionCount, rate }
```

- `from`/`to` : dates (jour) ISO `yyyy-MM-dd`. Bornes **inclusives** sur `meeting_date`.
- `rate` : nombre entre 0 et 1 (le tableau de bord affichera un %). 0 si `eligibleSessionCount = 0`.
- `averageValidPerSession` : décimal (0 si `sessionCount = 0`).

## Règles par endpoint

- **1 Synthèse** : agrège **par antenne** sur [from, to] ; `antennaId` filtre une antenne. **Seules les
  présences valides** comptent. Période sans session → `items` vide (200).
- **2 Synthèse CSV** : **mêmes données** que (1), rendues en **CSV** (en-têtes + une ligne par antenne).
  Réponse `Content-Type: text/csv`, `Content-Disposition: attachment; filename="...csv"`, UTF-8 (BOM),
  séparateur `;`.
- **3 Taux membre** : `validAttendanceCount` (présences valides du membre) et `eligibleSessionCount`
  (sessions de son **antenne d'origine** sur la période) → `rate`. Membre introuvable → `404`.

## Validation (400)

- `from`/`to` **requis** ; `to ≥ from` ; **plafond de période** (ex. 366 jours). `memberId > 0`.
- Message d'erreur clair, sans détail technique ; aucune agrégation n'est exécutée si la plage est
  invalide.

## RBAC & sécurité

- Droit **`manage_attendance`** (réutilisé, aucun nouveau droit). PII **minimale** (membre = id + nom ;
  aucune coordonnée). Accès journalisé (request logging). Aucune écriture, aucune migration.
