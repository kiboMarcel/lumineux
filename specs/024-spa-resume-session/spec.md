# Feature Specification: Console web — Reprendre une session de présence en cours

**Feature Branch**: `024-spa-resume-session`

**Created**: 2026-07-06

**Status**: Draft

**Input**: User description: "Reprise d'une session de présence en cours sur l'écran de démarrage (014),
consommant l'API mes sessions ouvertes (023). Encart « Vous avez une session en cours » + bouton
Reprendre ; gestion du conflit 409 au démarrage en proposant la reprise. Droit manage_attendance."

## Contexte & motivation

Aujourd'hui, quand un responsable **démarre** une session de présence puis **navigue ailleurs**
(« Accueil », « Membres »…), la console **perd** l'accès à la session ouverte (son identifiant ne vivait
que dans l'URL). En revenant sur l'écran de démarrage et en tentant d'ouvrir une nouvelle session, l'API
refuse (à juste titre) : **« Une session ouverte existe déjà pour cette antenne à ce créneau. »**. Le
responsable est alors **bloqué**.

L'API expose désormais (feature 023) la **liste des sessions encore ouvertes de l'utilisateur courant**.
Cette feature ajoute à l'écran de **démarrage** (feature 014) la possibilité de **reprendre** une session
en cours : un **encart** listant la (les) session(s) ouverte(s) avec un bouton **« Reprendre »**, et une
**gestion du conflit** au démarrage proposant explicitement la reprise plutôt qu'un simple message
d'erreur. Réservé au droit **`manage_attendance`**. **L'API n'est pas modifiée.**

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reprendre ma session en cours depuis l'écran de démarrage (Priority: P1) 🎯 MVP

En tant que responsable de présence, je veux **retrouver et reprendre** une session que j'ai laissée
ouverte, afin de continuer l'animation après avoir changé de page par mégarde.

**Why this priority**: C'est le cœur du correctif ; sans la reprise, une navigation accidentelle bloque
durablement l'ouverture d'une nouvelle session.

**Independent Test**: Démarrer une session, naviguer ailleurs, revenir sur l'écran de démarrage :
vérifier qu'un **encart** « Vous avez une session en cours » présente la session (antenne, date, heure
de début) avec un bouton **« Reprendre »** qui ouvre son écran d'animation.

**Acceptance Scenarios**:

1. **Given** un responsable ayant une session **ouverte** (démarrée puis quittée), **When** il ouvre
   l'écran de démarrage, **Then** un **encart** « Vous avez une session en cours » liste cette session
   avec son **libellé d'antenne**, sa **date** et son **heure de début**, et un bouton **« Reprendre »**.
2. **Given** l'encart affiché, **When** le responsable clique **« Reprendre »** sur une session, **Then**
   il est dirigé vers l'**écran d'animation** de **cette** session.
3. **Given** **aucune** session ouverte à son nom, **When** il ouvre l'écran de démarrage, **Then**
   **aucun encart** n'est affiché (seul le formulaire de démarrage est présent).
4. **Given** l'encart affiché, **When** le responsable préfère ouvrir une **nouvelle** session (autre
   antenne/date), **Then** le **formulaire de démarrage** reste disponible sous l'encart.

---

### User Story 2 - Proposer la reprise en cas de conflit au démarrage (Priority: P2)

En tant que responsable de présence, quand je tente de démarrer une session sur une antenne/date déjà
occupée par une session ouverte, je veux qu'on me propose de **reprendre** cette session plutôt qu'un
simple message d'erreur.

**Why this priority**: Complète le correctif pour le cas où l'utilisateur (re)tente un démarrage ;
important mais secondaire par rapport à l'encart proactif.

**Independent Test**: Tenter de démarrer une session pour une antenne/date ayant déjà une session
ouverte : vérifier qu'au lieu d'un simple message de conflit, un bouton **« Reprendre la session en
cours »** est proposé et ouvre l'écran d'animation de la session correspondante.

**Acceptance Scenarios**:

1. **Given** une session déjà ouverte pour une antenne/date, **When** le responsable tente d'en démarrer
   une nouvelle pour la **même** antenne/date, **Then** le **conflit** est présenté avec une action
   explicite **« Reprendre la session en cours »** (et non un simple message d'erreur).
2. **Given** le conflit et l'action de reprise, **When** le responsable clique **« Reprendre la session
   en cours »**, **Then** il est dirigé vers l'**écran d'animation** de la session ouverte correspondante
   (antenne + date choisies).
3. **Given** un démarrage réussi (aucun conflit), **When** la session est créée, **Then** le comportement
   **inchangé** s'applique (navigation vers l'écran d'animation de la nouvelle session).

### Edge Cases

- **Sans droit `manage_attendance`** : module Présences déjà masqué / accès refusé (403 géré) — l'écran
  de démarrage n'est pas atteignable.
- **Vérification en cours** : pendant la récupération des sessions ouvertes, un état de **chargement**
  est indiqué sans bloquer le formulaire de démarrage.
- **Échec de la récupération** (erreur réseau) : message clair ; le formulaire de démarrage **reste
  utilisable** (l'absence d'encart ne doit pas empêcher de travailler).
- **Plusieurs sessions ouvertes** (antennes/dates différentes) : **toutes** sont listées dans l'encart,
  chacune avec son bouton « Reprendre ».
- **Conflit 409 sans session correspondante retrouvée** (cas limite, ex. session ouverte par un autre
  membre) : afficher le message de conflit clair, sans action de reprise trompeuse.
- **Session expirée (401)** : purge et retour à la connexion (socle).

## Requirements *(mandatory)*

### Reprise proactive (US1)

- **FR-001**: L'écran de démarrage MUST, à son **chargement**, **récupérer** les sessions de présence
  **ouvertes** de l'utilisateur courant (via l'API 023).
