# Console web Lumineux (SPA)

Application **Angular 20** (feature 008) : socle + cycle de vie du compte utilisateur, consommant
l'API Lumineux (`../src`). Voir `specs/008-spa-foundation-auth/` pour la spécification complète.

## Prérequis

- Node.js 20+ (testé avec Node 25), npm.
- **API Lumineux démarrée** et joignable, avec **CORS autorisant l'origine de la SPA**
  (dev : `http://localhost:4200`).

> ⚠️ **Dépendance CORS** : l'API (`src/Lumineux.Api`) doit émettre les en-têtes CORS pour l'origine
> du SPA, sans quoi les appels échoueront en développement. À configurer côté API lors de la mise en
> service (tâche d'infrastructure, hors périmètre de la SPA).

## Configuration

`src/environments/environment.ts` :

- `apiBaseUrl` — URL de base de l'API (dev par défaut : `https://localhost:5001`).
- `passwordMinLength` — longueur minimale du mot de passe côté client (guidage, **non** autoritaire ;
  aligné sur la politique serveur, défaut 8).

La configuration de production (`environment.prod.ts`) est substituée à la build (`fileReplacements`).

## Commandes

```bash
npm install            # dépendances
npm start              # serveur de dev (http://localhost:4200)
npm run build          # build de production
npm test               # tests unitaires (Vitest, via ng test)
npm run e2e            # tests end-to-end (Playwright ; API requise)
```

## Sécurité (rappels de conception)

- **Jeton en mémoire uniquement** — jamais dans `localStorage`/`sessionStorage`/cookie. Un
  rechargement complet déconnecte (reconnexion simple ; pas de rafraîchissement de jeton).
- **Identité & droits** via `GET /auth/me` (la SPA ne décode jamais le jeton).
- **RBAC d'affichage** = confort ; l'API reste l'autorité (401/403 gérés).
- **Aucun secret** affiché/persisté/journalisé ; messages **génériques** anti-énumération relayés
  fidèlement (mot de passe oublié / réinitialisation).

## Structure

- `src/app/core/` — session, accès API, intercepteurs (Bearer, erreurs), gardes.
- `src/app/shared/` — validateurs (politique de mot de passe), notifications.
- `src/app/features/` — écrans (login, activate, forgot/reset-password, change-password, setup).
- `src/app/shell/` — coquille console (navigation adaptée aux droits).
- `e2e/` — scénarios Playwright.
