# Feature Specification: Console web — Installation découvrable du premier administrateur (SPA)

**Feature Branch**: `013-spa-discoverable-setup`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "Rendre la création du premier administrateur découvrable depuis la
console web : afficher un lien « Première installation » sur l'écran de connexion uniquement lorsque
l'instance n'est pas encore initialisée. Réutilise l'écran d'installation existant (feature 008) et
l'endpoint de statut (feature 012)."

## Contexte & motivation

La console web dispose déjà d'un **écran d'installation du premier administrateur** (feature 008),
mais il **n'est référencé nulle part dans l'interface** : il n'est atteignable qu'en **tapant l'URL à
la main**. Sur une **instance vierge**, la toute première personne n'a donc **aucun moyen de
découvrir** comment créer le compte super-administrateur depuis la console.

Ce lot ajoute la **découvrabilité** : sur l'écran de **connexion**, un lien discret « Première
installation » apparaît **uniquement** quand l'instance n'est **pas encore initialisée** (grâce au
statut d'installation exposé par l'API, feature 012), et **disparaît** dès qu'un administrateur
existe. L'écran d'installation lui-même et sa logique de création **ne sont pas modifiés** (réutilisés
tels quels).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Découvrir l'installation sur une instance vierge (Priority: P1) 🎯 MVP

En tant que **personne mettant en service une instance non encore initialisée**, je veux **trouver
depuis l'écran de connexion** comment créer le premier administrateur, afin d'amorcer l'usage de la
console sans connaître d'URL cachée.

**Why this priority**: C'est la raison d'être de la fonctionnalité — sans ce point d'entrée, une
instance vierge est inutilisable pour un non-initié. Livrée seule, elle rend l'amorçage autonome.

**Independent Test**: Sur une instance **vierge**, ouvrir l'écran de connexion → un lien « Première
installation » est visible ; le suivre mène à l'écran d'installation existant ; y créer le premier
administrateur aboutit à une session connectée.

**Acceptance Scenarios**:

1. **Given** une instance **non initialisée**, **When** l'utilisateur ouvre l'écran de connexion,
   **Then** un lien **« Première installation »** est **visible**.
2. **Given** ce lien, **When** l'utilisateur le suit, **Then** il arrive sur l'**écran d'installation
   du premier administrateur existant** (inchangé).
3. **Given** l'écran d'installation, **When** l'utilisateur crée le premier administrateur avec des
   informations valides, **Then** il est **connecté** à la console (comportement existant).

---

### User Story 2 - Ne pas proposer l'installation sur une instance déjà installée (Priority: P1)

En tant qu'**exploitant d'une instance déjà installée**, je veux qu'**aucun** point d'entrée
d'installation ne soit proposé dans l'interface, afin d'éviter toute confusion et toute tentative
inutile.

**Why this priority**: Indissociable d'US1 (le lien doit être **conditionnel**) ; sans elle, on
afficherait un lien trompeur/inutile sur toutes les instances installées.

**Independent Test**: Sur une instance **déjà installée**, ouvrir l'écran de connexion → **aucun**
lien « Première installation » n'est affiché.

**Acceptance Scenarios**:

1. **Given** une instance **déjà installée** (au moins un administrateur actif), **When** l'utilisateur
   ouvre l'écran de connexion, **Then** **aucun** lien/entrée « Première installation » n'est visible.
2. **Given** une tentative d'installation malgré tout (accès direct à l'URL d'installation), **When**
   l'utilisateur soumet le formulaire sur une instance déjà installée, **Then** l'opération est
   **refusée** (conflit « déjà installé ») avec un **message clair**, sans état incohérent
   (comportement existant réutilisé).

### Edge Cases

- **Statut indisponible** (erreur réseau / API injoignable au chargement de la connexion) : la
  **connexion n'est pas bloquée** ; par **prudence, le lien d'installation n'est PAS affiché**
  (défaut sûr = masqué). L'utilisateur peut toujours se connecter normalement.
- **Course avec une autre installation** : si l'instance est installée **pendant** la saisie (par un
  autre opérateur), la soumission est **refusée** (« déjà installé ») et l'utilisateur est ramené à la
  connexion avec un message adéquat — l'écran d'installation restant auto-bloqué côté serveur.
