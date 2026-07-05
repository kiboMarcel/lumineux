# Feature Specification: Console web — Présences (SPA, Lot 4)

**Feature Branch**: `014-spa-attendance`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "Lot 4 du SPA : module Présences côté bureau — animer une session
(démarrer, QR rotatif, suivi en temps réel, ajout/retrait manuel, clôture), en consommant l'API
existante."

## Contexte & motivation

La console web couvre déjà l'authentification, les membres et la gouvernance des droits. Ce lot livre
le **dernier module métier du bureau** : l'**animation d'une session de présence**. Un utilisateur
disposant du droit **gestion des présences** peut **démarrer** une session, **projeter un code QR
rotatif** que les membres scannent (depuis leur application mobile — hors périmètre web), **suivre les
présences en temps réel**, **ajouter/retirer** manuellement une présence, et **clôturer** la session.

Le module s'appuie sur les endpoints existants (sessions et présences) et sur le **référentiel des
antennes** (feature 010). Le **scan** par le membre et la **synchronisation hors ligne** relèvent d'un
**client mobile distinct** (hors périmètre). L'API **n'est pas modifiée** (sauf éventuellement une
petite recherche allégée, selon la décision de la dépendance §Requirements/FR-013).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Démarrer une session et projeter le QR rotatif (Priority: P1) 🎯 MVP

En tant que responsable de présence, je veux démarrer une session pour une antenne et une date, puis
**projeter un code QR** qui se **renouvelle automatiquement**, afin que les membres présents le
scannent pour enregistrer leur arrivée.

**Why this priority**: C'est le cœur de l'animation d'une réunion ; sans session ouverte et sans QR
projetable, aucune présence ne peut être collectée. Livrée seule, elle permet déjà de tenir une
séance.

**Independent Test**: Se connecter avec le droit de gestion des présences, démarrer une session
(antenne + date), et vérifier qu'un **code QR** s'affiche **en grand** et se **rafraîchit
automatiquement** avant expiration (le contenu change au rythme de rotation).

**Acceptance Scenarios**:

1. **Given** un utilisateur avec le droit de gestion des présences, **When** il démarre une session en
   choisissant une **antenne** (dans une liste) et une **date de réunion**, **Then** la session est
   créée et son écran d'animation s'affiche.
2. **Given** une session ouverte, **When** l'écran d'animation est affiché, **Then** un **code QR**
   lisible **en grand** (pour projection) est présenté.
3. **Given** le code QR affiché, **When** le pas de rotation s'écoule, **Then** le QR est
   **automatiquement renouvelé** (nouveau contenu) **avant** son expiration, sans action de
   l'utilisateur.
4. **Given** l'écran d'animation, **When** on inspecte l'interface, **Then** le **secret** du QR n'est
   **jamais** affiché en clair (uniquement l'image) ni conservé.

---

### User Story 2 - Suivre les présences en temps réel (Priority: P1)

En tant que responsable de présence, je veux voir la liste des présences se remplir et le **décompte**
des présents, afin de suivre l'affluence pendant la séance.

**Why this priority**: Indissociable de l'animation ; donne au bureau la visibilité sur la collecte
en cours.

**Independent Test**: Sur une session ouverte, afficher la liste des présences ; vérifier qu'elle se
**met à jour périodiquement** (nouvelles arrivées visibles) et affiche le **décompte des présences
valides** ; filtrer par statut.

**Acceptance Scenarios**:

1. **Given** une session ouverte, **When** l'écran d'animation est affiché, **Then** la **liste des
   présences** (membre, heure d'arrivée, source, statut) et le **décompte des présences valides**
   sont affichés.
2. **Given** de nouvelles présences enregistrées (par scan), **When** l'utilisateur reste sur l'écran,
   **Then** la liste et le décompte se **mettent à jour automatiquement** (rafraîchissement
   périodique).
3. **Given** la liste, **When** l'utilisateur choisit un **filtre de statut** (valides / annulées /
   toutes), **Then** l'affichage est restreint en conséquence.

---

### User Story 3 - Ajouter et retirer une présence manuellement (Priority: P2)

En tant que responsable de présence, je veux ajouter la présence d'un membre **non équipé** (sans
téléphone) et pouvoir **annuler** une présence erronée tant que la session est ouverte, afin de
corriger la collecte.

**Why this priority**: Complète la collecte pour les cas non couverts par le scan ; important mais
secondaire par rapport à l'ouverture/suivi.

**Independent Test**: Sur une session ouverte, ajouter manuellement une présence pour un membre
identifié → elle apparaît dans la liste ; réajouter le même membre ne crée pas de doublon
(idempotent) ; annuler une présence (avec confirmation) → elle passe au statut annulé.

