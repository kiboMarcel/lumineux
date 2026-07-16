# Feature Specification: Profession du membre

**Feature Branch**: `030-member-profession`

**Created**: 2026-07-13

**Status**: Draft

**Input**: User description: "Ajouter la profession d'un membre. Le vocal de modélisation demande de connaître la profession de chaque membre. Aujourd'hui l'entité Member ne porte aucun champ profession. Besoin : un champ profession optionnel, texte libre borné, saisissable et modifiable via la console web (création + correction), renvoyé dans la fiche membre et l'API membres. Purement additif."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Renseigner la profession à la création d'un membre (Priority: P1)

Un membre du bureau enregistre une nouvelle personne dans la communauté et souhaite,
en plus de l'identité déjà saisie, noter sa profession (ex. « Enseignant »,
« Commerçante », « Sans emploi »). La profession est un renseignement facultatif :
le bureau peut créer le membre sans la renseigner et la compléter plus tard.

**Why this priority**: C'est le besoin exprimé dans le vocal de modélisation
(« connaître la profession de chaque membre ») et le moment naturel de collecte de
l'information. Sans cette étape, la donnée n'existe jamais dans le système.

**Independent Test**: Créer un membre en renseignant une profession, puis rouvrir sa
fiche et vérifier que la profession saisie s'y affiche. Créer un autre membre sans
profession et vérifier que la création réussit et que le champ reste vide.

**Acceptance Scenarios**:

1. **Given** le formulaire de création de membre, **When** le bureau saisit une
   profession valide et enregistre, **Then** le membre est créé et sa fiche affiche
   la profession renseignée.
2. **Given** le formulaire de création de membre, **When** le bureau laisse la
   profession vide et enregistre, **Then** le membre est créé sans profession (champ
   vide sur la fiche).
3. **Given** une profession saisie avec des espaces superflus en début/fin, **When**
   le membre est enregistré, **Then** la valeur stockée est nettoyée (espaces de bord
   supprimés).
4. **Given** une profession dépassant la longueur maximale autorisée, **When** le
   bureau tente d'enregistrer, **Then** l'enregistrement est refusé avec un message
   clair indiquant la longueur maximale.

---

### User Story 2 - Corriger ou compléter la profession d'un membre existant (Priority: P2)

Un membre du bureau ouvre la fiche d'un membre déjà enregistré pour ajouter la
profession absente, la modifier (changement de métier) ou l'effacer (saisie erronée).

**Why this priority**: La donnée existante doit pouvoir être complétée après coup
(les membres déjà en base n'ont pas de profession) et corrigée au fil de la vie du
membre. Dépend de l'existence du champ (US1) mais apporte une valeur distincte.

**Independent Test**: Sur un membre sans profession, en ajouter une via la correction
et vérifier qu'elle est persistée ; puis la remplacer par une autre valeur ; puis la
vider et vérifier que le champ redevient vide.

**Acceptance Scenarios**:

1. **Given** la fiche de correction d'un membre sans profession, **When** le bureau
   saisit une profession et enregistre, **Then** la fiche affiche désormais cette
   profession.
2. **Given** un membre avec une profession renseignée, **When** le bureau la remplace
   par une autre valeur et enregistre, **Then** la nouvelle valeur remplace
   l'ancienne.
3. **Given** un membre avec une profession renseignée, **When** le bureau efface le
   champ et enregistre, **Then** la profession est supprimée (champ vide).

---

### User Story 3 - Consulter la profession dans la fiche membre (Priority: P3)

Un membre du bureau consulte la fiche d'un membre et y voit sa profession parmi les
informations d'identité, ou une indication claire d'absence si elle n'est pas
renseignée.

**Why this priority**: La lecture est le débouché de la donnée, mais elle n'a de sens
qu'une fois la saisie (US1/US2) en place. Valeur réelle mais moindre en isolation.

**Independent Test**: Ouvrir la fiche d'un membre ayant une profession et vérifier
son affichage ; ouvrir la fiche d'un membre sans profession et vérifier l'absence
d'affichage trompeur (champ vide ou mention « non renseignée »).

**Acceptance Scenarios**:

1. **Given** un membre avec profession, **When** le bureau ouvre sa fiche, **Then** la
   profession est affichée avec les autres informations d'identité.
2. **Given** un membre sans profession, **When** le bureau ouvre sa fiche, **Then**
   l'absence de profession est présentée sans valeur fictive.

---

### Edge Cases

- **Espaces uniquement** : une saisie composée uniquement d'espaces est traitée comme
  une absence de profession (stockée vide, pas une chaîne d'espaces).
- **Longueur maximale** : une saisie à la limite exacte est acceptée ; au-delà, elle
  est refusée avec un message indiquant la limite.
- **Caractères spéciaux / accents** : les intitulés de métiers avec accents, traits
  d'union ou apostrophes (« Aide-soignant », « Chargé d'affaires ») sont acceptés tels
  quels après nettoyage des bords.
- **Membres préexistants** : tous les membres créés avant cette fonctionnalité ont une
  profession vide ; aucune migration de données ne leur invente de valeur.
- **Contenu potentiellement malveillant** : une saisie contenant des fragments de code
  ou de balisage est stockée telle quelle en tant que texte (aucune interprétation) et
  restituée de façon sûre par les clients ; elle reste bornée en longueur.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système MUST permettre d'associer à un membre une profession
  facultative, sous forme de texte libre.
- **FR-002**: Le système MUST accepter la création d'un membre sans profession (champ
  optionnel), sans changer les autres règles de création existantes.
