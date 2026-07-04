# Feature Specification: Installation du premier administrateur

**Feature Branch**: `005-first-admin-setup`

**Created**: 2026-07-03

**Status**: Draft

**Input**: User description: "Installation du premier administrateur sur base vierge — endpoint
anonyme unique de bootstrap qui, tant qu'aucun membre actif ne dispose du droit
`manage_bureau_profiles`, crée atomiquement le premier compte administrateur, l'active, l'attribue
au profil « Administrateur » et retourne un jeton d'accès immédiat."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Installation initiale sur une base vierge (Priority: P1) 🎯 MVP

Un opérateur (personne technique déployant le système pour la première fois, sans aucun compte
existant en base) appelle une route publique unique en fournissant l'identité du futur super-admin
(nom, prénom, genre, mot de passe conforme à la politique). Le système crée en une opération
atomique le membre, son compte de connexion actif, le profil « Administrateur » (portant l'ensemble
des droits fonctionnels connus du système), l'attribution du profil au membre, puis retourne un
jeton d'accès **immédiatement utilisable** pour piloter la suite de la configuration.

**Why this priority**: C'est **l'unique** chemin qui rend une base vierge exploitable sans
intervention SQL manuelle. Sans cette route, le déploiement d'un nouvel environnement (production,
staging, démo) exige un accès direct à la base, ce qui est à la fois risqué et bloquant pour un
support non technique. C'est le MVP absolu de cette fonctionnalité.

**Independent Test**: Sur une base sans aucun membre ni profil, appeler la route avec un payload
valide → réception d'un jeton d'accès ; ce jeton permet immédiatement d'appeler un endpoint
protégé (créer un membre, démarrer une session, créer un profil) sans autre étape.

**Acceptance Scenarios**:

1. **Given** une base vierge (aucun membre actif titulaire du droit d'administration des profils),
   **When** un opérateur appelle la route publique d'installation avec un payload valide,
   **Then** le membre est créé actif, son compte est activé (aucun changement de mot de passe requis),
   un profil « Administrateur » existe et lui est attribué, et un jeton d'accès est retourné.
2. **Given** l'appel précédent, **When** l'opérateur utilise le jeton retourné pour appeler un
   endpoint réservé aux administrateurs des profils, **Then** l'accès est accordé.
3. **Given** une base vierge, **When** l'opérateur envoie un mot de passe non conforme à la
   politique (trop court, sans lettre, sans chiffre), **Then** la requête est refusée avec un
   message explicite et rien n'est créé en base.
4. **Given** une base vierge, **When** l'opérateur envoie un payload incomplet (nom vide, genre
   absent), **Then** la requête est refusée pour cause de validation et rien n'est créé.

---

### User Story 2 — Verrouillage automatique après première installation (Priority: P1)

Une fois qu'un administrateur des profils **actif** existe en base (installé par cette route, par
l'amorçage historique `Auth:Bootstrap:*`, ou tout autre chemin), la route publique d'installation
**refuse** tout nouvel appel et n'expose plus la possibilité de créer un super-admin. Cette
protection s'applique sans intervention de configuration et sans état supplémentaire.

**Why this priority**: Sans ce verrouillage, la route serait un vecteur d'escalade de privilèges
critique — n'importe qui pourrait se créer un compte administrateur. Ce verrouillage est
indissociable d'US1 et doit être livré en même temps.

**Independent Test**: Après avoir installé un premier admin (US1), appeler la même route une
seconde fois avec un payload valide → refus explicite avec code métier `already_installed`, aucun
nouveau membre ni compte créé, aucune modification de l'admin existant.

**Acceptance Scenarios**:

1. **Given** un administrateur des profils actif existe, **When** un tiers appelle la route
   d'installation avec un payload valide, **Then** la requête est refusée (409) avec le code
   `already_installed` et aucune modification n'est apportée à la base.
2. **Given** un administrateur des profils actif existe, **When** un tiers appelle la route avec un
   payload **invalide** (mot de passe faible, champs manquants), **Then** la requête est refusée
   **sans divulguer** si l'installation a déjà eu lieu (message générique de refus prioritaire).
3. **Given** un admin existait mais son membre a été archivé (statut non actif), **When**
   l'opérateur appelle la route, **Then** la requête est acceptée si aucun **autre** admin actif ne
   reste (garantie de non-blocage en cas de reprise).

---

### User Story 3 — Idempotence sur le profil « Administrateur » (Priority: P2)

Si un profil nommé « Administrateur » existe déjà en base (par exemple créé par la migration
« Amorçage » de la feature 004, ou par un premier admin qui aurait été archivé), la route
d'installation **le réutilise** sans créer de doublon. Le nouveau membre est simplement attribué à
ce profil existant.

