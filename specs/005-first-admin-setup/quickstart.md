# Quickstart — Validation de l'installation du premier administrateur

**Feature**: Installation du premier administrateur · **Date**: 2026-07-03

Guide de validation bout-en-bout. Détails : [data-model.md](./data-model.md) et
[contracts/openapi.yaml](./contracts/openapi.yaml).

## Prérequis

- Solution .NET 10 buildée ; SQL Server (ou SQLite en test) ; configuration via secrets (`Jwt`, `Auth`, `MemberReference`).
- Base **vierge** (aucun membre actif avec le droit `manage_bureau_profiles`).

## Mise en place

```powershell
dotnet build
dotnet ef database update --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Infrastructure
dotnet run --project src/Lumineux.Api
```
`{BASE}` = ex. `https://localhost:5001/api/v1`.

## Scénario de validation

### US1 — Installation initiale (base vierge, SC-001/SC-004)

```bash
curl -X POST {BASE}/setup/first-admin -H "Content-Type: application/json" \
  -d '{"lastName":"Kouassi","firstName":"Yao","gender":"M","password":"MotDePasse1","email":"yao@lumineux.example"}'
```
**Attendu** : `201 Created` avec `TokenResponse` (accessToken + tokenType=Bearer + expiresAt). La
séquence complète doit s'exécuter en **moins de 30 s** (SC-001) — en pratique < 500 ms p95.

Vérification immédiate — le jeton porte les 3 droits :
```bash
TOKEN="<accessToken de la réponse>"
# Le nouvel admin peut créer un profil (manage_bureau_profiles)
curl -X GET {BASE}/bureau-profiles -H "Authorization: Bearer $TOKEN"
# Il peut lister les membres (manage_members)
curl -X GET {BASE}/members -H "Authorization: Bearer $TOKEN"
# Il peut démarrer une session (manage_attendance)
curl -X POST {BASE}/attendance-sessions -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" -d '{"antennaId":1,"step":1,"scheduledAt":"2026-07-04T10:00:00Z"}'
```
**Attendu** : chaque appel répond **200/201** — aucune étape d'activation ou de changement de mot
de passe n'est nécessaire (SC-004).

### US2 — Verrouillage après première installation (SC-002)

```bash
# Second appel — même payload valide
curl -X POST {BASE}/setup/first-admin -H "Content-Type: application/json" \
  -d '{"lastName":"Autre","firstName":"Tentative","gender":"F","password":"AutreMdp1"}'
```
**Attendu** : `409 Conflict` avec `code = already_installed`. Aucun nouveau membre créé.

Vérification anti-fuite — même avec un payload invalide, le refus reste 409 :
```bash
curl -X POST {BASE}/setup/first-admin -H "Content-Type: application/json" \
  -d '{"lastName":"","password":"faible"}'
```
**Attendu** : `409 Conflict` (`already_installed`), **pas** `400 Bad Request` — le refus prioritaire
protège la structure du payload (FR-005).

### US3 — Idempotence du profil « Administrateur » (SC-006)

Scénario : la migration `BureauProfilesBootstrapper` (feature 004) a déjà créé un profil
« Amorçage », puis on renomme ce profil en « Administrateur » pour simuler le cas. Ou plus
simplement, on part d'une base où le profil « Administrateur » existe déjà mais n'a aucun titulaire
actif (l'unique admin ayant été archivé).

**Attendu** :
- `POST /setup/first-admin` sur cette base répond `201`.
- Le profil « Administrateur » **n'est pas dupliqué** — vérifiable via `GET /bureau-profiles` (un
  seul profil de ce nom).
- Sa liste de droits reste **inchangée** — pas d'écrasement (FR-013).

### Erreurs (base vierge, SC-003)

Mot de passe non conforme :
```bash
curl -X POST {BASE}/setup/first-admin -H "Content-Type: application/json" \
  -d '{"lastName":"X","firstName":"Y","gender":"M","password":"faible"}'
```
**Attendu** : `400 Bad Request` avec message explicite ; aucune ligne créée en base (atomicité).

Payload incomplet (nom absent) :
```bash
curl -X POST {BASE}/setup/first-admin -H "Content-Type: application/json" \
  -d '{"firstName":"Y","gender":"M","password":"MotDePasse1"}'
```
**Attendu** : `400 Bad Request`, atomicité préservée.

## Contrôles transverses (sécurité)

- **Anti-fuite** : après premier succès, TOUTES les tentatives retournent 409 `already_installed`,
  quelle que soit la validité du payload (SC-002, FR-005).
- **Aucun secret journalisé** : les logs ne contiennent ni le mot de passe fourni ni le jeton émis
  (SC-005, FR-010).
- **Atomicité** : en cas d'erreur pendant la séquence, aucune ligne partielle ne subsiste (SC-003).

## Vérification par les tests automatisés

```powershell
dotnet test tests/Lumineux.Application.Tests
dotnet test tests/Lumineux.Api.Tests
```
**Attendu** : suite verte, couvrant installation valide, verrou `already_installed` prioritaire,
idempotence profil, mot de passe faible, atomicité rollback, jeton immédiatement utilisable, et
non-régression des features 001–004.

## Critères de succès validés

| Critère | Vérification |
|---------|--------------|
| SC-001 | Installation < 30 s |
| SC-002 | 100 % des tentatives ultérieures → 409 |
| SC-003 | Aucune ligne partielle en cas d'erreur |
| SC-004 | Jeton immédiatement utilisable sur endpoints protégés |
| SC-005 | Aucun secret / jeton dans les logs |
| SC-006 | Profil « Administrateur » non dupliqué en cas de préexistence |