- **FR-003**: Le système MUST permettre de renseigner la profession lors de la
  création d'un membre.
- **FR-004**: Le système MUST permettre d'ajouter, modifier ou effacer la profession
  lors de la correction d'un membre existant.
- **FR-005**: Le système MUST nettoyer la valeur saisie en retirant les espaces de
  début et de fin avant stockage.
- **FR-006**: Le système MUST enregistrer une profession vide ou composée uniquement
  d'espaces comme une absence de profession (valeur non renseignée).
- **FR-007**: Le système MUST rejeter une profession dépassant la longueur maximale
  autorisée, avec un message d'erreur clair mentionnant cette limite.
- **FR-008**: Le système MUST restituer la profession du membre dans la fiche membre
  et dans les données membre exposées aux clients.
- **FR-009**: Le système MUST valider et borner la profession côté serveur,
  indépendamment de toute validation côté client.
- **FR-010**: Le système MUST NOT imposer de contrainte d'unicité sur la profession
  (plusieurs membres peuvent partager la même profession).
- **FR-011**: Le système MUST préserver l'intégralité du comportement existant des
  membres, des présences et des référentiels (ajout strictement additif, aucune
  donnée existante altérée).

### Key Entities *(include if feature involves data)*

- **Membre** : personne enregistrée dans la communauté. Reçoit un attribut
  **profession** optionnel (texte libre borné en longueur). Cet attribut n'entre dans
  aucune relation, n'est pas unique, et n'a aucun impact sur les autres attributs
  d'identité, les rattachements (antenne, quartier) ou les présences.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un membre du bureau peut renseigner la profession lors de la création
  d'un membre sans étape supplémentaire par rapport au flux actuel (même formulaire,
  un champ de plus).
- **SC-002**: Pour 100 % des membres, la profession peut être renseignée, modifiée ou
  effacée après création, et la valeur consultée reflète toujours la dernière
  saisie enregistrée.
- **SC-003**: Une saisie hors limite de longueur est refusée dans 100 % des cas avec
  un message compréhensible, sans enregistrement partiel.
- **SC-004**: 100 % des membres existants avant la fonctionnalité restent consultables
  et modifiables sans erreur, avec une profession vide tant qu'elle n'est pas
  renseignée.

## Assumptions

- **Texte libre plutôt que référentiel** : la profession est modélisée comme un champ
  texte libre borné (cohérent avec le champ adresse existant), et non comme un
  référentiel fermé de métiers. Ce choix est retenu par défaut ; il pourra être
  reconsidéré à l'étape de clarification si un référentiel de professions est souhaité
  (impacterait la saisie, la normalisation et les statistiques ultérieures).
- **Longueur maximale** : une borne de longueur raisonnable pour un intitulé de métier
  est appliquée, alignée sur les autres champs texte du membre ; la valeur exacte est
  un détail d'implémentation arrêté au plan.
- **Portée client** : seule la console web (module Membres : création et correction)
  est concernée pour la saisie. L'affichage suit là où la fiche membre est déjà
  présentée. L'application mobile membre n'est pas dans le périmètre de cette
  fonctionnalité.
- **Pas d'historisation** : seule la dernière valeur de profession est conservée ;
  aucun historique des changements de profession n'est demandé.
- **Réutilisation de l'existant** : les flux de création et de correction de membre,
  ainsi que le contrat de données membre, sont étendus, pas remplacés.
