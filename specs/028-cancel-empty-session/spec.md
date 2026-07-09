# Feature Specification: Annulation d'une session de présence vide

**Feature Branch**: `028-cancel-empty-session`

**Created**: 2026-07-09

**Status**: Draft

**Input**: User description : « Permettre d'annuler une session de présence en cours (statut ouvert)
uniquement à la condition qu'aucun membre n'y ait déjà été ajouté (aucune présence enregistrée). Si au
moins une présence existe, l'annulation doit être refusée avec un message clair. Contexte : module
Présences (feature 014, API attendance-sessions). Côté bureau (droit manage_attendance). »

## User Scenarios & Testing *(mandatory)*

Le bureau démarre parfois une session de présence **par erreur** (mauvaise antenne, mauvaise date, doublon,
test) avant qu'aucun membre n'ait pointé. Aujourd'hui, une session ouverte ne peut être que **clôturée** —
ce qui laisse une trace de séance « fantôme » sans présence. Ce lot ajoute la possibilité d'**annuler** une
session ouverte **tant qu'elle est vide**, pour corriger l'erreur proprement, sans jamais permettre
d'effacer une séance qui a déjà commencé à collecter des présences.

### User Story 1 - Annuler une session ouverte créée par erreur (Priority: P1)

En tant que **membre du bureau** ayant ouvert (ou pouvant gérer) une session **sans aucune présence**, je
veux **annuler** cette session, afin de corriger une erreur de démarrage sans laisser de séance vide dans
l'historique.

**Why this priority** : c'est le cœur du besoin — offrir une correction sûre d'une erreur fréquente
(session ouverte à tort). Une session vide annulable proprement est à elle seule un incrément démontrable.

**Independent Test** : démarrer une session, ne scanner/ajouter **aucun** membre, déclencher l'annulation
(avec confirmation) → la session **disparaît des sessions actives** et n'apparaît **pas** dans les listes ni
les rapports ; la reprise (« session en cours ») ne la propose plus.

**Acceptance Scenarios** :

1. **Given** une session **ouverte** sans aucune présence, **When** le bureau demande l'annulation et
   **confirme**, **Then** la session est **annulée** (retirée des sessions actives) et une confirmation
   claire s'affiche.
2. **Given** une session que le bureau vient d'annuler, **When** il consulte la liste des sessions ou la
   reprise de session en cours, **Then** la session annulée **n'apparaît plus** comme active.
3. **Given** l'écran de suivi d'une session ouverte vide, **When** le bureau ouvre l'action d'annulation,
   **Then** une **confirmation explicite** est demandée avant toute annulation (action irréversible).

---

### User Story 2 - Empêcher l'annulation d'une session non vide (Priority: P1)

En tant que **membre du bureau**, je veux que l'annulation soit **refusée** dès qu'au moins une présence a
été enregistrée, afin de ne **jamais** perdre des présences déjà collectées.

**Why this priority** : garde-fou indissociable de US1. Sans cette règle, l'annulation deviendrait
destructrice ; les deux histoires forment le MVP minimal sûr.

**Independent Test** : sur une session ayant **au moins une** présence (scan ou ajout manuel), tenter
l'annulation → elle est **refusée** avec un **message clair** expliquant la raison ; la session **reste
ouverte** et intacte.

**Acceptance Scenarios** :

1. **Given** une session ouverte avec **≥ 1 présence** (peu importe la source : scan QR ou ajout manuel),
   **When** le bureau tente l'annulation, **Then** l'action est **refusée** avec un message indiquant que la
   session contient des présences et ne peut pas être annulée.
2. **Given** une tentative d'annulation refusée, **When** le refus est traité, **Then** la session **reste
   ouverte** et **aucune** présence n'est modifiée ni supprimée.
3. **Given** une session dont on souhaite se débarrasser alors qu'elle **contient** des présences, **When**
   le bureau consulte les options, **Then** l'annulation n'est **pas** proposée comme moyen d'effacer les
   présences (le chemin normal reste la **clôture**).

