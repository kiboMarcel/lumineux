# Feature Specification: API de rapports & statistiques de présence

**Feature Branch**: `018-attendance-reports`

**Created**: 2026-07-06

**Status**: Draft

**Input**: User description: "Rapports/statistiques de présence — couche analytique (lecture seule) sur
les présences (001). MVP : synthèse par antenne + période, taux de présence par membre, export CSV.
API d'agrégation d'abord (tableau de bord SPA ultérieur). Accès réservé au droit manage_attendance."

## Contexte & motivation

La collecte des présences est en place (sessions + présences, feature 001) : chaque **session** se tient
dans une **antenne** à une **date**, et chaque **présence** rattache un **membre** avec un statut
(**valide** ou **annulée**). Il manque une **couche analytique** pour **exploiter** ces données : le
bureau veut mesurer l'**affluence** (par antenne, sur une période) et l'**assiduité** des membres.

Cette feature ajoute une **API de rapports** en **lecture seule** (agrégations), **sans modifier** les
données de présence. Le **tableau de bord SPA** (graphiques) relève d'une feature ultérieure. Accès
réservé au droit **gestion des présences** (`manage_attendance`).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Synthèse de présence par antenne et période (Priority: P1) 🎯 MVP

En tant que responsable de présence, je veux une **synthèse** de l'affluence par **antenne** sur une
**période** (nombre de sessions, total de présences valides, moyenne par séance), afin de piloter
l'activité des antennes.

**Why this priority**: C'est la vue de pilotage principale ; elle transforme la collecte en
information exploitable et suffit à elle seule à apporter de la valeur.

**Independent Test**: Interroger la synthèse pour une **plage de dates** (et éventuellement une
antenne) et vérifier que, pour chaque antenne concernée, on obtient le **nombre de sessions**, le
**total de présences valides** et la **moyenne par session**.

**Acceptance Scenarios**:

1. **Given** des sessions et présences sur une période, **When** l'utilisateur demande la synthèse pour
   une **plage de dates**, **Then** il obtient, **par antenne**, le **nombre de sessions**, le **total
   de présences valides** et la **moyenne de présences valides par session**.
2. **Given** la synthèse, **When** l'utilisateur **filtre sur une antenne**, **Then** seule cette
   antenne est renvoyée.
3. **Given** une période **sans aucune session**, **When** la synthèse est demandée, **Then** un
   résultat **vide** cohérent est renvoyé (aucune erreur).
4. **Given** les présences **annulées**, **When** la synthèse est calculée, **Then** elles **ne sont pas
   comptées** dans les totaux (seules les présences **valides** comptent).
5. **Given** un utilisateur **sans** le droit `manage_attendance`, **When** il demande la synthèse,
   **Then** l'accès est **refusé** (401 non authentifié / 403 sinon).

---

### User Story 2 - Taux de présence par membre (Priority: P2)

En tant que responsable de présence, je veux le **taux d'assiduité** d'un membre sur une période
(présences valides rapportées au nombre de sessions éligibles), afin de suivre l'engagement des
membres.

**Why this priority**: Complète la vue « affluence » par une vue « membre » ; important mais secondaire
par rapport à la synthèse d'antenne.

