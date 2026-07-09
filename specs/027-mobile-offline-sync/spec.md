# Feature Specification: Application mobile membre — capture hors ligne et synchronisation des présences

**Feature Branch**: `027-mobile-offline-sync`

**Created**: 2026-07-09

**Status**: Ready (implémenté — feature 027, M2)

**Input**: User description: « Lot M2 de l'app mobile membre (Flutter, `mobile/`) : capture de présence
**hors ligne** et **synchronisation par lot**. Quand le membre scanne le QR d'une séance (fonction M1)
sans réseau, l'app **capture** le scan dans une **file persistante** au lieu d'échouer, puis le
**synchronise** automatiquement au retour de la connexion via l'endpoint de lot **existant**. Le serveur
re-valide le jeton contre **l'heure du scan** (pas de l'heure de synchro), donc la capture reste valide.
Sans évolution d'API ni de base. Hors périmètre : tableau de bord membre (M3), fonctions bureau. »

## User Scenarios & Testing *(mandatory)*

Ce lot fiabilise la présence en **terrain à connectivité incertaine** : aucune présence ne doit être
perdue faute de réseau. Il s'appuie sur le scan (M1) et sur l'API de synchronisation par lot **déjà
livrée**. Le membre agit uniquement sur **sa propre** présence, sans droit de gestion.

### User Story 1 - Enregistrer ma présence même sans réseau (Priority: P1)

En tant que **membre** présent à une réunion **sans connexion**, je veux que **mon scan soit tout de même
enregistré localement**, afin de ne pas perdre ma présence et de la voir synchronisée plus tard.

**Why this priority** : c'est la valeur centrale du lot — garantir qu'aucune présence n'est perdue hors
ligne. Une app qui **capture** le scan hors réseau et **confirme** au membre constitue à elle seule un
incrément démontrable (le MVP de M2), même avant l'automatisation de la synchronisation.

**Independent Test** : couper le réseau, scanner un QR de séance valide → une **confirmation « enregistrée
hors ligne, à synchroniser »** s'affiche ; fermer et **relancer** l'app → la capture est **toujours
présente** dans la file (non perdue).

**Acceptance Scenarios** :

1. **Given** un membre sur l'écran Scanner **sans réseau**, **When** il scanne un QR de séance, **Then**
   le scan est **capturé localement** (file d'attente) et une confirmation indique qu'il est **enregistré
   hors ligne, à synchroniser** — **sans** message d'échec.
2. **Given** une capture hors ligne enregistrée, **When** le membre **ferme et relance** l'application,
   **Then** la capture est **toujours en file** (persistée), prête à être synchronisée.
3. **Given** un scan tenté **avec réseau** (M1), **When** il aboutit en ligne, **Then** le comportement M1
   est **inchangé** (présence enregistrée immédiatement) et **aucune** entrée n'est ajoutée à la file.
4. **Given** une capture hors ligne, **When** elle est enregistrée, **Then** l'app mémorise ce qui est
   nécessaire à la synchronisation (séance, jeton, **heure exacte du scan**, identifiant d'opération
   unique) **sans jamais** afficher le jeton.

---

### User Story 2 - Synchroniser automatiquement au retour du réseau (Priority: P2)

En tant que **membre** dont des présences ont été capturées hors ligne, je veux qu'elles soient
**synchronisées automatiquement** dès que la connexion revient, afin qu'elles soient **prises en compte
par le serveur** sans action de ma part.

**Why this priority** : transforme la capture en présence confirmée côté serveur ; essentielle mais vient
après la capture (qui, seule, préserve déjà la donnée).

**Independent Test** : avec des captures en file, rétablir le réseau → les captures sont **envoyées par
lot** (groupées par séance) et **retirées** de la file selon leur issue ; re-synchroniser ne crée **aucun
doublon**.

**Acceptance Scenarios** :

1. **Given** des captures en file et le **réseau rétabli**, **When** la synchronisation se déclenche
   (automatiquement), **Then** les captures sont **envoyées** au serveur, **groupées par séance**.
2. **Given** une réponse de synchronisation, **When** un élément revient **« enregistré »** ou **« déjà
   présent »**, **Then** il est **retiré de la file** (succès définitif, aucun doublon).
3. **Given** une nouvelle synchronisation d'un élément **déjà synchronisé**, **When** elle a lieu, **Then**
   elle **ne crée pas de doublon** (idempotence par l'identifiant d'opération) et l'élément est retiré.
4. **Given** une **erreur réseau/serveur transitoire** pendant la synchronisation, **When** elle survient,
   **Then** les éléments **non traités restent en file** pour un **nouvel essai** ultérieur.
5. **Given** l'application relancée avec des captures en attente, **When** elle démarre **avec réseau**,
   **Then** une synchronisation est **tentée automatiquement**.

---

### User Story 3 - Suivre l'état de synchronisation et les rejets (Priority: P3)

En tant que **membre**, je veux **voir combien de présences sont en attente/en cours** et être **informé
des rejets** avec leur raison, afin de comprendre l'état de mes présences et d'agir si besoin (relancer).

**Why this priority** : transparence et confiance ; utile mais non bloquant pour la préservation et la
synchronisation elles-mêmes.

**Independent Test** : capturer plusieurs scans hors ligne → un **indicateur** montre le nombre **en
attente** ; après synchronisation, un élément **rejeté** est **signalé avec sa raison** et **retiré** de la
file ; un bouton permet de **relancer** la synchronisation.

**Acceptance Scenarios** :

1. **Given** des captures hors ligne, **When** le membre consulte l'app, **Then** un **indicateur d'état**
   affiche le nombre d'éléments **en attente** (et **en cours** pendant la synchronisation).
2. **Given** une synchronisation renvoyant un **rejet** (jeton invalide au moment du scan, heure hors
   plage, arrivée après clôture), **When** elle est traitée, **Then** l'élément rejeté est **signalé
   clairement au membre avec sa raison** puis **retiré** de la file (aucune présence « coincée »).
3. **Given** des éléments en attente, **When** le membre déclenche une **relance manuelle**, **Then** une
   synchronisation est **tentée immédiatement**.

---

### Edge Cases

- **Jeton périmé à la synchronisation** : le serveur validant le jeton contre **l'heure du scan** (avec
  tolérance), une capture reste valide ; hors tolérance → **rejet** avec raison, signalé et retiré.
