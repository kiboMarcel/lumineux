# Feature Specification : Ajout d'un nouveau membre

**Feature Branch**: `002-member-registration`

**Created**: 2026-07-02

**Status**: Draft

**Input**: User description: "Je voudrais maintenant implémenter l'ajout d'un nouveau membre. C'est le bureau qui s'occupe d'ajouter un nouveau membre ; un compte est alors créé pour ce membre avec les informations basiques. Il pourra se connecter avec les identifiants, puis mettre son nouveau mot de passe et faire les mises à jour nécessaires sur les champs sur lesquels il a droit."

## Clarifications

### Session 2026-07-03

- Q: Quel identifiant le membre utilisera-t-il pour se connecter ? → A: La **référence membre** (unique, auto-générée) sert d'identifiant de connexion.
- Q: Règle d'unicité des coordonnées de contact ? → A: **Refuser** la création si l'e-mail ou le mobile est déjà utilisé par un membre **actif** (les membres archivés ne bloquent pas).
- Q: Statut initial d'un membre nouvellement créé ? → A: **Actif immédiatement** (pointable dès la création) ; l'activation du compte (1re connexion) est un état distinct porté par le compte, pas par le statut du membre.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Création d'un nouveau membre par le bureau (Priority: P1)

Un membre du bureau disposant du droit de gestion des membres saisit les informations de base d'une
nouvelle personne rejoignant la communauté (identité, sexe, coordonnées, antenne d'origine, etc.).
Le système enregistre le membre, lui attribue une référence unique et une date d'entrée, et
provisionne automatiquement un compte permettant à ce membre de se connecter ultérieurement.

**Why this priority**: C'est le cœur de la fonctionnalité et le point d'entrée de tout le cycle de
vie d'un membre (présence, parrainage, etc.). Sans elle, aucun membre ne peut exister dans le système.

**Independent Test**: Peut être testé en faisant créer un membre par un utilisateur autorisé avec des
informations valides, et en vérifiant que le membre est enregistré (référence unique, date d'entrée,
statut initial) et qu'un compte connectable a été provisionné.

**Acceptance Scenarios**:

1. **Given** un membre du bureau autorisé, **When** il crée un membre avec les informations
   obligatoires valides, **Then** le membre est enregistré avec une référence unique, une date
   d'entrée, un statut initial, et un compte est provisionné.
2. **Given** un utilisateur sans le droit de gestion des membres, **When** il tente de créer un
   membre, **Then** l'action est refusée avec un message explicite.
3. **Given** des informations obligatoires manquantes ou invalides (ex. coordonnée de contact
   absente), **When** la création est soumise, **Then** le système refuse et indique les champs à
   corriger, sans créer de membre ni de compte.

---

### User Story 2 - Prévention et gestion des doublons (Priority: P2)

Lors de la création, le système vérifie qu'il ne crée pas un membre en double. Si une personne
portant les mêmes nom et prénom existe déjà, le bureau est averti afin de confirmer qu'il s'agit
bien d'une personne distincte ou d'annuler.

**Why this priority**: Évite la pollution des données et les comptes en double, essentiels à la
fiabilité des présences et des statistiques ; dépend de la création (US1).

**Independent Test**: Peut être testé en tentant de créer deux membres de même identité et en
vérifiant que le second déclenche l'avertissement/le refus attendu selon la règle retenue.

**Acceptance Scenarios**:

1. **Given** un membre déjà enregistré, **When** le bureau tente de créer une personne avec les
   mêmes nom et prénom, **Then** le système signale le doublon potentiel avant enregistrement.
2. **Given** un doublon potentiel signalé, **When** le bureau confirme qu'il s'agit d'une personne
   distincte (selon la règle retenue), **Then** la création aboutit ; sinon elle est annulée.

---

### User Story 3 - Consultation et correction d'un membre par le bureau (Priority: P2)

Après création, le bureau peut rechercher et consulter la fiche d'un membre, et corriger les
informations saisies (ex. faute de frappe, coordonnée erronée) tant qu'il en a le droit.

**Why this priority**: Indispensable pour vérifier la création et corriger les erreurs de saisie,
mais secondaire par rapport à la création elle-même.

**Independent Test**: Peut être testé en recherchant un membre existant, en modifiant un champ
autorisé et en vérifiant que la modification est persistée et tracée.

**Acceptance Scenarios**:

1. **Given** un membre existant, **When** le bureau recherche par nom/référence, **Then** la fiche
   correspondante est affichée.
2. **Given** la fiche d'un membre, **When** le bureau corrige un champ autorisé, **Then** la
   modification est enregistrée et tracée (auteur, horodatage).

---

### Edge Cases

- **Coordonnée de contact déjà utilisée** : une adresse e-mail (ou un mobile) déjà rattaché à un
  autre membre → comportement à définir (refus/avertissement selon la règle d'unicité des contacts).
- **Références géographiques/nomenclatures inconnues** : antenne, nationalité, civilité ou ville
  fournies ne correspondant à aucune valeur connue → refus avec message clair.
- **Champs optionnels absents** : la création reste possible avec le minimum requis ; le membre
  complétera son profil ultérieurement.
- **Introducteur/parrain inexistant** : si un introducteur est renseigné mais introuvable → refus.
- **Membre inactif/supprimé portant la même identité** : la règle de doublon doit tenir compte du
  statut (un homonyme archivé ne doit pas bloquer indéfiniment).
- **Échec de provisionnement du compte** : si le compte ne peut être créé, le membre ne doit pas
  rester dans un état incohérent (création atomique membre + compte).

## Requirements *(mandatory)*

### Functional Requirements

**Création du membre**

- **FR-001**: Le système DOIT permettre à un membre du bureau disposant du droit de gestion des
  membres de créer un nouveau membre.
- **FR-002**: Le système DOIT restreindre la création de membre aux utilisateurs disposant de ce
  droit et refuser toute tentative non autorisée.
- **FR-003**: Le système DOIT exiger un ensemble minimal d'informations obligatoires à la création
  et valider chaque champ (format, cohérence). **Champs obligatoires** : nom, prénom, sexe, au moins
  une coordonnée de contact (mobile ou e-mail) et l'antenne d'origine. Les autres champs (civilité,
  date/lieu de naissance, nationalité, adresse/quartier, introducteur) sont **optionnels** à la
  création et peuvent être complétés ultérieurement par le membre ou le bureau.
- **FR-004**: Le système DOIT attribuer automatiquement à chaque nouveau membre une **référence
  unique** et une **date d'entrée**, et définir son **statut initial à « actif »** (le membre est
  immédiatement pointable, y compris avant sa première connexion).
- **FR-005**: Le système DOIT valider que les valeurs de référence fournies (antenne, nationalité,
  civilité, ville, introducteur…) existent, et refuser la création sinon.
- **FR-006**: La création du membre et le provisionnement de son compte DOIVENT être atomiques :
  en cas d'échec de l'un, aucun des deux n'est enregistré.

**Gestion des doublons**

- **FR-007**: Le système DOIT détecter les doublons potentiels sur l'identité (nom + prénom) avant
  enregistrement et en **avertir** le bureau. Le bureau DOIT pouvoir **confirmer explicitement**
  qu'il s'agit d'une personne distincte pour poursuivre la création, ou l'annuler. Sans confirmation,
  la création d'un homonyme n'aboutit pas.
- **FR-008**: Le système DOIT **refuser** la création lorsqu'une coordonnée de contact (e-mail ou
  mobile) est déjà utilisée par un **membre actif**, avec un message explicite. Les coordonnées
  rattachées à des membres archivés ne bloquent pas la création.

