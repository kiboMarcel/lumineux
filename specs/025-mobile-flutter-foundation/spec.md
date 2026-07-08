# Feature Specification: Application mobile membre — socle & cycle de vie du compte

**Feature Branch**: `025-mobile-flutter-foundation`

**Created**: 2026-07-07

**Status**: Draft

**Input**: User description: « Créer le socle de l'application mobile membre (Flutter, nouveau client
distinct dans le mono-dépôt sous `mobile/`) et livrer le cycle de vie complet du compte membre sur
mobile — connexion, activation à la 1re connexion, mot de passe oublié + réinitialisation, changement
de mot de passe, déconnexion — avec gestion sécurisée du jeton. Lot M0, premier de la piste Flutter,
préparant le futur scan de présence (M1) sans l'inclure. »

## User Scenarios & Testing *(mandatory)*

Cette fonctionnalité pose le **premier client mobile** de Lumineux, destiné au **membre** (compte simple,
sans droit de gestion). Elle couvre uniquement le **socle** (app installable, accès réseau sécurisé,
navigation) et le **cycle de vie du compte** (connexion et mot de passe). Le **scan de présence** et les
autres modules mobiles sont explicitement hors de ce lot.

### User Story 1 - Se connecter et accéder à un espace authentifié (Priority: P1)

En tant que **membre**, je veux **installer l'application mobile, saisir mon identifiant et mon mot de
passe, et accéder à un espace authentifié**, afin de disposer d'un compte prêt à l'emploi sur mon
téléphone (préalable indispensable au futur marquage de présence).

**Why this priority** : sans connexion, aucune autre fonction mobile n'existe. C'est le **MVP** : une app
qui s'ouvre, authentifie le membre de façon sûre et affiche un écran d'accueil identifié constitue à elle
seule un incrément démontrable et testable.

**Independent Test** : installer/lancer l'app, saisir des identifiants valides → arrivée sur un écran
d'accueil affichant l'identité du membre ; relancer l'app → la session est restaurée tant qu'elle est
valide ; à l'expiration, l'app ramène à la connexion.

**Acceptance Scenarios** :

1. **Given** un membre déjà provisionné avec un mot de passe défini, **When** il saisit un identifiant et
   un mot de passe valides, **Then** il accède à un écran d'accueil authentifié affichant au minimum son
   identité, et son jeton est conservé de façon sécurisée.
2. **Given** un membre sur l'écran de connexion, **When** il saisit des identifiants invalides, **Then**
   un message d'erreur clair et non révélateur s'affiche et il reste sur l'écran de connexion.
3. **Given** un membre authentifié, **When** il ferme puis rouvre l'application avant l'expiration du
   jeton, **Then** il retrouve son espace authentifié sans ressaisir ses identifiants.
4. **Given** un membre authentifié dont le jeton a expiré, **When** une action nécessitant l'API est
   tentée (ou au lancement suivant), **Then** l'état est purgé, l'app revient à la connexion avec un
   message clair, et aucune donnée protégée ne reste affichée.
5. **Given** un téléphone sans connectivité réseau, **When** le membre tente de se connecter, **Then** un
   message « réseau indisponible » explicite s'affiche, sans blocage de l'app.

---

### User Story 2 - Activer mon compte à la première connexion (Priority: P2)

En tant que **membre invité** disposant d'un **mot de passe temporaire**, je veux être **guidé pour
définir mon propre mot de passe** dès ma première connexion, afin d'activer mon compte et d'accéder à
l'application.

**Why this priority** : de nombreux membres reçoivent un mot de passe temporaire (remise bureau /
invitation). Sans ce parcours, ils sont bloqués au premier accès. Il vient juste après la connexion car
il en est la continuité directe.

**Independent Test** : se connecter avec un mot de passe temporaire → l'app détecte l'obligation de
changement → écran de définition d'un nouveau mot de passe → après validation, accès à l'espace
authentifié.

**Acceptance Scenarios** :

