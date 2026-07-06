# Quickstart — API de rapports & statistiques de présence

Guide de validation de la feature 018. Mappe les critères **SC-001..006**.

## Prérequis

- API Lumineux démarrée (SQL Server) avec des **sessions** et **présences** (feature 001) sur une
  période de test (idéalement plusieurs antennes, quelques présences **annulées**).
- Un compte avec le droit **`manage_attendance`** et un compte **sans** ce droit (pour 401/403).

## Commandes

```powershell
dotnet run --project src/Lumineux.Api      # API
dotnet test                                 # suite complète
```

## Scénarios de validation

### A. Synthèse par antenne + période — US1 (SC-001, SC-002)

1. `GET /api/v1/reports/attendance/antenna-summary?from=2026-06-01&to=2026-06-30`
   → **200**, `items` avec, par antenne : `sessionCount`, `validAttendanceCount`,
   `averageValidPerSession` (**SC-001**).
2. Vérifier qu'une présence **annulée** n'est **pas** comptée dans `validAttendanceCount` (**SC-002**).
3. Ajouter `&antennaId=<id>` → seule cette antenne est renvoyée.
4. Période sans session → `items: []` (200, aucune erreur).

### B. Taux de présence par membre — US2 (SC-003)

1. `GET /api/v1/reports/attendance/member-rate?memberId=<id>&from=2026-06-01&to=2026-06-30`
   → **200**, `validAttendanceCount`, `eligibleSessionCount` (= sessions de l'**antenne d'origine**),
   `rate`.
2. Membre **sans présence** sur la période → `rate = 0`, **sans erreur** (pas de division par zéro)
   (**SC-003**).
3. `memberId` inexistant → **404**.

### C. Export CSV — US3 (SC-004)

1. `GET /api/v1/reports/attendance/antenna-summary.csv?from=2026-06-01&to=2026-06-30`
   → **200**, `Content-Type: text/csv`, en-tête `Content-Disposition` (nom de fichier).
2. Ouvrir le CSV : en-têtes clairs, **une ligne par antenne**, **mêmes chiffres** que la synthèse JSON
   (**SC-004**).

### D. Validation & sécurité (SC-005, SC-006)

1. Plage invalide (`to` < `from`, ou bornes manquantes) → **400**, message clair, **aucune agrégation**
   (**SC-006**).
2. Sans jeton → **401** ; authentifié **sans** `manage_attendance` → **403** (**SC-005**).

## Résultat attendu

Tous les scénarios A→D passent ; `dotnet test` vert (Application/Infrastructure/Api) ; aucune écriture
en base, aucune migration ; présences annulées exclues ; CSV cohérent avec le JSON.
