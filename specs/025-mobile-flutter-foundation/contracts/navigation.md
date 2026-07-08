# Contrat de navigation (UI) — Feature 025

Routage `go_router` avec **redirection globale** pilotée par `SessionState`
(voir [../data-model.md](../data-model.md)).

## Routes

| Route | Écran | Accès | Notes |
|-------|-------|-------|-------|
| `/login` | Connexion | anonyme | Liens vers `/auth/forgot`. |
| `/auth/activate` | Activation 1re connexion | anonyme | Atteint automatiquement sur `403 password_change_required` (référence pré-remplie). |
| `/auth/forgot` | Mot de passe oublié | anonyme | Message générique après envoi. |
| `/auth/reset` | Réinitialisation | anonyme | Champs : jeton (e-mail) + nouveau mot de passe. |
| `/home` | Accueil authentifié | **session valide** | Affiche l'identité ; actions compte. |
| `/account/change-password` | Changement de mot de passe | **session valide** | Ancien + nouveau. |

## Règles de redirection (garde)

- `unknown` / `restoring` → écran de **chargement** (splash), aucune navigation utilisateur.
- `anonymous` → forcer `/login` (sauf routes anonymes explicitement demandées : `/auth/forgot`,
  `/auth/reset`).
- `passwordChangeRequired` → forcer `/auth/activate`.
- `authenticated` → si l'utilisateur est sur une route anonyme, rediriger vers `/home`.
- **Expiration/401 en cours d'usage** → `SessionController` passe à `anonymous` ; `refreshListenable`
  déclenche la redirection vers `/login` avec message « Session expirée ».
- **Déconnexion** → purge coffre + `anonymous` → `/login`. Un relancement d'app ne restaure **pas** la
  session.

## États d'écran (transverses)

Chaque écran d'action expose : **chargement** (bouton neutralisé anti double-soumission), **erreur**
(message FR mappé), **succès** (navigation ou confirmation). Aucun secret affiché de façon persistante.