- **Accès direct à l'URL d'installation sur instance vierge** : reste possible (comportement existant),
  ce lot ne fait qu'**ajouter** le point d'entrée depuis la connexion.
- **Bascule après installation** : après création du premier administrateur, un retour ultérieur à
  l'écran de connexion ne propose **plus** le lien (l'instance est désormais installée).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Au chargement de l'écran de **connexion**, l'application MUST déterminer si l'instance
  est **déjà initialisée** (état d'installation), **sans authentification**.
- **FR-002**: L'application MUST afficher un lien **« Première installation »** sur l'écran de
  connexion **si et seulement si** l'instance **n'est pas encore initialisée**.
- **FR-003**: Lorsque l'instance est **déjà initialisée**, l'application MUST **ne proposer aucun**
  point d'entrée d'installation dans l'interface.
- **FR-004**: Le lien « Première installation » MUST mener à l'**écran d'installation existant** (non
  modifié), qui crée le premier administrateur et connecte l'utilisateur.
- **FR-005**: En cas d'**échec de détermination du statut** (erreur réseau/service), l'application MUST
  **ne pas afficher** le lien (**défaut sûr = masqué**) et MUST **ne pas empêcher** la connexion
  normale.
- **FR-006**: L'écran d'installation MUST rester **accessible directement par son URL** (comportement
  existant inchangé) ; ce lot n'**ajoute** qu'un point d'entrée conditionnel depuis la connexion.
- **FR-007**: Si l'installation est tentée alors que l'instance est **déjà installée**, l'application
  MUST restituer un **refus clair** (« déjà installé ») et ramener à la connexion, **sans** état
  incohérent (réutilise le comportement serveur existant).
- **FR-008**: Le parcours MUST être **anonyme** (avant toute session) ; **aucune** donnée sensible
  n'est affichée ou conservée. L'API reste l'autorité (le verrou d'installation protège de toute
  façon).
- **FR-009**: Les écrans MUST être en **français** et **responsive** (poste de bureau et tablette).

### Key Entities *(include if feature involves data)*

- **État d'installation (lecture, consommé)** : un indicateur **installé / non installé** fourni par
  l'API (feature 012), utilisé **uniquement** pour décider de l'affichage du lien. Aucune donnée
  persistée côté client.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Sur une instance **vierge**, un utilisateur trouve, **depuis l'écran de connexion et sans
  connaître d'URL**, comment créer le premier administrateur (lien visible) et **y parvient** de bout
  en bout.
- **SC-002**: Sur une instance **déjà installée**, **aucun** lien/entrée d'installation n'est proposé
  dans l'interface (**100 %** des cas).
- **SC-003**: En cas de **statut indisponible**, la connexion reste **pleinement utilisable** et le
  lien d'installation **n'apparaît pas** (défaut sûr).
- **SC-004**: **Aucune** donnée sensible n'est affichée ou conservée dans ce parcours anonyme ; le
  verrou d'installation reste **effectif** (une installation sur instance déjà amorcée est refusée).

## Assumptions

- **Réutilisation** : l'**écran d'installation** et l'endpoint de **création** du premier administrateur
  (feature 008 / `POST setup/first-admin`) sont **réutilisés tels quels** ; le **statut d'installation**
  (feature 012 / `GET setup/status`) est **déjà disponible**. L'API **n'est pas modifiée** par ce lot.
- **Consultation du statut** : effectuée **à l'ouverture de l'écran de connexion**, de façon anonyme ;
  un seul appel suffit à décider de l'affichage.
- **Défaut sûr** : en cas d'incertitude (statut indisponible), le lien est **masqué** — on préfère ne
  pas proposer l'installation plutôt que de l'afficher à tort.
- **Hors périmètre** : la création elle-même et le formulaire d'installation (feature 008) ; l'endpoint
  de statut (feature 012) ; toute détection/bascule automatique « premier démarrage » plus poussée
  (on se limite au lien conditionnel) ; gestion des membres/profils/présences (autres lots).
