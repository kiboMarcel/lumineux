# Feature Specification: Console web Lumineux — socle & cycle de vie du compte (SPA)

**Feature Branch**: `008-spa-foundation-auth`

**Created**: 2026-07-04

**Status**: Draft

**Input**: User description: "Mettre en place le socle de la console web Lumineux (SPA) et le cycle de
vie du compte utilisateur — Lot 0 + Lot 1 du brief PO (PO_description.md)."

## Contexte & motivation

L'API Lumineux (features 001→007) est mûre mais **sans interface**. Ce premier incrément livre la
**console web** (application monopage, front distinct du back-end, dans le mono-dépôt) qui la
consomme : d'abord son **socle** (navigation, communication authentifiée, gestion cohérente des
erreurs, contrôle d'accès à l'affichage selon les droits), puis le **cycle de vie complet du compte
utilisateur** (connexion, activation, mot de passe oublié/réinitialisation, changement de mot de
passe, déconnexion, et installation du tout premier administrateur).

L'objectif : qu'un membre du bureau puisse **se connecter en autonomie** et que l'application
**n'affiche que ce à quoi il a droit**, tout en laissant l'API seule autorité sur les autorisations.
Ce lot **ne couvre pas** la gestion des membres, des profils/droits ni des présences (lots
ultérieurs).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Se connecter et accéder à la console selon ses droits (Priority: P1) 🎯 MVP

En tant que membre du bureau, je veux me connecter avec ma référence et mon mot de passe et arriver
sur une console dont la navigation reflète **mes droits**, afin de travailler sans voir d'options qui
me sont interdites.

**Why this priority**: C'est le cœur du socle : sans authentification, session et navigation adaptée
aux droits, aucune autre fonctionnalité n'est atteignable. Livrée seule, elle constitue déjà un MVP
démontrable (se connecter → voir une console cohérente → se déconnecter).

**Independent Test**: Se connecter avec un compte valide ; vérifier que l'application charge
l'identité et les droits de session, affiche la navigation correspondante, masque/désactive les
zones interdites, et qu'une déconnexion ramène à l'écran de connexion.

**Acceptance Scenarios**:

1. **Given** un compte actif, **When** l'utilisateur saisit une référence et un mot de passe valides,
   **Then** il est connecté et arrive sur la console, qui affiche son nom et une navigation
   correspondant à ses droits.
2. **Given** des identifiants erronés, **When** l'utilisateur soumet le formulaire, **Then** un
   message d'erreur clair et **non révélateur** s'affiche (sans distinguer « référence inconnue » de
   « mot de passe faux »), et il reste sur l'écran de connexion.
3. **Given** un utilisateur connecté sans le droit « gestion des membres », **When** la console
   s'affiche, **Then** aucune entrée de navigation ni action liée à ce droit n'est visible/activable.
4. **Given** un utilisateur connecté, **When** sa session devient invalide/expirée (l'API refuse une
   requête), **Then** l'application le ramène à l'écran de connexion avec un message explicite, sans
   perdre l'URL qu'il visait (retour après reconnexion).
5. **Given** un utilisateur connecté, **When** il se déconnecte, **Then** la session est purgée et il
   revient à l'écran de connexion ; un rafraîchissement de page ne le reconnecte pas
   automatiquement.
6. **Given** un utilisateur **non connecté**, **When** il tente d'accéder à une URL protégée de la
   console, **Then** il est redirigé vers l'écran de connexion.

---

### User Story 2 - Première connexion / activation du compte (Priority: P2)

En tant que membre venant de recevoir un **mot de passe temporaire**, je veux, à ma première
connexion, définir un nouveau mot de passe conforme à la politique, afin d'activer mon compte et
d'accéder à la console.

**Why this priority**: Indispensable pour que les comptes nouvellement provisionnés deviennent
utilisables, mais dépend du socle d'US1.

**Independent Test**: Avec un compte en attente d'activation, saisir la référence + le mot de passe
temporaire, définir un nouveau mot de passe valide ; vérifier l'accès à la console. Vérifier qu'un
nouveau mot de passe non conforme est refusé avant envoi.

**Acceptance Scenarios**:

