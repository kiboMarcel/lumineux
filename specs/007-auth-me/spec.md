# Feature Specification: Profil de l'utilisateur courant (auth/me)

**Feature Branch**: `007-auth-me`

**Created**: 2026-07-04

**Status**: Draft

**Input**: User description: "Ajouter un endpoint GET /api/v1/auth/me qui retourne l'identité et les droits effectifs de l'utilisateur actuellement authentifié (via son JWT). Objectif : permettre à la SPA (console web Lumineux) de connaître, au bootstrap et après connexion, qui est l'utilisateur connecté et quels droits fonctionnels il possède, afin d'adapter la navigation et l'affichage (RBAC côté UI). Endpoint authentifié, aucun droit de gestion requis, 401 si non authentifié, ne jamais exposer de secret."

## Contexte & motivation

La console web (SPA, feature à venir) et à terme l'application mobile ont besoin, dès leur
démarrage et juste après une connexion, de savoir **qui est l'utilisateur connecté** et **ce qu'il a
le droit de faire**, pour afficher la bonne navigation et n'exposer que les actions autorisées
(RBAC côté interface). Aujourd'hui, l'API délivre un jeton d'accès lors de la connexion (feature
003) mais **n'offre aucun moyen de relire l'identité et les droits** portés par ce jeton : le client
serait contraint de décoder le jeton lui-même, ce qui le couple au format interne du jeton.

Cette fonctionnalité ajoute une ressource **« mon profil de session »** : une lecture, par
l'utilisateur, de sa propre identité et de ses droits **effectifs pour la session en cours**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Connaître mon identité et mes droits de session (Priority: P1)

En tant qu'utilisateur authentifié, je veux consulter mon identité (qui je suis) et la liste de mes
droits fonctionnels effectifs, afin que l'application cliente adapte sa navigation et n'affiche que
les fonctionnalités auxquelles j'ai droit.

**Why this priority**: C'est la seule raison d'être de la fonctionnalité et le prérequis au bootstrap
de la SPA. Sans elle, aucune interface ne peut décider quoi montrer à l'utilisateur connecté.

**Independent Test**: Se connecter (obtenir un jeton), appeler la ressource « mon profil » avec ce
jeton, et vérifier qu'elle renvoie l'identité attendue et l'ensemble exact des droits portés par le
jeton — testable de bout en bout, sans aucune autre fonctionnalité nouvelle.

**Acceptance Scenarios**:

1. **Given** un utilisateur connecté disposant des droits « gestion des membres » et « gestion des
   présences », **When** il consulte son profil de session, **Then** la réponse contient son
   identité et exactement ces deux droits (ni plus, ni moins).
2. **Given** un utilisateur connecté ne disposant d'aucun droit de gestion, **When** il consulte son
   profil de session, **Then** la réponse contient son identité et une liste de droits **vide**.
3. **Given** un utilisateur connecté, **When** il consulte son profil, **Then** la réponse ne
   contient **aucune donnée secrète** (mot de passe, empreinte de mot de passe, jeton).

### User Story 2 - Détecter une session absente ou expirée (Priority: P2)

En tant qu'application cliente, je veux qu'une demande de profil sans session valide soit refusée de
manière claire et uniforme, afin de savoir que je dois rediriger l'utilisateur vers l'écran de
connexion.

**Why this priority**: Complète le parcours de bootstrap (savoir qu'on n'est **pas** connecté est
aussi utile que de savoir qu'on l'est), mais dépend d'US1 pour exister.

**Independent Test**: Appeler la ressource « mon profil » sans jeton, puis avec un jeton
invalide/expiré, et vérifier qu'un refus d'authentification uniforme est renvoyé dans les deux cas.

**Acceptance Scenarios**:

1. **Given** aucune session (aucun jeton fourni), **When** on demande le profil, **Then** la demande
   est refusée pour défaut d'authentification.
2. **Given** un jeton expiré ou invalide, **When** on demande le profil, **Then** la demande est
   refusée pour défaut d'authentification, **sans** révéler la cause précise du rejet.

### Edge Cases

- **Droits modifiés après émission du jeton** : si les droits de l'utilisateur ont changé (profil
  attribué/révoqué) après la délivrance de son jeton, le profil de session reflète les droits
  **portés par la session en cours** (donc l'état au moment de la connexion), pas l'état recalculé en
  base. Les nouveaux droits deviennent visibles après une reconnexion. Ce choix garantit que ce que
  l'interface affiche correspond **exactement** à ce que l'API autorise pour ce jeton (cohérence
  UI/autorisations).
- **Membre désactivé/archivé après connexion** : tant que le jeton reste valide, le profil de
  session demeure consultable (l'API d'authentification est sans état côté session). La restriction
  d'accès effective intervient au renouvellement du jeton (reconnexion refusée).
- **Compte nécessitant un changement de mot de passe** : cas sans objet ici — un tel compte ne peut
  pas obtenir de jeton d'accès (bloqué à la connexion, feature 003), donc n'atteint jamais cette
  ressource.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système MUST exposer une ressource permettant à un **utilisateur authentifié** de
  lire son propre profil de session (identité + droits effectifs).
- **FR-002**: L'accès à cette ressource MUST être réservé aux utilisateurs authentifiés ; **aucun
  droit de gestion particulier** n'est requis (tout membre connecté peut lire ses propres
  informations).
