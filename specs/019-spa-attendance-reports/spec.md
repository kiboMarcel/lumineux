# Feature Specification: Console web — Tableau de bord des rapports de présence (SPA)

**Feature Branch**: `019-spa-attendance-reports`

**Created**: 2026-07-06

**Status**: Draft

**Input**: User description: "Tableau de bord SPA des rapports de présence consommant l'API 018 :
synthèse par antenne/période (tableau + barres CSS/SVG), export CSV, taux d'assiduité par membre (via
recherche allégée 015). Réservé au droit manage_attendance. Visualisation légère sans dépendance."

## Contexte & motivation

L'API de rapports de présence (feature 018) calcule déjà les agrégats : **synthèse par antenne sur une
période**, **export CSV** et **taux d'assiduité par membre**. Il manque la **brique front** pour que le
bureau **consulte** ces statistiques depuis la console web (`web/`), sans requêtes techniques.

Ce module ajoute un **tableau de bord « Rapports »** à la SPA Angular, réservé au droit **gestion des
présences** (`manage_attendance`). Il **présente** les chiffres fournis par l'API (aucun calcul
statistique côté client) avec une **visualisation légère** (barres/jauges CSS/SVG, **sans bibliothèque
de graphiques**). Il réutilise le **socle** (feature 008), la **recherche membre allégée** (feature
015) et la **lecture des antennes** (feature 010). **L'API n'est pas modifiée.**

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Synthèse d'affluence par antenne et période (Priority: P1) 🎯 MVP

En tant que responsable de présence, je veux consulter, pour une **période** choisie, l'affluence **par
antenne** (sessions, présences valides, moyenne par séance) sous forme de **tableau** et de **barres**,
afin de piloter l'activité des antennes.

**Why this priority**: C'est la vue de pilotage principale ; elle rend la collecte exploitable
visuellement et suffit à elle seule à apporter de la valeur.

**Independent Test**: Se connecter avec le droit `manage_attendance`, ouvrir « Rapports », choisir une
plage de dates et vérifier que la synthèse par antenne s'affiche (tableau + barres comparatives).

**Acceptance Scenarios**:

1. **Given** un utilisateur avec le droit `manage_attendance`, **When** il choisit une **plage de
   dates** valide, **Then** un **tableau par antenne** (sessions, présences valides, moyenne par
   séance) et une **visualisation en barres** (comparaison des antennes) sont affichés.
2. **Given** la synthèse affichée, **When** l'utilisateur applique un **filtre d'antenne**, **Then**
   seule cette antenne est présentée.
3. **Given** une **période sans donnée**, **When** la synthèse est demandée, **Then** un **état vide
   explicite** est affiché (aucune erreur).
4. **Given** une **plage invalide** (fin avant début, bornes manquantes), **When** l'utilisateur
   valide, **Then** un **message clair** est affiché et aucune donnée erronée n'est présentée.
5. **Given** un utilisateur **sans** le droit `manage_attendance`, **When** il regarde la navigation,
   **Then** l'entrée « Rapports » **n'est pas visible** et l'accès direct à l'URL est **refusé**.

---

### User Story 2 - Exporter la synthèse en CSV (Priority: P2)

En tant que responsable de présence, je veux **télécharger** la synthèse (période courante) au format
**CSV**, afin de la retraiter dans un tableur.

**Why this priority**: Prolonge la consultation par le partage/retraitement ; utile mais non bloquant.

**Independent Test**: Depuis la synthèse d'une période, cliquer sur **Exporter (CSV)** et vérifier
qu'un fichier CSV est téléchargé, cohérent avec le tableau affiché, avec un **nom de fichier reflétant
la période**.

**Acceptance Scenarios**:

1. **Given** une synthèse affichée pour une période, **When** l'utilisateur clique **Exporter (CSV)**,
   **Then** un **fichier CSV** est téléchargé, avec un **nom** reflétant la période.
2. **Given** l'export téléchargé, **When** il est ouvert dans un tableur, **Then** ses chiffres
   **correspondent** au tableau affiché pour la même période.
3. **Given** un utilisateur **sans** le droit `manage_attendance`, **When** il tente l'export, **Then**
   l'action est **indisponible / refusée** (l'API reste l'autorité).

---

### User Story 3 - Taux d'assiduité d'un membre (Priority: P2)

En tant que responsable de présence, je veux consulter le **taux d'assiduité** d'un membre sur une
période (présences valides, sessions éligibles, taux en **pourcentage**), afin de suivre son
engagement.

**Why this priority**: Complète la vue « affluence » par une vue « membre » ; important mais secondaire.

**Independent Test**: Sélectionner un membre via la **recherche allégée**, choisir une période et
vérifier l'affichage du nombre de présences valides, du nombre de sessions éligibles et du **taux en %**
(avec une jauge/barre légère).

**Acceptance Scenarios**:

