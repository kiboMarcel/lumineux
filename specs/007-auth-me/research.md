# Research — Profil de l'utilisateur courant (auth/me)

Peu d'inconnues : la fonctionnalité réutilise l'authentification existante. Les décisions ci-dessous
figent la conception avant la Phase 1.

## 1. Source de l'identité et des droits : session (jeton) vs base de données

- **Décision** : les droits et l'identité retournés proviennent **exclusivement de la session**
  (claims du jeton courant), **jamais** d'un recalcul en base.
- **Rationale** : l'API autorise chaque action à partir des **claims du jeton** (feature 003/004).
  Restituer ces mêmes claims garantit que ce que la SPA affiche correspond **exactement** à ce que
  l'API autorisera (FR-006, SC-002) — pas de fenêtre où l'UI proposerait une action que l'API
  refuserait, ni l'inverse. C'est aussi **zéro I/O base** (SC-001) et cohérent avec le caractère
  **sans état** du jeton (déjà en place).
- **Conséquence assumée** : un changement de droits (attribution/révocation de profil) n'est visible
  qu'après **reconnexion** (le jeton actuel porte encore les anciens droits jusqu'à expiration
  ~60 min). Documenté en Edge Cases de la spec.
- **Alternatives écartées** :
  - *Recalcul en base des permissions effectives* : diverge de ce que le jeton autorise → l'UI
    pourrait afficher des actions que l'API refuse (incohérence, faux sentiment de droit). Rejeté.
  - *Décodage du jeton côté client* : couple la SPA au format interne du jeton, contraire à l'objet
    même de la fonctionnalité. Rejeté.

## 2. Exposition des droits : étendre `ICurrentUser` vs nouveau port vs lecture dans le contrôleur

- **Décision** : **étendre le port existant `ICurrentUser`** (couche Application) avec une propriété
  **liste des permissions** de la session. L'implémentation API `CurrentUser` énumère les claims
  `permission`.
- **Rationale** : `ICurrentUser` **est déjà** l'abstraction du contexte utilisateur de session
  (expose `MemberId`, `UserName`, `HasPermission`). Il ne lui manque que l'**énumération** des
  droits. Extension **additive** et minimale, dans la bonne couche (Constitution I). L'ajout d'une
  propriété est **sans rupture** : tous les doubles de test existants utilisent
  `Substitute.For<ICurrentUser>()` (NSubstitute) et continuent de compiler.
- **Alternatives écartées** :
  - *Lire les claims dans le contrôleur / via `IHttpContextAccessor` en Application* : fait fuiter le
    détail HTTP dans une couche interne → violation de la règle de dépendance (Constitution I).
    Rejeté.
  - *Nouveau port dédié `ICurrentUserPermissions`* : redondant avec `ICurrentUser`, fragmente le
    contexte de session sans bénéfice. Rejeté.

## 3. Cas d'usage mince (`GetCurrentUserHandler`) vs logique dans le contrôleur

- **Décision** : introduire un **cas d'usage Application** `GetCurrentUserHandler`, même s'il est
  mince, qui mappe `ICurrentUser` → `CurrentUserResponse` et porte la **garde défensive**.
- **Rationale** : cohérence avec le reste de la solution (chaque endpoint a son handler) et
  **testabilité unitaire** du mapping et de la garde (Constitution III), sans dépendance HTTP.
- **Alternative écartée** : logique directement dans le contrôleur → non couvert par les tests
  unitaires de la couche Application, contraire aux Principes I et III. Rejeté.

## 4. Gestion du refus d'authentification (401)

- **Décision** : le refus « jeton absent / invalide / expiré » est produit **uniformément** par le
  **middleware d'authentification** ASP.NET Core via `[Authorize]` (challenge 401), comme pour tous
  les endpoints protégés existants (membres, sessions, profils). Aucune divulgation de cause. Le
  handler ajoute une **garde défensive** : si le contexte est authentifié mais **sans `member_id`
  exploitable** (jeton malformé), il lève `UnauthorizedException` (→ 401 via
  `ExceptionHandlingMiddleware`) et **journalise** le refus (`IAuditLogger`, sans secret) — FR-009.
- **Rationale** : réutilise le mécanisme éprouvé et **uniforme** de l'API (SC-003, FR-003) sans
  introduire de chemin d'erreur spécifique divergent. Le format d'erreur reste ProblemDetails
  (Constitution V).
- **Alternative écartée** : endpoint anonyme renvoyant un « profil vide » quand non connecté →
  ambiguïté (200 pour un non-connecté), complexifie le contrat côté client. Rejeté au profit d'un
  401 franc.

## 5. Contenu de la réponse (identité minimale v1)

- **Décision** : `CurrentUserResponse { memberId, displayName, permissions[] }`.
  - `memberId` = claim `member_id` (identifiant technique du membre).
  - `displayName` = `ICurrentUser.UserName` = claim `ClaimTypes.Name` (nom complet posé à la
    connexion/activation).
  - `permissions` = liste (éventuellement **vide**) des droits de la session ; ordre non significatif,
    sans doublon.
- **Rationale** : strictement ce dont la SPA a besoin pour nommer l'utilisateur et piloter le RBAC.
  **Aucun** secret, **aucune** donnée personnelle superflue (FR-007). Référence de connexion et
  e-mail **différés** (hors périmètre v1, cf. Assumptions de la spec) — pourront être ajoutés sans
  rupture si la SPA le requiert.
- **Alternative écartée** : exposer profils, e-mail, référence, dates… → sur-exposition non requise,
  élargit la surface de données personnelles. Rejeté pour la v1.

## Synthèse sécurité (Constitution IV/VI)

- Authentification vérifiée **côté serveur** (`[Authorize]`), moindre privilège (aucun droit de
  gestion requis, lecture de ses **propres** informations).
- **Aucun secret** (empreinte, jeton, mot de passe) dans la réponse — vérifié par test d'intégration.
- Droits = ceux de la **session** → cohérence UI/autorisation.
- Lecture **sans effet de bord**, **répétable** (FR-008) ; refus défensif **journalisé** sans secret.