- **Doublon hors ligne** : plusieurs scans de la même séance par le même membre → **déduplication locale**
  (FR-014), une seule capture en file ; à la synchro, l'idempotence côté serveur (**« déjà présent »**)
  couvre le cas résiduel où une présence existerait déjà côté serveur.
- **App fermée avec des captures** → file **persistée**, synchronisée au prochain lancement avec réseau.
- **Synchronisation partielle** (certains éléments traités, d'autres en erreur transitoire) → seuls les
  **non traités** restent en file.
- **Séance close avant la synchro** → selon la règle serveur : accepté si l'arrivée précède la clôture,
  sinon **rejeté** (« arrivée postérieure à la clôture »), signalé.
- **Horloge de l'appareil incorrecte** → l'heure d'arrivée peut être jugée **hors plage** par le serveur →
  rejet signalé.
- **Session utilisateur expirée** pendant la synchronisation (réponse « non autorisé ») → l'app purge la
  session et revient à la connexion ; les captures **restent en file** pour une synchro après
  reconnexion.
- **Perte de connexion en cours de synchronisation** → les éléments non confirmés restent en file.
- **QR non reconnu hors ligne** (mauvais format, illisible, non lié à une séance) → **erreur immédiate
  « QR non reconnu »**, aucune capture en file (FR-001a) ; pas de fausse confirmation « enregistré hors
  ligne ».
- **Erreur transitoire persistante** (serveur durablement indisponible ou capture trop ancienne) → au-delà
  du **plafond de tentatives ou d'âge** (FR-013), l'élément passe en **« échec définitif »**, est signalé
  avec sa raison et retiré ; il ne reste pas indéfiniment en file.

## Clarifications

### Session 2026-07-09

- Q: Que faire d'un élément en **erreur transitoire persistante** (jamais synchronisé) au regard de SC-004 (« aucune présence coincée ») ? → A: Plafond combiné **tentatives + âge** — après un nombre maximal de tentatives **ou** une ancienneté maximale, l'élément passe en **« échec définitif »**, est **signalé au membre avec raison** et **retiré** de la file (seuils précis arrêtés au `/speckit-plan`).
- Q: Comment relancer une synchro qui échoue de façon transitoire alors que le réseau est présent et l'app ouverte ? → A: Réessai automatique avec **backoff exponentiel** tant que l'app est active et que des éléments restent en file, jusqu'au plafond FR-013 (en plus des 3 déclencheurs discrets et de la relance manuelle).
- Q: Que faire d'un re-scan hors ligne de la **même séance** déjà en file (même membre) ? → A: **Déduplication locale** — le re-scan est **ignoré** (la capture existante, avec son heure et son identifiant d'opération, est conservée) et le membre reçoit un retour « déjà capturée hors ligne » ; une seule entrée en file par séance.
- Q: Faut-il capturer hors ligne un QR non pertinent (mauvais format/illisible) ou le refuser localement ? → A: **Validation structurelle locale** avant capture — ne mettre en file que si le QR a la **forme attendue** (identifiant de séance + jeton présents) ; sinon **erreur immédiate « QR non reconnu »**, rien en file. Vérification purement structurelle, **sans** re-validation des règles métier (FR-010 préservé).

