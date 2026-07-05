# Contrat — Routage de la SPA

Routes de l'application, leur nature (publique / protégée) et les gardes associées. Les gardes
d'affichage sont un **confort** ; l'API reste l'autorité (401/403 gérés par l'intercepteur).

## Routes

| Chemin | Écran | Accès | Garde | Notes |
|--------|-------|-------|-------|-------|
| `/login` | Connexion | Public | `guestOnly` (rediriger vers console si déjà connecté) | Message d'échec non révélateur. |
| `/auth/activate` | Activation (1re connexion) | Public | — | Atteint aussi automatiquement sur `403 password_change_required`. |
| `/auth/forgot-password` | Mot de passe oublié | Public | — | Message **générique**. |
| `/auth/reset-password` | Réinitialisation | Public | — | Lit `?token=…` depuis l'URL ; correspond au lien e-mail. |
| `/setup/first-admin` | Installation premier admin | Public | `setupGuard` | Inaccessible si instance amorcée (l'API rejette → redirection). |
| `/` (coquille console) | Shell + accueil | **Protégé** | `authGuard` | Navigation adaptée aux droits. |
| `/account/change-password` | Changement de mot de passe | **Protégé** | `authGuard` | Utilisateur connecté. |
| *(futurs modules)* `/members`, `/bureau-profiles`, `/sessions`… | — | **Protégé** | `authGuard` + `permissionGuard(data.permission)` | **Hors périmètre** de cet incrément (placeholders de navigation seulement). |

## Gardes

- **`authGuard`** : exige une session (`SessionStore.isAuthenticated`). Sinon → `/login` en
  conservant l'URL visée (`returnUrl`) pour y revenir après connexion (FR-006).
- **`permissionGuard(permission)`** : exige que `SessionStore.permissions` contienne le droit requis
  (déclaré dans `route.data.permission`). Sinon → écran « accès refusé » / redirection. Sert les
  **futurs** modules ; défini dès cet incrément pour le socle RBAC.
- **`guestOnly`** : empêche d'afficher la connexion à un utilisateur déjà authentifié (→ console).
- **`setupGuard`** : n'autorise `/setup/first-admin` que si l'installation est possible ; l'API fait
  foi (409 si déjà installé).

## Comportements transverses

- **Bootstrap** : à froid, aucune session en mémoire → route publique (login). Après login/activation
  → `establish(token)` + `GET /auth/me` → redirection vers la console (ou `returnUrl`).
- **401 sur toute requête** → `SessionStore.clear()` + redirection `/login` (FR-007).
- **Déconnexion** → `clear()` + `/login` ; un rechargement ne reconnecte pas (jeton volatil).
