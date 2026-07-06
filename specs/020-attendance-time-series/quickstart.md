# Quickstart — API de série temporelle des présences

Guide de validation de la feature 020. Mappe les critères **SC-001..006**.

## Prérequis

- API Lumineux démarrée (SQL Server) avec des **sessions** et **présences** (feature 001) réparties sur
  **plusieurs semaines/mois** (idéalement avec quelques présences **annulées** et des intervalles
  **sans** présence).
- Un compte avec le droit **`manage_attendance`** et un compte **sans** ce droit (401/403).

## Commandes

```powershell
dotnet run --project src/Lumineux.Api      # API
dotnet test                                 # suite complète
```

## Scénarios de validation

### A. Évolution par mois / semaine — US1 (SC-001, SC-002, SC-003)

1. `GET /api/v1/reports/attendance/time-series?from=2026-01-01&to=2026-06-30&granularity=Month`
   → **200**, `points` = **un point par mois** (janv.→juin), chacun avec `validAttendanceCount` et
   `sessionCount` (**SC-001**).
2. Vérifier que la série est **continue** : un mois **sans présence** apparaît à **0** (**SC-002**).
3. Vérifier qu'une présence **annulée** n'est **pas** comptée (**SC-003**).
4. Rejouer avec `granularity=Week` → **un point par semaine ISO** (lundi).

### B. Filtre par antenne — US2

1. Ajouter `&antennaId=<id>` → les valeurs ne concernent que cette antenne.
2. Antenne **sans présence** → tous les points à **0** (aucune erreur).

### C. Validation & sécurité (SC-004, SC-005)

1. `granularity=Day` (non supporté) → **400**, message clair, **aucune agrégation** (**SC-004**).
2. Plage invalide (`to` < `from`, bornes manquantes) → **400**.
3. Sans jeton → **401** ; authentifié **sans** `manage_attendance` → **403** (**SC-005**).

### D. Cohérence inter-rapports (SC-006)

1. Pour une même période **sans filtre d'antenne**, comparer la **somme** des `validAttendanceCount` de
   la série au **total** de `antenna-summary` (feature 018) → **identiques** (**SC-006**).

## Résultat attendu

Tous les scénarios A→D passent ; `dotnet test` vert (Application/Infrastructure/Api) ; série continue,
annulées exclues, granularité/plage validées ; aucune écriture, aucune migration ; cohérence avec 018.