- **FR-003**: Une demande **sans authentification** valide (jeton absent, invalide ou expiré) MUST
  être refusée pour défaut d'authentification, avec un refus **uniforme** ne révélant pas la cause
  précise.
- **FR-004**: La réponse MUST contenir l'**identité** de l'utilisateur : au minimum son identifiant
  de membre et un **libellé d'affichage** (nom complet) permettant à l'interface de le nommer.
- **FR-005**: La réponse MUST contenir la **liste des droits fonctionnels effectifs** de la session
  courante (les permissions portées par le jeton), sous une forme exploitable par le client pour
  piloter l'affichage.
- **FR-006**: Les droits retournés MUST correspondre **exactement** aux droits que la session en
  cours confère réellement (cohérence stricte avec ce que l'API autorise pour ce jeton) — ni
  sur-ensemble, ni sous-ensemble.
- **FR-007**: La réponse MUST **exclure toute donnée secrète ou sensible superflue** : jamais de mot
  de passe, d'empreinte de mot de passe, de jeton, ni de donnée personnelle non nécessaire à
  l'identification et à l'affichage.
- **FR-008**: La lecture du profil MUST être **sans effet de bord** (aucune modification de données,
  opération de lecture pure) et **répétable** (même résultat pour une même session).
- **FR-009**: Le refus pour défaut d'authentification (FR-003) MUST être **journalisé** à des fins de
  diagnostic/sécurité, sans consigner de secret (Constitution VI).

### Key Entities *(include if feature involves data)*

- **Profil de session (lecture seule)** : représentation, destinée au client, de l'utilisateur
  connecté pour la session en cours. Attributs :
  - *Identifiant de membre* : référence technique du membre connecté.
  - *Libellé d'affichage* : nom complet à afficher dans l'interface.
  - *Droits effectifs* : ensemble des permissions fonctionnelles conférées par la session.

  Cette représentation est **dérivée** de la session/jeton existant (features 003/004) ; elle
  n'introduit **aucune nouvelle donnée persistée** et **aucune migration**.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un utilisateur connecté peut récupérer son identité et ses droits en **un seul appel**,
  immédiatement après connexion, sans étape intermédiaire.
- **SC-002**: Pour une session donnée, l'ensemble des droits retournés est **identique** à l'ensemble
  des droits que l'API autorise réellement pour cette session (vérifiable en confrontant l'accès
  effectif aux fonctionnalités protégées et le contenu du profil) — **100 %** de correspondance.
- **SC-003**: **100 %** des demandes sans session valide (aucun jeton, jeton invalide, jeton expiré)
  sont refusées pour défaut d'authentification.
- **SC-004**: **Aucune** donnée secrète (mot de passe, empreinte, jeton) n'apparaît dans la réponse,
  quel que soit le profil de l'utilisateur.
- **SC-005**: L'interface cliente peut décider quoi afficher **sans décoder elle-même le jeton**
  (elle s'appuie uniquement sur le profil de session renvoyé).

## Assumptions

- **Réutilisation de l'authentification existante** : la fonctionnalité s'appuie sur le mécanisme de
  jeton d'accès et les droits (permissions issues des profils du bureau) déjà en place (features
  003/004). Aucun nouveau mécanisme d'authentification n'est introduit.
- **Source des droits = la session** : les droits effectifs retournés sont ceux portés par le jeton
  courant (calculés à la connexion), afin de rester cohérents avec les décisions d'autorisation de
  l'API. La prise en compte d'un changement de droits nécessite une reconnexion — comportement
  assumé, identique au reste de l'API (session sans état).
- **Identité minimale v1** : la réponse expose l'identifiant de membre, un libellé d'affichage (nom
  complet) et les droits. L'ajout d'autres champs d'identité (référence de connexion, e-mail…) est
  hors périmètre de cette version et pourra être décidé plus tard selon les besoins de la SPA.
- **Aucune persistance nouvelle** : lecture pure dérivée de la session ; pas d'entité, pas de
  migration, pas de nouvelle table.
- **Pas de rafraîchissement de jeton** : cette fonctionnalité n'introduit pas de mécanisme de
  renouvellement ; la gestion de l'expiration (reconnexion) reste du ressort du client.
- **Consommateur principal** : la console web Lumineux (SPA) ; l'application mobile pourra consommer
  la même ressource ultérieurement.