**Provisionnement du compte**

- **FR-009**: Le système DOIT créer, à la création du membre, un compte permettant à ce membre de se
  connecter, dont **l'identifiant de connexion est la référence membre** (unique, auto-générée),
  assorti d'un secret initial.
- **FR-010**: Le système DOIT générer des identifiants initiaux de manière sécurisée et exiger le
  changement du mot de passe à la première connexion. L'**état d'activation du compte** (compte non
  encore activé / mot de passe à changer) est porté par le compte et **distinct du statut du membre**
  (qui est « actif » dès la création).
- **FR-011**: Le système DOIT transmettre au nouveau membre ses identifiants initiaux **par e-mail
  d'invitation** (lien d'activation / mot de passe temporaire) lorsqu'une adresse e-mail est
  renseignée ; **à défaut d'e-mail**, le système DOIT présenter les identifiants initiaux au membre
  du bureau afin qu'il les remette au nouveau membre (repli). Le mot de passe temporaire ne DOIT
  jamais être journalisé ni renvoyé en dehors de ce canal.
- **FR-012**: Le compte provisionné DOIT disposer uniquement des droits d'un membre standard (pas de
  droits du bureau), selon le principe du moindre privilège.

**Consultation et correction**

- **FR-013**: Le système DOIT permettre au bureau autorisé de rechercher un membre (par nom, prénom
  ou référence) et de consulter sa fiche.