- **FR-002**: S'il existe au moins une session ouverte, l'écran MUST afficher un **encart** « Vous avez
  une session en cours » listant **chaque** session avec son **libellé d'antenne**, sa **date de réunion**
  et son **heure de début**, et un bouton **« Reprendre »** par session.
- **FR-003**: Le bouton **« Reprendre »** MUST diriger vers l'**écran d'animation** (`/attendance/sessions/:id`)
  de la session choisie.
- **FR-004**: En **l'absence** de session ouverte, **aucun encart** ne MUST être affiché ; le formulaire
  de démarrage reste présent et utilisable.
- **FR-005**: Le formulaire de **démarrage d'une nouvelle session** MUST rester disponible **en plus** de
  l'encart de reprise.

### Reprise sur conflit (US2)

- **FR-006**: Si le **démarrage** échoue avec le **conflit** « session déjà ouverte pour cette
  antenne/date » (409), l'écran MUST proposer une action explicite **« Reprendre la session en cours »**
  au lieu d'un simple message d'erreur, en **retrouvant** la session ouverte correspondant à l'antenne +
  date choisies (via l'API 023).
- **FR-007**: Si aucune session correspondante n'est retrouvée pour ce conflit (cas limite), l'écran MUST
  afficher le **message de conflit** clair **sans** action de reprise trompeuse.

### Présentation & transverses

- **FR-008**: Les **libellés d'antenne** MUST être affichés (pas seulement des identifiants), en
  réutilisant le référentiel des antennes.
- **FR-009**: L'écran MUST gérer les états **chargement** (vérification), **absence** de session, et
  **erreur** mappée en message clair, sans empêcher le démarrage d'une nouvelle session.
- **FR-010**: Aucun **calcul ni règle métier** ne MUST être dupliqué côté client : la liste des sessions
  ouvertes provient de l'API 023 ; le client ne fait que **présenter** et **naviguer**. L'écran MUST
  être en **français** et **responsive** ; aucun secret ne MUST être journalisé.

### Key Entities *(include if feature involves data)*

- **Session ouverte (vue)** : identifiant, antenne (identifiant + libellé), date de réunion, heure de
  début, statut (ouverte). Fournie par l'API 023 ; réservée aux sessions **de l'utilisateur courant**.
- **Antenne (référentiel, lecture)** : identifiant + libellé, pour l'affichage lisible (feature 010).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Après une navigation accidentelle, un responsable **retrouve et reprend** sa session en
  cours en **moins de 20 secondes** depuis l'écran de démarrage (encart + « Reprendre »).
- **SC-002**: Dans **100 %** des cas où une session ouverte de l'utilisateur existe, l'encart de reprise
  est affiché avec les informations utiles (antenne, date, heure de début).
- **SC-003**: Dans **100 %** des cas de **conflit** au démarrage correspondant à une session ouverte de
  l'utilisateur, une action **« Reprendre la session en cours »** est proposée (au lieu d'un simple
  message d'erreur).
- **SC-004**: Un utilisateur **sans** session ouverte ne voit **aucun** encart de reprise (formulaire de
  démarrage seul) dans **100 %** des cas.
- **SC-005**: Le module (et donc la reprise) est **inaccessible** à un utilisateur sans le droit
  `manage_attendance` (module déjà gardé) dans **100 %** des cas.
- **SC-006**: Un échec de la vérification des sessions ouvertes n'**empêche pas** de démarrer une
  nouvelle session (formulaire toujours utilisable).

## Assumptions

- **Socle & module réutilisés** (features 008/014) : session, intercepteurs, gardes RBAC, mapping
  d'erreurs, notifications ; écran de démarrage (session-start) et d'animation (session-run) existants ;
  route `/attendance` déjà gardée `manage_attendance`. **API 023 inchangée.**
- **Référentiel des antennes** (feature 010) déjà chargé par l'écran de démarrage pour les libellés.
- **API mes sessions ouvertes** (feature 023) : `GET .../mine/open` renvoie **uniquement** les sessions
  ouvertes de l'utilisateur courant ; le client ne filtre pas par membre (l'API l'a déjà fait).
- **Conflit** : le message de conflit au démarrage reste inchangé côté API ; la reprise sur conflit
  s'appuie sur la correspondance **antenne + date** avec une session ouverte de l'utilisateur.
- **Rechargement de page** : hors périmètre — le socle garde le jeton en mémoire ; après un rechargement
  complet, l'utilisateur se reconnecte puis récupère de nouveau ses sessions ouvertes via cet écran.
- **Hors périmètre** : toute modification d'API ; la reprise des sessions ouvertes par **d'autres**
  membres ; la **clôture** ; toute modification de la règle « une session ouverte par antenne/date ».