1. **Given** un compte en attente d'activation, **When** l'utilisateur se connecte avec son mot de
   passe temporaire, **Then** l'application détecte l'obligation de changement (code métier renvoyé
   par l'API) et présente l'écran de définition d'un nouveau mot de passe.
2. **Given** l'écran d'activation, **When** l'utilisateur saisit un nouveau mot de passe conforme
   (et différent du temporaire), **Then** son compte est activé et il est connecté à la console.
3. **Given** l'écran d'activation, **When** le nouveau mot de passe ne respecte pas la politique
   (trop court, sans lettre ou sans chiffre) ou est identique au temporaire, **Then** un message de
   validation s'affiche et le formulaire n'est pas soumis.

---

### User Story 3 - Mot de passe oublié et réinitialisation (Priority: P2)

En tant qu'utilisateur ayant oublié son mot de passe, je veux en demander la réinitialisation et le
redéfinir via le lien reçu par e-mail, afin de retrouver l'accès sans intervention d'un tiers.

**Why this priority**: Complète l'autonomie du compte ; s'appuie sur le socle mais est indépendante
de l'activation.

**Independent Test**: Depuis l'écran de connexion, demander une réinitialisation ; vérifier le
**message générique** ; ouvrir la route publique de réinitialisation avec un jeton ; définir un
nouveau mot de passe ; vérifier le retour à la connexion et l'accès avec le nouveau mot de passe.

**Acceptance Scenarios**:

1. **Given** l'écran de connexion, **When** l'utilisateur demande une réinitialisation en saisissant
   une référence, **Then** un **message générique identique** s'affiche quel que soit l'état réel du
   compte (aucune information permettant de deviner si un compte/e-mail existe).
2. **Given** un lien de réinitialisation reçu par e-mail, **When** l'utilisateur ouvre la route
   publique correspondante (jeton en paramètre), **Then** un écran de saisie d'un nouveau mot de
   passe s'affiche.
3. **Given** l'écran de réinitialisation, **When** l'utilisateur définit un nouveau mot de passe
   conforme, **Then** l'opération réussit et il est ramené à l'écran de connexion avec un message de
   succès.
4. **Given** un lien invalide, expiré ou déjà utilisé, **When** l'utilisateur soumet un nouveau mot
   de passe, **Then** un message d'échec **générique** s'affiche (sans distinguer les causes), avec
   la possibilité de redemander un lien.

---

### User Story 4 - Changer son mot de passe (utilisateur connecté) (Priority: P3)

En tant qu'utilisateur connecté, je veux changer mon mot de passe depuis la console, afin de le
renouveler quand je le souhaite.

**Why this priority**: Valeur de confort/sécurité, non bloquante pour l'usage initial.

**Independent Test**: Connecté, ouvrir le changement de mot de passe, fournir l'actuel + un nouveau
conforme et différent ; vérifier le succès ; vérifier qu'un mot de passe actuel erroné ou un nouveau
non conforme est refusé.

**Acceptance Scenarios**:

1. **Given** un utilisateur connecté, **When** il fournit son mot de passe actuel et un nouveau
   conforme et différent, **Then** le changement réussit et une confirmation s'affiche.
2. **Given** l'écran de changement, **When** le mot de passe actuel est erroné, **Then** un message
   d'erreur s'affiche sans changer le mot de passe.
3. **Given** l'écran de changement, **When** le nouveau mot de passe est non conforme ou identique à
   l'actuel, **Then** un message de validation s'affiche avant soumission.

---

### User Story 5 - Installer le premier administrateur (Priority: P3)

En tant que personne mettant en service une instance **non encore initialisée**, je veux créer le
premier administrateur, afin d'amorcer l'usage de la console.

**Why this priority**: Nécessaire une seule fois par instance ; sans objet une fois l'instance
amorcée. Faible fréquence, donc priorité basse.

**Independent Test**: Sur une instance sans administrateur, accéder à l'écran d'installation, créer
le premier administrateur, être connecté. Vérifier que l'écran devient **inaccessible** une fois
l'instance amorcée.

**Acceptance Scenarios**:

1. **Given** une instance non initialisée, **When** l'utilisateur remplit le formulaire
   d'installation avec des données valides, **Then** le premier administrateur est créé et
   l'utilisateur est connecté à la console.
2. **Given** une instance déjà amorcée, **When** on tente d'accéder à l'écran d'installation,
   **Then** l'accès est refusé/redirigé (l'API rejette l'opération) et un message adéquat s'affiche.

### Edge Cases

- **Rafraîchissement de page / nouvel onglet** : le jeton n'étant pas conservé dans un stockage
  persistant exposé, un rechargement complet **déconnecte** l'utilisateur (reconnexion simple, pas de
  rafraîchissement automatique dans cet incrément).
