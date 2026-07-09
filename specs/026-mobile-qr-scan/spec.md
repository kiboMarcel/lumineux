# Feature Specification: Application mobile membre — scan de présence par QR

**Feature Branch**: `026-mobile-qr-scan`

**Created**: 2026-07-08

**Status**: Draft

**Input**: User description: « Lot M1 de l'app mobile membre (Flutter, `mobile/`) : scan de présence par
QR. Le membre authentifié scanne, avec la caméra de son téléphone, le code QR rotatif projeté par le
bureau (feature 014) pour enregistrer sa présence à une session ouverte — ce qui "ferme la boucle"
présence. Consomme l'API de scan existante, sans évolution d'API. Décision : le QR encode désormais le
sessionId **et** le token (petit prérequis sur la projection QR du bureau). Hors périmètre : capture
hors ligne / synchronisation par lot (M2), tableau de bord membre (M3), fonctions bureau. »

## Clarifications

### Session 2026-07-08

- Q: Format du payload encodé dans le QR (contrat bureau SPA ↔ mobile) → A: **JSON versionné compact**
  `{"v":1,"s":<sessionId>,"t":"<token>"}` (champ `v` = version du format, `s` = identifiant de séance,
  `t` = jeton rotatif courant).
- Q: Comportement après un résultat de scan (succès ou erreur) → A: **overlay de résultat modal** qui
  **suspend** la détection ; le scan **reprend uniquement à la fermeture manuelle** (« Fermer » /
  « Scanner à nouveau »), ce qui garantit l'absence de double-soumission.
- Q: Point d'entrée du Scanner dans la navigation → A: **onglet « Scanner » permanent** dans la barre de
  nav basse → **3 onglets** (Accueil, Scanner, Profil), conforme au design `template_mobile`.

## User Scenarios & Testing *(mandatory)*

Ce lot ajoute la **fonction cœur** de l'app membre : marquer sa présence en scannant le QR de la
séance. Il s'appuie sur le socle et l'authentification livrés (lot M0) et sur l'API de présence
existante. Le membre n'a **aucun droit de gestion** ; il agit uniquement sur **sa propre** présence.

### User Story 1 - Enregistrer ma présence en scannant le QR (Priority: P1)

En tant que **membre authentifié** présent à une réunion, je veux **scanner le code QR affiché par le
bureau** afin d'**enregistrer ma présence** en un geste, sans intervention d'un responsable.

**Why this priority** : c'est la raison d'être du lot et de l'app mobile membre — sans le scan, l'app ne
« ferme pas la boucle » de présence. Une app qui ouvre la caméra, reconnaît le QR de séance et confirme
la présence constitue à elle seule un incrément démontrable et testable (le MVP de M1).

**Independent Test** : depuis l'écran Scanner, viser un QR de séance valide → un écran/overlay de
confirmation « Présence enregistrée » s'affiche avec l'identité du membre et l'heure ; re-scanner la même
séance → confirmation « déjà enregistrée » sans doublon.

**Acceptance Scenarios** :

1. **Given** un membre authentifié sur l'écran Scanner et une **séance ouverte** dont le bureau projette
   le QR, **When** il vise le code QR valide, **Then** sa présence est enregistrée et un **overlay de
   succès** confirme l'enregistrement (identité + heure).
2. **Given** un membre **déjà enregistré** à la séance, **When** il re-scanne le QR de cette séance,
   **Then** un message indique qu'il est **déjà présent** (succès, sans créer de doublon).
3. **Given** un membre venant d'enregistrer sa présence, **When** l'overlay de succès est fermé, **Then**
   il peut **re-scanner** (pour une autre séance) sans quitter l'écran.
4. **Given** le QR **rotatif** dont le jeton change régulièrement, **When** le membre vise le code
   actuellement affiché, **Then** l'enregistrement réussit avec le jeton courant, **sans** que le jeton
   soit jamais affiché en clair ni conservé sur l'appareil.

---

### User Story 2 - Comprendre et surmonter un échec de scan (Priority: P2)

En tant que **membre**, je veux des **messages clairs** quand un scan échoue (code périmé, séance close,
réseau, session expirée), afin de savoir quoi faire et réessayer sans blocage.

**Why this priority** : sur le terrain, les échecs sont fréquents (jeton expiré entre deux rotations,
réseau instable) ; sans retour clair, le membre est bloqué. Vient juste après le scénario nominal car il
le fiabilise.

**Independent Test** : provoquer chaque cas (jeton invalide/expiré, séance close, hors réseau, session
expirée) et vérifier qu'un message distinct et une action appropriée s'affichent, sans faux succès.

**Acceptance Scenarios** :

1. **Given** un QR dont le **jeton est expiré ou invalide**, **When** le membre le scanne, **Then** un
   message clair (« code expiré, refaites le scan ») s'affiche et il peut re-scanner immédiatement.
2. **Given** une **séance déjà close**, **When** le membre scanne son QR, **Then** un message « séance
   close » s'affiche, sans enregistrement.
