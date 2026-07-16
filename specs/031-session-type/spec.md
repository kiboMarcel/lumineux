# Feature Specification: Type de session de présence

**Feature Branch**: `031-session-type`

**Created**: 2026-07-16

**Status**: Draft

**Input**: User description: "Ajouter un discriminant session_type sur AttendanceSession, valeur par défaut AntennaMeeting appliquée à tout l'existant, pour préparer un type Teaching sans rien casser. Périmètre strictement limité au discriminant (pas de domaine cours). Additif, type fixé à la création, exposé dans les DTO de session, validé côté serveur contre un ensemble fermé."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Toute session porte un type, par défaut « réunion d'antenne » (Priority: P1)

Le système attache à chaque session de présence une **nature** (type). Les sessions
existantes et toute nouvelle session sans type explicite sont des **réunions d'antenne**
(comportement actuel inchangé). Le type est visible dans les données de session consultées
par le bureau (suivi en cours, reprise de session, etc.).

**Why this priority**: C'est la fondation demandée — sans un discriminant présent et rempli
par défaut sur tout l'existant, il est impossible de distinguer plus tard une séance
d'enseignement d'une réunion. Cette étape n'ajoute aucune obligation de saisie et ne modifie
aucun comportement : c'est le socle non cassant.

**Independent Test**: consulter une session existante (créée avant la fonctionnalité) et une
session nouvellement démarrée sans préciser de type : les deux exposent le type
« réunion d'antenne ».

**Acceptance Scenarios**:

1. **Given** une session démarrée sans préciser de type, **When** on consulte ses données,
   **Then** son type est « réunion d'antenne ».
2. **Given** une session créée avant l'introduction de la fonctionnalité, **When** on la
   consulte, **Then** son type est « réunion d'antenne » (aucune session sans type).
3. **Given** une session de présence quelconque, **When** on consulte ses données, **Then** le
   type de session fait partie des informations restituées.

---

### User Story 2 - Démarrer une session en précisant sa nature (Priority: P2)

Le système permet de démarrer une session en indiquant explicitement sa nature parmi un
ensemble fermé de types reconnus (aujourd'hui : réunion d'antenne, enseignement). Une valeur
inconnue est refusée. Le type choisi est **fixé à la création** et ne change plus ensuite.

**Why this priority**: prépare concrètement l'arrivée des séances d'enseignement en rendant le
type sélectionnable et fiable (valeurs contrôlées), tout en restant sans effet fonctionnel
distinct pour l'instant. Dépend de US1 (le champ doit exister) mais apporte la capacité de
qualifier une séance.

**Independent Test**: démarrer une session en précisant le type « enseignement » et vérifier
qu'elle le conserve ; tenter de démarrer avec un type non reconnu et vérifier le refus.

**Acceptance Scenarios**:

1. **Given** un démarrage de session avec le type « enseignement », **When** la session est
   créée, **Then** ses données exposent le type « enseignement ».
2. **Given** un démarrage de session avec un type non reconnu, **When** la demande est soumise,
   **Then** elle est refusée avec un message clair, sans création de session.
3. **Given** une session déjà démarrée, **When** on effectue une opération ultérieure (suivi,
   clôture, annulation), **Then** son type reste celui fixé à la création (non modifiable).

---

### Edge Cases

- **Valeur inconnue / vide** : un type non reconnu (ou une casse/valeur arbitraire) est rejeté
  côté serveur ; l'absence de type au démarrage retombe sur « réunion d'antenne ».
- **Sessions préexistantes** : toutes reçoivent « réunion d'antenne » sans intervention et sans
  valeur inventée d'un autre type.
- **Aucun effet de bord fonctionnel** : quel que soit le type, le code QR rotatif, le pointage,
  l'ajout manuel, la clôture, l'annulation, la clôture automatique de secours, les rapports et le
  scan mobile se comportent exactement comme aujourd'hui.
- **Type « enseignement » sans domaine associé** : accepté structurellement, mais ne déclenche
  aujourd'hui aucune règle métier particulière (pas de cours/chapitre rattaché).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Chaque session de présence MUST porter un type appartenant à un ensemble fermé de
  valeurs reconnues.
- **FR-002**: Le système MUST attribuer le type « réunion d'antenne » par défaut à toute session
  démarrée sans type explicite.
- **FR-003**: Le système MUST garantir que toutes les sessions existantes portent le type
  « réunion d'antenne » après introduction de la fonctionnalité (aucune session sans type).
- **FR-004**: Le système MUST permettre de démarrer une session en précisant sa nature parmi les
  types reconnus (aujourd'hui : réunion d'antenne, enseignement).
- **FR-005**: Le système MUST refuser, côté serveur, tout type de session non reconnu, avec un
  message d'erreur clair et sans créer de session.
- **FR-006**: Le type d'une session MUST être fixé à sa création et MUST NOT être modifiable
  ensuite.
- **FR-007**: Le système MUST exposer le type de session dans les données de session restituées
  aux clients.
- **FR-008**: Le système MUST préserver à l'identique tout le comportement existant des sessions
  (code QR rotatif, pointage, ajout manuel, clôture, annulation, clôture automatique, rapports,
  scan mobile), quel que soit le type — ajout strictement additif.
- **FR-009**: Le type « enseignement » MUST être accepté comme valeur valide sans déclencher, à
  ce stade, aucune règle métier distincte (pure préparation).

### Key Entities *(include if feature involves data)*

- **Session de présence** : reçoit un attribut **type** (nature) issu d'un ensemble fermé de
  valeurs. Cet attribut est obligatoire (valeur par défaut « réunion d'antenne »), fixé à la
  création, sans relation vers d'autres entités, sans effet sur les autres attributs (antenne,
  dates, statut, code QR) ni sur les présences rattachées.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100 % des sessions (préexistantes et nouvelles) exposent un type ; aucune session
  sans type après la mise en place.
- **SC-002**: Une session démarrée sans préciser de type est « réunion d'antenne » dans 100 % des
  cas (comportement actuel inchangé).
- **SC-003**: Une demande de démarrage avec un type non reconnu est refusée dans 100 % des cas,
  avec message clair et sans création.
- **SC-004**: Aucune régression fonctionnelle sur les parcours de session existants (démarrage,
  suivi, pointage, clôture, annulation, rapports, scan) : les jeux de vérification existants
  restent tous valides.

## Assumptions

- **Ensemble fermé de deux types** : « réunion d'antenne » (défaut) et « enseignement ». D'autres
  types pourront être ajoutés ultérieurement ; l'ensemble est volontairement minimal.
- **Portée strictement discriminant** : aucun domaine « cours/chapitres », aucune association du
  type à d'autres données, aucune logique métier conditionnelle au type ne sont dans le périmètre.
  Ce sont des évolutions ultérieures distinctes.
- **Livraison côté service d'abord (API-only)** : la sélection du type au démarrage est disponible
  côté service ; l'exposition de ce choix dans l'interface web de démarrage de session est
  **différée** et sera branchée avec le futur domaine des enseignements. Par défaut, le démarrage
  depuis l'interface actuelle continue de produire des « réunions d'antenne ».
- **Type immuable** : une fois la session créée, son type ne change pas (cohérent avec le fait que
  la nature d'une séance est décidée à l'ouverture).
- **Réutilisation de l'existant** : le flux de démarrage de session et le contrat de données de
  session sont étendus, pas remplacés ; la validation du type suit la même approche que les autres
  valeurs contraintes de session (ensemble fermé validé côté serveur).