## Requirements *(mandatory)*

### Functional Requirements

**Capture hors ligne**

- **FR-001** : Lorsqu'un scan de QR est tenté **sans réseau**, l'application MUST **capturer** le scan
  dans une **file locale** au lieu d'échouer, et **confirmer** au membre qu'il est **enregistré hors
  ligne, à synchroniser**.
- **FR-001a** : Avant toute capture hors ligne, l'application MUST effectuer une **validation
  structurelle locale** du QR : ne mettre en file que si le payload a la **forme attendue** (identifiant de
  séance et jeton présents). Un QR **non reconnu** (mauvais format, illisible) MUST produire une **erreur
  immédiate « QR non reconnu »** et **ne rien** ajouter à la file. Cette vérification est **purement
  structurelle** et ne constitue **pas** une re-validation des règles métier (cf. FR-010).
- **FR-002** : Chaque élément en file MUST conserver : l'**identifiant de séance**, le **jeton scanné**,
  l'**heure exacte du scan** (heure d'arrivée côté client) et un **identifiant d'opération unique**
  (≤ 64 caractères) garantissant l'**idempotence**.
- **FR-003** : La file MUST **persister** au redémarrage de l'application.
- **FR-004** : Le comportement du **scan en ligne (M1)** MUST rester **inchangé** ; **seul** le chemin
  d'**échec réseau** change (capture au lieu d'erreur). Un scan **réussi en ligne** ne MUST **pas** être
  ajouté à la file.
- **FR-014** : Un **re-scan hors ligne d'une séance déjà présente en file** (même membre) MUST être
  **dédupliqué localement** : la capture existante (heure de scan et identifiant d'opération d'origine) est
  **conservée**, aucune nouvelle entrée n'est créée, et le membre reçoit un retour **« déjà capturée hors
  ligne »**. La file MUST contenir **au plus une** capture en attente par séance.

**Synchronisation par lot**

- **FR-005** : L'application MUST **synchroniser** les éléments en file via le **service de lot existant**,
  en les **groupant par séance** (un envoi par séance), **sans aucune évolution d'API**.
- **FR-006** : La synchronisation MUST se déclencher **automatiquement** au **retour de la connectivité**
  et au **lancement** de l'application, et MUST pouvoir être **relancée manuellement**. En cas d'**échec
  transitoire** avec app active et éléments restant en file, l'application MUST **réessayer
  automatiquement** avec un **backoff exponentiel** (délai croissant entre tentatives), jusqu'au plafond
  défini en **FR-013**, sans marteler le serveur.
- **FR-007** : Pour chaque élément, l'application MUST **réconcilier** l'issue renvoyée : **retirer** de la
  file les éléments **« enregistré »** et **« déjà présent »** (succès définitif) ; **retirer** les
  éléments **« rejetés »** en **informant** le membre de la **raison** ; **conserver** pour un nouvel essai
  **uniquement** les éléments **non traités** pour cause d'erreur **réseau/serveur transitoire**, dans la
  limite du plafond défini en **FR-013**.
- **FR-013** : Un élément en **erreur transitoire persistante** MUST être borné par un **plafond combiné** :
  au-delà d'un **nombre maximal de tentatives** OU d'une **ancienneté maximale** (seuils par défaut fixés
  en conception, voir `research.md` D4 : 8 tentatives / 7 jours, ajustables par configuration),
  il MUST passer en état **« échec définitif »**, être **signalé au membre avec sa raison**, puis **retiré**
  de la file — de sorte qu'**aucune** présence ne reste indéfiniment « coincée » (cf. SC-004).
- **FR-008** : La re-synchronisation MUST être **idempotente** : ré-envoyer un élément déjà synchronisé ne
  MUST **pas** créer de doublon (via l'identifiant d'opération) et l'élément est retiré.

**Sécurité & frontière**

