# Quickstart — Validation des Profils du bureau

**Feature**: Profils du bureau · **Date**: 2026-07-03

Guide de validation bout-en-bout. Détails : [data-model.md](./data-model.md) et
[contracts/openapi.yaml](./contracts/openapi.yaml).

## Prérequis

- Solution .NET 10 buildée ; SQL Server ; configuration via secrets (`Jwt`, `Auth`).
- Au moins un membre bootstrap doté du droit `manage_bureau_profiles` : soit via la migration au
  démarrage (profil « Amorçage ») s'il y avait des droits amorcés dans `Auth:Bootstrap:*`, soit en
  ajoutant `manage_bureau_profiles` à `Auth:Bootstrap:Permissions` avant le premier démarrage.

## Mise en place

```powershell
dotnet build
dotnet ef database update --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Infrastructure
dotnet run --project src/Lumineux.Api
```
`{BASE}` = ex. `https://localhost:5001/api/v1`. `{ADMIN_TOKEN}` = jeton d'un membre disposant de
`manage_bureau_profiles` (obtenu via `/auth/login`).

## Scénario de validation

### US1 — Créer, modifier, supprimer un profil (SC-001, FR-001..003, FR-015)

```bash
# Créer un profil
curl -X POST {BASE}/bureau-profiles \
  -H "Authorization: Bearer {ADMIN_TOKEN}" -H "Content-Type: application/json" \
  -d '{"name":"Gestion des présences","description":"Sessions et présences","permissions":["manage_attendance"]}'

# Modifier (ajouter manage_members)
curl -X PUT {BASE}/bureau-profiles/1 \
  -H "Authorization: Bearer {ADMIN_TOKEN}" -H "Content-Type: application/json" \
  -d '{"name":"Gestion des présences","description":"…","permissions":["manage_attendance","manage_members"]}'

# Supprimer (tant qu'aucune attribution)
curl -X DELETE {BASE}/bureau-profiles/1 -H "Authorization: Bearer {ADMIN_TOKEN}"
```
**Attendu** : 201 (création), 200 (modif), 204 (suppression). Un droit inconnu → 400 ; nom dupliqué
→ 409 (`duplicate_name`) ; utilisateur sans `manage_bureau_profiles` → 403.

### US2 — Attribuer un profil et vérifier l'effet sur le jeton (SC-005, FR-004..007)

```bash
# Attribuer
curl -X POST {BASE}/members/{MEMBER_ID}/bureau-profiles \
  -H "Authorization: Bearer {ADMIN_TOKEN}" -H "Content-Type: application/json" \
  -d '{"profileId": 1}'

# Reconnexion du membre concerné
curl -X POST {BASE}/auth/login -H "Content-Type: application/json" \
  -d '{"reference":"LUM-2026-00042","password":"MdpValide1"}'

# Vérification : le membre peut désormais démarrer une session (endpoint qui exige manage_attendance)
curl -X POST {BASE}/attendance-sessions \
  -H "Authorization: Bearer {NEW_TOKEN}" -H "Content-Type: application/json" -d '{...}'
```
**Attendu** : 204 (attribution) ; 200 (login) ; le nouveau jeton porte les droits du profil.
Ré-attribution idempotente : 204 également. Attribution à un membre inactif : 400.

### US3 — Révoquer / faire évoluer les droits (FR-004, FR-006, FR-012)

```bash
# Révoquer une attribution
curl -X DELETE {BASE}/members/{MEMBER_ID}/bureau-profiles/1 -H "Authorization: Bearer {ADMIN_TOKEN}"

# Retirer un droit d'un profil (retire ce droit pour tous les titulaires à leur prochaine émission de jeton)
curl -X PUT {BASE}/bureau-profiles/2 \
  -H "Authorization: Bearer {ADMIN_TOKEN}" -H "Content-Type: application/json" \
  -d '{"name":"…","permissions":["manage_members"]}'  # manage_attendance retiré
```
**Attendu** : 204 (révocation) ; 200 (modif) ; les titulaires perdent le droit à leur prochain login.
Une révocation qui laisserait 0 administrateur des profils → 409 `last_administrator` (SC-004).

### US4 — Consulter les profils et leurs titulaires (FR-009)

```bash
# Catalogue des profils (accès manage_bureau_profiles OU manage_members)
curl -X GET {BASE}/bureau-profiles -H "Authorization: Bearer {ADMIN_OR_MEMBERS_TOKEN}"

# Profils et droits effectifs d'un membre
curl -X GET {BASE}/members/{MEMBER_ID}/bureau-profiles -H "Authorization: Bearer {ADMIN_OR_MEMBERS_TOKEN}"
```
**Attendu** : 200 avec la liste des profils (nom, droits, nombre de titulaires) ; pour un membre,
la liste de ses profils et l'union `effectivePermissions`.

### Migration au démarrage — Profil « Amorçage » (FR-013)

Au premier démarrage post-livraison, si la table `member_permissions` contient des lignes issues du
bootstrap (feature 003), le service `BureauProfilesBootstrapper` :
1. crée un profil `Amorçage` avec l'ensemble des droits amorcés ;
2. l'assigne au membre référencé par `Auth:Bootstrap:MemberReference` ;
3. journalise l'action ; **n'écrase pas** un profil `Amorçage` déjà existant (idempotence).

Vérification :
```bash
curl -X GET {BASE}/bureau-profiles -H "Authorization: Bearer {ADMIN_TOKEN}" | jq '.[] | select(.name=="Amorçage")'
```
**Attendu** : un profil `Amorçage` visible avec les droits amorcés et 1 titulaire.

## Contrôles transverses (sécurité)

- Aucune donnée sensible dans les journaux (FR-010, SC-006) — mots de passe et jetons interdits.
- Tentative d'écriture sans `manage_bureau_profiles` → **403** systématique (SC-002).
- Tentative d'auto-révocation qui laisserait 0 admin → **409** `last_administrator` (SC-004).

## Vérification par les tests automatisés

```powershell
dotnet test tests/Lumineux.Domain.Tests
dotnet test tests/Lumineux.Application.Tests
dotnet test tests/Lumineux.Api.Tests
```
**Attendu** : suite verte, couvrant CRUD des profils, attribution/révocation (dont idempotence et
garde-fou dernier admin), union des droits effectifs, migration idempotente au démarrage, et non-
régression des flux d'authentification (features 001/002/003).

## Critères de succès validés

| Critère | Vérification |
|---------|--------------|
| SC-001 | Créer + attribuer + reconnecter < 2 min |
| SC-002 | 100 % des écritures non autorisées → 403 |
| SC-003 | Aucune suppression de profil attribué |
| SC-004 | Aucun état sans administrateur (garde-fou triple) |
| SC-005 | Union sans doublon des droits — vérifiée par tests |
| SC-006 | Aucun secret dans les logs |
