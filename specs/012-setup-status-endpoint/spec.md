# Feature Specification: Statut d'installation (setup/status)

**Feature Branch**: `012-setup-status-endpoint`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "Ajouter un endpoint anonyme GET /api/v1/setup/status indiquant si
l'instance est déjà initialisée (au moins un administrateur actif). Prérequis de la découvrabilité de
l'installation du premier administrateur côté SPA."

## Contexte & motivation

L'installation du **premier administrateur** (feature 005) est protégée par un **verrou naturel** :
l'opération est refusée dès qu'un **administrateur actif** existe déjà. Côté console web, on souhaite
n'**exposer le lien d'installation que lorsque l'instance n'est pas encore initialisée** — mais rien,
aujourd'hui, ne permet à un client de **savoir**, **avant toute connexion**, si l'installation est
encore possible.

Cette fonctionnalité ajoute une **lecture publique et minimale** de l'**état d'installation** : un
simple indicateur « installé / non installé », aligné **exactement** sur le verrou de l'installation.
Elle sert de **prérequis** à la découvrabilité de l'installation côté SPA (afficher/masquer le lien
« Première installation »).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Savoir si l'instance est installée (avant connexion) (Priority: P1) 🎯 MVP

En tant que **client (console web) au tout premier démarrage**, je veux savoir si l'instance dispose
déjà d'un administrateur, afin de proposer l'installation **seulement** quand elle est encore
nécessaire, **sans** exiger d'authentification.

**Why this priority**: C'est la seule raison d'être de la fonctionnalité et le prérequis de la
découvrabilité de l'installation. Sans elle, le client ne peut pas décider d'afficher le lien.

**Independent Test**: Sur une instance **vierge**, demander le statut → « non installé ». Après
l'installation du premier administrateur, redemander → « installé ». Vérifier que l'accès **ne requiert
aucune authentification** et que la réponse **ne contient qu'un indicateur** (aucune autre donnée).

**Acceptance Scenarios**:

1. **Given** une instance **sans administrateur actif**, **When** on demande le statut d'installation
   (sans authentification), **Then** la réponse indique **non installé**.
2. **Given** une instance **avec au moins un administrateur actif**, **When** on demande le statut,
   **Then** la réponse indique **installé**.
3. **Given** une instance vierge, **When** le premier administrateur vient d'être installé, **Then**
   un nouvel appel au statut bascule de **non installé** à **installé** (cohérence avec le verrou).
4. **Given** n'importe quel appelant, **When** il consulte le statut, **Then** la réponse ne contient
   **qu'un indicateur booléen** — **aucune** information sur des comptes, membres, coordonnées ou
   identifiants.

### Edge Cases

- **Aucune session / anonyme** : le statut est **toujours** consultable sans authentification (c'est
  son objet). Il ne renvoie jamais 401/403.
- **Course avec l'installation** : si l'instance est installée entre la consultation du statut et une
  tentative d'installation, l'installation reste **refusée** par son verrou (conflit « déjà
  installé ») — le statut n'affaiblit pas cette protection.
- **Définition d'« administrateur actif »** : identique au verrou d'installation — un **membre actif**
  titulaire du **droit d'administration des profils**. Le statut compte les mêmes administrateurs
  actifs que le verrou.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système MUST exposer une **lecture publique** de l'**état d'installation** de
  l'instance, **accessible sans authentification**.
- **FR-002**: L'état MUST valoir **« installé »** si et seulement s'il existe **au moins un
  administrateur actif** (membre actif titulaire du droit d'administration des profils), selon la
  **même règle** que le verrou d'installation du premier administrateur (feature 005).
- **FR-003**: La réponse MUST se limiter à un **indicateur booléen** d'installation ; elle MUST NE
  **jamais** contenir de donnée sensible ni permettant l'énumération (comptes, membres, e-mails,
  identifiants, comptages détaillés).
- **FR-004**: La lecture MUST être **sans effet de bord** (aucune écriture, aucune donnée persistée
  nouvelle) et **répétable** (même état pour un même état d'instance).
- **FR-005**: Cette lecture MUST NE **pas** modifier ni affaiblir le **verrou d'installation** : la
  création du premier administrateur reste refusée dès qu'un administrateur actif existe.

### Key Entities *(include if feature involves data)*

- **Statut d'installation (lecture seule)** : un unique indicateur **installé / non installé**,
  **dérivé** du décompte des administrateurs actifs existants. Aucune nouvelle donnée persistée.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Le statut est obtenu en **un seul appel**, **sans authentification**, avant toute
  session.
- **SC-002**: Le statut reflète **exactement** le verrou d'installation : **non installé** tant
  qu'aucun administrateur actif n'existe, **installé** dès qu'il en existe au moins un (**100 %** de
  concordance avec le comportement du verrou).
- **SC-003**: La réponse ne contient **qu'un indicateur** ; **aucune** donnée sensible ou
  d'énumération n'est observable, quel que soit l'état de l'instance.
- **SC-004**: L'existence de cette lecture **ne permet pas** de créer un administrateur sur une
  instance déjà installée (le verrou reste effectif — refus « déjà installé »).

## Assumptions

- **Règle partagée avec le verrou** : « installé » = au moins un administrateur actif, réutilisant le
  **même décompte** que l'installation du premier administrateur (feature 005) pour éviter toute
  divergence.
- **Accès anonyme** : cohérent avec l'installation du premier administrateur, elle-même anonyme ; le
  statut sert précisément à décider, **avant session**, si l'installation est proposée.
- **Aucune persistance nouvelle** : lecture pure dérivée de l'existant ; aucune table, aucune
  migration.
- **Consommateur** : la console web (SPA) pour afficher/masquer le lien « Première installation »
  (feature SPA distincte à suivre).
- **Hors périmètre** : la création elle-même (feature 005) ; l'écran/lien côté SPA ; tout autre
  statut système (santé, version, métriques).
