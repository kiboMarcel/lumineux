# Feature Specification : Authentification et connexion des membres

**Feature Branch**: `003-authentication-login`

**Created**: 2026-07-03

**Status**: Draft

**Input**: User description: "Authentification et connexion des membres. Chaque membre possède déjà un
compte (loginId = référence membre, mot de passe haché, indicateur « mot de passe à changer », état
d'activation). Implémenter l'émission des jetons et le parcours de connexion : se connecter avec la
référence et le mot de passe et recevoir un jeton d'accès portant ses droits ; imposer le changement
du mot de passe à la première connexion et activer le compte ; permettre de changer son mot de passe ;
gérer les échecs de connexion de façon sécurisée (messages génériques, protection anti-force brute).
Les droits portés par le jeton proviennent des droits assignés au membre. Sécurité : mots de passe
hachés, pas de fuite sur l'existence d'un compte, jetons signés et expirants."

## Clarifications

### Session 2026-07-03

- Q: Jeton d'accès seul ou avec rafraîchissement ? → A: **Jeton d'accès seul, expirant** (pas de refresh token dans cette itération ; reconnexion à l'expiration).
- Q: Mécanisme de première connexion ? → A: **Endpoint dédié** prenant référence + mot de passe temporaire + nouveau mot de passe (sans jeton préalable).
- Q: Politique anti-force brute ? → A: **Verrouillage temporaire** du compte après N échecs consécutifs pendant une durée D (N/D configurables ; défauts N=5, D=15 min ; compteur remis à zéro au succès).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Connexion et obtention d'un jeton d'accès (Priority: P1)

Un membre (standard ou du bureau) dont le compte est actif saisit sa référence et son mot de passe.
Le système vérifie les identifiants et, s'ils sont valides, délivre un jeton d'accès à durée limitée
portant l'identité du membre et ses droits. Le membre utilise ensuite ce jeton pour accéder aux
fonctionnalités autorisées (présence, gestion des membres, etc.).

**Why this priority**: C'est la porte d'entrée de toute l'application ; sans elle, aucune des
fonctionnalités protégées (features 001/002) n'est réellement utilisable par les membres.

**Independent Test**: Peut être testé en fournissant des identifiants valides d'un compte actif et en
vérifiant qu'un jeton signé, expirant et porteur des droits est délivré ; et en fournissant des
identifiants invalides pour vérifier le refus générique.

**Acceptance Scenarios**:

1. **Given** un compte actif et des identifiants valides, **When** le membre se connecte, **Then** un
   jeton d'accès signé, à durée limitée et portant ses droits lui est délivré.
2. **Given** une référence inconnue **ou** un mot de passe erroné, **When** la connexion est tentée,
   **Then** le système refuse avec un **message générique identique** (sans révéler lequel est en cause).
3. **Given** un compte non actif (archivé/suspendu), **When** des identifiants pourtant corrects sont
   fournis, **Then** aucun jeton n'est délivré.

---

### User Story 2 - Première connexion : changement de mot de passe et activation (Priority: P1)

Un nouveau membre se connecte pour la première fois avec le mot de passe temporaire reçu. Comme son
compte exige un changement de mot de passe, le système ne lui accorde pas d'accès normal tant qu'il
n'a pas défini un nouveau mot de passe. Après avoir choisi un mot de passe conforme à la politique,
son compte est activé et il obtient un accès normal.

**Why this priority**: Sans ce parcours, les comptes provisionnés (feature 002) restent inutilisables ;
c'est la condition d'activation de tout nouveau membre.

**Independent Test**: Peut être testé avec un compte « à activer » : la connexion signale l'obligation
de changement ; après définition d'un nouveau mot de passe valide, le compte passe à « actif »,
l'indicateur de changement est levé, et une connexion normale devient possible.

**Acceptance Scenarios**:

1. **Given** un compte dont le mot de passe doit être changé, **When** le membre se connecte avec le
   mot de passe temporaire correct, **Then** le système signale l'obligation de changement et n'accorde
   pas d'accès normal.
2. **Given** ce même contexte, **When** le membre définit un nouveau mot de passe conforme à la
   politique, **Then** le compte est activé, l'obligation est levée, et un accès normal est accordé.
3. **Given** un nouveau mot de passe non conforme (trop faible) ou un mot de passe temporaire erroné,
   **When** le changement est soumis, **Then** l'opération est refusée avec un message explicite.

---

### User Story 3 - Changement de mot de passe par un utilisateur connecté (Priority: P2)

Un membre déjà connecté souhaite changer son mot de passe. Il fournit son mot de passe actuel et un
nouveau mot de passe conforme à la politique ; le système met à jour son mot de passe.

**Why this priority**: Bonne pratique de sécurité (rotation, compromission suspectée) ; secondaire par
rapport à la connexion et à l'activation.

**Independent Test**: Peut être testé en changeant le mot de passe d'un compte actif (mot de passe
actuel correct + nouveau conforme) puis en vérifiant que l'ancien ne fonctionne plus et le nouveau oui.

**Acceptance Scenarios**:

1. **Given** un membre connecté, **When** il fournit son mot de passe actuel correct et un nouveau
   conforme, **Then** le mot de passe est mis à jour.
2. **Given** un mot de passe actuel erroné, **When** le changement est tenté, **Then** il est refusé.

---

### User Story 4 - Protection contre les tentatives abusives (Priority: P2)

Après plusieurs échecs de connexion successifs sur un même compte, le système bloque temporairement
les tentatives afin de contrer les attaques par force brute, sans révéler d'information exploitable.

**Why this priority**: Protège les comptes contre le devinement de mot de passe ; complète la sécurité
de la connexion (US1).

**Independent Test**: Peut être testé en enchaînant des échecs de connexion jusqu'au seuil et en
vérifiant que les tentatives suivantes sont temporairement refusées, puis de nouveau autorisées après
la période définie.

**Acceptance Scenarios**:

1. **Given** un compte ayant subi le nombre maximal d'échecs consécutifs, **When** une nouvelle
   tentative est faite avant la fin du blocage, **Then** elle est refusée (même avec le bon mot de passe).
2. **Given** un blocage expiré (ou un succès après réinitialisation du compteur), **When** le membre
   se connecte avec des identifiants valides, **Then** l'accès est accordé.

---

### Edge Cases

- **Horloge et expiration** : un jeton présenté après son expiration est refusé ; l'expiration
  s'appuie sur une source de temps fiable côté serveur.
- **Compte non activé au-delà du parcours de première connexion** : tant que le mot de passe n'a pas
  été changé, aucun accès normal n'est accordé.
- **Tentative de changement de mot de passe réutilisant l'ancien** : comportement à définir (rejet
  recommandé).
