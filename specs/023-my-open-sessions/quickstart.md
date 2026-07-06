# Quickstart — API : mes sessions de présence ouvertes

Guide de validation de la feature 023. Mappe les critères **SC-001..005**.

## Prérequis

- API Lumineux démarrée (SQL Server) avec au moins une **antenne** active.
- Deux comptes avec le droit **`manage_attendance`** (pour vérifier l'isolation par initiateur) et un
  compte **sans** ce droit (401/403).

## Commandes

```powershell
dotnet run --project src/Lumineux.Api      # API
dotnet test                                 # suite complète
```

## Scénarios de validation

### A. Retrouver ma session ouverte — US1 (SC-001, SC-002)

1. Avec le compte **A** (`manage_attendance`), démarrer une session
   (`POST /api/v1/attendance-sessions`).
2. `GET /api/v1/attendance-sessions/mine/open` (compte A) → **200**, la liste **contient** cette session
   (id, antenne, date, heure de début, statut ouvert) (**SC-001**).
3. **Clôturer** la session (`POST .../{id}/close`), rappeler `mine/open` → la session **n'apparaît plus**
   (**SC-002**).
4. Sans aucune session ouverte → **liste vide** (200).

### B. Isolation par initiateur — (SC-003)

1. Avec le compte **B**, démarrer une session sur une **autre** antenne.
2. `GET .../mine/open` avec le compte **A** → ne renvoie **que** les sessions de A (pas celle de B)
   (**SC-003**).

### C. Sécurité (SC-004)

1. Sans jeton → **401**.
2. Authentifié **sans** `manage_attendance` → **403** (**SC-004**).

### D. Lecture seule (SC-005)

1. Avant/après l'appel `mine/open`, l'état des sessions est **inchangé** (aucune création/clôture)
   (**SC-005**).

## Résultat attendu

Tous les scénarios A→D passent ; `dotnet test` vert (Application/Infrastructure/Api) ; seules les
sessions ouvertes de l'utilisateur sont renvoyées ; aucune écriture, aucune migration.
