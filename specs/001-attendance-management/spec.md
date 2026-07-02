# Feature Specification : Gestion de la présence aux réunions

**Feature Branch**: `001-attendance-management`

**Created**: 2026-07-02

**Status**: Draft

**Input**: User description: "Gestion de présence des membres de la communauté Lumineux — première fonctionnalité de l'application. À chaque réunion tenue dans une antenne, un membre du bureau démarre une session (date : année, mois, jour, heure). Chaque membre disposant de l'application mobile scanne, à son arrivée, un code QR généré par la session ; l'heure du scan est enregistrée comme heure d'arrivée. Le membre du bureau peut aussi ajouter la présence de personnes n'ayant pas l'application mobile. L'heure de clôture de la session devient l'heure de fin de réunion enregistrée pour toutes les personnes présentes (scan ou ajout manuel)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Démarrage d'une session de réunion par le bureau (Priority: P1)

Un membre du bureau, présent physiquement dans une antenne au moment d'une réunion, ouvre l'application et démarre une nouvelle session de présence. Il sélectionne l'antenne concernée et renseigne (ou confirme) la date et l'heure de la réunion. Le système génère alors un code QR propre à cette session, que le bureau affiche (écran, projection ou impression) pour que les membres puissent le scanner.

**Why this priority**: Sans session ouverte, aucune présence ne peut être enregistrée. C'est le point de départ obligatoire de tout le flux — c'est le socle minimal (MVP) qui rend la fonctionnalité utilisable.

**Independent Test**: Peut être testé entièrement en faisant démarrer une session par un membre du bureau autorisé et en vérifiant qu'une session active est créée, rattachée à la bonne antenne et à la bonne date/heure, et qu'un code QR exploitable est produit.

**Acceptance Scenarios**:

1. **Given** un membre du bureau authentifié disposant du droit de gérer les présences, **When** il démarre une session pour une antenne donnée avec une date et une heure valides, **Then** une session « ouverte » est créée, rattachée à l'antenne et à l'horaire, et un code QR unique lui est présenté.
2. **Given** une session déjà ouverte pour une antenne à une date/heure, **When** un membre du bureau tente d'en démarrer une seconde pour la même antenne au même moment, **Then** le système empêche le doublon et redirige vers la session existante.
3. **Given** un utilisateur sans le droit de gérer les présences, **When** il tente de démarrer une session, **Then** l'action est refusée avec un message explicite.

---

### User Story 2 - Enregistrement de la présence par scan du code QR (Priority: P1)

Un membre de la communauté disposant de l'application mobile arrive à la réunion. Il ouvre l'application, scanne le code QR affiché par le bureau, et sa présence est enregistrée automatiquement avec l'heure exacte du scan comme heure d'arrivée. Le membre reçoit une confirmation visuelle que sa présence a bien été prise en compte.

**Why this priority**: C'est la valeur centrale de la fonctionnalité — automatiser et fiabiliser le pointage des membres. Combinée à la User Story 1, elle constitue le cœur fonctionnel livrable.

**Independent Test**: Peut être testé en ouvrant une session, en faisant scanner le QR par un membre authentifié, et en vérifiant qu'un enregistrement de présence est créé avec l'heure d'arrivée = heure du scan, rattaché au bon membre et à la bonne session.

**Acceptance Scenarios**:

1. **Given** une session ouverte et un membre authentifié sur l'application mobile, **When** il scanne le code QR valide de la session, **Then** sa présence est enregistrée avec l'heure du scan comme heure d'arrivée et il reçoit une confirmation.
2. **Given** un membre ayant déjà scanné le QR de la session, **When** il le scanne à nouveau, **Then** aucun doublon n'est créé et l'heure d'arrivée initiale est conservée.
3. **Given** un membre dont l'antenne d'origine diffère de l'antenne de la réunion, **When** il scanne le QR, **Then** sa présence est enregistrée pour la session de cette antenne (la réunion visitée), tout en conservant la traçabilité de son antenne d'origine.
4. **Given** une session déjà clôturée, **When** un membre scanne son code QR, **Then** l'enregistrement est refusé avec un message indiquant que la réunion est terminée.