3. **Given** un téléphone **sans réseau**, **When** le membre scanne, **Then** un message « réseau
   indisponible, réessayez » s'affiche (aucune mise en file hors ligne dans ce lot) et le scan reste
   possible dès le réseau revenu.
4. **Given** une **session utilisateur expirée** (réponse « non autorisé »), **When** le membre tente un
   scan, **Then** l'état de session est purgé et l'app **revient à la connexion** avec un message clair,
   sans donnée protégée résiduelle.
5. **Given** un **code non reconnu** (QR étranger à Lumineux ou charge illisible), **When** il est capté,
   **Then** un message « code non reconnu » s'affiche et la caméra **continue** de chercher un code
   valide.

---

### User Story 3 - Autoriser et utiliser la caméra (Priority: P3)

En tant que **membre**, je veux que l'app me **demande l'accès à la caméra** et me **guide** si je l'ai
refusé, afin de pouvoir scanner sans confusion.

**Why this priority** : la caméra est indispensable au scan ; sa gestion (demande, refus, retour depuis
les réglages) conditionne l'usage mais peut être livrée après le scénario nominal (qui suppose l'accès
accordé).

**Independent Test** : au premier accès à l'écran Scanner, l'app demande l'autorisation caméra ;
refuser → un message explique comment l'activer (réglages) et le reste de l'app reste utilisable ;
accorder → l'aperçu caméra s'affiche.

**Acceptance Scenarios** :

1. **Given** un premier accès à l'écran Scanner, **When** l'écran s'ouvre, **Then** l'app **demande
   l'autorisation d'accès à la caméra**.
2. **Given** un membre ayant **refusé** l'accès caméra, **When** il ouvre l'écran Scanner, **Then** un
   message clair explique que la caméra est nécessaire et **oriente vers les réglages** ; les autres
   écrans (accueil, profil) restent accessibles.
3. **Given** l'accès caméra **accordé**, **When** l'écran Scanner est actif, **Then** l'**aperçu de la
   caméra** s'affiche avec un cadre de visée, prêt à détecter un QR.

---

### Edge Cases

- **Jeton entre deux rotations** : un QR capté juste après rotation (jeton périmé) → message « code
  expiré », re-scan immédiat possible.
- **Séance close entre projection et scan** → message « séance close », aucun enregistrement.
- **Détections répétées rapides** (le QR reste dans le cadre) → la détection est **suspendue** dès le
  premier code capté et un **overlay de résultat** s'affiche ; **une seule** soumission tant qu'il n'est
  pas fermé (anti double-soumission).
- **Plusieurs QR dans le champ** → un seul code est traité à la fois ; les autres sont ignorés.
- **Mise en arrière-plan / verrouillage** pendant le scan → la caméra est libérée puis réactivée au
  retour ; aucune donnée protégée résiduelle si la session a expiré entre-temps.
- **Autorisation caméra révoquée en cours d'usage** → nouvelle demande / message d'orientation.
- **QR non-Lumineux ou charge malformée** (sessionId/token manquant) → « code non reconnu », la recherche
  continue.

## Requirements *(mandatory)*

### Functional Requirements

**Écran & scan**

- **FR-001** : L'application MUST proposer un écran **Scanner** accessible au **membre authentifié** via
  un **onglet permanent** de la barre de navigation basse (portant le nombre d'onglets à **3** : Accueil,
  Scanner, Profil), ouvrant la **caméra** de l'appareil pour détecter un code QR de séance.
- **FR-002** : Le contenu du **QR de séance** MUST véhiculer **à la fois l'identifiant de séance et le
  jeton courant**, sous la forme d'un **JSON versionné** `{"v":1,"s":<sessionId>,"t":"<token>"}` (`v` =
  version de format, `s` = identifiant de séance, `t` = jeton) ; la **projection du QR côté bureau**
  (console web existante) MUST être mise à jour pour encoder ce payload (au lieu du jeton seul). L'app
  mobile MUST **rejeter proprement** un payload dont la version est inconnue ou la structure invalide
  (→ « code non reconnu », FR-010). Ce changement de charge du QR est un **prérequis** de ce lot,
  **sans** évolution de l'API ni de la base.
- **FR-003** : À la détection d'un QR de séance valide, l'application MUST **enregistrer la présence** du
  membre pour la séance correspondante en s'appuyant sur le **service de présence existant**, **sans
  aucune évolution d'API**.
- **FR-004** : En cas de succès, l'application MUST afficher une **confirmation** (« Présence
  enregistrée ») avec l'**identité** du membre et l'**heure**, en distinguant le cas **« nouvellement
  enregistré »** du cas **« déjà présent »** (les deux étant des succès, sans doublon).
- **FR-005** : Tout **résultat** de scan (succès ou erreur) MUST être présenté dans un **overlay modal**
  qui **suspend** la détection ; le scan **reprend uniquement** lorsque le membre **ferme** l'overlay
  (« Fermer » / « Scanner à nouveau »), sans quitter l'écran.
- **FR-014** : Tant qu'un résultat n'est pas résolu (overlay ouvert), l'application MUST **suspendre** la
  détection et n'émettre **qu'une seule** soumission par code capté — garantissant l'absence de
  double-soumission même si le code reste dans le cadre.

