# Feature Specification: Recherche membre allégée (member lookup)

**Feature Branch**: `015-member-lookup-endpoint`

**Created**: 2026-07-05

**Status**: Draft

**Input**: User description: "Ajouter un endpoint de recherche membre allégée, accessible au droit de
gestion des présences, pour identifier un membre lors de l'ajout manuel d'une présence (prérequis du
Lot 4 SPA)."

## Contexte & motivation

Pour l'**ajout manuel d'une présence** (feature 014), un opérateur de **gestion des présences** doit
pouvoir **retrouver un membre** (par référence ou nom) afin d'obtenir son identifiant. Or la
**recherche de membres complète** est réservée au droit de **gestion des membres** et expose une fiche
riche (coordonnées, etc.) — un opérateur de présence n'y a **pas accès**.

Cette fonctionnalité ajoute une **recherche minimale, distincte**, exposant **uniquement** les champs
nécessaires à l'identification (identifiant, référence, nom complet, statut), **accessible aux
opérateurs de présence** (et aux gestionnaires de membres). Elle **ne remplace pas** la recherche
complète : c'est une **vue réduite** dédiée, limitant l'exposition des données personnelles.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retrouver un membre pour l'identifier (Priority: P1) 🎯 MVP

En tant qu'**opérateur de gestion des présences**, je veux rechercher un membre par sa **référence**
ou son **nom** et obtenir une **liste courte** de correspondances **minimales**, afin de sélectionner
le bon membre pour un ajout manuel de présence, **sans** disposer du droit de gestion des membres.

**Why this priority**: C'est la seule raison d'être de la fonctionnalité et le prérequis de l'ajout
manuel côté SPA. Sans elle, un opérateur de présence ne peut pas identifier un membre.

**Independent Test**: Avec un compte disposant du droit de gestion des présences, rechercher par un
terme (référence ou nom) et vérifier qu'on obtient une **liste courte** d'entrées **minimales**
(identifiant, référence, nom complet, statut) **sans** aucune coordonnée ni donnée superflue.

**Acceptance Scenarios**:

1. **Given** un utilisateur disposant du droit de **gestion des présences**, **When** il recherche par
   un terme correspondant à une **référence** ou à un **nom**, **Then** il reçoit une **liste courte**
   d'entrées **minimales** (identifiant, référence, nom complet, statut).
2. **Given** un utilisateur disposant du droit de **gestion des membres**, **When** il utilise cette
   recherche allégée, **Then** il obtient le **même** résultat minimal (lecture élargie aux deux
   droits).
3. **Given** un utilisateur **sans** aucun de ces deux droits, **When** il tente la recherche allégée,
   **Then** l'accès est **refusé**.
4. **Given** une demande **sans terme de recherche** (critère vide), **When** elle est soumise, **Then**
   elle est **refusée** (un critère est requis — pas d'aspiration de tout l'annuaire).
5. **Given** un terme sans correspondance, **When** la recherche est exécutée, **Then** une **liste
   vide** est renvoyée (pas d'erreur).

### Edge Cases

- **Grand nombre de correspondances** : le résultat est **plafonné** (liste courte) ; l'opérateur
  affine son terme si besoin.
- **Non authentifié** : refus (pas d'accès anonyme).
- **Aucune donnée sensible** : la réponse ne contient **jamais** e-mail, mobile, adresse, date de
  naissance ni rattachements — uniquement de quoi identifier le membre.
- **Terme très court / espaces** : traité comme critère éventuellement insuffisant ; l'implémentation
  peut exiger une longueur minimale (documenté en Assumptions) mais un critère non vide reste requis.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Le système MUST exposer une **recherche membre allégée** par **terme** (correspondant à
  la **référence** et/ou au **nom/prénom**).
- **FR-002**: Un **terme de recherche** MUST être **requis** ; une demande sans critère MUST être
  **refusée** (aucun listing complet du fichier des membres).
- **FR-003**: Chaque résultat MUST se limiter à des **champs minimaux** d'identification : identifiant
  technique, **référence**, **nom complet**, **statut**. La réponse MUST NE **jamais** contenir de
  coordonnée (e-mail, mobile), d'adresse, de date de naissance ni de rattachement.
- **FR-004**: Le résultat MUST être une **liste courte plafonnée** (nombre maximal de correspondances)
  ; au-delà, l'utilisateur affine son terme.
- **FR-005**: L'accès MUST être réservé aux utilisateurs disposant du droit de **gestion des
  présences** **OU** du droit de **gestion des membres** ; une demande **non authentifiée** ou **sans
  l'un de ces droits** MUST être **refusée**.
- **FR-006**: La recherche MUST être en **lecture seule** (aucun effet de bord, aucune donnée persistée
  nouvelle) et **répétable**.
- **FR-007**: Cette recherche MUST NE **pas** remplacer ni affaiblir la **recherche complète** (droit
  de gestion des membres) : c'est une **vue réduite** additionnelle.

### Key Entities *(include if feature involves data)*

- **Résultat de recherche membre allégé (lecture seule)** : identifiant technique, **référence**, **nom
  complet**, **statut**. Aucune donnée personnelle superflue. Dérivé des données membres existantes ;
  aucune nouvelle donnée persistée.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un opérateur de **gestion des présences** (sans droit de gestion des membres) peut
  retrouver un membre par référence ou nom en **un seul appel** et obtenir de quoi l'identifier.
- **SC-002**: **100 %** des réponses ne contiennent **que** les champs minimaux (identifiant,
  référence, nom, statut) — **aucune** coordonnée ni donnée personnelle superflue.
- **SC-003**: **100 %** des demandes **sans critère** de recherche sont **refusées**.
- **SC-004**: **100 %** des demandes **non authentifiées** ou **sans** l'un des deux droits requis sont
  **refusées**.
- **SC-005**: Le nombre de résultats renvoyés ne dépasse **jamais** le **plafond** défini (liste
  courte).

## Assumptions

- **Réutilisation des données membres** : la recherche s'appuie sur les membres existants (feature
  002) ; **aucune** nouvelle table, **aucune** migration.
- **Critère de recherche** : terme unique appliqué à la référence et au nom/prénom ; une longueur
  minimale (ex. 2 caractères) peut être exigée pour éviter des recherches trop larges (réglage
  d'implémentation ; un critère non vide reste dans tous les cas requis).
- **Plafond de résultats** : quelques dizaines maximum (ex. 20–50) ; valeur de conception, non
  autoritaire pour le métier.
- **Lecture élargie** : accès au droit de gestion des présences **ou** de gestion des membres, cohérent
  avec l'usage (identifier un membre pour une présence, ou dans le cadre de la gestion des membres).
- **Consommateur** : le SPA (feature 014, sélecteur d'ajout manuel de présence). L'app mobile pourra
  réutiliser la même recherche.
- **Hors périmètre** : la recherche complète de membres (déjà couverte, gestion des membres) ; le
  sélecteur/écran SPA et l'ajout manuel de présence (feature 014) ; toute modification de membre.