---

### User Story 3 - Ajout manuel de présences par le bureau (Priority: P2)

Certains membres présents ne disposent pas de l'application mobile. Le membre du bureau, depuis la session ouverte, recherche ces membres dans la liste de la communauté et les marque présents manuellement. Leur heure d'arrivée est celle de l'ajout (ou une heure saisie par le bureau).

**Why this priority**: Indispensable pour que la présence soit complète et fiable, mais dépend des User Stories 1 et 2 qui fournissent déjà un MVP fonctionnel pour les membres équipés.

**Independent Test**: Peut être testé en ouvrant une session puis en faisant ajouter par le bureau un membre sans application, et en vérifiant qu'un enregistrement de présence est créé pour ce membre, rattaché à la session, avec la source « ajout manuel ».

**Acceptance Scenarios**:

1. **Given** une session ouverte et un membre du bureau autorisé, **When** il recherche un membre existant et le marque présent, **Then** une présence est enregistrée pour ce membre avec la source « ajout manuel » et une heure d'arrivée.
2. **Given** un membre déjà enregistré comme présent (par scan ou ajout), **When** le bureau tente de l'ajouter à nouveau, **Then** le système signale qu'il est déjà présent et n'ajoute pas de doublon.
3. **Given** une présence ajoutée par erreur, **When** le bureau la retire avant la clôture de la session, **Then** l'enregistrement de présence est supprimé (ou marqué annulé) et tracé.

---

### User Story 4 - Clôture de la session et enregistrement de l'heure de fin (Priority: P2)

À la fin de la réunion, le membre du bureau clôture la session. L'heure de clôture devient l'heure de fin de réunion, qui est renseignée pour toutes les personnes présentes (par scan ou ajout manuel). Une fois clôturée, la session n'accepte plus de nouvelles présences.

**Why this priority**: Complète le cycle de vie de la réunion et fige les données pour l'exploitation ultérieure. Nécessite les User Stories précédentes.

**Independent Test**: Peut être testé en clôturant une session comportant plusieurs présences et en vérifiant que l'heure de fin est appliquée à tous les enregistrements de présence et que la session refuse tout nouveau pointage.

**Acceptance Scenarios**:

1. **Given** une session ouverte avec des présences enregistrées, **When** le bureau la clôture, **Then** l'heure de clôture est enregistrée comme heure de fin de réunion pour toutes les présences de la session.
2. **Given** une session clôturée, **When** un membre tente de scanner ou le bureau tente d'ajouter une présence, **Then** l'action est refusée.
3. **Given** une session ouverte depuis une longue durée sans clôture explicite, **When** le délai maximal configuré est dépassé, **Then** le système applique le comportement de clôture défini (voir Hypothèses).

---

### Edge Cases

- **Scan hors ligne** : le scan est mis en file localement avec l'heure réelle d'arrivée, puis synchronisé à la reconnexion (voir FR-023) ; cas particulier d'une synchronisation intervenant après la clôture de la session (voir FR-023b).
- **Session inexistante ou QR périmé** : un membre scanne un ancien QR ou un QR ne correspondant à aucune session ouverte → refus avec message clair.
- **Membre inactif/suspendu** : un membre dont le statut n'est pas actif tente de scanner → comportement à définir (refus recommandé, voir Hypothèses).
- **Horloge de l'appareil décalée** : l'heure d'arrivée doit s'appuyer sur une source de temps fiable côté système, pas sur l'horloge locale de l'appareil.
- **Double session sur la même antenne le même jour** (ex. réunion matin et soir) : le système doit distinguer deux sessions par leur horaire.
- **Fraude par partage du QR** : un membre absent reçoit une photo du QR et scanne à distance → le jeton rotatif (FR-013) rend la photo rapidement invalide ; un scan présentant un jeton périmé est rejeté.
- **Retrait d'une présence après clôture** : interdit ou soumis à une correction tracée (voir Hypothèses).
- **Membre présent physiquement mais introuvable dans la communauté** lors d'un ajout manuel → le bureau ne peut pas créer de présence sans membre existant (la création de membre relève d'une autre fonctionnalité).