- **Casse/espaces de la référence** : la référence de connexion est comparée de façon déterministe
  (règle de normalisation à préciser à la planification).
- **Fuite d'information** : les messages d'erreur et, dans la mesure du possible, les temps de réponse
  ne doivent pas permettre de distinguer « compte inexistant » de « mot de passe erroné ».
- **Verrouillage et déni de service** : le blocage anti-force brute ne doit pas permettre de bloquer
  indéfiniment un compte tiers (voir décision de clarification).

## Requirements *(mandatory)*

### Functional Requirements

**Connexion & jeton**

- **FR-001**: Le système DOIT permettre à un membre de se connecter en fournissant sa **référence** et
  son **mot de passe**.
- **FR-002**: En cas d'identifiants valides et de **compte actif**, le système DOIT délivrer un **jeton
  d'accès signé et à durée de validité limitée**, portant l'identité du membre et ses **droits**.
- **FR-003**: Les **droits/permissions** portés par le jeton (p. ex. gestion des présences, gestion
  des membres) DOIVENT provenir des droits **assignés au membre** (leur attribution relève d'une autre
  fonctionnalité).
- **FR-004**: En cas d'identifiants invalides (référence inconnue **ou** mot de passe erroné), le
  système DOIT refuser avec un **message générique identique**, sans révéler la cause ni l'existence
  du compte.
- **FR-005**: Le système NE DOIT PAS délivrer de jeton d'accès normal à un compte **non actif**
  (archivé/suspendu) ni à un compte **en attente de changement de mot de passe** (voir FR-007).
- **FR-006**: La durée de validité du jeton DOIT être limitée et configurable ; un jeton expiré DOIT
  être refusé. Cette itération se limite à un **jeton d'accès expirant** (pas de jeton de
  rafraîchissement) : à l'expiration, l'utilisateur se reconnecte. La déconnexion est côté client
  (abandon du jeton).

**Première connexion & activation**

- **FR-007**: Lorsqu'un compte exige un changement de mot de passe (première connexion), la connexion
  normale DOIT échouer en signalant l'obligation de changement, sans accorder d'accès. Le changement
  s'effectue via un **endpoint dédié** acceptant **référence + mot de passe temporaire + nouveau mot
  de passe**, sans jeton préalable.
- **FR-008**: Lors du changement de mot de passe de première connexion, le système DOIT vérifier le
  mot de passe temporaire **en premier**, appliquer la **politique de mot de passe**, enregistrer le
  nouveau mot de passe (haché), **lever l'obligation de changement** et **activer le compte** (passage
  à « actif »). Pour éviter l'énumération, l'information « compte déjà activé » NE DOIT être renvoyée
  qu'**après** vérification correcte du mot de passe temporaire ; sinon un refus **générique** est
  renvoyé.

