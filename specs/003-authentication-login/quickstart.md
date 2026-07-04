# Quickstart — Validation de l'authentification

**Feature**: Authentification et connexion des membres · **Date**: 2026-07-03

Guide de validation bout-en-bout. Détails : [data-model.md](./data-model.md) et
[contracts/openapi.yaml](./contracts/openapi.yaml).

## Prérequis

- Solution .NET 10 buildée ; SQL Server ; configuration via secrets (`Jwt`, `Auth`).
- Un **compte membre** existant (feature 002) : soit un compte à activer (`PendingActivation`,
  `mustChangePassword`), soit un compte actif. Éventuellement des lignes `member_permissions` pour le
  membre afin de vérifier les claims.

## Mise en place

```powershell
dotnet build
dotnet ef database update --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Infrastructure
dotnet run --project src/Lumineux.Api
```
`{BASE}` = ex. `https://localhost:5001/api/v1`.

## Scénario de validation

### US2 — Première connexion : activation (compte à activer)

```bash
# La connexion normale échoue tant que non activé → 403 password_change_required
curl -X POST {BASE}/auth/login -H "Content-Type: application/json" \
  -d '{"reference":"LUM-2026-00001","password":"<mot de passe temporaire>"}'

# Activation via l'endpoint dédié
curl -X POST {BASE}/auth/activate -H "Content-Type: application/json" \
  -d '{"reference":"LUM-2026-00001","temporaryPassword":"<temp>","newPassword":"NouveauMdp1"}'
```
**Attendu** : login → **403** `code=password_change_required` ; activate → **200** `TokenResponse`
(jeton + `expiresAt`), compte passé à `Active`, obligation levée. Nouveau mot de passe non conforme
→ **400** ; mot de passe temporaire erroné → **401**.

### US1 — Connexion normale (compte actif)

```bash
curl -X POST {BASE}/auth/login -H "Content-Type: application/json" \
  -d '{"reference":"LUM-2026-00001","password":"NouveauMdp1"}'
```
**Attendu** : **200** `TokenResponse` ; le jeton porte les droits (`permission`) du membre. Le jeton
permet d'appeler les endpoints protégés (features 001/002). Identifiants invalides → **401 générique**
(indistinguable entre référence inconnue et mauvais mot de passe).

### US4 — Verrouillage anti-force brute

```bash
# Répéter des connexions à mauvais mot de passe jusqu'au seuil (défaut 5)
for i in 1 2 3 4 5; do curl -X POST {BASE}/auth/login -H "Content-Type: application/json" \
  -d '{"reference":"LUM-2026-00001","password":"faux"}'; done
# Tentative suivante, même avec le bon mot de passe → refusée (verrouillage)
curl -X POST {BASE}/auth/login -H "Content-Type: application/json" \
  -d '{"reference":"LUM-2026-00001","password":"NouveauMdp1"}'
```
**Attendu** : après le seuil, **401 générique** même avec le bon mot de passe, pendant la durée D ;
après expiration (ou reset au succès), la connexion redevient possible.

### US3 — Changement de mot de passe (connecté)

```bash
curl -X POST {BASE}/auth/change-password -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"currentPassword":"NouveauMdp1","newPassword":"EncorePlusMdp2"}'
```
**Attendu** : **204** ; l'ancien mot de passe ne fonctionne plus, le nouveau oui. Mot de passe actuel
erroné → **401** ; nouveau non conforme → **400**.

## Contrôles transverses (sécurité)

- Aucun mot de passe ni jeton en clair dans les journaux (FR-013, SC-005).
- Messages d'erreur identiques pour « référence inconnue » et « mot de passe erroné » (SC-002).
- Jeton expiré refusé par les endpoints protégés (SC-006).

## Vérification par les tests automatisés

```powershell
dotnet test tests/Lumineux.Domain.Tests
dotnet test tests/Lumineux.Application.Tests
dotnet test tests/Lumineux.Api.Tests
```
**Attendu** : suite verte, couvrant login (succès/générique/verrouillage/must-change), activation,
changement de mot de passe, et le verrouillage effectif.

## Critères de succès validés

| Critère | Vérification |
|---------|--------------|
| SC-001 | Login < 2 s |
| SC-002 | Erreurs invalides indistinguables |
| SC-003 | Accès normal impossible avant activation |
| SC-004 | Blocage après N échecs pendant D |
| SC-005 | Aucun secret/jeton en clair dans les logs |
| SC-006 | Jeton expiré refusé |