## Requirements *(mandatory)*

### Functional Requirements

**Gestion de la session**

- **FR-001**: Le système DOIT permettre à un membre du bureau autorisé de démarrer une session de présence rattachée à une antenne et à une date/heure de réunion.
- **FR-002**: Le système DOIT générer, pour chaque session, un code QR permettant l'enregistrement des présences.
- **FR-003**: Le système DOIT empêcher l'existence de deux sessions ouvertes simultanées pour la même antenne au même créneau horaire.
- **FR-004**: Le système DOIT gérer le cycle de vie d'une session avec au minimum les états « ouverte » et « clôturée ».
- **FR-005**: Le système DOIT permettre à un membre du bureau autorisé de clôturer une session.
- **FR-006**: À la clôture, le système DOIT enregistrer l'heure de clôture comme heure de fin de réunion pour toutes les présences rattachées à la session.
- **FR-007**: Le système DOIT refuser tout nouvel enregistrement de présence (scan ou ajout) sur une session clôturée.
- **FR-024**: Le système DOIT appliquer une **clôture automatique de secours** : si une session reste `ouverte` au-delà d'un délai configurable après son heure de réunion, le système la clôture automatiquement et renseigne une heure de fin par défaut (définie en configuration), afin d'éviter les sessions orphelines. Cette clôture automatique produit les mêmes effets qu'une clôture manuelle (propagation de l'heure de fin, refus des nouveaux pointages).

**Enregistrement de la présence par scan**

- **FR-008**: Le système DOIT permettre à un membre authentifié disposant de l'application mobile d'enregistrer sa présence en scannant le code QR d'une session ouverte.
- **FR-009**: Le système DOIT enregistrer l'heure d'arrivée d'un membre comme l'instant du scan, en s'appuyant sur une source de temps fiable côté serveur. **Exception hors ligne** : pour un scan effectué sans connexion (FR-023), l'heure d'arrivée est l'heure réelle du scan capturée côté client, **bornée/validée par le serveur** lors de la synchronisation (rejetée si hors de la plage plausible de la session).
- **FR-010**: Le système DOIT empêcher les doublons de présence : un même membre ne peut être enregistré qu'une seule fois par session, l'heure d'arrivée initiale étant conservée.
- **FR-011**: Le système DOIT permettre à un membre de s'enregistrer à une session d'une antenne différente de son antenne d'origine, tout en conservant la traçabilité de son antenne d'origine.
- **FR-012**: Le système DOIT fournir une confirmation à l'utilisateur lorsque sa présence a été enregistrée avec succès, et un message d'erreur explicite en cas d'échec (session close, QR invalide, membre non autorisé).
- **FR-013**: Le système DOIT protéger le code QR contre l'enregistrement frauduleux de présences à distance au moyen d'un **jeton rotatif** : le code QR affiché DOIT changer périodiquement (intervalle court, configurable) de sorte qu'une photo capturée devienne rapidement invalide.
- **FR-013a**: Le système DOIT rejeter un scan présentant un jeton QR expiré ou déjà remplacé, avec un message invitant à scanner le code QR courant affiché par le bureau.
- **FR-025**: Le système DOIT refuser l'enregistrement de présence (par scan ou par ajout manuel) d'un membre dont le statut n'est pas « actif », avec un message explicite. Ce contrôle s'applique aux User Stories 2 et 3.

**Ajout manuel de présences**