1. **Given** un membre dont le compte exige un changement de mot de passe, **When** il se connecte avec
   son mot de passe temporaire, **Then** l'app le redirige vers un écran de définition d'un nouveau mot de
   passe (au lieu d'aller à l'accueil).
2. **Given** l'écran d'activation, **When** le membre soumet un nouveau mot de passe conforme à la
   politique, **Then** le compte est activé et le membre accède à l'espace authentifié.
3. **Given** l'écran d'activation, **When** le membre saisit un mot de passe non conforme (trop court, sans
   chiffre ou sans lettre), **Then** un retour de validation immédiat l'indique et la soumission est
   refusée.

---

### User Story 3 - Récupérer l'accès via « mot de passe oublié » (Priority: P3)

En tant que **membre ayant oublié son mot de passe**, je veux **demander une réinitialisation puis définir
un nouveau mot de passe**, afin de retrouver l'accès à mon compte de façon autonome.

**Why this priority** : parcours de récupération essentiel à l'autonomie, mais moins fréquent que la
connexion/activation ; il peut être livré après le MVP.

**Independent Test** : depuis l'écran de connexion, demander une réinitialisation pour un identifiant →
message générique de confirmation ; saisir le jeton reçu par e-mail + un nouveau mot de passe → succès →
connexion possible avec le nouveau mot de passe.

**Acceptance Scenarios** :

1. **Given** l'écran de connexion, **When** le membre demande une réinitialisation, **Then** un message
   **générique** identique s'affiche que le compte existe ou non (aucune divulgation).
2. **Given** un membre ayant reçu un jeton de réinitialisation valide, **When** il le saisit avec un
   nouveau mot de passe conforme, **Then** le mot de passe est réinitialisé et il peut se connecter avec.
3. **Given** un jeton de réinitialisation invalide ou expiré, **When** le membre tente de l'utiliser,
   **Then** un message d'erreur clair s'affiche sans révéler d'information sensible.

---

### User Story 4 - Gérer mon mot de passe et me déconnecter (Priority: P4)

En tant que **membre authentifié**, je veux pouvoir **changer mon mot de passe** et **me déconnecter**,
afin de garder le contrôle de la sécurité de mon compte sur mon appareil.

**Why this priority** : complète le cycle de vie ; utile mais non bloquant pour les premiers usages.

**Independent Test** : connecté, changer son mot de passe (ancien + nouveau conforme) → succès ; se
déconnecter → retour à la connexion et jeton effacé du stockage sécurisé.

**Acceptance Scenarios** :

1. **Given** un membre authentifié, **When** il change son mot de passe en fournissant l'actuel et un
   nouveau conforme, **Then** le changement est confirmé.
2. **Given** un membre authentifié, **When** il se déconnecte, **Then** l'état de session et le jeton sont
   effacés du stockage sécurisé et l'app revient à l'écran de connexion.
3. **Given** un membre venant de se déconnecter, **When** il relance l'application, **Then** l'écran de
   connexion s'affiche (aucune session restaurée).

---

### Edge Cases

- **Réseau indisponible / lent** : chaque appel réseau présente un état de chargement puis, en cas
  d'échec réseau, un message « réseau indisponible » distinct d'une erreur d'identifiants.
- **Jeton expiré en cours d'usage** (pas de rafraîchissement) : toute réponse « non autorisé » purge la
  session et ramène à la connexion avec un message clair.
- **Politique de mot de passe non respectée** : retour de validation immédiat, sans appel réseau inutile.
- **Anti-énumération** : le parcours « mot de passe oublié » ne laisse jamais deviner l'existence d'un
  compte (message et comportement identiques).
- **Mise en arrière-plan / verrouillage de l'appareil** : à la reprise, aucune donnée protégée ne doit
  rester visible si la session a expiré entre-temps.
- **Jeton de réinitialisation issu de l'e-mail** : le membre doit pouvoir fournir ce jeton à l'app (voir
  Assumptions) ; un jeton absent, tronqué ou expiré produit un message clair.
- **Double soumission** : les boutons d'action sont neutralisés pendant le traitement pour éviter les
  envois multiples.

