# Feature Specification: Profils du bureau

**Feature Branch**: `004-bureau-profiles`

**Created**: 2026-07-03

**Status**: Draft

**Input**: User description: "feature : Profils du bureau"

## Clarifications

### Session 2026-07-03

- Q: Un membre du bureau peut-il détenir plusieurs profils simultanément, ou un seul à la fois ? → A: Plusieurs profils (union) — un membre peut cumuler N profils ; ses droits effectifs sont l'union.
- Q: Que faire des droits existants directement attachés aux membres (table `member_permissions` amorcée par `Auth:Bootstrap:*`) quand cette feature entre en service ? → A: Créer au déploiement un profil système « Amorçage » portant les droits amorcés et l'assigner au membre bootstrap ; les profils deviennent la source unique de vérité.
- Q: Qui peut consulter le catalogue des profils (liste des profils et leur détail) ? → A: L'administrateur des profils ET les membres disposant de `manage_members` (lecture seule) ; les modifications restent réservées aux administrateurs des profils.
- Q: Comment le garde-fou « au moins un administrateur des profils » doit-il concrètement bloquer les actions ? → A: Refuser (a) la révocation d'une attribution qui priverait le dernier administrateur, (b) le retrait du droit `manage_bureau_profiles` d'un profil qui laisserait zéro administrateur, et (c) la suppression d'un tel profil.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Définir un profil du bureau (Priority: P1)