- **FR-014**: Le système DOIT permettre à un membre du bureau autorisé d'enregistrer manuellement la présence d'un membre existant ne disposant pas de l'application mobile, sur une session ouverte.
- **FR-015**: Le système DOIT distinguer la source de chaque présence (scan par le membre vs ajout manuel par le bureau).
- **FR-016**: Le système DOIT permettre au bureau de retirer/annuler une présence enregistrée par erreur tant que la session est ouverte, en conservant une trace de l'opération.
- **FR-017**: Le système NE DOIT PAS permettre l'ajout d'une présence pour une personne non enregistrée comme membre de la communauté (la création de membre relève d'une autre fonctionnalité).

**Contrôle d'accès et traçabilité**

- **FR-018**: Le système DOIT restreindre le démarrage, la clôture, l'ajout et le retrait de présences aux membres du bureau disposant du droit correspondant.
- **FR-019**: Le système DOIT tracer, pour chaque session et chaque présence, l'auteur et l'horodatage de création et de modification (piste d'audit).
- **FR-020**: Le système DOIT consigner les tentatives d'enregistrement refusées (session close, QR invalide, droit manquant) à des fins de diagnostic et de sécurité.

**Consultation**

- **FR-021**: Le système DOIT permettre à un membre du bureau autorisé de consulter, en temps réel pendant la session, la liste des présents et le décompte courant.
- **FR-022**: Le système DOIT permettre de consulter, après clôture, la liste des présences d'une session (membre, antenne, heure d'arrivée, heure de fin, source).
- **FR-022a**: La restitution statistique et les tableaux de bord d'assiduité (taux de présence par membre/antenne/période, exports) sont HORS PÉRIMÈTRE de cette fonctionnalité et relèvent d'une fonctionnalité ultérieure dédiée au reporting. Cette fonctionnalité se limite à la consultation en direct et à la liste par session.

**Contexte hors ligne (mobile)**

- **FR-023**: Lorsqu'un scan est effectué sans connexion réseau, le système DOIT mettre le scan en file localement sur l'appareil en conservant l'heure réelle d'arrivée, puis le synchroniser automatiquement à la reconnexion.
- **FR-023a**: Lors de la synchronisation d'un scan hors ligne, le système DOIT appliquer l'heure réelle d'arrivée capturée lors du scan et respecter les règles anti-doublon (FR-010), même si plusieurs scans sont synchronisés simultanément.
- **FR-023b**: Le système DOIT gérer le cas où un scan hors ligne est synchronisé après la clôture de la session, selon une règle déterministe définie à la planification (par défaut : accepté si l'heure d'arrivée est antérieure à l'heure de clôture, sinon rejeté et signalé).

### Key Entities *(include if feature involves data)*

- **Session de présence** : représente une réunion tenue dans une antenne à une date/heure donnée. Attributs clés : antenne concernée, date et heure de réunion, état (ouverte/clôturée), heure de clôture / heure de fin, membre du bureau initiateur, référence du code QR associé, informations d'audit.
- **Présence** : représente la participation d'un membre à une session. Attributs clés : session rattachée, membre concerné, heure d'arrivée, heure de fin (héritée de la clôture de session), source (scan mobile / ajout manuel), état (valide/annulée), antenne d'origine du membre au moment de la réunion, informations d'audit.
- **Code QR de session (jeton rotatif)** : jeton permettant d'associer un scan à une session ouverte. Attributs clés : session rattachée, valeur/jeton courant, horodatage de génération et fenêtre de validité (le jeton est renouvelé périodiquement ; un jeton expiré ou remplacé n'est plus accepté).
- **Membre** *(existant)* : personne de la communauté susceptible d'être marquée présente. Rattaché à une antenne d'origine et à un statut. Réutilisé depuis le modèle de données existant.
- **Antenne** *(existante)* : lieu où se tiennent les réunions. Réutilisée depuis le modèle de données existant.
- **Bureau / Profil de droits** *(existant ou dépendance)* : ensemble des membres et des droits déterminant qui peut gérer les sessions et les présences.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un membre du bureau peut démarrer une session et obtenir un code QR exploitable en moins de 30 secondes.
- **SC-002**: Un membre peut enregistrer sa présence par scan en moins de 5 secondes entre l'ouverture du scanner et la confirmation affichée.
- **SC-003**: Pour une réunion de 100 participants, 100 % des présences enregistrées correspondent à des membres réellement présents et distincts (aucun doublon, aucune présence fantôme dans les conditions de test).
- **SC-004**: L'heure d'arrivée enregistrée correspond à l'instant réel du scan avec un écart inférieur à 5 secondes.
- **SC-005**: À la clôture, 100 % des présences de la session reçoivent la même heure de fin de réunion.
- **SC-006**: Le système traite l'affluence d'arrivée d'au moins 200 scans en moins de 2 minutes sans perte ni dégradation perceptible.
- **SC-007**: Le temps moyen de constitution de la liste de présence d'une réunion est réduit d'au moins 60 % par rapport à un pointage manuel sur papier.
- **SC-008**: 90 % des membres équipés réussissent l'enregistrement de leur présence par scan dès la première tentative, sans assistance.

## Assumptions

- **Authentification préexistante** : un mécanisme d'authentification des membres (mobile) et des membres du bureau existe ou sera fourni en dépendance ; cette fonctionnalité s'appuie dessus sans le redéfinir. Le premier login/changement de mot de passe des membres relève d'une autre fonctionnalité.
- **Modèle de gestion des droits** : la notion de « bureau » et de profils de droits existe (ou sera fournie) ; on suppose l'existence d'un droit « gérer les présences » assignable. Le détail de la gestion des profils relève d'une autre fonctionnalité.
- **Entités géographiques et membres existants** : les entités Membre et Antenne du modèle de données existant sont réutilisées ; cette fonctionnalité ne crée ni membre ni antenne.
- **Antenne d'origine du membre** : l'entité Membre est enrichie d'un rattachement direct à son **antenne d'origine** (champ `antenna`, nullable) — décision de modélisation retenue pour FR-011. `Attendance.originAntenna` en est un **instantané** au moment de la réunion. L'historique des changements d'antenne d'origine n'est pas conservé dans cette itération.
- **Source de temps** : l'heure d'arrivée et l'heure de fin s'appuient sur l'horloge du serveur (source fiable), et non sur l'horloge de l'appareil mobile.
- **Une session par créneau et par antenne** : deux réunions distinctes le même jour dans la même antenne (matin/soir) sont modélisées comme deux sessions distinctes par leur horaire.
- **Membres inactifs** : un membre dont le statut n'est pas « actif » ne peut pas enregistrer sa présence (scan ou ajout manuel) — désormais formalisé par FR-025.
- **Clôture automatique de secours** : formalisée par FR-024 ; le délai et l'heure de fin par défaut sont fixés en configuration.
- **Corrections après clôture** : par défaut, une session clôturée est figée ; toute correction ultérieure relève d'une procédure d'ajustement tracée à définir (hors périmètre initial).
- **Plateforme de livraison** : l'enregistrement par scan se fait via l'application mobile ; la gestion des sessions et l'ajout manuel sont disponibles pour le bureau (application mobile et/ou tableau de bord). Le périmètre précis des interfaces sera arrêté à la planification.
- **Jeton QR rotatif** : le renouvellement périodique du code QR (FR-013) suppose un affichage capable de rafraîchir le code côté bureau ; l'intervalle de rotation exact sera fixé en configuration à la planification.
- **Reporting reporté** : les statistiques et tableaux de bord d'assiduité feront l'objet d'une fonctionnalité ultérieure dédiée (FR-022a) ; les données de présence doivent néanmoins être structurées de façon à rendre ce reporting futur possible.