---

### Edge Cases

- **Course entre ajout et annulation** : une présence est enregistrée **juste avant** que l'annulation ne
  soit finalisée → la condition « vide » est **re-vérifiée au moment de l'annulation** ; si une présence est
  apparue, l'annulation est **refusée** (aucune présence perdue).
- **Session déjà clôturée** : l'annulation ne s'applique qu'aux sessions **ouvertes** ; une session
  **clôturée** ne peut pas être annulée (message clair).
- **Session déjà annulée** (double annulation, ex. deux onglets) : la seconde tentative est **sans effet
  destructeur** et signale que la session n'est plus active.
- **Session close automatiquement** entre-temps (clôture de secours) → l'annulation est refusée (n'est plus
  ouverte).
- **Présence ajoutée puis annulée** : une session dont l'unique présence a été **annulée** (décompte valide
  = 0) est de nouveau **annulable** ; l'annulation de la session est autorisée.
- **Droit manquant** : une tentative sans le droit de gestion des présences est **refusée** et **consignée**.
- **Synchronisation hors ligne tardive** (lien avec la capture mobile) : un scan hors ligne **synchronisé
  après** l'annulation vise une session qui n'est plus active → il est **rejeté** côté serveur (session
  introuvable/close), comme tout scan sur une séance non ouverte.

## Clarifications

### Session 2026-07-09

- Q: Une session annulée est-elle supprimée physiquement ou conservée pour l'audit ? → A: **Statut terminal « annulée » conservé** (soft) — la session reste enregistrée avec auteur/horodatage d'annulation à des fins d'audit (Principe VI), mais est **exclue** de toutes les vues actives, listes et rapports.
- Q: Qui peut annuler une session vide — l'auteur seul ou tout détenteur du droit ? → A: **Tout détenteur `manage_attendance`** (même autorité que la clôture), pas seulement l'auteur de l'ouverture.
- Q: « Vide » = 0 présence valide (annulée ne compte pas) ou jamais aucune présence ? → A: **0 présence valide** — une présence annulée individuellement ne compte pas ; une session dont la seule présence a été annulée redevient **annulable**.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001** : Le système MUST permettre au bureau (droit **manage_attendance**) d'**annuler** une session
  de présence dont le statut est **ouvert**.
- **FR-002** : L'annulation MUST être autorisée **uniquement** si la session ne comporte **aucune présence
  valide** — c'est-à-dire un décompte de présences valides égal à **0** (toutes sources confondues : scan QR
  ou ajout manuel). Une présence **annulée** individuellement **ne compte pas** : une session dont la seule
  présence a été annulée redevient **annulable**.
- **FR-003** : Si la session comporte **au moins une** présence, l'annulation MUST être **refusée** et le
  système MUST renvoyer un **message clair** expliquant que la session n'est pas vide.
- **FR-004** : La condition « aucune présence » MUST être **vérifiée au moment de l'annulation** (et non
  seulement à l'affichage), afin d'éviter toute perte due à une présence ajoutée entre-temps.
- **FR-005** : Une session **clôturée** (ou non ouverte) MUST **ne pas** pouvoir être annulée ; la tentative
  est refusée avec un message clair.
- **FR-006** : Une session **annulée** MUST prendre un **statut terminal « annulée »** (conservé, non
  supprimé physiquement) et MUST être **exclue** des sessions actives, de la reprise « session en cours »,
  des listes de sessions et des rapports — comme si la séance n'avait jamais eu lieu côté vues, tout en
  restant **traçable** pour l'audit (auteur, horodatage d'annulation).
- **FR-007** : L'annulation MUST être **confirmée explicitement** par l'utilisateur avant exécution (action
  irréversible), avec un libellé distinguant clairement **annuler** (supprimer une séance vide) de
  **clôturer** (terminer une séance avec présences).
- **FR-008** : L'annulation d'une session ne MUST **jamais** supprimer, modifier ou masquer des présences ;
  en présence de présences, seule la **clôture** reste possible.
