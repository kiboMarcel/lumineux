# Contrat — Endpoints consommés par le module Profils & droits (vue client)

Endpoints **existants** (profils feature 004, membres feature 002, catalogue). Aucune modification
d'API. Tous requièrent un jeton Bearer. **Lecture** = `manage_bureau_profiles` OU `manage_members` ;
**écriture** = `manage_bureau_profiles`.

## Profils du bureau

| # | Méthode & chemin | Accès | Requête | Réponse | Statuts notables |
|---|------------------|-------|---------|---------|------------------|
| 1 | `GET /api/v1/bureau-profiles` | lecture | — | `200 BureauProfileSummary[]` | `401`, `403` |
| 2 | `GET /api/v1/bureau-profiles/{id}` | lecture | — | `200 BureauProfileDetail` | `401`, `403`, `404` |
| 3 | `POST /api/v1/bureau-profiles` | écriture | `BureauProfileWriteRequest` | `201 BureauProfileDetail` | `400` (permission inconnue), `401`, `403`, `409` (`duplicate_name`) |
| 4 | `PUT /api/v1/bureau-profiles/{id}` | écriture | `BureauProfileWriteRequest` | `200 BureauProfileDetail` | `400`, `401`, `403`, `404`, `409` (`duplicate_name` / `last_administrator`) |
| 5 | `DELETE /api/v1/bureau-profiles/{id}` | écriture | — | `204` | `401`, `403`, `404`, `409` (`profile_in_use` / `last_administrator`) |

## Attribution aux membres

| # | Méthode & chemin | Accès | Requête | Réponse | Statuts notables |
|---|------------------|-------|---------|---------|------------------|
| 6 | `GET /api/v1/members/{memberId}/bureau-profiles` | lecture | — | `200 MemberProfilesResponse` (profils + droits effectifs) | `401`, `403`, `404` |
| 7 | `POST /api/v1/members/{memberId}/bureau-profiles` | écriture | `{ profileId }` | `204` (**idempotent**) | `400`, `401`, `403`, `404`, `409` (`member_inactive`) |
| 8 | `DELETE /api/v1/members/{memberId}/bureau-profiles/{profileId}` | écriture | — | `204` | `401`, `403`, `404`, `409` (`last_administrator`) |

## Catalogue de droits

| # | Méthode & chemin | Accès | Réponse |
|---|------------------|-------|---------|
| 9 | `GET /api/v1/permissions` | authentifié | `200 PermissionDescriptor[]` (`{ code, label }`) |

## Notes

- **3/4** : `permissions` = **codes** issus du catalogue (9). Un code hors catalogue → `400`.
- **7** : réattribuer un profil déjà présent renvoie succès (idempotent) — pas d'erreur ni doublon.
- **Garde-fous** (`last_administrator`, `profile_in_use`, `member_inactive`) : erreurs **bloquantes**
  restituées avec un message clair (jamais contournées côté client).
- Mapping via le socle (`messageForError`) pour 400/404/5xx ; traitement des **409 par `code`**.