- **Accès direct à une URL protégée sans session** : redirection vers la connexion, puis retour à
  l'URL visée après authentification.
- **Droit refusé côté API malgré une UI permissive** : si un écran/action était affiché à tort,
  l'appel est **refusé par l'API** (403) et l'application affiche un message « action non
  autorisée » sans planter.
- **Champ e-mail absent lors du mot de passe oublié** : la réponse reste **générique** (l'UI ne peut
  pas et ne doit pas révéler l'absence d'e-mail).
- **Politique de mot de passe** : la validation côté client guide l'utilisateur, mais un cas limite
  non détecté côté client reste tranché par l'API (message d'erreur affiché).
- **Perte de connexion réseau / API indisponible** : un message d'erreur non technique s'affiche et
  l'utilisateur peut réessayer.

## Requirements *(mandatory)*

### Socle & transverses (Lot 0)

- **FR-001**: L'application MUST présenter une structure de navigation et un agencement **responsive**
  utilisables sur poste de bureau et tablette, **en français**.
- **FR-002**: Toute requête vers l'API pour le compte d'un utilisateur connecté MUST porter son
  **jeton d'accès** ; les ressources protégées ne sont jamais appelées sans jeton.
- **FR-003**: Le jeton d'accès MUST NOT être conservé dans un **stockage exposé aux attaques XSS**
  (p. ex. stockage local persistant) ; sa durée de vie est celle de la session applicative.
- **FR-004**: Au **démarrage**, l'application MUST déterminer s'il existe une **session valide** (en
  récupérant l'identité et les droits effectifs de l'utilisateur) et adapter l'affichage en
  conséquence (console si connecté, écran de connexion sinon).
- **FR-005**: L'application MUST adapter la **navigation et les actions** aux **droits effectifs** de
  l'utilisateur (masquer/désactiver ce qui est interdit). Ce contrôle est un **confort d'affichage** :
  l'API reste l'autorité et toute tentative non autorisée demeure refusée côté serveur.
- **FR-006**: L'application MUST empêcher l'accès aux **écrans protégés** sans session valide
  (redirection vers la connexion), en **conservant l'URL visée** pour y revenir après connexion.
- **FR-007**: En cas de **refus d'authentification** de l'API (session absente/expirée) survenant sur
  une requête, l'application MUST **purger la session** et ramener l'utilisateur à l'écran de
  connexion avec un message explicite.
- **FR-008**: L'application MUST **mapper de façon cohérente** les réponses d'erreur de l'API vers des
  messages exploitables par l'utilisateur : non authentifié, **droit manquant**, **conflit**,
  **erreurs de validation**, et **codes métier** (p. ex. obligation de changement de mot de passe),
  sans exposer de détail technique.
- **FR-009**: L'application MUST NOT **afficher ni conserver** de secret (mot de passe saisi, jeton).
  Les champs de mot de passe sont masqués ; aucun secret n'est journalisé côté client ni placé dans
  une URL persistée.

### Cycle de vie du compte (Lot 1)

- **FR-010**: L'utilisateur MUST pouvoir **se connecter** avec sa référence et son mot de passe ; un
  échec d'identification affiche un message **non révélateur** (indistinct entre référence inconnue et
  mot de passe erroné).
- **FR-011**: Lorsqu'un **changement de mot de passe est requis** (première connexion), l'application
  MUST présenter l'écran d'**activation** et permettre de définir un nouveau mot de passe **conforme à
  la politique** et **différent** du temporaire, puis connecter l'utilisateur.
- **FR-012**: L'utilisateur MUST pouvoir demander une **réinitialisation de mot de passe** par
  référence ; l'application MUST afficher un **message générique identique** quel que soit l'état du
  compte (anti-énumération).
- **FR-013**: L'application MUST fournir une **route publique de réinitialisation** acceptant le jeton
  transmis par e-mail, permettant de définir un nouveau mot de passe puis de revenir à la connexion ;
  un jeton invalide/expiré/consommé donne un **échec générique** (sans distinction de cause).
- **FR-014**: L'utilisateur connecté MUST pouvoir **changer son mot de passe** (fournir l'actuel + un
  nouveau conforme et différent), avec confirmation en cas de succès et message d'erreur sinon.