## Requirements *(mandatory)*

### Functional Requirements

**Socle & accès réseau**

- **FR-001** : L'application MUST être un **client mobile distinct** de la console web, hébergé dans le
  mono-dépôt sous un dossier dédié (`mobile/`), et consommer l'API Lumineux existante **sans aucune
  évolution de l'API**.
- **FR-002** : L'application MUST fournir une **navigation** entre les écrans du compte (connexion,
  activation, mot de passe oublié, réinitialisation, changement de mot de passe, accueil authentifié).
- **FR-003** : L'accès réseau MUST être **encapsulé** dans un composant dédié qui ajoute automatiquement
  le jeton d'authentification aux appels protégés (schéma « porteur »).
- **FR-004** : L'application MUST **centraliser la gestion des erreurs** : identifiants invalides, droit
  refusé, erreurs de validation, indisponibilité réseau, et **expiration** de session, chacune donnant un
  message clair et une action appropriée.
- **FR-005** : L'application MUST **traduire les erreurs structurées et codes métier** renvoyés par l'API
  (notamment l'obligation de changer de mot de passe) en messages compréhensibles et en orientations de
  parcours.
- **FR-006** : L'application MUST être en **français** et adaptée à un usage tactile mobile (cibles
  suffisantes, claviers adaptés, états chargement/erreur/vide).

**Gestion sécurisée du jeton & session**

- **FR-007** : Le jeton d'authentification MUST être conservé exclusivement dans le **coffre sécurisé du
  système d'exploitation** (stockage protégé de l'appareil) et **jamais** en clair ailleurs ni dans les
  journaux.
- **FR-008** : Au lancement, l'application MUST **restaurer** une session existante tant que le jeton est
  valide, sans redemander les identifiants ; sinon elle présente l'écran de connexion.
- **FR-009** : Sur **expiration** ou réponse « non autorisé », l'application MUST **purger** l'état de
  session et le jeton du coffre sécurisé et revenir à la connexion.
- **FR-010** : Aucun **secret** (mot de passe, jeton, mot de passe temporaire) MUST apparaître dans les
  journaux, l'affichage persistant ou tout stockage non sécurisé.

**Cycle de vie du compte**

- **FR-011** : Les membres MUST pouvoir **se connecter** avec leur identifiant et leur mot de passe et
  atteindre un écran d'accueil authentifié affichant au minimum leur identité.
- **FR-012** : Lorsque l'API signale l'**obligation de changer de mot de passe** (première connexion),
  l'application MUST diriger le membre vers un écran de **définition d'un nouveau mot de passe** avant tout
  accès à l'espace authentifié.
- **FR-013** : Les membres MUST pouvoir demander une **réinitialisation de mot de passe** depuis l'écran
  de connexion ; l'application MUST afficher un **message générique** ne révélant pas l'existence du
  compte.
- **FR-014** : Les membres MUST pouvoir **réinitialiser** leur mot de passe en fournissant le **jeton reçu
  par e-mail** et un nouveau mot de passe conforme.
- **FR-015** : Les membres authentifiés MUST pouvoir **changer leur mot de passe** (ancien + nouveau).
- **FR-016** : Les membres authentifiés MUST pouvoir **se déconnecter**, ce qui **efface** la session et
  le jeton du coffre sécurisé.
- **FR-017** : L'application MUST appliquer un **retour de validation immédiat** de la politique de mot de
  passe (longueur minimale, présence d'une lettre et d'un chiffre) avant soumission, l'API restant
  l'autorité finale.

**Frontière de responsabilité**