Un administrateur du bureau (membre disposant du droit d'administration des profils) crée un
**profil du bureau** : il lui donne un nom parlant (ex. « Gestion des présences ») et coche l'ensemble
des droits fonctionnels que ce profil confère (ex. gérer les présences, gérer les membres). Il peut
ensuite modifier la description ou la liste de droits d'un profil existant, et retirer un profil qui
n'est plus utilisé.

**Why this priority**: Sans profils, l'attribution des droits repose sur un amorçage de configuration
(feature 003, F1) qui ne permet ni d'ouvrir de nouveaux droits à d'autres membres, ni de faire
évoluer les responsabilités du bureau. C'est le prérequis à toutes les autres opérations
(assignation, révocation).

**Independent Test**: Un administrateur crée un profil « Gestion des présences » avec le seul droit
`manage_attendance`, le modifie pour ajouter `manage_members`, puis le supprime tant qu'il n'est
attribué à personne — tout cela via l'API, en observant l'état persisté à chaque étape.

**Acceptance Scenarios**:

1. **Given** un administrateur des profils authentifié, **When** il crée un profil nommé
   « Gestion des présences » contenant le droit `manage_attendance`, **Then** le profil est enregistré,
   consultable, et les droits associés sont visibles.
2. **Given** un profil existant, **When** l'administrateur modifie son nom, sa description ou sa liste
   de droits, **Then** les changements sont appliqués et audités.
3. **Given** un profil qui n'est attribué à aucun membre, **When** l'administrateur le supprime,
   **Then** le profil disparaît définitivement et l'action est journalisée.
4. **Given** un utilisateur non administrateur des profils, **When** il tente de créer, modifier ou
   supprimer un profil, **Then** l'action est refusée (droit manquant).

---

### User Story 2 - Assigner un profil à un membre du bureau (Priority: P1)

L'administrateur des profils sélectionne un membre et lui **attribue** un ou plusieurs profils du
bureau. À sa prochaine connexion (ou à la prochaine émission de jeton), ce membre dispose des droits
correspondants et peut accéder aux opérations réservées (démarrer une session, corriger une fiche
membre, etc.).

**Why this priority**: C'est le mécanisme qui rend les droits vivants : sans assignation, la création
de profils n'a aucun effet observable. Pair avec US1 pour couvrir le parcours minimal
« créer un profil → l'attribuer → l'utilisateur en bénéficie ».

**Independent Test**: L'administrateur crée un profil `manage_attendance`, l'assigne à un membre
n'ayant aucun droit, puis vérifie que ce membre — après nouvelle authentification — peut désormais
démarrer une session de présence (endpoint qui exige `manage_attendance`).

**Acceptance Scenarios**:

1. **Given** un profil existant et un membre du bureau actif, **When** l'administrateur assigne le
   profil au membre, **Then** l'assignation est enregistrée et le membre voit apparaître ce profil
   dans son détail.
2. **Given** un membre à qui un profil est assigné, **When** il obtient un nouveau jeton d'accès,
   **Then** ce jeton porte l'ensemble des droits des profils qui lui sont attribués.
3. **Given** un membre à qui plusieurs profils sont assignés, **When** un jeton est émis, **Then**
   les droits portés sont l'**union** des droits de ces profils (sans doublon).
4. **Given** un profil déjà assigné à un membre, **When** l'administrateur tente d'assigner à nouveau
   le même profil au même membre, **Then** le système traite l'opération comme idempotente
   (aucune duplication).

---

### User Story 3 - Révoquer un profil ou faire évoluer ses droits (Priority: P2)

Lorsque les responsabilités d'un membre changent, l'administrateur des profils **révoque** un profil
qui lui était attribué, ou **met à jour la liste des droits** d'un profil existant. Ces changements
prennent effet à la prochaine émission de jeton pour les membres concernés.

**Why this priority**: La révocation et la mise à jour sont indispensables à la vie du bureau
(remplacements, réorganisations) mais restent secondes après la création et l'assignation initiales.

**Independent Test**: Un membre disposant de `manage_attendance` via un profil se voit retirer ce
profil ; après nouvelle authentification, son jeton ne porte plus le droit et le démarrage d'une
session lui est refusé (403).

**Acceptance Scenarios**:

1. **Given** un membre à qui un profil est assigné, **When** l'administrateur révoque cette
   assignation, **Then** l'assignation est retirée et journalisée.
2. **Given** un membre dont on a révoqué le seul profil portant `manage_attendance`, **When** il
   obtient un nouveau jeton, **Then** ce jeton ne porte plus `manage_attendance` et les opérations
   correspondantes sont refusées.
3. **Given** un profil existant assigné à plusieurs membres, **When** l'administrateur retire un droit
   de ce profil, **Then** à leur prochaine authentification, tous les membres concernés perdent ce
   droit sans intervention individuelle.
4. **Given** un profil actuellement attribué à au moins un membre, **When** l'administrateur tente de
   le supprimer, **Then** la suppression est refusée avec un message explicite (le profil doit
   d'abord être révoqué pour tous les membres).

---

### User Story 4 - Consulter les profils et leurs titulaires (Priority: P2)

L'administrateur des profils, ou tout membre disposant d'un droit de consultation, peut afficher la
liste des profils du bureau et, pour chaque profil, la liste des membres à qui il est attribué. Il
peut aussi voir, pour un membre donné, la liste des profils qu'il détient et les droits effectifs qui
en découlent.

**Why this priority**: Nécessaire pour piloter la gouvernance (audit interne, préparation d'une
révocation) mais pas bloquant pour l'assignation elle-même.

**Independent Test**: Après avoir créé deux profils et attribué le premier à deux membres, la
consultation du profil affiche ces deux membres ; la consultation d'un membre affiche le profil.

**Acceptance Scenarios**:

1. **Given** plusieurs profils créés et attribués, **When** un utilisateur autorisé liste les profils,
   **Then** chaque profil est retourné avec son nom, sa description, ses droits et le nombre de
   membres qui en bénéficient.
2. **Given** un membre à qui des profils sont attribués, **When** un utilisateur autorisé consulte la
   fiche de ce membre, **Then** la liste des profils et l'ensemble consolidé des droits effectifs
   sont visibles.

---

### Edge Cases

- **Suppression d'un profil non assigné** : autorisée, journalisée, définitive.
- **Suppression d'un profil assigné** : refusée (409) — l'administrateur doit d'abord révoquer les
  assignations.
- **Assignation à un membre inactif** (statut ≠ actif) : refusée. Un profil n'a de sens que pour un
  membre actif.
- **Assignation d'un profil à un membre déjà titulaire du même profil** : idempotente (pas d'erreur,
  pas de doublon).
- **Droit inexistant dans la liste d'un profil** : refusé à la création ou à la modification —
  seuls les droits connus du système sont acceptés.
- **Nom de profil dupliqué** : refusé (unicité insensible à la casse) pour éviter les collisions.
- **Effets sur les jetons en cours** : les jetons émis avant un changement restent valides jusqu'à
  leur expiration ; le changement s'applique à la **prochaine** émission de jeton (cohérence avec
  la stratégie « pas de rafraîchissement » — feature 003, FR-006).
- **Chaîne d'administration** : si le seul membre disposant du droit d'administration des profils
  perd ce droit (auto-révocation ou révocation par un pair), le système avertit et refuse tant
  qu'un autre administrateur n'est pas désigné (garantie de non-verrouillage).
