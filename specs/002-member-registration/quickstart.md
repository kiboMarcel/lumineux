# Quickstart — Validation de l'ajout d'un membre

**Feature**: Ajout d'un nouveau membre · **Date**: 2026-07-03

Guide de validation bout-en-bout de l'API. Détails non dupliqués : voir
[data-model.md](./data-model.md) et [contracts/openapi.yaml](./contracts/openapi.yaml).

## Prérequis

- Solution .NET 10 (feature 001) buildée ; SQL Server accessible ; configuration via secrets.
- Un jeton JWT pour un **membre du bureau** disposant du droit `manage_members`.
- Données de référence minimales : au moins une **Antenne** (et, pour les tests optionnels,
  civilité/nationalité/ville/district).
- Fournisseur d'e-mail configuré (`Email:Provider`) : `Logging` en dev, `Smtp` en prod (secrets SMTP).

## Mise en place

```powershell
dotnet build
# Migration d'enrichissement members + création member_accounts
dotnet ef database update --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Infrastructure
dotnet run --project src/Lumineux.Api
```

Remplacer `{BASE}` (ex. `https://localhost:5001/api/v1`) et `{BUREAU_JWT}`.

## Scénario de validation

### US1 — Créer un nouveau membre (bureau) — P1

```bash
curl -X POST {BASE}/members \
  -H "Authorization: Bearer {BUREAU_JWT}" -H "Content-Type: application/json" \
  -d '{"lastName":"Doe","firstName":"Jane","gender":"F","email":"jane.doe@example.com","antennaId":1}'
```
**Attendu** : `201` + `MemberCreatedResponse` avec `member.reference` renseignée, `member.status = Active`,
`loginId = reference`, `credentialsDelivery = EmailSent` (e-mail fourni). Aucun `passwordHash` ni mot de
passe dans la réponse (e-mail présent → pas de `temporaryPassword`). (FR-001, FR-004, FR-009)

Repli **remise-bureau** (membre sans e-mail) :
```bash
curl -X POST {BASE}/members \
  -H "Authorization: Bearer {BUREAU_JWT}" -H "Content-Type: application/json" \
  -d '{"lastName":"Traore","firstName":"Ali","gender":"M","mobile":"+2250700000000","antennaId":1}'
```
**Attendu** : `201` avec `credentialsDelivery = BureauHandout` et un `temporaryPassword` **présent une
seule fois** (à remettre au membre). (FR-011)

Refus (droit / validation / référence) :
- Sans droit `manage_members` → `403`. Sans jeton → `401`.
- Champs obligatoires manquants (ni mobile ni email, ou antenne absente) → `400` ProblemDetails. (FR-003)
- `antennaId`/nomenclature inconnue → `404`. (FR-005)

### US2 — Doublons et unicité des contacts — P2

Homonyme (FR-007) :
```bash
# Après avoir créé "Jane Doe", recréer une "Jane Doe"
curl -X POST {BASE}/members -H "Authorization: Bearer {BUREAU_JWT}" -H "Content-Type: application/json" \
  -d '{"lastName":"Doe","firstName":"Jane","gender":"F","mobile":"+2250711111111","antennaId":1}'
```
**Attendu** : `409` `code = duplicate_name` + `duplicateMemberIds`. En **confirmant** :
```bash
curl -X POST {BASE}/members -H "Authorization: Bearer {BUREAU_JWT}" -H "Content-Type: application/json" \
  -d '{"lastName":"Doe","firstName":"Jane","gender":"F","mobile":"+2250711111111","antennaId":1,"confirmDuplicate":true}'
```
**Attendu** : `201` (homonyme distinct créé).

Coordonnée déjà utilisée par un membre actif (FR-008) :
```bash
# Réutiliser l'e-mail de "Jane Doe" active
curl -X POST {BASE}/members -H "Authorization: Bearer {BUREAU_JWT}" -H "Content-Type: application/json" \
  -d '{"lastName":"Autre","firstName":"Personne","gender":"F","email":"jane.doe@example.com","antennaId":1}'
```
**Attendu** : `409` `code = contact_in_use` (aucune création).

### US3 — Consultation et correction (bureau) — P2

```bash
curl "{BASE}/members?query=Doe" -H "Authorization: Bearer {BUREAU_JWT}"          # 200 + résultats paginés
curl {BASE}/members/{MEMBER_ID} -H "Authorization: Bearer {BUREAU_JWT}"           # 200 fiche
curl -X PUT {BASE}/members/{MEMBER_ID} -H "Authorization: Bearer {BUREAU_JWT}" \
  -H "Content-Type: application/json" -d '{"address":"Nouvelle adresse"}'         # 200, modification tracée
```
**Attendu** : recherche par nom/prénom/référence (FR-013) ; correction persistée et tracée
(auteur/horodatage, FR-014).

## Contrôles transverses (sécurité)

- Aucun `passwordHash` ni mot de passe temporaire dans les journaux (vérifier les logs après création). (FR-016, SC-005)
- Atomicité : provoquer une erreur (ex. nomenclature invalide) et vérifier qu'**aucun** membre ni
  compte n'est créé. (FR-006, SC-003)
- Compte provisionné sans droit de gestion (le membre ne peut pas créer d'autres membres). (FR-012)

## Vérification par les tests automatisés

```powershell
dotnet test tests/Lumineux.Domain.Tests
dotnet test tests/Lumineux.Application.Tests
dotnet test tests/Lumineux.Infrastructure.Tests
dotnet test tests/Lumineux.Api.Tests
```
**Attendu** : suite verte, couvrant création + provisionnement, atomicité, doublons/confirmation,
refus de contact, hachage du mot de passe, unicités (référence, contact actif), recherche et correction.

## Critères de succès validés

| Critère | Vérification |
|---------|--------------|
| SC-001 | Chronométrer la création complète < 3 min |
| SC-002 | Chaque membre créé a une référence unique + un compte (aucun doublon de référence) |
| SC-003 | Données manquantes / références inconnues → refus sans création partielle |
| SC-004 | Homonyme signalé avant enregistrement (409 duplicate_name) |
| SC-005 | Aucun identifiant/mot de passe en clair dans logs/réponses |