- **FR-018** : L'application MUST se limiter à **présenter et orchestrer** les parcours ; **aucune règle
  métier** (validation d'identifiants, activation, réinitialisation, politique de mot de passe) MUST être
  réimplémentée côté client — l'API reste l'unique autorité (elle renvoie les refus 401/403).
- **FR-019** : L'application MUST **communiquer exclusivement en HTTPS**.

### Key Entities *(include if feature involves data)*

- **Session membre** : représente l'état authentifié courant sur l'appareil — le **jeton** (opaque pour le
  client, conservé au coffre sécurisé), son **échéance** et l'**identité** du membre. Aucune donnée
  métier n'est stockée durablement au-delà de ce qui est nécessaire à la session.
- **Identité du membre** : informations minimales d'identification affichées après connexion (telles que
  fournies par l'API d'identité), sans droit de gestion.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : Un membre nouvellement provisionné peut, **depuis son téléphone et sans assistance**,
  activer son compte (mot de passe temporaire → nouveau) puis atteindre l'espace authentifié en **moins de
  2 minutes**.
- **SC-002** : Le parcours « mot de passe oublié → e-mail → réinitialisation → connexion » est réalisable
  **de bout en bout** depuis l'application.
- **SC-003** : Le **jeton** n'est **jamais** observable en clair (journaux, stockage non sécurisé) ; il
  est effacé à la déconnexion et à toute réponse « non autorisé ».
- **SC-004** : À l'expiration du jeton, l'application **purge** l'état et **ramène à la connexion** avec un
  message clair, **sans blocage** ni donnée protégée résiduelle à l'écran.
- **SC-005** : Une session valide est **restaurée** au relancement de l'application **sans nouvelle saisie
  d'identifiants**, et n'est **jamais** restaurée après une déconnexion.
- **SC-006** : Le parcours « mot de passe oublié » présente un **comportement identique** que le compte
  existe ou non (aucune énumération observable).
- **SC-007** : 100 % des tentatives d'action avec un mot de passe non conforme à la politique sont
  **rejetées côté client avec un retour immédiat**, avant tout appel réseau.

## Assumptions

- **API inchangée** : tous les points d'accès nécessaires existent déjà (connexion, activation,
  mot de passe oublié, réinitialisation, changement de mot de passe, identité du membre). Ce lot
  n'introduit **aucune** évolution d'API et **aucune** migration de base.
- **Identifiant de connexion** : le membre s'authentifie avec la **même référence/identifiant** que sur la
  console web (aligné sur l'API existante), accompagnée de son mot de passe.
- **Persistance de session** : contrairement à la console web (jeton en mémoire), le client mobile
  **conserve** le jeton dans le **coffre sécurisé** afin d'éviter une reconnexion à chaque lancement ; la
  session est restaurée tant que le jeton est valide. **Pas de jeton de rafraîchissement** (aligné sur
  l'API) : à l'expiration, reconnexion simple.
- **Saisie du jeton de réinitialisation** : l'e-mail de réinitialisation existant contient un lien de
  réinitialisation ; sur mobile, le membre **fournit le jeton** de ce lien à l'application (saisie /
  collage). L'ouverture automatique de l'app via lien profond (deep link) est une **évolution ultérieure**
  hors de ce lot. Si le jeton ne peut pas être fourni, le membre peut toujours réinitialiser via la page
  web publique existante.
- **Politique de mot de passe** : identique à celle de l'API (longueur minimale, lettre + chiffre) ; la
  validation client est un confort, l'API fait foi.
- **Cible technique** : le client est développé en **Flutter** (choix produit/constitution) ; les écrans
  sont pensés pour smartphone. Le **Flutter SDK** sera requis à l'étape d'implémentation (installation
  d'outillage à approuver), sans incidence sur cette spécification.
- **Périmètre membre** : l'application ne propose **aucune** fonction de gestion (membres, profils,
  présences) ; celles-ci restent sur la console web bureau.

## Out of Scope

- Le **scan de présence** par code QR et le marquage de présence (feature M1).
- La **capture hors ligne** et la synchronisation par lot des scans (feature M2).
- Le **tableau de bord membre** / historique de présences (feature M3 ; prérequis API probable).
- La **consultation de la fiche membre** détaillée (nécessiterait un point d'accès API dédié — évolution
  ouverte).
- L'ouverture de l'app par **lien profond** depuis l'e-mail de réinitialisation.
- L'ajout d'un **jeton de rafraîchissement** (reconnexion simple au lancement expiré).
- Toute **évolution d'API** ou de base de données.