- **Amorçage initial** : la configuration d'amorçage introduite par la feature 003 reste disponible
  comme **repli d'urgence** (ex. base vierge, plus aucun administrateur) mais n'est plus le mode
  d'attribution nominal. Voir FR-013 pour la migration au déploiement.
- **Migration au déploiement** : lors du premier démarrage après livraison de cette fonctionnalité,
  un profil système « Amorçage » est créé automatiquement pour préserver les droits accordés
  antérieurement via `member_permissions`. L'opération est idempotente et journalisée.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système DOIT permettre à un membre disposant du droit d'administration des profils
  de créer un profil du bureau avec un **nom** unique (insensible à la casse), une **description**
  facultative et une **liste de droits** choisis parmi les droits fonctionnels connus.
- **FR-002**: Le système DOIT permettre de **modifier** le nom, la description et la liste de droits
  d'un profil existant, sous les mêmes règles de validation qu'à la création.
- **FR-003**: Le système DOIT permettre de **supprimer** un profil **s'il n'est attribué à aucun
  membre**. Toute tentative de suppression d'un profil encore attribué DOIT être refusée avec un
  message explicite.
- **FR-004**: Le système DOIT permettre d'**attribuer plusieurs profils** simultanément à un même
  membre du bureau actif, et de **révoquer** ces attributions individuellement. Un membre peut
  détenir un nombre quelconque de profils (0 à N).
- **FR-005**: L'attribution d'un profil à un membre déjà titulaire du même profil DOIT être traitée
  comme **idempotente** (pas d'erreur, pas de duplication ; unicité stricte sur le couple
  `(membre, profil)`).
- **FR-006**: Les **droits effectifs** d'un membre DOIVENT être l'**union** des droits portés par les
  profils qui lui sont attribués, sans doublon. Aucun droit n'est accordé en dehors des profils
  attribués (le mécanisme d'attribution directe précédent, table `member_permissions`, cesse d'être
  utilisé comme source d'écriture — voir FR-013 pour la migration).
- **FR-007**: Les **jetons d'accès** émis pour un membre DOIVENT porter, comme droits (claims), ses
  droits effectifs au moment de l'émission. Les changements ultérieurs de profils/attributions
  DOIVENT prendre effet à la **prochaine émission de jeton** (les jetons déjà émis restent valides
  jusqu'à expiration).
- **FR-008**: Le système DOIT exposer la liste des **droits fonctionnels connus** (référentiel figé
  côté serveur) afin que l'interface d'administration puisse offrir un choix borné. Toute liste de
  droits non reconnus dans une création/modification DOIT être refusée.
- **FR-009**: Le système DOIT permettre la **consultation** du catalogue des profils par les
  administrateurs des profils **et** par les membres disposant de `manage_members` (lecture seule) :
  (a) la liste des profils, avec pour chacun le nom, la description, les droits et le nombre de
  titulaires ; (b) la liste des membres attributaires d'un profil donné ;
  (c) pour un membre, la liste des profils qu'il détient et l'ensemble consolidé de ses droits
  effectifs. Toute opération d'écriture (création, modification, suppression, attribution,
  révocation) reste réservée aux administrateurs des profils.
- **FR-010**: Toutes les opérations sensibles (création/modification/suppression d'un profil,
  attribution/révocation) DOIVENT être **journalisées** avec l'auteur, l'horodatage, l'action et
  l'entité concernée, sans divulguer d'information sensible (Constitution VI).
- **FR-011**: Le système DOIT introduire un **droit d'administration des profils** (référentiel des
  permissions) qui protège les opérations de cette fonctionnalité. Ce droit DOIT lui-même pouvoir
  être attribué via un profil (récursivité assumée).
- **FR-012**: Le système DOIT garantir qu'**au moins un** membre actif dispose en permanence du droit
  d'administration des profils. Il DOIT **refuser** explicitement les trois actions dangereuses
  suivantes lorsqu'elles laisseraient zéro administrateur :
  (a) la **révocation** d'une attribution de profil admin au dernier administrateur ;
  (b) le **retrait** du droit `manage_bureau_profiles` d'un profil dont la disparition de ce droit
  laisserait zéro administrateur ;
  (c) la **suppression** d'un tel profil (couverte par FR-003 si le profil est attribué, et par ce
  garde-fou même s'il est réattribuable).
