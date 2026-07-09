# Contrat consommé — API de scan de présence (existant, inchangé)

**Base** : `{{API_BASE_URL}}/api/v1` — HTTPS. Le client mobile **consomme** ce contrat **sans le
modifier** (aucune évolution d'API dans ce lot). Source : `AttendancesController` + `ScanAttendanceHandler`.

## Endpoint

| Méthode & route | Auth | Corps requête | Réponses OK | Erreurs notables |
|-----------------|------|---------------|-------------|------------------|
| `POST /attendance-sessions/{sessionId}/scan` | **Bearer** (membre) | `{ "token": "<jeton>" }` | `201` (créée) · `200` (déjà présente) → `AttendanceResponse` | `410`, `409`, `404`, `403`, `401`, `400` |

### `AttendanceResponse` (corps des réponses 2xx)

```json
{
  "id": 987,
  "sessionId": 123,
  "memberId": 42,
  "memberFullName": "Aline Kouadio",
  "arrivalTime": "2026-07-09T14:32:11Z",
  "endTime": null,
  "source": "Scan",
  "status": "Valid",
  "originAntennaId": 3
}
```

Seuls **`memberFullName`** et **`arrivalTime`** alimentent l'overlay de succès. `arrivalTime` = **heure
serveur** (autorité), affichée telle quelle.

## Règles de consommation

- **Jeton porteur** : `Authorization: Bearer <accessToken>` ajouté par l'intercepteur du socle. Prérequis
  socle : étendre l'intercepteur pour attacher le jeton à `/attendance-sessions/**` (voir plan §Structure /
  research §5).
- **Distinction 201/200** : lire `response.statusCode` — **201** = présence **créée** (`created=true`),
  **200** = **déjà présente** (`created=false`). Les deux sont des **succès** (aucun doublon créé).
- **Corps** : exactement `{ "token": <t du payload QR> }`. Le `sessionId` de l'URL provient du payload QR (`s`).

## Table des erreurs (ProblemDetails, messages FR fournis par le serveur)

| HTTP | Exception serveur | `detail` (exemple) | Action mobile |
|------|-------------------|--------------------|---------------|
| `410 Gone` | `GoneException` | « Code QR expiré : scannez le code affiché actuellement. » | Overlay erreur, re-scan |
| `409 Conflict` | `ConflictException` | « La réunion est terminée : enregistrement impossible. » | Overlay erreur |
| `404 NotFound` | `NotFoundException` | « Session introuvable. » | Overlay erreur |
| `403 Forbidden` | `ForbiddenException` | « Votre compte n'est pas actif… » / « Membre inconnu. » | Overlay erreur |
| `401 Unauthorized` | (jeton de session absent/expiré) | — | **Purge session** → connexion (socle) |
| `400 Bad Request` | validation (`Token` vide) | ProblemDetails validation | Overlay erreur générique |

Aucun secret (jeton, mot de passe) n'apparaît dans ces réponses. Le client **n'ajoute aucune** règle
métier : il présente le résultat/erreur renvoyé par le serveur.

## Ce que le client NE fait PAS (périmètre M1)

- N'appelle **pas** `POST .../scan/batch` (synchronisation hors ligne = lot **M2**).
- N'appelle **aucun** endpoint bureau (`attendances` manuel, `close`, `qr`, liste, membres).