1. **Given** un membre sélectionné (via la recherche allégée) et une période, **When** l'utilisateur
   demande son taux, **Then** sont affichés : **présences valides**, **sessions éligibles** (sessions
   de son antenne d'origine) et **taux en pourcentage** (jauge/barre).
2. **Given** un membre **sans présence** sur la période, **When** le taux est demandé, **Then** il est
   présenté à **0 %**, sans erreur.
3. **Given** aucun membre sélectionné, **When** l'utilisateur tente d'afficher le taux, **Then** il est
   invité à **choisir un membre** (pas d'appel inutile).

### Edge Cases

- **Sans droit `manage_attendance`** : entrée « Rapports » masquée ; accès direct à une URL du module
  refusé (403 géré).
- **Plage invalide** : message clair, aucune requête d'agrégation présentée comme un résultat.
- **Période sans donnée** : état vide explicite (synthèse) ; taux à 0 % (membre sans présence).
- **Membre introuvable / non trouvé par la recherche** : message clair, aucune donnée affichée.
- **Session expirée (401)** : purge et retour à la connexion (comportement du socle).
- **Indisponibilité momentanée** (référentiel des antennes, export) : message clair sans planter le
  reste du tableau de bord.

## Requirements *(mandatory)*

### Accès & navigation

- **FR-001**: L'entrée de navigation « Rapports » et l'accès au module MUST être réservés aux
  utilisateurs disposant du droit **`manage_attendance`** ; l'entrée MUST être **masquée** sinon, et
  l'accès direct à une URL du module **refusé** (l'API reste l'autorité, 403 géré).

### Période & synthèse par antenne (US1)

- **FR-002**: Le module MUST permettre de **choisir une plage de dates** (début, fin) commune aux
  rapports ; une **plage invalide** (fin avant début, bornes manquantes) MUST être **signalée** par un
  message clair, sans présenter de résultat erroné.
- **FR-003**: Le module MUST afficher la **synthèse par antenne** de la période sous forme de
  **tableau** (sessions, présences valides, moyenne par séance) **et** d'une **visualisation en barres**
  comparant les antennes.
- **FR-004**: Le module MUST permettre de **filtrer** la synthèse par **antenne** (optionnel).
- **FR-005**: Une **période sans donnée** MUST afficher un **état vide explicite** (aucune erreur).

### Export CSV (US2)

- **FR-006**: Le module MUST permettre de **télécharger** la synthèse de la période au format **CSV**,
  avec un **nom de fichier** reflétant la période ; le contenu MUST correspondre au tableau affiché.

### Taux par membre (US3)

- **FR-007**: Le module MUST permettre de **sélectionner un membre** via la **recherche allégée**
  (feature 015) puis d'afficher, pour la période, ses **présences valides**, ses **sessions éligibles**
  et son **taux en pourcentage** (jauge/barre légère).
- **FR-008**: Un membre **sans présence** sur la période MUST afficher un taux de **0 %** sans erreur ;
  tant qu'**aucun membre** n'est sélectionné, le taux MUST **ne pas** être demandé.

### Présentation & transverses

- **FR-009**: Le module MUST **présenter** les libellés d'**antenne** et de **membre** (pas seulement
  des identifiants) et exprimer le **taux en pourcentage** lisible.
- **FR-010**: Le module MUST **ne dupliquer aucun calcul statistique** côté client : les chiffres
  proviennent de l'API 018 ; le client ne fait que **mettre en forme** (pourcentage d'affichage,
  hauteurs de barres proportionnelles).
- **FR-011**: L'application MUST **mapper** les erreurs de l'API en messages exploitables (validation,
  introuvable, non autorisé) sans détail technique.
- **FR-012**: Les écrans MUST être en **français** et **responsive** (poste de bureau et tablette) ;
  aucun secret ne MUST être journalisé.

### Key Entities *(include if feature involves data)*

- **Synthèse d'antenne (vue)** : antenne (identifiant + libellé), nombre de sessions, présences valides,
  moyenne par séance — pour la période. Fournie par l'API 018.
- **Taux de membre (vue)** : membre (identifiant + nom), période, présences valides, sessions
  éligibles, taux (pourcentage). Fourni par l'API 018.
- **Antenne (référentiel, lecture)** : identifiant + libellé, pour le filtre (feature 010).
- **Membre (recherche allégée, lecture)** : identifiant + nom, pour la sélection (feature 015).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un responsable obtient la **synthèse par antenne** d'une période (tableau + barres) en
  **moins de 1 minute** après connexion.
- **SC-002**: La **visualisation** reflète fidèlement les chiffres : la **hauteur/longueur des barres**
  est **proportionnelle** aux valeurs renvoyées par l'API (aucune valeur inventée côté client).
- **SC-003**: L'**export CSV** téléchargé **correspond** au tableau affiché pour la même période
  (cohérence 100 %).
- **SC-004**: Le **taux d'un membre** est présenté **sans erreur** même **sans aucune présence**
  (0 %).
- **SC-005**: Dans **100 %** des cas, l'entrée « Rapports » et les actions du module sont **absentes**
  pour un utilisateur sans le droit `manage_attendance`.
- **SC-006**: **100 %** des plages de dates invalides sont **signalées** par un message clair sans
  présenter de résultat.

## Assumptions

- **Socle réutilisé** (feature 008) : session (jeton en mémoire), intercepteurs Bearer/erreurs, gardes
  RBAC (`permissionGuard`), mapping d'erreurs (`messageForError`), notifications. **API 018 inchangée.**
- **Recherche membre allégée** (feature 015) pour le sélecteur de membre ; **référentiel des antennes**
  (feature 010) pour le filtre d'antenne.
- **Visualisation** : barres/jauges **CSS/SVG légères**, **sans** bibliothèque de graphiques ni
  dépendance npm (décidé le 2026-07-06).
- **Taux** : l'API renvoie une fraction (0..1) ; le module l'affiche en **pourcentage**.
- **Export CSV** : le téléchargement s'effectue via une **requête authentifiée** (le jeton de session
  ne transitant pas par un simple lien), puis un **enregistrement de fichier** côté navigateur.
- **Navigation** : une entrée dédiée « Rapports ».
- **Hors périmètre** : toute modification d'API ; la **série temporelle** (évolution jour/semaine/mois)
  et l'**export PDF** (incréments ultérieurs) ; toute statistique non fournie par 018 ; l'usage d'une
  bibliothèque de graphiques externe.