**Why this priority**: Évite les états incohérents en cas de reprise après incident (perte du seul
admin, création manuelle antérieure d'un profil éponyme). Non bloquant pour le MVP, mais critique
pour la robustesse opérationnelle.

**Independent Test**: Créer manuellement un profil « Administrateur » (via SQL ou seed), archiver
l'unique admin, puis appeler la route d'installation → succès, avec attribution au profil
préexistant sans duplication du nom.

**Acceptance Scenarios**:

1. **Given** un profil « Administrateur » existe déjà mais aucun admin actif n'en bénéficie,
   **When** l'opérateur appelle la route, **Then** un nouveau membre est créé et attribué au
   profil existant ; aucun nouveau profil n'est créé.
2. **Given** un profil existant nommé « Administrateur » (insensible à la casse) contient déjà
   `manage_bureau_profiles`, **When** l'installation aboutit, **Then** ce droit est simplement
   confirmé, pas dupliqué.
3. **Given** un profil « administrateur » (casse différente) existe, **When** l'installation
   aboutit, **Then** le profil existant est réutilisé (unicité insensible à la casse — feature 004).

---

### Edge Cases

- **Race concurrente** : deux appels simultanés sur base vierge. La cohérence est garantie par une
  vérification atomique côté serveur (contrainte d'unicité + transaction) — un des deux appels
  gagne, l'autre reçoit `409 already_installed`.
- **Coordonnée déjà utilisée** : rare sur base vierge mais possible en redéploiement partiel — la
  requête est refusée (409, code hérité `contact_in_use`).
- **Référence auto-générée en collision** : la génération réutilise le mécanisme existant (feature
  002) ; en cas de collision persistante (extrêmement improbable), échec 409.
- **Anti-fuite d'énumération** : contrairement à `/auth/login` (feature 003), la route n'a pas de
  problématique d'énumération de comptes puisqu'elle est destinée à agir sur une base vierge ; en
  revanche, elle ne DOIT pas divulguer d'informations sur l'admin existant lorsqu'elle refuse.
- **Enveloppe de rejeu** : un appel légitime accepté puis rejoué (par erreur réseau côté client)
  reçoit `409 already_installed`, exactement comme un tiers malveillant. Comportement volontaire.
- **Rétrocompatibilité `Auth:Bootstrap:*`** : si l'amorçage historique a déjà créé un admin, la
  route est automatiquement verrouillée — aucune migration n'est requise.
- **Aucun contact fourni** : l'installation est possible sans email ni mobile (contrairement à la
  création standard de la feature 002 qui les recommande), pour permettre une installation
  minimale et rapide. Le bureau pourra compléter la fiche ultérieurement via l'endpoint standard.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001** : Le système DOIT exposer une route **publique** (anonyme) unique permettant
  d'installer le premier administrateur du bureau.
- **FR-002** : La route DOIT accepter un payload minimal contenant : **nom**, **prénom**, **genre**,
  **mot de passe** (obligatoires) ; **email** et **mobile** (optionnels).
- **FR-003** : Le mot de passe fourni DOIT être validé selon la **politique de mot de passe
  existante** (feature 003 : longueur minimale, lettre, chiffre) ; un mot de passe non conforme
  provoque un refus 400 sans effet en base.
- **FR-004** : La route DOIT **refuser** tout appel dès qu'**au moins un** membre actif dispose du
  droit `manage_bureau_profiles`, avec un code métier explicite `already_installed` (409). Aucune
  modification n'est apportée à la base en cas de refus.
- **FR-005** : Le refus `already_installed` DOIT être prioritaire sur les autres erreurs de
  validation, afin de ne divulguer aucune information sur la structure du payload attendu à un
  tiers non légitime.
- **FR-006** : En cas de succès, le système DOIT créer **atomiquement** (une seule transaction
  logique, tout-ou-rien) : (a) un membre au statut **actif**, avec référence auto-générée par le
  mécanisme existant ; (b) un compte de connexion **actif** (l'obligation de changement de mot
  de passe est levée) ; (c) une **attribution** au profil « Administrateur » (créé ou réutilisé).
- **FR-007** : Le profil « Administrateur » DOIT contenir **l'ensemble des droits fonctionnels
  connus** du système (référentiel figé côté serveur — feature 004) au moment de sa création.
- **FR-008** : Si un profil de même nom (comparaison insensible à la casse) existe déjà, le système
  DOIT **le réutiliser** sans en créer un nouveau, ni redéfinir sa liste de droits, ni écraser sa
  description. La seule action est l'ajout d'une nouvelle attribution.
- **FR-009** : En cas de succès, la route DOIT retourner un **jeton d'accès** signé et expirant,
  portant l'ensemble des droits effectifs du nouveau membre. Le jeton DOIT être immédiatement
  utilisable sur les endpoints protégés (aucun changement de mot de passe ni activation requis).
- **FR-010** : L'opération d'installation DOIT être **journalisée** (auteur = système, cible =
  membre créé, action = création du premier admin), **sans divulguer** le mot de passe ni le jeton.
- **FR-011** : La route NE DOIT PAS exiger de flag de configuration, de jeton d'installation, ou
  d'action opérationnelle préalable ; le contrôle « aucun admin actif » suffit comme verrou.
- **FR-012** : Le mécanisme d'amorçage historique (`Auth:Bootstrap:*` — feature 003) DOIT rester
  fonctionnel en parallèle (filet de sécurité pour déploiements manuels), sans interférer avec
  cette route ni être court-circuité par elle.
- **FR-013** : La route DOIT être **idempotente en effet** vis-à-vis du profil « Administrateur » :
  un profil existant est réutilisé (FR-008) ; aucun droit n'est retiré ni ajouté à un profil
  existant.
- **FR-014** : En cas de **collision** sur base non strictement vierge, la route DOIT refuser (409)
  en réutilisant les codes métier existants de la feature 002 plutôt que d'inventer un nouveau code :
  (a) `contact_in_use` si l'email ou le mobile fourni est déjà utilisé par un membre actif ;
  (b) `duplicate_reference` si la référence auto-générée entre en collision (cas très rare,
  typiquement une reprise sans purge complète des séquences).

### Key Entities *(include if data involved)*

- **Membre** *(entité existante, feature 001/002)* — Le membre créé porte le statut « actif » et
  reçoit une **référence auto-générée** selon les règles existantes. Aucune donnée sensible n'est
  exigée au-delà du nom/prénom/genre.
- **Compte de connexion** *(entité existante, features 002/003)* — Le compte est créé au statut
  « actif », avec l'obligation de changement de mot de passe **levée** (contrairement au parcours
  standard de création d'un membre par le bureau où le mot de passe est temporaire).
- **Profil « Administrateur »** *(entité existante, feature 004)* — Groupe nommé portant
  l'**ensemble** des droits fonctionnels connus. Créé par cette route si absent, réutilisé sinon.
- **Attribution profil ↔ membre** *(entité existante, feature 004)* — Lie le nouveau membre au
  profil « Administrateur ».
- **Jeton d'accès** *(objet transitoire, feature 003)* — Émis en réponse à la route ; porte les
  droits effectifs du nouveau membre.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : Un opérateur peut installer le premier admin **en moins de 30 secondes** à partir
  d'une base vierge, sans accès SQL et sans configuration additionnelle.
- **SC-002** : Dès qu'un admin est installé, **100 %** des tentatives ultérieures d'appel à la
  route sont refusées avec le code `already_installed`, quel que soit le payload (valide ou
  invalide).
- **SC-003** : Aucun payload invalide (mot de passe faible, champs manquants) n'aboutit à la
  création d'un membre ou d'un compte — atomicité vérifiée dans **100 %** des cas de test.
- **SC-004** : Le jeton d'accès retourné après installation permet **immédiatement** l'accès à
  tous les endpoints protégés (au moins un endpoint par droit fonctionnel connu) sans étape
  intermédiaire d'activation ou de changement de mot de passe.
- **SC-005** : Aucune donnée sensible (mot de passe, jeton) n'apparaît dans les journaux liés à
  cette fonctionnalité — vérifiable par revue automatique.
- **SC-006** : En cas de profil « Administrateur » préexistant, **aucun doublon** n'est créé et la
  description/liste de droits du profil existant reste **inchangée** — vérifiable par test.

## Assumptions

- L'installation initiale est une opération **rare et sensible** ; elle sera pilotée par la SPA
  Angular via un écran d'installation dédié, mais la route reste utilisable en direct (curl,
  Postman) par un opérateur technique.
