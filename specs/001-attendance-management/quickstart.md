# Quickstart — Validation de la gestion de présence

**Feature**: Gestion de la présence aux réunions · **Date**: 2026-07-02

Guide de validation **bout-en-bout** de l'API. Il prouve que les 4 user stories fonctionnent. Les
détails de modèle et de contrat ne sont pas dupliqués ici : voir [data-model.md](./data-model.md) et
[contracts/openapi.yaml](./contracts/openapi.yaml).

## Prérequis

- .NET 10 SDK installé.
- SQL Server accessible (instance locale, LocalDB, ou conteneur) ; chaîne de connexion fournie via
  configuration/secrets (jamais en dur — Constitution IV).
- Un jeton JWT valide pour **un membre du bureau** (droit `manage_attendance`) et un jeton pour **un
  membre standard** (émission via le socle d'auth / fournisseur de test — voir `research.md` §5).
- Données de référence minimales : au moins une **Antenne** et deux **Membres** existants.

## Mise en place

```powershell
# Restaurer et compiler
dotnet build

# Appliquer les migrations code-first (crée/actualise le schéma SQL Server)
dotnet ef database update --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Api

# Lancer l'API (expose Swagger sur /swagger)
dotnet run --project src/Lumineux.Api
```

> Toutes les heures des réponses sont en UTC. Remplacer `{BASE}` par l'URL locale (ex.
> `https://localhost:5001/api/v1`), `{BUREAU_JWT}` et `{MEMBRE_JWT}` par les jetons.

## Scénario de validation

### US1 — Démarrer une session (bureau) — P1

```bash
curl -X POST {BASE}/attendance-sessions \
  -H "Authorization: Bearer {BUREAU_JWT}" -H "Content-Type: application/json" \
  -d '{"antennaId": 1, "meetingDate": "2026-07-05T09:00:00Z", "qrStepSeconds": 30}'
```
**Attendu** : `201` + `SessionResponse` avec `status = Open`, `startTime` renseigné, `id` de session.
Aucun `qrSecret` dans la réponse. Un second appel identique renvoie `409` (une seule session ouverte
par antenne/créneau — FR-003).

Récupérer le QR courant à afficher :
```bash
curl {BASE}/attendance-sessions/{SESSION_ID}/qr -H "Authorization: Bearer {BUREAU_JWT}"
```
**Attendu** : `200` + `token`, `stepSeconds`, `expiresAt`. Le `token` change après `stepSeconds`.

### US2 — Enregistrer sa présence par scan (membre) — P1

```bash
curl -X POST {BASE}/attendance-sessions/{SESSION_ID}/scan \
  -H "Authorization: Bearer {MEMBRE_JWT}" -H "Content-Type: application/json" \
  -d '{"token": "{TOKEN_COURANT}"}'
```
**Attendu** : `201` + `AttendanceResponse` (`source = QrScan`, `arrivalTime` ≈ heure serveur, écart
< 5 s — SC-004). Re-scan du même membre → `200` « déjà présent », `arrivalTime` inchangé (FR-010).
Scan avec un ancien token → `410` (jeton expiré — FR-013a). Membre d'une autre antenne → `201`, la
présence est rattachée à la session de l'antenne visitée (FR-011).

### US3 — Ajouter manuellement une présence (bureau) — P2

```bash
curl -X POST {BASE}/attendance-sessions/{SESSION_ID}/attendances \
  -H "Authorization: Bearer {BUREAU_JWT}" -H "Content-Type: application/json" \
  -d '{"memberId": 2}'
```
**Attendu** : `201` + `AttendanceResponse` (`source = Manual`). Membre déjà présent → `200` sans
doublon. `memberId` inexistant → `404` (FR-017).

Consulter la liste en direct :
```bash
curl {BASE}/attendance-sessions/{SESSION_ID}/attendances -H "Authorization: Bearer {BUREAU_JWT}"
```
**Attendu** : `200` + `validCount` cohérent avec les présences enregistrées (FR-021).

Retirer une présence (avant clôture) :
```bash
curl -X DELETE {BASE}/attendance-sessions/{SESSION_ID}/attendances/2 \
  -H "Authorization: Bearer {BUREAU_JWT}"
```
**Attendu** : `204` ; la présence passe à `Cancelled` (trace conservée — FR-016).

### Synchro hors ligne (US2 étendue) — file locale

```bash
curl -X POST {BASE}/attendance-sessions/{SESSION_ID}/scan/batch \
  -H "Authorization: Bearer {MEMBRE_JWT}" -H "Content-Type: application/json" \
  -d '{"items":[{"clientOperationId":"11111111-1111-1111-1111-111111111111","token":"{TOKEN}","clientArrivalTime":"2026-07-05T09:05:00Z"}]}'
```
**Attendu** : `200` + `results[0].outcome = Created` avec `arrivalTime = clientArrivalTime`. Rejouer
le même lot → `outcome = AlreadyPresent` (idempotence — FR-023a). Un élément dont `clientArrivalTime`
est postérieur à la clôture → `outcome = Rejected` (FR-023b).

### US4 — Clôturer la session (bureau) — P2

```bash
curl -X POST {BASE}/attendance-sessions/{SESSION_ID}/close \
  -H "Authorization: Bearer {BUREAU_JWT}"
```
**Attendu** : `200` + `status = Closed`, `endTime` renseigné. Toutes les présences valides reçoivent
le même `endTime` (FR-006, SC-005). Après clôture : un scan → `409`, un ajout manuel → `409`, une
annulation → `409` (FR-007).

## Contrôles transverses

- **Autorisation** : appeler un endpoint bureau avec `{MEMBRE_JWT}` → `403` ; sans jeton → `401`
  (FR-018).
- **Validation** : corps invalide (ex. `antennaId` manquant) → `400` au format ProblemDetails.
- **Journalisation** : vérifier que ouverture, clôture, scan, ajout, retrait et refus sont journalisés
  sans secret ni donnée personnelle superflue (FR-019, FR-020).

## Vérification par les tests automatisés

```powershell
# Tests unitaires (cœur métier — rapides, sans base)
dotnet test tests/Lumineux.Domain.Tests
dotnet test tests/Lumineux.Application.Tests

# Tests d'intégration (repositories + endpoints, base éphémère)
dotnet test tests/Lumineux.Infrastructure.Tests
dotnet test tests/Lumineux.Api.Tests
```
**Attendu** : suite verte. Les tests couvrent les invariants (transitions de session/présence,
anti-doublon, jeton rotatif, règles hors ligne) et les parcours des 4 user stories.

## Critères de succès validés

| Critère | Vérification |
|---------|--------------|
| SC-001 | Chronométrer US1 (démarrage + QR) < 30 s |
| SC-002 | Chronométrer scan → confirmation < 5 s |
| SC-003 | Aucun doublon / présence fantôme sur un jeu de 100 membres |
| SC-004 | Écart `arrivalTime` vs instant réel du scan < 5 s |
| SC-005 | Après clôture, `endTime` identique pour toutes les présences valides |
| SC-006 | Test de charge : ≥ 200 scans en < 2 min sans erreur |