**Acceptance Scenarios**:

1. **Given** une session ouverte, **When** l'utilisateur ajoute manuellement la présence d'un membre
   identifié, **Then** la présence est enregistrée (source « manuel ») et apparaît dans la liste ; un
   **réajout** du même membre **ne crée pas de doublon** (idempotent).
2. **Given** une présence existante, **When** l'utilisateur demande son **annulation**, **Then** une
   **confirmation** est requise ; à la confirmation, la présence passe au statut **annulé**.
3. **Given** une session **clôturée**, **When** l'utilisateur tente un ajout/une annulation, **Then**
   l'opération est **refusée** (« session close ») avec un message clair.

---

### User Story 4 - Clôturer la session (Priority: P2)

En tant que responsable de présence, je veux clôturer la session en fin de séance, afin de figer les
présences et l'heure de fin.

**Why this priority**: Termine proprement le cycle ; sans clôture la séance reste ouverte.

**Independent Test**: Sur une session ouverte, demander la clôture (avec confirmation) ; vérifier que
la session passe au statut clôturé, qu'une heure de fin est renseignée, et que l'ajout/annulation
n'est plus possible.

**Acceptance Scenarios**:

1. **Given** une session ouverte, **When** l'utilisateur demande la **clôture**, **Then** une
   **confirmation** est requise ; à la confirmation, la session est **clôturée** (heure de fin
   renseignée) et le QR n'est plus proposé.
2. **Given** une session clôturée, **When** l'écran s'affiche, **Then** les actions d'ajout,
   d'annulation et de projection du QR ne sont **plus** proposées.

### Edge Cases

- **Sans droit de gestion des présences** : l'entrée « Présences » n'est pas visible ; l'accès direct
  à une URL du module est refusé (403 géré).
- **Session déjà close** : toute opération d'écriture (ajout, annulation, clôture) est refusée (409)
  avec un message explicite, sans état incohérent.
- **QR indisponible momentanément** (erreur réseau lors du rafraîchissement) : l'écran signale un état
  transitoire et **réessaie**, sans planter ; la session reste ouverte.
- **Aucune antenne active** : le démarrage d'une session est **empêché** avec un message explicite (le
  choix d'antenne provient du référentiel).
- **Rafraîchissement de page pendant une session** : l'écran d'animation peut être rechargé à partir de
  l'identifiant de session (l'état de session est côté serveur) ; le QR reprend son cycle.
- **Membre déjà présent** (ajout manuel) : aucun doublon (idempotent), la présence existante est
  conservée.

## Requirements *(mandatory)*

### Consultation & animation (US1/US2)

- **FR-001**: L'entrée de navigation « Présences » et l'accès au module MUST être réservés aux
  utilisateurs disposant du droit de **gestion des présences** ; l'API reste l'autorité (403 géré).
- **FR-002**: L'utilisateur MUST pouvoir **démarrer une session** en sélectionnant une **antenne**
  (issue du référentiel des antennes) et une **date de réunion**, avec un **pas de rotation du QR**
  optionnel ; si **aucune antenne active** n'est disponible, le démarrage MUST être **empêché** avec
  un message explicite.
- **FR-003**: L'écran d'animation MUST afficher un **code QR** dérivé du jeton courant, **lisible en
  grand** (projection).
- **FR-004**: Le code QR MUST être **automatiquement renouvelé** **avant expiration** (au rythme du pas
  de rotation), sans action de l'utilisateur.
- **FR-005**: Le **secret** du QR MUST NE **jamais** être affiché en clair ni **persisté** ; seule
  l'image du code est présentée.
- **FR-006**: L'écran d'animation MUST afficher la **liste des présences** (membre, heure d'arrivée,
  source, statut) et le **décompte des présences valides**.
- **FR-007**: La liste et le décompte MUST se **rafraîchir périodiquement** (les nouvelles présences
  apparaissent sans action manuelle).
- **FR-008**: L'utilisateur MUST pouvoir **filtrer** les présences par statut (valides / annulées /
  toutes).

### Corrections & clôture (US3/US4)

- **FR-009**: L'utilisateur MUST pouvoir **ajouter manuellement** une présence pour un membre
  identifié ; l'opération MUST être **idempotente** (réajout sans doublon).
- **FR-010**: L'utilisateur MUST pouvoir **annuler** une présence tant que la session est ouverte,
  **après confirmation**.
- **FR-011**: L'utilisateur MUST pouvoir **clôturer** la session **après confirmation** ; après
  clôture, l'ajout, l'annulation et la projection du QR ne MUST plus être proposés.
