# Quickstart — Valider « Mot de passe oublié » (feature 006)

Guide de validation **de bout en bout** du parcours de récupération. À exécuter après implémentation
(migration appliquée). Ne contient pas de code d'implémentation — voir `data-model.md` et
`contracts/openapi.yaml` pour les détails.

## Prérequis

- Solution `.NET 10` compilée (`dotnet build`).
- Migration `PasswordReset` créée et appliquée :
  ```powershell
  dotnet ef migrations add PasswordReset --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Api
  dotnet ef database update --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Api
  ```
- Configuration (`appsettings.Development.json`) — section `Auth` complétée :
  ```json
  "Auth": {
    "PasswordResetMinutes": 30,
    "PasswordResetUrlBase": "https://localhost:4200/auth/reset-password"
  },
  "Email": { "Provider": "Logging" }
  ```
  En dev, `Provider = "Logging"` : aucun SMTP requis. Le jeton est récupéré par le test via un
  `IEmailSender` de test (le lien n'est **jamais** journalisé en clair — SC-004).
- Un membre **actif** avec un **email** en fiche et un compte de connexion (features 002/003).

## Lancer l'API

```powershell
dotnet run --project src/Lumineux.Api
```

## Scénario nominal (US1 + US2) — récupération complète

1. **Demander le reset** — `POST /api/v1/auth/forgot-password`
   ```json
   { "reference": "LUM-2026-00042" }
   ```
   **Attendu** : `200` + message générique. Un lien de reset est produit pour l'email du membre.

2. **Récupérer le jeton** : dans un test d'intégration, via l'`IEmailSender` de test qui capture le
   `resetLink` ; en manuel, via l'inbox (SMTP) ou le point de capture de dev.

3. **Réinitialiser** — `POST /api/v1/auth/reset-password`
   ```json
   { "token": "<jeton-extrait-du-lien>", "newPassword": "Nouveau2Mdp" }
   ```
   **Attendu** : `204` sans corps.

4. **Vérifier l'effet** (SC-006) — `POST /api/v1/auth/login`
   - ancien mot de passe → `401` générique ;
   - `Nouveau2Mdp` → `200` + jeton d'accès.

**Critère SC-001** : étapes 1→4 réalisables en < 5 minutes.

## Scénarios de sécurité (à couvrir en tests d'intégration)

| # | Cas | Requête | Attendu | Critère |
|---|-----|---------|---------|---------|
| A | Référence inexistante | forgot-password `{ "reference": "INCONNU" }` | `200` identique, **aucun** email | SC-002, FR-002 |
| B | Compte sans email | forgot-password (membre sans email) | `200` identique, **aucun** email | FR-011 |
| C | Membre archivé / non actif | forgot-password | `200` identique, **aucun** email | FR-012 |
| D | Compte verrouillé (feature 003) | forgot-password | `200` identique | FR-002 |
| E | Rejeu d'un jeton consommé | reset-password (jeton de l'étape 3) | `401` générique | SC-005, FR-008 |
| F | Jeton expiré | reset-password (jeton > 30 min) | `401` générique | FR-004, FR-008 |
| G | Jeton inexistant | reset-password `{ "token": "xxx", ... }` | `401` générique | FR-008 |
| H | Mot de passe faible | reset-password `{ "newPassword": "abc" }` | `400`, jeton **non consommé** (réessai OK) | FR-006 |
| I | Verrouillage levé | verrouiller le compte, reset, puis login immédiat | login `200` sans attendre l'expiration | SC-007 |

**Égalité de réponse (SC-002)** : les cas A/B/C/D et le nominal (compte actif+email) doivent renvoyer
un **corps et un code identiques**. À asserter octet à octet dans le test.

**Anti-fuite (SC-004)** : vérifier qu'aucun log ni la base ne contient le **jeton en clair** — seule
l'empreinte (`token_hash`) figure en base.

## Tests automatisés attendus

- **Domain** (`PasswordResetTokenTests`) : `Issue` (invariants), `IsUsable` (actif/expiré/consommé),
  `Consume` (marque + refus double consommation).
- **Application** :
  - `RequestPasswordResetHandler` — réponse générique + email envoyé (compte actif+email) ; **aucun**
    email + opération factice (compte absent/sans email/non actif) ; jeton persisté sous forme
    d'empreinte uniquement.
  - `ResetPasswordHandler` — succès (empreinte mise à jour, jeton consommé, compteurs remis à zéro) ;
    rejeu → 401 ; expiré → 401 ; introuvable → 401 ; mot de passe faible → 400 sans consommation.
- **API** (`AuthForgotPasswordEndpointsTests`, `AuthResetPasswordEndpointsTests`) : codes HTTP et
  égalité de réponse des scénarios A–I ci-dessus.

## Nettoyage

Les jetons expirés/consommés peuvent rester en base sans effet (refusés). Aucun job de purge n'est
livré dans cette itération (voir `spec.md` §Assumptions).