- Les jetons émis par cette route respectent la même durée de vie que ceux émis par `/auth/login`
  (paramètre `Auth:AccessTokenMinutes`, feature 003).
- La politique de mot de passe (`Auth:PasswordMinLength`) est celle héritée de la feature 003 ;
  aucune règle spécifique à l'installation n'est introduite.
- Les droits fonctionnels connus au moment de la livraison sont `manage_attendance`,
  `manage_members`, `manage_bureau_profiles` ; toute évolution du référentiel (feature future)
  s'appliquera automatiquement au profil « Administrateur » créé par cette route lors des
  installations ultérieures — pas de mise à jour rétroactive pour les installations existantes.
- Le premier admin **hérite d'une référence auto-générée** selon le format déjà en place (feature
  002 : `LUM-{yyyy}-{seq:00000}`) ; il pourra la modifier via l'endpoint standard s'il le souhaite
  (ou pas — la référence est stable par convention).
- L'installation ne se soucie pas de l'antenne d'origine (`AntennaId = null`) ni des autres champs
  optionnels (`birthDate`, `address`, `civilityId`, etc.) ; le membre les complétera par
  `PUT /members/{id}`.
- La route est destinée à une utilisation **unique par base** ; aucune stratégie de
  « réinitialisation » n'est prévue dans cette itération. Un environnement qui devrait « repartir
  de zéro » procèdera par restauration d'une sauvegarde ou par purge de la base (opérations DBA).