- **FR-009** : Toute annulation MUST être **tracée** (qui, quand, quelle session) ; les **refus métier**
  (session non ouverte, session non vide) MUST être **consignés** par le service. La consignation d'un
  **refus pour droit manquant** (403) relève du **mécanisme d'audit d'autorisation global** du projet
  (pipeline), conformément au Principe VI — pas d'un traitement spécifique à cette feature.
- **FR-010** : L'annulation MUST être **idempotente/sûre** vis-à-vis des tentatives répétées : ré-annuler une
  session déjà annulée ne MUST pas produire d'effet destructeur et MUST être signalé clairement.

### Key Entities *(include if feature involves data)*

- **Session de présence** : séance ouverte par le bureau. Possède un **statut** (ouverte / clôturée /
  **annulée**), l'auteur et l'horodatage d'ouverture, et — le cas échéant — l'auteur/horodatage de clôture
  ou d'annulation. Une session **annulée** est un état terminal distinct de « clôturée » : elle n'a **aucune
  présence** et est exclue des vues actives, listes et rapports.
- **Présence** : enregistrement du passage d'un membre à une session (via scan QR ou ajout manuel), pouvant
  être **annulée** individuellement. Le **nombre de présences valides** d'une session (les annulées exclues)
  détermine si elle est **annulable** (0) ou non (≥ 1).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : **100 %** des sessions ouvertes **sans présence** peuvent être annulées par un utilisateur
  autorisé, avec confirmation, en **moins de 5 secondes**.
- **SC-002** : **100 %** des tentatives d'annulation sur une session **contenant au moins une présence
  valide** sont **refusées**, **sans** perte ni modification d'aucune présence.
- **SC-003** : **Aucune** présence n'est perdue en cas d'ajout **concurrent** au moment de l'annulation (la
  re-vérification empêche toute annulation d'une session devenue non vide).
- **SC-004** : Une session **annulée** n'apparaît dans **aucune** vue active (reprise, listes, rapports) —
  taux de fuite **0 %**.
- **SC-005** : **100 %** des annulations et des refus pour droit manquant sont **traçables** (auteur,
  horodatage, session) dans le journal d'audit.

## Assumptions

- **Capacité nouvelle** : l'annulation de session est **distincte** de la clôture (existante) et de
  l'annulation d'une **présence** individuelle (existante). Elle introduit un **état terminal « annulée »**
  pour la session.
- **Autorité** : tout détenteur du droit **manage_attendance** peut annuler une session vide (même autorité
  que la clôture), pas uniquement l'auteur de l'ouverture (décision verrouillée, cf. Clarifications).
- **Définition de « vide »** : une session est vide si son **nombre de présences valides** est **0**, toutes
  sources confondues (scan et ajout manuel). Le serveur reste **l'autorité** sur ce décompte.
- **Traçabilité** : l'annulation est **conservée** pour l'audit (auteur/horodatage) via un **statut terminal
  « annulée »** (décision verrouillée, cf. Clarifications) — **pas** de suppression physique ; la session
  annulée reste **exclue** de toutes les vues actives (FR-006).
- **Cohérence hors ligne** : un scan hors ligne synchronisé après annulation est traité comme un scan sur
  une séance non ouverte (rejeté), sans traitement spécial nouveau.
- **Périmètre client** : l'action est exposée côté **console bureau** (module Présences) ; l'app mobile
  membre n'annule pas de session.

## Out of Scope

- L'annulation d'une session **contenant** des présences (interdite ; la **clôture** reste le chemin normal).
- La **suppression** ou l'édition de présences individuelles (déjà couvert par l'annulation de présence
  existante).
- La **restauration** d'une session annulée (pas de « corbeille » / undo dans ce lot).
- Toute modification du **scan** (mobile) ou de la **synchronisation hors ligne**.
- Les statistiques/rapports sur les sessions annulées (elles sont simplement exclues).
