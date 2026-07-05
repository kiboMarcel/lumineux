# Contrat — Endpoints API consommés par la SPA (vue client)

La SPA consomme les endpoints **existants** de l'API Lumineux (`/api/v1`). Aucune modification d'API.
Ce document fige la **vue client** (requête / réponse / statuts) pour cet incrément.

## Endpoints

| # | Méthode & chemin | Auth | Requête (corps) | Réponse succès | Statuts notables |
|---|------------------|------|-----------------|----------------|------------------|
| 1 | `POST /api/v1/auth/login` | Anonyme | `{ reference, password }` | `200 { accessToken, tokenType, expiresAt }` | `400`, `401` (identifiants), `403` `password_change_required` |
| 2 | `POST /api/v1/auth/activate` | Anonyme | `{ reference, temporaryPassword, newPassword }` | `200 { accessToken, tokenType, expiresAt }` | `400`, `401`, `409` |
| 3 | `POST /api/v1/auth/forgot-password` | Anonyme | `{ reference }` | `200 { message }` (générique) | `400` |
| 4 | `POST /api/v1/auth/reset-password` | Anonyme | `{ token, newPassword }` | `204` | `400`, `401` (générique) |
| 5 | `POST /api/v1/auth/change-password` | **Bearer** | `{ currentPassword, newPassword }` | `204` | `400`, `401` |
| 6 | `GET /api/v1/auth/me` | **Bearer** | — | `200 { memberId, displayName, permissions[] }` | `401` |
| 7 | `POST /api/v1/setup/first-admin` | Anonyme | `{ lastName, firstName, gender, password, email?, mobile? }` | `201 { accessToken, tokenType, expiresAt }` | `400`, `409` (déjà installé) |

- `tokenType` vaut `"Bearer"`. `expiresAt` est informatif (pas de refresh dans cet incrément).
- Les endpoints 1/2/7 renvoient un **jeton** → `SessionStore.establish(token)` puis `GET /auth/me`.
- `permissions[]` (endpoint 6) pilote le RBAC d'affichage. Peut être **vide**.

## Format d'erreur (ProblemDetails RFC 7807)

Corps type : `{ type?, title, status, detail?, instance?, code? }` (`code` dans les extensions).

| Statut | Origine | `code` (extension) | Traitement SPA |
|--------|---------|--------------------|----------------|
| `400` | validation / domaine | — | Messages de champ/formulaire (`detail` = messages concaténés). |
| `401` | jeton absent/invalide (**corps vide**, challenge) **ou** `UnauthorizedException` (ProblemDetails) | — | **Purge session + redirection connexion** (conserver `returnUrl`). Se baser sur le **statut**, pas le corps. |
| `403` | `PasswordChangeRequiredException` (au login) | `password_change_required` | Basculer vers l'écran d'**activation** (référence pré-remplie). |
| `403` | `ForbiddenException` | — | Message « accès refusé » (l'API reste l'autorité). |
| `404` | ressource introuvable | — | Message adéquat. |
| `409` | conflit (ex. premier admin déjà installé) | code éventuel | Message de conflit. |
| `410` | ressource expirée | — | Message adéquat. |
| `5xx` | erreur interne | — | Message générique non technique + réessai. |

## Notes de sécurité (rappels)

- **Anti-énumération** : pour `forgot-password` (3) et `reset-password` (4), relayer **fidèlement** le
  message générique de l'API — n'ajouter **aucune** information distinctive.
- **Aucun secret** dans les journaux client, les URL persistées, ou le stockage.
- Le jeton reçu (1/2/4/7) est gardé **en mémoire** uniquement.