**Changement de mot de passe**

- **FR-009**: Le système DOIT permettre à un utilisateur authentifié de changer son mot de passe en
  fournissant son **mot de passe actuel** et un **nouveau** mot de passe conforme à la politique.
- **FR-010**: Le système DOIT définir et appliquer une **politique de mot de passe** (longueur
  minimale et règles de complexité de base) et refuser tout mot de passe non conforme.

**Sécurité & anti-abus**

- **FR-011**: Le système DOIT **verrouiller temporairement** un compte après **N échecs de connexion
  consécutifs** (défaut N=5) pendant une **durée D** (défaut D=15 min), N et D étant configurables.
  Pendant le verrouillage, toute tentative est refusée (même avec le bon mot de passe), avec un
  message générique. Le compteur d'échecs est **remis à zéro** après une connexion réussie ou à
  l'expiration du verrouillage.
- **FR-012**: Le système DOIT hacher les mots de passe (jamais stockés ni journalisés en clair) et NE
  DOIT PAS divulguer d'information permettant l'énumération des comptes.
- **FR-013**: Le système DOIT tracer les événements d'authentification (succès, échec, verrouillage,
  changement de mot de passe, activation) **sans** journaliser de mot de passe ni de jeton.
- **FR-014**: Le système DOIT invalider/refuser l'accès en cas de jeton absent, malformé, non signé
  correctement ou expiré.

### Key Entities *(include if data involved)*

- **Compte de connexion (MemberAccount)** *(existant, feature 002)* : identifiant de connexion
  (= référence), empreinte du mot de passe, indicateur « mot de passe à changer », état d'activation.
  Cette fonctionnalité **complète** ce compte avec les éléments de sécurité de connexion : compteur
  d'échecs consécutifs, éventuelle date de fin de verrouillage, date de dernière connexion.
- **Jeton d'accès** : jeton signé, à durée limitée, portant l'identité du membre et ses droits.
  **Non persisté** (sans état serveur ; pas de jeton de rafraîchissement dans cette itération).
- **Droits/permissions du membre** *(existant ou dépendance)* : ensemble des droits déterminant les
  claims du jeton ; leur attribution relève de la gestion du bureau/profils (hors périmètre).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un membre disposant d'identifiants valides obtient un jeton d'accès en moins de 2 secondes.
- **SC-002**: 100 % des tentatives avec identifiants invalides renvoient un message générique
  indistinguable entre « compte inexistant » et « mot de passe erroné ».
- **SC-003**: 100 % des nouveaux comptes ne peuvent accéder aux fonctionnalités normales qu'après avoir
  changé leur mot de passe et activé leur compte.
- **SC-004**: Après le nombre défini d'échecs consécutifs, 100 % des tentatives suivantes sont bloquées
  pendant la durée définie.
- **SC-005**: Aucun mot de passe ni jeton n'apparaît en clair dans les journaux (vérifié par revue et tests).
- **SC-006**: 100 % des jetons expirés sont refusés à l'accès.
- **SC-007**: 90 % des nouveaux membres réussissent leur première connexion et leur changement de mot
  de passe sans assistance.

## Assumptions

- **Comptes préexistants** : les comptes de connexion sont créés par la feature 002 (référence, mot de
  passe temporaire haché, obligation de changement, état PendingActivation). Cette fonctionnalité ne
  crée pas de compte ; elle gère la connexion, l'activation et le changement de mot de passe.
- **Socle JWT** : la validation des jetons (signature, expiration, claims) existe déjà (features
  001/002) ; cette fonctionnalité ajoute l'**émission** des jetons et le parcours de connexion.
- **Attribution des droits** : les droits (permissions) d'un membre sont **lus** à la connexion pour
  peupler le jeton ; leur **gestion complète** (profils du bureau) relève d'une autre fonctionnalité.
  Par défaut, un membre standard n'a aucun droit de gestion. **Amorçage minimal** : pour rendre le
  système utilisable de bout en bout dès cette itération, les droits d'un **compte bureau initial**
  peuvent être accordés via configuration (bootstrap idempotent) ; l'attribution fine ultérieure
  reste hors périmètre.
- **Politique de mot de passe par défaut** : longueur minimale de 8 caractères avec règles de base ;
  valeurs précises fixées à la planification/configuration.
- **Source de temps** : l'expiration des jetons et les fenêtres de verrouillage s'appuient sur
  l'horloge serveur.
- **Canal sécurisé** : les échanges d'identifiants se font sur un canal chiffré (HTTPS) ; la gestion
  du transport relève du déploiement.
- **Réinitialisation de mot de passe oublié** (self-service via e-mail/SMS) est **hors périmètre** de
  cette itération (pourra faire l'objet d'une fonctionnalité ultérieure).