- **FR-009** : Les **jetons** en file MUST être **stockés de façon protégée** et **purgés** après
  traitement définitif ; le jeton MUST **ne jamais** être **affiché** ni **journalisé**.
- **FR-010** : L'application MUST **communiquer exclusivement en HTTPS** et laisser le **serveur** seul
  **autorité** : elle **capture, met en file et transmet** — elle ne **re-valide aucune** règle métier
  (validité du jeton, plage horaire, clôture, unicité). La **validation structurelle** du QR (FR-001a) ne
  fait **pas** partie des règles métier : elle vérifie uniquement la **forme** du payload, jamais sa
  validité fonctionnelle.

**Suivi & UX**

- **FR-011** : L'application MUST **afficher l'état de synchronisation** : nombre d'éléments **en attente**,
  **en cours**, et **rejetés/échoués** (avec **raisons** accessibles).
- **FR-012** : L'application MUST être en **français**, adaptée à un usage **tactile mobile**, avec des
  états clairs (confirmation de capture hors ligne, synchronisation en cours, terminée, rejet).

### Key Entities *(include if feature involves data)*

- **Capture hors ligne en attente** : un scan mémorisé localement tant qu'il n'est pas traité — comporte
  l'**identifiant de séance**, le **jeton** (protégé, éphémère), l'**heure d'arrivée** (moment du scan),
  l'**identifiant d'opération unique**, un **compteur de tentatives** et un **état** (en attente / en cours /
  échoué transitoire / échec définitif). Retirée à l'issue définitive (enregistrée, déjà présente, rejetée,
  ou **échec définitif** après plafond de tentatives/âge).
- **État de synchronisation** : agrégat présenté au membre — **compte** des captures **en attente**, **en
  cours**, **rejetées** (avec raisons), et l'horodatage/résultat de la dernière tentative.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : Un scan effectué **sans réseau** est **capturé et confirmé** en **moins de 3 secondes** et
  **survit** à un redémarrage de l'application (**aucune perte**).
- **SC-002** : Au **retour de la connectivité**, les captures en attente sont synchronisées
  **automatiquement** en **moins de 30 secondes**, **sans** action manuelle.
- **SC-003** : Re-synchroniser des captures **ne crée jamais de doublon** de présence (idempotence).
- **SC-004** : **100 %** des éléments **rejetés** **ou** en **échec définitif** (plafond de tentatives/âge
  atteint, FR-013) sont **signalés au membre avec une raison** et **retirés** de la file — **aucune**
  présence « coincée » ni perte silencieuse.
- **SC-005** : Les **jetons** en file ne sont **jamais** observables en clair (journaux, affichage) et sont
  **purgés** après traitement.
- **SC-006** : Le membre peut, **à tout moment**, connaître le **nombre** de captures **en attente** et
  **rejetées**.

## Assumptions

- **API inchangée** : le service de **synchronisation par lot** existe déjà, est **autorisé au membre
  simple**, prend des éléments `{ identifiant d'opération, jeton, heure d'arrivée }` groupés par séance, et
  renvoie **par élément** une issue **« enregistré » / « déjà présent » / « rejeté (raison) »**. Le serveur
  **re-valide le jeton contre l'heure d'arrivée** (avec tolérance) et applique les règles de plage horaire,
  d'unicité et de synchronisation post-clôture. Ce lot n'introduit **aucune** évolution d'API ni migration.
- **Identifiant d'opération** : généré **localement**, **unique**, **≤ 64 caractères**.
- **Stockage local persistant** de la file, avec **protection** des jetons ; mécanisme précis arrêté à
  l'étape de conception.
- **Détection de connectivité** : un moyen de savoir quand la connexion revient (composant décidé à la
  conception).
- **Horloge appareil** : supposée raisonnablement exacte ; le serveur **borne** l'heure d'arrivée et
  **rejette** hors plage.
- **Scan en ligne (M1) inchangé** ; M2 n'ajoute que la capture hors ligne et la synchronisation.
- **Membre authentifié** : la session mobile et l'identité proviennent du socle (M0) ; l'app ne gère que
  **sa propre** présence.

## Out of Scope

- Le **tableau de bord / historique** de présence du membre (lot M3).
- Toutes les **fonctions bureau** (démarrer/clôturer une séance, projeter le QR, ajout manuel, gestion).
- Toute **évolution d'API** ou de base de données.
- La **résolution de conflits d'horloge** côté client (le serveur fait foi sur la plage horaire).
- La synchronisation de **données autres** que les scans de présence.
