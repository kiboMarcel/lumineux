# Contrat — Endpoints consommés par le module Présences (vue client)

Endpoints **existants** (présences 001, référentiels 010, lookup 015). Aucune modification d'API. Tous
requièrent un jeton Bearer. Les opérations de session/présence exigent le droit **`manage_attendance`**.

## Sessions (`manage_attendance`)

| # | Méthode & chemin | Requête | Réponse | Statuts notables |
|---|------------------|---------|---------|------------------|
| 1 | `POST /api/v1/attendance-sessions` | `{ antennaId, meetingDate, qrStepSeconds? }` | `201 SessionResponse` | `400`, `401`, `403` |
| 2 | `GET /api/v1/attendance-sessions/{id}` | — | `200 SessionResponse` | `401`, `403`, `404` |
| 3 | `GET /api/v1/attendance-sessions/{id}/qr` | — | `200 QrTokenResponse` | `401`, `403`, `404`, `409` (session close) |
| 4 | `POST /api/v1/attendance-sessions/{id}/close` | — | `200 SessionResponse` | `401`, `403`, `404`, `409` |

## Présences (`manage_attendance`)

| # | Méthode & chemin | Requête | Réponse | Statuts notables |
|---|------------------|---------|---------|------------------|
| 5 | `GET /api/v1/attendance-sessions/{id}/attendances?status=Valid|Cancelled|All` | — | `200 AttendanceListResponse` | `401`, `403`, `404` |
| 6 | `POST /api/v1/attendance-sessions/{id}/attendances` | `{ memberId, arrivalTime? }` | `201/200 AttendanceResponse` (**idempotent**) | `400`, `401`, `403`, `404`, `409` (session close) |
| 7 | `DELETE /api/v1/attendance-sessions/{id}/attendances/{memberId}` | — | `204` | `401`, `403`, `404`, `409` (session close) |

## Recherche membre allégée (feature 015 — `manage_attendance` OU `manage_members`)

| # | Méthode & chemin | Réponse |
|---|------------------|---------|
| 8 | `GET /api/v1/members/lookup?query=…` | `200 MemberLookupItem[]` (`{ id, reference, fullName, status }`) |

## Antennes (feature 010 — authentifié)

| # | Méthode & chemin | Réponse |
|---|------------------|---------|
| 9 | `GET /api/v1/reference/antennas` | `200 ReferenceItem[]` (choix de l'antenne au démarrage) |

## Notes

- **QR (3)** : `QrTokenResponse.token` sert **uniquement** à générer l'image du QR côté client ;
  **jamais** affiché en clair ni persisté. Le SPA ré-interroge (3) et regénère avant `expiresAt` /
  au rythme `stepSeconds`.
- **Ajout manuel (6)** : `memberId` obtenu via le lookup (8). **Idempotent** : réajout d'un membre déjà
  présent renvoie l'existant (pas de doublon).
- **Session close** : après clôture (4), les endpoints d'écriture (6/7) et le QR (3) répondent **409** ;
  l'UI masque ces actions et restitue un message clair.
- Mapping via le socle (`messageForError`) ; 401 gérés globalement (purge + connexion).
