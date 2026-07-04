# Quickstart — Validation de `GET /api/v1/auth/me` (feature 007)

Guide de validation de bout en bout. Prérequis : solution buildée, une instance avec au moins un
compte actif (via l'installation premier admin, feature 005, ou un membre provisionné + activé).

> Rappel : les droits retournés sont ceux **de la session** (jeton courant). Après un changement de
> droits, se **reconnecter** pour les voir évoluer.

## Scénario A — Profil d'un utilisateur connecté (US1, chemin nominal)

1. **Se connecter** pour obtenir un jeton :
   `POST /api/v1/auth/login` avec `{ "reference": "<REF>", "password": "<MDP>" }` → 200 + `accessToken`.
2. **Appeler le profil** :
   `GET /api/v1/auth/me` avec l'en-tête `Authorization: Bearer <accessToken>`.
3. **Attendu** : `200 OK` avec un corps
   `{ "memberId": <id>, "displayName": "<nom complet>", "permissions": [ ... ] }`.
   - `memberId` et `displayName` correspondent au membre connecté.
   - `permissions` contient **exactement** les droits du compte (ex. `manage_members`), ou `[]`.
   - **Aucun** champ secret (`passwordHash`, `password`, `token`…) présent — vérifier le corps brut.

**Critères couverts** : SC-001 (un seul appel), SC-004 (aucun secret), SC-005 (client sans décodage
de jeton). FR-001/002/004/005/007/008.

## Scénario B — Correspondance stricte droits ↔ autorisations (US1, FR-006)

1. Se connecter avec un compte disposant du droit `manage_attendance` **et non** `manage_members`.
2. `GET /api/v1/auth/me` → `permissions` contient `manage_attendance` et **pas** `manage_members`.
3. Confirmer la cohérence côté autorisation :
   - un appel à une ressource protégée par `manage_attendance` (ex. démarrer une session) est
     **autorisé** ;
   - un appel à une ressource protégée par `manage_members` (ex. créer un membre) est **refusé** (403).
4. **Attendu** : ce que `/me` annonce = ce que l'API autorise réellement.

**Critères couverts** : SC-002 (100 % de correspondance), FR-006.

## Scénario C — Membre sans droit de gestion (US1)

1. Se connecter avec un compte membre **sans** profil du bureau.
2. `GET /api/v1/auth/me` → `200` avec `permissions: []` et l'identité renseignée.

**Critères couverts** : FR-005 (liste vide valide).

## Scénario D — Aucune session (US2, FR-003)

1. Appeler `GET /api/v1/auth/me` **sans** en-tête `Authorization`.
2. **Attendu** : `401 Unauthorized` (ProblemDetails), sans détail sur la cause.

## Scénario E — Jeton invalide / expiré (US2, FR-003)

1. Appeler `GET /api/v1/auth/me` avec `Authorization: Bearer <jeton bidon ou expiré>`.
2. **Attendu** : `401 Unauthorized`, refus **identique** au scénario D (pas de divulgation de cause).

**Critères couverts (D+E)** : SC-003 (100 % des demandes sans session valide refusées), FR-003.

## Vérification finale (checklist SC)

- [ ] SC-001 : identité + droits obtenus en un seul appel après connexion.
- [ ] SC-002 : droits de `/me` = droits réellement autorisés par l'API (scénario B).
- [ ] SC-003 : 401 pour jeton absent / invalide / expiré (scénarios D, E).
- [ ] SC-004 : aucune donnée secrète dans la réponse (scénario A).
- [ ] SC-005 : l'affichage RBAC ne nécessite pas de décoder le jeton côté client.