- **FR-013**: **Migration au déploiement** : le système DOIT créer un profil système nommé
  « Amorçage » portant l'ensemble des droits déjà accordés directement (table `member_permissions`
  peuplée par `Auth:Bootstrap:*` — feature 003), l'assigner au **membre bootstrap** (celui référencé
  par `Auth:Bootstrap:MemberReference`), puis considérer les profils comme **source unique de
  vérité** pour les droits effectifs.
  - Si `Auth:Bootstrap:MemberReference` est **vide** ou pointe vers un membre **introuvable**, le
    profil « Amorçage » DOIT être attribué à **tous les membres présents dans `member_permissions`**
    (préservation des droits acquis, aucune perte silencieuse).
  - Si `member_permissions` est **vide**, le système NE DOIT créer **aucun** profil « Amorçage »
    (rien à préserver).
  - L'opération DOIT être **idempotente** : si le profil « Amorçage » existe déjà, aucune
    duplication n'est effectuée (ni du profil, ni des attributions).
  Le mécanisme `Auth:Bootstrap:*` reste disponible comme **repli d'urgence idempotent** au démarrage
  (base vierge / plus aucun administrateur), sans écraser les attributions faites via profils.
- **FR-014**: Le système DOIT refuser une attribution de profil à un membre dont le statut n'est pas
  actif, avec un message explicite.
- **FR-015**: Le système DOIT rejeter la création ou la modification d'un profil dont le **nom est
  vide** ou dont la longueur excède une limite raisonnable (bornes définies à l'étape de plan).
- **FR-016**: L'administration des profils n'expose **jamais** d'informations sensibles des membres
  au-delà de ce qui est déjà exposé par la fonctionnalité membres (nom, prénom, référence, statut).

### Key Entities *(include if data involved)*

- **Profil du bureau** : ensemble nommé de droits fonctionnels. Attributs clés : identifiant, nom
  unique (insensible à la casse), description facultative, liste de droits, horodatages/auteurs
  d'audit.
- **Attribution profil ↔ membre** : lien entre un membre du bureau et un profil, unique par couple
  `(membre, profil)`. Attributs d'audit : auteur et date d'attribution.
- **Droit fonctionnel (référentiel)** : identifiant textuel connu du système (ex. `manage_members`,
  `manage_attendance`, `manage_bureau_profiles`). Le référentiel est **fixé** côté serveur ; les
  profils ne peuvent référencer que des droits existants.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un administrateur peut créer un profil, l'attribuer à un membre, et voir ce membre
  bénéficier de ses nouveaux droits à sa prochaine authentification en **moins de 2 minutes**.
- **SC-002**: 100 % des tentatives d'action d'administration réalisées par un utilisateur non
  administrateur sont **refusées** et journalisées.
- **SC-003**: Aucune suppression de profil attribué n'est jamais effectuée : le taux de conservation
  des attributions vis-à-vis des suppressions demandées est **100 %** (les demandes non conformes
  sont refusées avec un message explicite).
- **SC-004**: À tout moment, le système garantit qu'**au moins un** administrateur des profils est
  actif ; aucune action ne peut aboutir à un état sans administrateur (vérifiable par tentative
  d'auto-révocation).
- **SC-005**: Les droits portés par le jeton d'un membre reflètent exactement l'union de ses profils
  actuels, sans doublon, dans **100 %** des cas de test (unitaires et intégration).
- **SC-006**: Aucune donnée sensible (mot de passe, jeton) n'apparaît dans les journaux liés à cette
  fonctionnalité — vérifiable par revue automatique.

## Assumptions

- Les seuls droits fonctionnels connus au moment de la livraison sont `manage_attendance`,
  `manage_members` (déjà exploités) et un nouveau droit `manage_bureau_profiles` introduit par
  cette fonctionnalité. Le référentiel restera figé côté serveur ; l'ajout d'un nouveau droit
  applicatif reste une évolution de code.
- Les **jetons d'accès n'étant pas rafraîchis** (feature 003, FR-006), les changements de profils
  s'appliquent à la prochaine authentification. Aucune révocation en cours de session n'est prévue
  dans cette itération.
- L'unicité de nom de profil est **insensible à la casse** ; la casse d'origine reste préservée à
  l'affichage.
- Cette fonctionnalité **n'introduit pas** d'interface d'affectation en masse (assignation d'un
  profil à N membres en une opération) ; les opérations restent unitaires.
- L'auditabilité s'appuie sur les mécanismes existants (`IAuditLogger`) déjà en place.
- Le volume attendu est modeste (dizaines de membres, poignée de profils) ; aucune contrainte
  d'échelle particulière n'est requise au-delà des standards de l'API.
