# Feature Specification: API — Récupérer mes sessions de présence ouvertes

**Feature Branch**: `023-my-open-sessions`

**Created**: 2026-07-06

**Status**: Draft

**Input**: User description: "Endpoint API en lecture seule renvoyant les sessions de présence encore
ouvertes démarrées par l'utilisateur courant, pour permettre à la console (014) de proposer de
reprendre une session en cours après une navigation accidentelle. Droit manage_attendance ; l'utilisateur
ne voit que ses propres sessions ouvertes. Aucune écriture, aucune migration."

## Contexte & motivation

Lorsqu'un responsable **démarre** une session de présence, l'identifiant de la session n'est connu que
via l'URL de l'écran d'animation. S'il **quitte cette page** (clic sur « Accueil », « Membres »…), la
console **perd** la trace de la session ouverte. En tentant d'en démarrer une nouvelle, l'API refuse (à
juste titre) car **une seule session peut être ouverte par antenne et par date de réunion** — d'où le
message « Une session ouverte existe déjà pour cette antenne à ce créneau. ».

Il manque un moyen de **retrouver** la session ouverte pour proposer sa **reprise**. L'API n'expose
aujourd'hui que des vérifications internes booléennes. Cette feature ajoute un **endpoint de lecture**
renvoyant les **sessions encore ouvertes démarrées par l'utilisateur courant**, prérequis de la
**reprise côté SPA** (feature suivante). **Aucune écriture, aucune migration** ; la règle de conflit au
démarrage et la clôture ne changent pas.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retrouver ma session ouverte (Priority: P1) 🎯 MVP

En tant que responsable de présence, je veux **récupérer la (les) session(s) que j'ai ouverte(s) et non
clôturée(s)**, afin que la console puisse me proposer de **reprendre** une session en cours plutôt que
d'échouer au redémarrage.

**Why this priority**: C'est le prérequis indispensable à la reprise ; sans lui, une navigation
accidentelle bloque durablement l'ouverture d'une nouvelle session (conflit).

**Independent Test**: Avec le droit de gestion des présences, après avoir démarré une session, appeler
l'endpoint et vérifier qu'il renvoie **cette** session (identifiant, antenne, date, heure de début,
statut ouvert, décompte) ; après clôture, elle **n'apparaît plus**.

**Acceptance Scenarios**:

1. **Given** un responsable ayant **démarré** une session (non clôturée), **When** il demande ses
   sessions ouvertes, **Then** la réponse **contient** cette session avec ses informations de reprise
   (identifiant, antenne, date de réunion, heure de début, statut ouvert, décompte de présences).
2. **Given** aucune session ouverte à son nom, **When** il demande ses sessions ouvertes, **Then** la
   réponse est une **liste vide** (aucune erreur).
3. **Given** une session qu'il a ouverte puis **clôturée**, **When** il demande ses sessions ouvertes,
   **Then** cette session **n'apparaît pas** (seules les **ouvertes** sont renvoyées).
4. **Given** une session ouverte par **un autre** membre, **When** l'utilisateur courant demande ses
   sessions ouvertes, **Then** cette session **n'apparaît pas** (uniquement **les siennes**).
5. **Given** un utilisateur **sans** le droit de gestion des présences (ou non authentifié), **When**
   il demande ses sessions ouvertes, **Then** l'accès est **refusé** (401/403).

### Edge Cases

- **Plusieurs sessions ouvertes** au nom de l'utilisateur (antennes/dates différentes) : **toutes** sont
  renvoyées (liste).
- **Aucune session** : liste vide (pas d'erreur).
- **Session clôturée automatiquement** (clôture de secours) : n'est plus « ouverte » → exclue.
- **Sans droit / sans authentification** : 401/403.
- **Lecture seule** : l'appel ne modifie rien (aucune session créée/clôturée).

## Requirements *(mandatory)*

### Récupération (US1)

- **FR-001**: Le système MUST fournir une **lecture** renvoyant la **liste** des sessions de présence
  **encore ouvertes** dont l'**initiateur** est l'**utilisateur courant** (membre porté par le jeton).
- **FR-002**: Chaque session renvoyée MUST porter les informations utiles à la **reprise** : identifiant,
  antenne, date de réunion, heure de début, **statut ouvert**, décompte de présences — via la
  **représentation de session existante**.
- **FR-003**: Seules les sessions **ouvertes** MUST être renvoyées ; les sessions **clôturées** (par
  l'utilisateur ou automatiquement) MUST être **exclues**.
- **FR-004**: L'absence de session ouverte MUST renvoyer une **liste vide** (aucune erreur).

### Accès & sécurité

- **FR-005**: L'accès MUST être réservé au droit **gestion des présences** ; une demande sans
  authentification ou sans ce droit MUST être **refusée** (401/403). L'API reste l'autorité.
- **FR-006**: Le résultat MUST être **strictement limité** aux sessions **de l'utilisateur courant** ;
  les sessions ouvertes par **d'autres** membres MUST **ne jamais** être renvoyées.
- **FR-007**: L'opération MUST être en **lecture seule** : aucun effet de bord, aucune écriture, aucune
  migration ; ni la règle de conflit au démarrage ni la clôture ne sont modifiées. Aucune donnée
  sensible superflue n'est exposée.

### Key Entities *(include if feature involves data)*

- **Session de présence (vue)** : identifiant, antenne, date de réunion, heure de début, heure de fin
  (nulle si ouverte), statut (ouverte/clôturée), initiateur, décompte de présences. Source existante
  (feature 001) ; **filtrée** ici sur *statut = ouvert* et *initiateur = utilisateur courant*.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Après avoir démarré une session, le responsable peut la **retrouver** en **une seule
  demande** (avec ses informations de reprise), sans connaître son identifiant.
- **SC-002**: **100 %** des sessions **clôturées** sont **exclues** du résultat.
- **SC-003**: **100 %** des sessions ouvertes par **d'autres** membres sont **exclues** du résultat.
- **SC-004**: **100 %** des demandes sans le droit de gestion des présences sont refusées (401/403).
- **SC-005**: L'appel **ne modifie aucune donnée** (aucune session créée/clôturée ; état inchangé).

## Assumptions

- **Données source réutilisées** (feature 001) : sessions de présence (antenne, date, statut,
  initiateur). **Aucune modification** ni migration ; le **DTO de session existant** est réutilisé.
- **Unicité** : une seule session ouverte par antenne + date (règle inchangée) ; en pratique
  l'utilisateur a 0 ou 1 session ouverte, mais une **liste** est renvoyée par robustesse.
- **Droit d'accès** : réutilise **la gestion des présences** (comme les autres opérations de session) ;
  pas de nouveau droit.
- **Identité** : l'utilisateur courant (initiateur) est déterminé par le **jeton** (identité de session),
  jamais par un paramètre client.
- **Hors périmètre** : la **reprise côté SPA** (feature suivante) ; toute modification de la règle
  empêchant deux sessions ouvertes pour une même antenne/date ; la **clôture** ; la liste des sessions
  ouvertes par **d'autres** ; l'historique des sessions clôturées.
