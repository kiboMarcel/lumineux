# Quickstart — API de gestion des antennes

Guide de validation bout-en-bout de la feature 016. Mappe les critères **SC-001..SC-006**.

## Prérequis

- API Lumineux démarrée (SQL Server ; migration d'index unique appliquée).
- Un compte disposant du droit **`manage_referentials`** (attribué via un profil du bureau, feature
  011) et un compte **sans** ce droit (pour vérifier 403).
- Au moins un **district** existant (référentiel), pour le rattachement.

## Commandes

```powershell
# Appliquer la migration puis lancer l'API
dotnet ef database update --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Api
dotnet run --project src/Lumineux.Api

# Tests
dotnet test
```

## Scénarios de validation

### A. Créer une antenne — US1 (SC-001, SC-002)

1. `POST /api/v1/antennas` `{ code: "ANT-TEST", label: "Antenne Test", districtId: <existant> }`
   → **201**, `status = Active`.
2. `GET /api/v1/reference/antennas` (lecture publique 010) → l'antenne **apparaît** (active).
3. Rejouer le `POST` avec le **même code** → **409 `duplicate_code`** (aucun doublon) (**SC-002**).
4. `POST` avec `districtId` inexistant → **400** (validation).

### B. Modifier — US2

1. `PUT /api/v1/antennas/{id}` `{ label: "Antenne Test 2", districtId: <existant> }` → **200**, libellé
   à jour.
2. Tenter d'inclure un `code` différent dans le corps → le **code reste inchangé** (immuable).
3. `PUT` sur un id inconnu → **404**.

### C. Désactiver / réactiver — US3 (SC-003, SC-004)

1. `POST /api/v1/antennas/{id}/deactivate` → **200**, `status = Inactive`.
2. `GET /api/v1/reference/antennas` → l'antenne **n'apparaît plus** (**SC-004**) ; les membres/sessions
   déjà rattachés **conservent** leur référence (**SC-003**).
3. `POST /api/v1/antennas/{id}/activate` → **200**, `status = Active`, réapparaît dans la liste
   publique.
4. **Règle sessions ouvertes** : ouvrir une session sur l'antenne, puis `deactivate` →
   **409 `antenna_has_open_sessions`** ; clôturer la session, re-`deactivate` → **200**.

### D. Liste de gestion — US4

1. `GET /api/v1/antennas` → renvoie **toutes** les antennes, **inactives incluses**, avec `status`
   (contrairement à la lecture publique 010).

### E. Contrôle d'accès — sécurité (SC-005)

1. Sans jeton → **401** sur tous les endpoints.
2. Authentifié **sans** `manage_referentials` → **403** ; la lecture publique 010 reste, elle,
   accessible et **inchangée**.

### F. Traçabilité (SC-006)

1. Après création/modif/désactivation/réactivation, vérifier la présence d'entrées de **journal**
   (opération + auteur + horodatage) et la consignation des **refus**.

## Résultat attendu

Tous les scénarios A→F passent ; suite `dotnet test` verte (Domain/Application/Infrastructure/Api),
lecture publique 010 inchangée, aucun doublon de code, aucune suppression physique.
