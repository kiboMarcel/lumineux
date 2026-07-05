# Contrat — Endpoints consommés par le module Membres (vue client)

Le module consomme les endpoints **existants** de l'API (membres feature 002, référentiels feature
010). Aucune modification d'API. Tous requièrent un jeton Bearer.

## Membres (`manage_members` requis)

| # | Méthode & chemin | Requête | Réponse succès | Statuts notables |
|---|------------------|---------|----------------|------------------|
| 1 | `GET /api/v1/members?query&page&pageSize` | — | `200 MemberListResponse` | `401`, `403` |
| 2 | `GET /api/v1/members/{id}` | — | `200 MemberResponse` | `401`, `403`, `404` |
| 3 | `POST /api/v1/members` | `CreateMemberRequest` (dont `confirmDuplicate`) | `201 MemberCreatedResponse` | `400`, `401`, `403`, `409` (`duplicate_name` / `contact_in_use`) |
| 4 | `PUT /api/v1/members/{id}` | `UpdateMemberRequest` | `200 MemberResponse` | `400`, `401`, `403`, `404`, `409` (`contact_in_use`) |

- **3** : `MemberCreatedResponse = { member, loginId, credentialsDelivery, temporaryPassword? }`.
  `temporaryPassword` **présent uniquement** si `credentialsDelivery = "BureauHandout"` → **affiché une
  seule fois**, jamais persisté.
- **Homonymie (3)** : premier envoi sans `confirmDuplicate` ; si `409 code=duplicate_name`, confirmer
  puis renvoyer avec `confirmDuplicate=true`.
- **Contact (3 & 4)** : `409 code=contact_in_use` = **bloquant** (non confirmable).

## Référentiels (authentifié — feature 010)

| # | Méthode & chemin | Réponse |
|---|------------------|---------|
| 5 | `GET /api/v1/reference/antennas` | `200 ReferenceItem[]` (**requis** pour la création) |
| 6 | `GET /api/v1/reference/civilities` | `200 ReferenceItem[]` |
| 7 | `GET /api/v1/reference/cities` | `200 ReferenceItem[]` |
| 8 | `GET /api/v1/reference/districts` | `200 ReferenceItem[]` |
| 9 | `GET /api/v1/reference/countries` | `200 Country[]` (`{ id, code, country, nationality }`) |

## Format d'erreur (rappel)

ProblemDetails `{ title, status, detail?, code?, duplicateMemberIds? }`. Le client mappe via le socle
(`messageForError`) pour 400/404/5xx, et traite spécifiquement les **409** selon `code` (cf.
`data-model.md`). Les 401 sont gérés globalement (purge + connexion) ; les 403 affichent « accès
refusé » (l'API reste l'autorité malgré le RBAC d'affichage).