- **FR-012**: Toute opération d'écriture sur une **session close** MUST être **refusée** avec un
  message clair (« session close »), sans état incohérent.

### Dépendances de conception

- **FR-013**: L'**ajout manuel** identifie le membre via une **recherche allégée** (par référence et/ou
  nom → membre) **accessible au droit de gestion des présences**. Cette recherche est fournie par un
  **endpoint API préalable dédié** (cf. Assumptions — décision 2026-07-05) afin qu'un opérateur de
  présence **n'ait pas besoin** du droit de gestion des membres. L'ajout manuel MUST proposer un
  **sélecteur** de membre s'appuyant sur cette recherche.
- **FR-014**: Le module MUST **générer et afficher** le code QR **côté client** à partir du jeton (l'API
  ne fournit qu'un jeton, jamais d'image), au moyen d'une **bibliothèque cliente de génération de QR**
  (décision 2026-07-05). Le jeton alimente uniquement le rendu de l'image (FR-005).

### Transverses

- **FR-015**: L'application MUST **mapper** les erreurs de l'API en messages exploitables : validation
  (400), introuvable (404), conflit/ session close (409), non autorisé (403), sans détail technique.
- **FR-016**: Les **actions sensibles** (annulation d'une présence, clôture de session) MUST demander
  une **confirmation** explicite.
- **FR-017**: Les écrans MUST être en **français** et **responsive** (poste de bureau et tablette ; le
  QR projeté doit être **lisible en grand**).

### Key Entities *(include if feature involves data)*

- **Session de présence (vue)** : identifiant, antenne, date de réunion, heure de début, heure de fin
  (si close), statut (ouverte/close), décompte de présences.
- **Jeton QR (éphémère, consommé)** : jeton courant + pas de rotation + expiration ; sert **uniquement**
  à générer l'image du QR ; jamais affiché en clair ni persisté.
- **Présence (vue)** : identifiant, membre (identifiant + nom affiché), heure d'arrivée, source
  (scan / manuel), statut (valide / annulée). Aucune donnée sensible.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un responsable de présence démarre une session et projette un QR **rotatif** en **moins
  de 1 minute** ; le QR se renouvelle automatiquement (contenu différent) au moins une fois avant son
  expiration.
- **SC-002**: La liste des présences et le décompte se mettent à jour **automatiquement** pendant la
  séance (nouvelles arrivées visibles sans action manuelle).
- **SC-003**: Un ajout manuel du **même** membre **n'échoue jamais** et **ne crée pas de doublon**
  (idempotence).
- **SC-004**: **100 %** des opérations d'écriture sur une session **close** sont refusées avec un
  message clair (jamais d'état incohérent).
- **SC-005**: **Aucun** secret (jeton QR) n'est observable en clair (stockage, URL, console) ; seule
  l'image du QR est présentée.
- **SC-006**: Dans **100 %** des cas, l'entrée « Présences » et les actions du module sont **absentes**
  pour un utilisateur sans le droit correspondant.
- **SC-007**: Chaque action sensible (annulation, clôture) requiert une **confirmation** avant
  exécution.

## Assumptions

- **Socle & référentiels réutilisés** : session, intercepteurs, gardes, RBAC d'affichage (feature 008) ;
  liste des antennes via le référentiel (feature 010) ; droits via `/auth/me`. L'API des présences
  (feature 001) **n'est pas modifiée**.
- **Prérequis API — recherche membre allégée (décidé le 2026-07-05)** : une **petite feature API
  préalable** expose une recherche de membre allégée (référence et/ou nom → identifiant + nom),
  **accessible au droit de gestion des présences**, pour alimenter le sélecteur de l'**ajout manuel**
  (US3, FR-013). **US1/US2/US4** (démarrer, QR, suivi, clôture) sont **indépendantes** de ce prérequis ;
  **US3** en dépend et sera livrée après.
- **Génération du QR (décidé le 2026-07-05)** : image du code QR **générée côté client** via une
  **bibliothèque cliente** (dépendance front — installation à approuver au moment de l'implémentation).
- **Temps réel = rafraîchissement périodique** (polling) : l'API n'expose pas de flux temps réel ; la
  liste/le décompte et le QR sont **ré-interrogés** à intervalle régulier.
- **QR rotatif** : le jeton change au rythme du **pas de rotation** fourni par l'API ; le client
  ré-interroge le jeton et **regénère** l'image avant expiration.
- **Scan & synchronisation hors ligne** : **hors périmètre** (client mobile membre distinct).
- **Hors périmètre** : gestion des membres (Lot 2), profils/droits (Lot 3), statistiques/rapports de
  présence, internationalisation au-delà du français.