- **FR-014**: Le système DOIT permettre au bureau autorisé de corriger les informations d'un membre
  et enregistrer chaque modification avec son auteur et son horodatage (piste d'audit).

**Traçabilité et sécurité**

- **FR-015**: Le système DOIT tracer la création et les modifications d'un membre (auteur,
  horodatage) et consigner les tentatives refusées (droit manquant, doublon, validation).
- **FR-016**: Le système DOIT protéger les données personnelles des membres (exposition minimale) et
  ne DOIT jamais journaliser ni persister en clair un mot de passe. L'empreinte du mot de passe
  (hash) n'est jamais exposée. **Seule exception encadrée** : en repli remise-bureau (FR-011), le mot
  de passe temporaire est renvoyé **une unique fois** dans la réponse de création, pour transmission
  par le bureau.

### Key Entities *(include if data involved)*

- **Membre** : personne de la communauté. Attributs clés : référence unique, date d'entrée, nom,
  prénom, sexe, coordonnées (mobile, e-mail, adresse), rattachements géographiques (antenne
  d'origine, quartier/district, ville/lieu de naissance), nationalité, civilité, introducteur
  éventuel, statut, informations d'audit. (Réutilise/complète l'entité Membre existante.)
- **Compte de connexion** : moyen d'authentification rattaché à un membre. Attributs clés :
  identifiant de connexion (**= référence membre**), secret initial (non exposé), indicateur
  « changement de mot de passe requis », **état d'activation du compte** (distinct du statut du
  membre), rattachement au membre.
- **Nomenclatures et références** *(existantes)* : Antenne, Nationalité (Pays), Civilité, Ville,
  District/Quartier, Membre introducteur — réutilisées et validées à la création.
- **Bureau / Profil de droits** *(existant ou dépendance)* : détermine qui peut créer/corriger un
  membre (droit de gestion des membres).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un membre du bureau peut créer un nouveau membre complet en moins de 3 minutes.
- **SC-002**: 100 % des membres créés possèdent une référence unique et un compte connectable
  (aucun membre sans compte, aucun doublon de référence) dans les conditions de test.
- **SC-003**: Les tentatives de création avec des données obligatoires manquantes ou des références
  inconnues sont refusées dans 100 % des cas, sans création partielle.
- **SC-004**: Les doublons d'identité sont signalés avant enregistrement dans 100 % des cas testés.
- **SC-005**: Aucun mot de passe initial n'apparaît en clair dans les **journaux**, ni n'est
  **persisté** en clair (stockage haché uniquement). Le mot de passe temporaire n'apparaît **que**
  dans la **réponse unique de création en repli remise-bureau** (FR-011), et jamais ailleurs (vérifié
  par revue et tests).
- **SC-006**: 95 % des nouveaux membres réussissent leur première connexion et leur changement de
  mot de passe sans assistance (mesuré une fois le parcours de connexion disponible).

## Assumptions

- **Périmètre confirmé** : cette fonctionnalité couvre la **création du membre et le provisionnement
  du compte** (identifiant + secret initial + indicateur « mot de passe à changer »). Le parcours de
  **première connexion et de changement de mot de passe** est **hors périmètre** et relève de la
  fonctionnalité d'authentification (dépendance).
- **Modèle de droits** : un droit « gestion des membres » assignable au bureau existe (ou sera
  fourni en dépendance), distinct du droit de gestion des présences.
- **Nomenclatures existantes** : les entités de référence (antennes, pays/nationalités, civilités,
  villes, districts) existent et sont réutilisées ; leur gestion (CRUD) relève d'autres
  fonctionnalités.
- **Antenne d'origine** : le champ d'antenne d'origine du membre (introduit précédemment) est
  renseigné à la création.
- **Statut initial** : un membre nouvellement créé est « actif » dès la création (confirmé) ;
  l'activation du compte (première connexion) est un état distinct porté par le compte.
- **Références uniques** : la génération de la référence membre suit un format défini en
  configuration et garantit l'unicité.
- **Conservation des données** : les membres ne sont pas supprimés physiquement mais archivés
  (changement de statut), afin de préserver l'historique (présences, parrainages).