- **FR-015**: L'utilisateur MUST pouvoir **se déconnecter** ; la session est alors purgée et l'accès
  aux écrans protégés requiert une nouvelle connexion.
- **FR-016**: Sur une **instance non initialisée**, l'application MUST proposer la **création du
  premier administrateur** ; une fois l'instance amorcée, cet écran MUST devenir inaccessible (refus
  côté API pris en compte par l'UI).
- **FR-017**: Les formulaires de mot de passe MUST appliquer une **validation côté client alignée** sur
  la politique (longueur minimale, au moins une lettre et un chiffre) à des fins de guidage ; cette
  validation **ne fait jamais autorité** — l'API tranche.

### Key Entities *(include if feature involves data)*

- **Session utilisateur (état applicatif, non persisté durablement)** : identité (identifiant de
  membre, libellé d'affichage) et **droits effectifs** de l'utilisateur connecté, plus le jeton
  d'accès courant. Vit le temps de la session applicative ; sert à adapter la navigation et à porter
  l'authentification des requêtes. Aucun secret n'est stocké de façon persistante.
- **Formulaire d'identifiants / de mot de passe (données de saisie transitoires)** : références et
  mots de passe saisis, jamais conservés après soumission ; support de la validation côté client.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un membre du bureau nouvellement provisionné peut, **sans assistance**, activer son
  compte et atteindre la console (parcours activation complet réalisable de bout en bout).
- **SC-002**: Le parcours « mot de passe oublié → lien e-mail → nouveau mot de passe → connexion »
  est réalisable **entièrement depuis l'application**.
- **SC-003**: Dans **100 %** des cas, l'application **n'affiche pas** d'entrée de navigation ni
  d'action pour laquelle l'utilisateur n'a pas le droit correspondant.
- **SC-004**: Dans **100 %** des cas de session invalide/expirée détectée, l'utilisateur est ramené à
  l'écran de connexion (jamais bloqué sur un écran protégé inutilisable).
- **SC-005**: **Aucun** secret (mot de passe, jeton) n'est observable dans le stockage du navigateur,
  les URL persistées ou la console de développement, quel que soit le parcours.
- **SC-006**: La demande de mot de passe oublié produit un **message strictement identique** quel que
  soit l'état du compte (vérifiable en comparant les retours pour une référence existante et une
  référence inexistante).
- **SC-007**: L'utilisateur reçoit, pour chaque type d'erreur de l'API (non authentifié, droit
  manquant, conflit, validation, code métier), un **message compréhensible et distinct** guidant
  l'action suivante.
- **SC-008**: L'application est utilisable et lisible sur une largeur d'écran de **poste de bureau et
  de tablette** (mise en page adaptée, sans défilement horizontal du contenu principal).

## Assumptions

- **Stack & emplacement** : application web monopage (le choix technique précis, Angular, est arrêté à
  l'étape `plan` — la constitution cible Angular pour la SPA), hébergée dans le **mono-dépôt** (dossier
  front dédié). Le back-end (API) n'est **pas** modifié par cet incrément.
- **Endpoints consommés (existants)** : `POST /auth/login`, `POST /auth/activate`,
  `POST /auth/forgot-password`, `POST /auth/reset-password`, `POST /auth/change-password`,
  `GET /auth/me`, `POST /setup/first-admin`. Leurs contrats font foi.
- **Conservation du jeton** : en mémoire de l'application (pas de stockage persistant exposé) ; un
  **rechargement complet déconnecte** l'utilisateur. **Pas de rafraîchissement automatique** du jeton
  dans cet incrément (décision PO : reconnexion simple à l'expiration).
- **RBAC d'affichage** : dérivé des **droits effectifs** exposés par `GET /auth/me` ; il ne remplace
  pas l'autorisation serveur.
- **Anti-énumération** : l'application relaie fidèlement les **réponses génériques** de l'API (mot de
  passe oublié / réinitialisation) sans ajouter d'information distinctive.
- **Politique de mot de passe** : longueur minimale + au moins une lettre et un chiffre (alignée sur
  l'API) ; la validation client est indicative.
- **Hors périmètre** : gestion des membres, des profils/droits, des sessions de présence ; application
  mobile / scan QR ; internationalisation au-delà du français ; personnalisation graphique avancée.
- **CORS** : l'API devra autoriser l'origine de l'application (configuration d'infrastructure, hors
  périmètre fonctionnel de cette spec).