**Sécurité & frontière de responsabilité**

- **FR-006** : Le **jeton** contenu dans le QR MUST **ne jamais** être affiché en clair, journalisé, ni
  persisté ; il ne sert qu'à l'appel d'enregistrement.
- **FR-007** : L'application MUST **communiquer exclusivement en HTTPS**.
- **FR-008** : L'application MUST se limiter à **présenter et orchestrer** le parcours ; **aucune règle
  métier** (validité du jeton, appartenance à la séance, état de la séance, unicité de présence) MUST être
  réimplémentée côté client — le **serveur** reste l'unique autorité et renvoie les refus.
- **FR-009** : Sur réponse **« non autorisé »** (session expirée), l'application MUST **purger** la
  session et **revenir à la connexion** avec un message clair, sans donnée protégée résiduelle.

**Gestion des erreurs & permissions**

- **FR-010** : L'application MUST présenter des **messages clairs et distincts** pour : jeton
  invalide/expiré, séance close, réseau indisponible, code non reconnu/malformé — chacun laissant la
  possibilité de **re-scanner**.
- **FR-011** : L'application MUST **demander l'autorisation d'accès à la caméra** ; en cas de **refus**,
  elle MUST afficher un message d'orientation (activer dans les réglages) sans bloquer le reste de l'app.
- **FR-012** : L'application MUST être en **français** et adaptée à un usage **tactile mobile** (cible de
  visée lisible, états chargement/erreur/succès).
- **FR-013** : L'application MUST **ne pas** mettre en file les scans hors ligne dans ce lot (la capture
  hors ligne et la synchronisation par lot relèvent d'un lot ultérieur) ; hors réseau, elle invite à
  réessayer.

### Key Entities *(include if feature involves data)*

- **Charge du QR de séance** : information portée par le code projeté — un **JSON versionné**
  `{"v":1,"s":<sessionId>,"t":"<token>"}` contenant la **version de format**, l'**identifiant de séance**
  et le **jeton rotatif** courant. Opaque et éphémère côté client (jeton jamais affiché/persisté).
- **Confirmation de présence** : résultat affiché après un scan réussi — **identité** du membre, **heure**
  d'enregistrement, et **statut** (« enregistrée » ou « déjà présente »).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : Un membre peut **enregistrer sa présence** en **moins de 10 secondes** entre l'ouverture de
  l'écran Scanner et la confirmation, caméra déjà autorisée.
- **SC-002** : **100 %** des scans avec un **jeton expiré/invalide** ou une **séance close** sont
  **refusés avec un message clair** — **aucun faux succès** ni doublon.
- **SC-003** : Le **jeton** n'est **jamais** observable en clair (écran, journaux) ni persisté sur
  l'appareil.
- **SC-004** : À l'**expiration de session** (« non autorisé »), l'application **revient à la connexion**
  avec un message, **sans** donnée protégée résiduelle.
- **SC-005** : Re-scanner une séance où le membre est **déjà présent** produit un résultat
  **« déjà présent »** **sans créer de doublon** ni erreur bloquante.
- **SC-006** : Un **refus d'autorisation caméra** aboutit à un **message d'orientation clair** (activation
  dans les réglages), **sans plantage**, le reste de l'app restant utilisable.

## Assumptions

- **API inchangée** : le point d'accès de scan existe déjà et est autorisé au **membre simple** ; il prend
  l'identifiant de séance et le jeton, et distingue « enregistré » de « déjà présent ». Ce lot
  n'introduit **aucune** évolution d'API ni migration de base.
- **Charge du QR** : décision retenue — le QR encode **sessionId + token** sous forme de **JSON
  versionné** `{"v":1,"s":<sessionId>,"t":"<token>"}` (cf. Clarifications). La mise à jour de la
  **projection du QR côté bureau** (console web, feature 014) pour encoder ce payload est un
  **prérequis in-scope** de ce lot ; elle ne modifie ni l'API ni la base.
- **Membre authentifié** : la session mobile et l'identité proviennent du socle livré (lot M0) ; l'app ne
  gère ici que **sa propre** présence, sans droit de gestion.
- **Appareil** : téléphone doté d'une **caméra** ; le modèle d'autorisation suit celui du système
  d'exploitation (demande à l'usage).
- **Réseau** : usage terrain à connectivité incertaine ; ce lot **n'inclut pas** de file hors ligne (lot
  ultérieur), mais présente des états réseau explicites.
- **Outillage** : l'implémentation nécessitera un **composant de scan par caméra** et la **gestion des
  permissions** (installation d'outillage à approuver), sans incidence sur cette spécification.

## Out of Scope

- La **capture hors ligne** et la **synchronisation par lot** des scans (lot M2).
- Le **tableau de bord / historique** de présence du membre (lot M3).
- Toutes les **fonctions bureau** : démarrer/clôturer une séance, **projeter** le QR, ajout manuel,
  gestion des membres et des profils.
- L'ouverture de l'app par **lien profond** et tout autre mode d'entrée que le scan caméra.
- Toute **évolution d'API** ou de base de données.
