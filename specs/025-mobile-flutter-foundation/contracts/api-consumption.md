# Contrats consommés — API auth (features 003/006/007)

**Base** : `{{API_BASE_URL}}/api/v1/auth` — HTTPS. Le client mobile **consomme** ces contrats **sans les
modifier**. Toutes les routes du cycle de vie du compte sont **anonymes** sauf `me` et `change-password`.

| # | Méthode & route | Auth | Corps requête | Réponse OK | Erreurs notables |
|---|-----------------|------|---------------|-----------|------------------|
| 1 | `POST /auth/login` | Anonyme | `{ reference, password }` | `200 { accessToken, tokenType, expiresAt }` | `400`, `401` (identifiants), **`403` `code=password_change_required`** |
| 2 | `POST /auth/activate` | Anonyme | `{ reference, temporaryPassword, newPassword }` | `200 { accessToken, tokenType, expiresAt }` | `400`, `401`, `409` |
| 3 | `POST /auth/forgot-password` | Anonyme | `{ reference }` | `200 { message }` **générique** | `400` |
| 4 | `POST /auth/reset-password` | Anonyme | `{ token, newPassword }` | `204` | `400`, `401` (jeton invalide/expiré) |
| 5 | `GET /auth/me` | **Bearer** | — | `200 { memberId, displayName, permissions[] }` | `401` |
| 6 | `POST /auth/change-password` | **Bearer** | `{ currentPassword, newPassword }` | `204` | `400`, `401` |

## Règles de consommation

- **Jeton** : `Authorization: Bearer <accessToken>` ajouté par l'intercepteur sur les routes protégées
  (5, 6) et non sur les routes anonymes (1–4).
- **`403 password_change_required`** (route 1) : l'obligation de changement est portée par
  `extensions.code` dans le `ProblemDetails`. → basculer vers l'écran d'**activation** (route 2) avec la
  **référence pré-remplie**.
- **Anti-énumération** (route 3) : afficher le **message générique** renvoyé, identique que le compte
  existe ou non ; ne jamais déduire l'existence d'un compte.
- **Réinitialisation** (route 4) : le `token` provient de l'e-mail de réinitialisation (saisie/collage en
  M0 ; deep link ultérieur). `204` = succès (aucun corps).
- **Restauration de session** : au lancement, si un jeton non expiré existe au coffre, appeler `me`
  (route 5) pour confirmer et charger l'identité ; un `401` → purge + connexion.
- **Format d'erreur** : `ProblemDetails` (RFC 7807) ; lire `title`/`detail` et `extensions.code`. Aucun
  secret n'est présent dans les réponses (contrats serveur garantis).

## Ce que le client NE fait PAS

- Ne réimplémente aucune règle de validation d'identifiants, d'activation ni de réinitialisation
  (autorité serveur).
- N'appelle aucun endpoint de gestion (membres, profils, présences, scan) — hors périmètre M0.
