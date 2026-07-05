# Research — Console web Lumineux (socle & cycle de vie du compte)

Décisions techniques figées avant la conception. La SPA consomme l'API existante **sans la modifier**.

## 1. Framework & langage

- **Décision** : **Angular** (dernière version stable, ≥ v19 : composants **standalone**, **signals**,
  control flow intégré), en **TypeScript**.
- **Rationale** : imposé par la Constitution (« SPA : Angular — tableau de bord de gestion complet »).
  Standalone + signals = socle moderne, sans NgModules, adapté à un état de session léger.
- **Alternatives écartées** : React / Vue — contraires à la Constitution.

## 2. Emplacement dans le dépôt

- **Décision** : nouveau dossier **`web/`** à la racine du **mono-dépôt**, isolé de la solution .NET
  (`src/`).
- **Rationale** : décision PO (mono-dépôt) — CI commune, synchronisation des contrats plus simple,
  versionnement conjoint API/SPA.
- **Alternatives écartées** : dépôt séparé (cycles indépendants mais synchro des contrats à gérer) ;
  imbrication dans `src/` (mélange avec les assemblies .NET).

## 3. Conservation du jeton

- **Décision** : jeton d'accès **en mémoire** dans un **service de session** (signal), jamais dans
  `localStorage`/`sessionStorage`.
- **Rationale** : FR-003 (pas de stockage exposé XSS) ; pas de refresh token dans cet incrément →
  le plus simple **et** le plus sûr. Conséquence : un **rechargement complet déconnecte**
  (reconnexion simple), documenté en spec.
- **Alternatives écartées** : `localStorage` (surface XSS — rejeté) ; cookie `HttpOnly`+`Secure`
  (nécessiterait une évolution de l'API pour émettre/lire le cookie — hors périmètre).

## 4. Source de l'identité et des droits

- **Décision** : après connexion (et pour les gardes de routes), appeler **`GET /auth/me`** (feature
  007) pour obtenir identité + **droits effectifs** ; la SPA **ne décode jamais** le jeton.
- **Rationale** : découple l'UI du format interne du jeton ; les droits retournés = ceux que l'API
  autorise réellement (cohérence RBAC/serveur).
- **Alternatives écartées** : décodage du JWT côté client (couplage au format, rejeté).

## 5. Bootstrap de session

- **Décision** : le jeton étant volatil, un **démarrage à froid** (ou rechargement) sans jeton en
  mémoire ⇒ **non authentifié** ⇒ écran de connexion. `GET /auth/me` alimente identité/droits **après
  connexion** et sert aux gardes.
- **Rationale** : cohérent avec §3 (pas de persistance). Pas de « restauration silencieuse » de
  session dans cet incrément.
- **Alternatives écartées** : restauration via stockage persistant (contraire à §3).

## 6. Gestion centralisée des erreurs

- **Décision** : un **intercepteur d'erreurs** HTTP mappe les réponses de l'API vers des messages
  exploitables :
  - **401** (quelle qu'en soit la forme : challenge middleware **sans corps** pour jeton absent/
    invalide, **ou** ProblemDetails `UnauthorizedException`) → **purge de session + redirection
    connexion**, en conservant l'URL visée (`returnUrl`).
  - **403** `PasswordChangeRequiredException` (extension `code = "password_change_required"`) reçu au
    **login** → bascule vers l'écran d'**activation**.
  - **403** autre (`ForbiddenException`) → message « accès refusé » (l'UI a été trop permissive ;
    l'API reste l'autorité).
  - **400** validation → messages de champ/formulaire (détail = messages concaténés).
  - **409** conflit (avec `code` éventuel) / **404** / **410** → messages adéquats.
- **Rationale** : format ProblemDetails (RFC 7807) homogène de l'API (Constitution V) ; les codes
  métier (`password_change_required`, codes de conflit) sont exposés dans `extensions.code`.
- **Point d'attention** : le 401 du challenge `JwtBearer` a un **corps vide** → l'intercepteur doit
  se baser sur le **statut** (401), pas sur la présence d'un ProblemDetails.

## 7. Contrôle d'accès à l'affichage (RBAC)

- **Décision** : **gardes de routes** (`authGuard` pour « connecté », `permissionGuard` lisant un
  droit requis dans les `data` de route) + **masquage/désactivation** des entrées de navigation selon
  un signal de droits. L'**API reste l'autorité** (403 géré, §6).
- **Rationale** : FR-005 (confort d'affichage) + Constitution IV (autorité serveur). Les droits
  proviennent de `GET /auth/me` (§4).
- **Alternatives écartées** : contrôle purement côté client sans repli serveur (faux sentiment de
  sécurité, rejeté).

## 8. Validation de la politique de mot de passe (côté client)

- **Décision** : validateurs de formulaire réactifs alignés sur la politique API — **longueur
  minimale (8)**, **au moins une lettre et un chiffre** — à titre de **guidage** ; l'API tranche
  (FR-017). La longueur minimale est un **paramètre d'environnement** (défaut 8).
- **Rationale** : l'API n'expose pas d'endpoint révélant `PasswordMinLength` ; la valeur par défaut
  (8) est alignée sur `AuthOptions`. Validation **non autoritaire**.
- **Alternatives écartées** : récupérer la politique via l'API (aucun endpoint dédié — non retenu).

## 9. Détection de la première connexion (activation)

- **Décision** : à la connexion, un **403 `password_change_required`** signale l'obligation de
  changement → la SPA route vers l'écran d'**activation** (référence pré-remplie), qui appelle
  `POST /auth/activate`.
- **Rationale** : comportement confirmé de l'API (feature 003, middleware `code`). Évite un état
  ambigu côté client.

## 10. Outils de test

- **Décision** : **Vitest** (unitaires : `SessionStore`, `AuthApi`, intercepteurs, gardes,
  validateurs) + **Playwright** (e2e : connexion, activation, oublié→reset, changement, garde de
  routes, retour connexion sur 401).
- **Rationale** : Vitest = runner rapide supporté par Angular récent ; Karma est déprécié. Playwright
  = e2e robuste multi-navigateurs.
- **Alternatives écartées** : Karma/Jasmine (déprécié) ; Cypress (équivalent, mais Playwright retenu
  pour la couverture multi-navigateurs).

## 11. Configuration & intégration API

- **Décision** : **URL de base de l'API** via fichiers d'environnement Angular (`apiBaseUrl`).
  **CORS** activé côté API pour l'origine de la SPA (dev `http://localhost:4200`, prod ensuite).
- **Rationale** : séparation config/déploiement standard. Le CORS est une **tâche d'infrastructure
  côté API** (hors périmètre fonctionnel, mais dépendance à lever pour l'exécution).
- **Note** : si l'API ne renvoie pas encore les en-têtes CORS pour l'origine du SPA, l'appel échouera
  en dev — à configurer lors de la mise en service (dépendance identifiée).