**Independent Test**: Pour un membre donné et une période, obtenir le **nombre de présences valides**,
le **nombre de sessions éligibles** (= sessions de son **antenne d'origine** sur la période) et le
**taux** (présences / éligibles).

**Acceptance Scenarios**:

1. **Given** un membre avec des présences sur une période, **When** l'utilisateur demande son taux,
   **Then** il obtient le **nombre de présences valides**, le **nombre de sessions éligibles** et le
   **taux** (%).
2. **Given** un membre **sans aucune présence** sur la période, **When** le taux est demandé, **Then**
   le taux est **0 %** (pas de division par zéro ni d'erreur) avec le décompte des éligibles.
3. **Given** des présences **annulées** d'un membre, **When** le taux est calculé, **Then** elles **ne
   comptent pas** comme présences.

---

### User Story 3 - Export CSV des agrégats (Priority: P2)

En tant que responsable de présence, je veux **exporter** la synthèse (par antenne / période) au format
**CSV**, afin de la retraiter dans un tableur.

**Why this priority**: Facilite le partage/retraitement ; utile mais non bloquant pour la consultation.

**Independent Test**: Demander l'export CSV de la synthèse pour une période et vérifier qu'un fichier
CSV structuré (en-têtes + lignes par antenne) est renvoyé, cohérent avec la synthèse consultable.

**Acceptance Scenarios**:

1. **Given** une synthèse pour une période, **When** l'utilisateur demande l'**export CSV**, **Then**
   un **fichier CSV** est renvoyé avec des **en-têtes** clairs et **une ligne par antenne** (mêmes
   chiffres que la synthèse).
2. **Given** l'export, **When** il est ouvert dans un tableur, **Then** les colonnes sont lisibles
   (séparateur cohérent, encodage correct, décimales pour la moyenne).
3. **Given** un utilisateur **sans** le droit `manage_attendance`, **When** il demande l'export,
   **Then** l'accès est **refusé** (401/403).

### Edge Cases

- **Plage de dates invalide** (fin avant début, ou absente) : requête **refusée** avec un message clair
  (validation).
- **Plage très large** : la réponse reste cohérente ; un plafond raisonnable de période peut être
  appliqué pour éviter les abus (voir Assumptions).
- **Présences annulées** : exclues de tous les totaux (seules les valides comptent).
- **Antenne inexistante** en filtre : résultat **vide** (aucune erreur) ou message clair.
- **Membre inexistant** (taux) : message « introuvable ».
- **Sans droit `manage_attendance`** : 401/403.
- **Fuseau/dates** : les bornes de période s'entendent en dates de réunion (jour), l'heure serveur
  faisant foi.

## Requirements *(mandatory)*

### Synthèse par antenne & période (US1)

- **FR-001**: Le système MUST fournir une **synthèse de présence** pour une **plage de dates** (début,
  fin) donnée, agrégée **par antenne** : **nombre de sessions**, **total de présences valides**,
  **moyenne de présences valides par session**.
- **FR-002**: La synthèse MUST pouvoir être **filtrée par antenne** (optionnel) ; sans filtre, toutes
  les antennes ayant des sessions sur la période sont renvoyées.
- **FR-003**: Seules les présences **valides** MUST être comptées ; les présences **annulées** MUST être
  exclues des totaux.
- **FR-004**: Une période **sans session** MUST renvoyer un résultat **vide** cohérent (aucune erreur).

### Taux de présence par membre (US2)

- **FR-005**: Le système MUST fournir, pour un **membre** et une **période**, le **nombre de présences
  valides**, le **nombre de sessions éligibles** et le **taux** (présences valides / sessions
  éligibles), sans division par zéro (taux 0 % si aucune session éligible).
- **FR-006**: Les **sessions éligibles** au dénominateur du taux d'un membre MUST être les sessions
  tenues dans **l'antenne d'origine du membre** sur la période. Le taux = présences valides du membre
  ÷ nombre de sessions de son antenne d'origine sur la période. (Si le membre n'a pas d'antenne
  d'origine, le dénominateur est 0 → taux 0 %, sans erreur.)

### Export (US3)

- **FR-007**: Le système MUST permettre d'**exporter** la synthèse par antenne/période au format
  **CSV** (en-têtes explicites, une ligne par antenne, valeurs identiques à la synthèse consultable).

### Transverses & sécurité

- **FR-008**: Tous les rapports MUST être réservés au droit **`manage_attendance`** ; toute demande sans
  authentification ou sans ce droit MUST être **refusée** (401/403). L'API reste l'autorité.
- **FR-009**: Les rapports MUST être en **lecture seule** : aucun effet de bord, aucune écriture, aucune
  migration (réutilise les données de présence existantes).
- **FR-010**: Les **paramètres** (plage de dates, antenne, membre) MUST être **validés** côté serveur ;
  une plage invalide (fin < début, bornes manquantes) MUST être refusée avec un message clair, sans
  fuite technique ; les contrats d'échange MUST utiliser des **représentations dédiées** (pas
  d'exposition d'entités de persistance).
- **FR-011**: Les données personnelles exposées MUST rester **minimales** (le taux par membre référence
  le membre par identifiant et nom ; aucune coordonnée superflue).

### Key Entities *(include if feature involves data)*

- **Synthèse d'antenne (vue calculée)** : antenne (identifiant + libellé), nombre de sessions, total de
  présences valides, moyenne de présences valides par session — sur la période demandée.
- **Taux de membre (vue calculée)** : membre (identifiant + nom), période, présences valides, sessions
  éligibles, taux (%).
- **Session de présence (source, lecture)** : antenne, date de réunion, statut — base du décompte des
  sessions. Non modifiée.
- **Présence (source, lecture)** : membre, session, statut (valide/annulée) — base du décompte des
  présences valides. Non modifiée.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un responsable obtient la **synthèse par antenne** d'une période en **une seule
  demande**, avec, par antenne, sessions / présences valides / moyenne.
- **SC-002**: **100 %** des présences **annulées** sont **exclues** des totaux (aucune n'est comptée).
- **SC-003**: Le **taux de présence** d'un membre est calculé **sans erreur** même **sans aucune
  présence** (résultat 0 %, pas de division par zéro).
- **SC-004**: L'**export CSV** contient **exactement les mêmes chiffres** que la synthèse consultable
  pour la même période (cohérence 100 %).
- **SC-005**: **100 %** des demandes sans le droit `manage_attendance` sont refusées (401/403).
- **SC-006**: **100 %** des plages de dates invalides (fin < début / bornes manquantes) sont refusées
  avec un message clair, sans exécuter d'agrégation.

## Assumptions

- **Données source réutilisées** (feature 001) : sessions (antenne, date de réunion, statut) et
  présences (membre, statut valide/annulée). **Aucune modification** des données ni migration.
- **Périmètre du décompte de sessions** : les sessions sont comptées par **date de réunion** dans la
  plage demandée ; par défaut, **toutes** les sessions de la période (ouvertes ou clôturées) sont
  prises en compte, sauf décision contraire.
- **Plafond de période** : une plage maximale raisonnable (ex. 1 an) peut être imposée pour éviter les
  agrégations abusives (valeur à arrêter au plan).
- **Export** : le **CSV** est le format du MVP ; le **PDF / graphiques riches** relèvent du **tableau de
  bord SPA** (feature ultérieure).
- **Droit d'accès** : réutilise **`manage_attendance`** (décidé le 2026-07-06) ; pas de nouveau droit.
- **Hors périmètre** : le **tableau de bord SPA** (écrans/graphiques) ; la **série temporelle**
  (évolution par jour/semaine/mois) reportée à un incrément ultérieur ; les exports PDF ; toute
  écriture sur les présences.
