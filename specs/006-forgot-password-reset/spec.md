# Feature Specification: Mot de passe oublié

**Feature Branch**: `006-forgot-password-reset`

**Created**: 2026-07-03

**Status**: Draft

**Input**: User description: "Mot de passe oublié — permettre à un membre (y compris le super-admin)
qui a perdu son mot de passe de le réinitialiser via un lien à usage unique envoyé par email.
Deux endpoints anonymes : demande de reset + validation du token + nouveau mot de passe."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Demander la réinitialisation de son mot de passe (Priority: P1) 🎯 MVP

Un membre qui a perdu son mot de passe se rend sur l'écran de connexion, choisit
« mot de passe oublié », saisit sa **référence** (identifiant de connexion) et valide.
Le système répond immédiatement de manière **générique** (aucun signal indiquant si le compte
existe ou non) : « Si un compte correspond à cette référence et qu'un email est enregistré, un
lien de réinitialisation vient d'être envoyé. » Le membre consulte sa boîte mail et clique sur
le lien.

**Why this priority**: C'est le premier maillon indispensable — sans lui, aucune récupération
autonome n'est possible. La confidentialité de la réponse (anti-énumération) est un enjeu de
sécurité : ne pas révéler à un tiers si une référence correspond à un compte existant.

**Independent Test**: Sur une base contenant un membre actif avec un email, la demande envoie
un email (observable par l'inbox de test) ; sur une référence inexistante, la réponse est
strictement identique (même code, même corps) et aucun email n'est envoyé.

**Acceptance Scenarios**:

1. **Given** un compte actif avec un email en fiche, **When** le membre demande la
   réinitialisation avec sa référence, **Then** la réponse est générique (200) et un email
   contenant un lien de réinitialisation est envoyé à son adresse.
2. **Given** une référence inexistante ou un compte sans email, **When** la demande est envoyée,
   **Then** la réponse est **strictement identique** (200 générique) et aucun email n'est envoyé.
3. **Given** un compte **verrouillé** temporairement (feature 003) ou en attente d'activation,
   **When** la demande est envoyée, **Then** la réponse reste générique ; le système journalise
   l'événement pour diagnostic sans révéler le blocage à l'appelant.
4. **Given** une demande légitime enchaînée par une seconde demande pour le même compte,
   **When** le second appel est traité, **Then** un nouveau lien est envoyé ; les liens précédents
   restent techniquement utilisables jusqu'à leur expiration ou consommation (pas d'invalidation
   proactive dans cette itération — voir Assumptions).

---

### User Story 2 — Réinitialiser son mot de passe avec le lien reçu (Priority: P1) 🎯 MVP

Le membre clique sur le lien de son email, qui l'amène sur une page de saisie du nouveau mot
de passe (côté SPA). Il saisit un mot de passe conforme à la politique et confirme. Le système
vérifie le token, applique le nouveau mot de passe, invalide le token (usage unique), et
répond par un succès sans détail exploitable. Le membre peut ensuite se connecter normalement
via `/auth/login` avec son nouveau mot de passe.

**Why this priority**: C'est la seconde moitié du parcours — le lien reçu doit permettre le
changement effectif. Pair inséparable d'US1 pour le MVP.

**Independent Test**: Avec un token de reset valide (obtenu via US1), l'appel change
effectivement le mot de passe (l'ancien ne fonctionne plus, le nouveau oui via `/auth/login`),
et le même token ne peut plus être utilisé une seconde fois.

**Acceptance Scenarios**:

1. **Given** un token valide, non expiré et non consommé, **When** le membre le présente avec un
   nouveau mot de passe conforme à la politique, **Then** son mot de passe est mis à jour, le
   token est marqué comme consommé, et la réponse est 204 (succès sans corps).
2. **Given** un token **expiré** ou **déjà consommé** ou **inexistant**, **When** l'appel est
   fait, **Then** la réponse est un refus **générique** (401) sans distinguer les trois cas.
3. **Given** un token valide mais un nouveau mot de passe non conforme (trop court, sans
   lettre, sans chiffre), **When** l'appel est fait, **Then** la réponse est 400 avec un message
   de validation explicite ; le token **reste utilisable** (non consommé) — le membre peut
   retenter avec un mot de passe conforme.
4. **Given** un compte **verrouillé** temporairement (feature 003) au moment du reset, **When**
   le reset réussit, **Then** le compteur d'échecs est remis à zéro et le verrouillage éventuel
   est levé, permettant une connexion immédiate.
5. **Given** un reset réussi, **When** l'ancien mot de passe est utilisé sur `/auth/login`,
   **Then** la connexion échoue (401 générique) ; **When** le nouveau mot de passe est utilisé,
   **Then** la connexion réussit.

---

### Edge Cases

- **Rejeu de token consommé** : refus générique 401, même après la fenêtre d'expiration.
- **Deux tokens actifs pour le même compte** : les deux restent utilisables (un seul est
  consommé ; l'autre peut être consommé jusqu'à sa propre expiration OU l'utilisateur ignore le
  second email). C'est un choix d'ergonomie — voir Assumptions pour l'invalidation proactive
  différée.
- **Attaque par timing** : les réponses de `/forgot-password` doivent avoir un coût de calcul
  proche pour les cas « membre existe / n'existe pas » (une opération factice compense l'absence
  de génération de token).
- **Anti-énumération sur `/reset-password`** : ne pas distinguer « token inconnu », « token
  expiré », « token déjà utilisé » — même réponse 401 générique. Le membre légitime saura par
  contexte (délai écoulé, page déjà visitée).
- **Mot de passe identique à l'ancien** : accepté sans distinction dans cette itération
  (contrairement à `/auth/change-password` de la feature 003 qui l'interdit) — le membre a par
  définition oublié l'ancien, la comparaison est sans objet ici.
- **Membre archivé** (statut ≠ actif) : la demande est traitée comme pour un compte inexistant
  (réponse générique, aucun email). Un membre non actif ne doit pas pouvoir récupérer son accès
  via ce canal — la réactivation relève d'une action du bureau.
- **Envoi d'email en échec** : la génération du token a lieu, la persistance aussi ; l'échec
  est journalisé côté serveur mais la réponse reste 200 générique (anti-énumération). Le membre
  peut réessayer.
- **Cas super-admin** : le super-admin utilise **exactement le même chemin** que tous les
  membres. La documentation d'installation recommandera d'ajouter un email dès la création
  (feature 005). Sans email, le super-admin ne peut pas se récupérer via cette route — il
  faut alors passer par le repli d'urgence `Auth:Bootstrap:*` ou une intervention SQL (hors
  périmètre).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001** : Le système DOIT exposer une route **publique** (anonyme) permettant à un
  utilisateur de **demander** la réinitialisation de son mot de passe en fournissant sa
  référence.
- **FR-002** : La route de demande DOIT répondre par une **réponse générique** identique dans
  tous les cas (membre inexistant, sans email, verrouillé, en attente d'activation, archivé)
  afin de ne pas divulguer d'information sur l'existence ou l'état du compte (SC-002).
- **FR-003** : Si le compte est **actif** et dispose d'un **email enregistré**, le système DOIT
  générer un jeton de réinitialisation à **usage unique**, en persister une **empreinte** (le
  jeton en clair n'est **jamais** stocké côté serveur), et envoyer un lien contenant le jeton en
  clair à l'adresse email du membre.
- **FR-004** : Chaque jeton DOIT avoir une **durée de vie limitée** (défaut 30 minutes,
  configurable). Après expiration, il est refusé.
- **FR-005** : Le système DOIT exposer une seconde route **publique** (anonyme) permettant de
  **valider** un jeton et de définir un nouveau mot de passe.
- **FR-006** : Le nouveau mot de passe fourni DOIT être validé selon la **politique existante**
  (feature 003 : longueur minimale, lettre, chiffre). Un mot de passe non conforme entraîne un
  refus 400, **sans consommer le jeton** (le membre peut retenter).
- **FR-007** : Après un succès de réinitialisation, le système DOIT :
  (a) mettre à jour l'empreinte du mot de passe du compte ;
  (b) marquer le jeton comme **consommé** (usage unique) ;
  (c) **remettre à zéro** le compteur d'échecs et lever tout verrouillage temporaire (feature 003) ;
  (d) répondre 204 sans corps.
- **FR-008** : En cas de jeton **inexistant**, **expiré** ou **déjà consommé**, la route de
  réinitialisation DOIT répondre par un refus **générique** (401), sans distinguer les trois cas.
- **FR-009** : Le jeton en clair présent dans le lien envoyé par email NE DOIT JAMAIS être
  stocké côté serveur. Seule une **empreinte cryptographique** est persistée.
- **FR-010** : Toutes les opérations sensibles (demande, envoi d'email, tentative de reset,
  succès, refus) DOIVENT être **journalisées** avec l'auteur (compte ciblé si connu), l'action
  et l'horodatage. Le jeton en clair et le mot de passe fourni ne DOIVENT JAMAIS apparaître dans
  les journaux.
- **FR-011** : Le canal d'envoi est l'**email**. Si le membre n'a pas d'email en fiche, aucun
  envoi n'a lieu et la réponse reste générique (FR-002). Le SMS et tout autre canal sont hors
  périmètre.
- **FR-012** : Un **membre non actif** (statut archivé ou compte en attente d'activation) NE
  DOIT PAS pouvoir se récupérer via ce canal. La demande est traitée comme un compte
  inexistant (réponse générique, aucun envoi).
- **FR-013** : La feature n'introduit **aucun changement** au parcours `/auth/change-password`
  (feature 003, utilisateur connecté), à `/auth/login` (feature 003), ni au verrouillage
  anti-force brute — cette dernière protection continue de s'appliquer sur `/auth/login`
  indépendamment du canal de reset.
- **FR-014** : Le super-administrateur (feature 005) utilise **le même chemin** que tout autre
  membre. Aucune route dédiée n'est introduite pour lui.
- **FR-015** : Les jetons DOIVENT être générés avec une **entropie cryptographique suffisante**
  (32 octets aléatoires ou équivalent) pour rendre la devinette d'un jeton valide statistiquement
  impossible.
- **FR-016** : Le stockage du jeton (empreinte) DOIT rester unique en base : deux emissions ne
  peuvent pas produire la même empreinte (protection par index unique).

### Key Entities *(include if data involved)*

- **Jeton de réinitialisation** *(nouvelle entité)* — Objet à durée de vie limitée liant un
  compte de connexion à une empreinte de jeton. Attributs clés : identifiant, référence du
  compte cible, empreinte du jeton, date d'expiration (UTC), date de consommation (UTC,
  nullable), horodatages/auteurs d'audit hérités.
- **Compte de connexion** *(entité existante, features 002/003)* — Mis à jour par la
  réinitialisation : nouvelle empreinte de mot de passe, compteur d'échecs remis à zéro,
  verrouillage éventuel levé.
- **Membre** *(entité existante, features 001/002)* — Consulté pour lire l'email et le statut.
  Un membre non actif fait basculer la demande en refus silencieux (FR-012).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001** : Un membre qui a perdu son mot de passe peut retrouver l'accès à son compte en
  **moins de 5 minutes** de bout en bout (demande → email → clic sur lien → saisie → nouvelle
  connexion), en supposant l'email disponible immédiatement.
- **SC-002** : **100 %** des demandes de reset renvoient une réponse **strictement identique**
  (même code HTTP, même corps) pour les cas : membre inexistant, membre sans email, membre
  verrouillé, membre archivé, membre actif avec email — vérifiable par test.
- **SC-003** : **100 %** des tentatives de réinitialisation avec un jeton
  inexistant/expiré/consommé retournent la **même réponse générique** — vérifiable par test.
- **SC-004** : Aucun jeton en clair n'apparaît dans les journaux serveur ni dans la base de
  données — vérifiable par revue et test.
- **SC-005** : Un jeton consommé une fois est **définitivement refusé** au deuxième usage — le
  taux de rejeu réussi est **0 %**.
- **SC-006** : Après un reset réussi, l'ancien mot de passe est refusé sur `/auth/login` et le
  nouveau est accepté, dans **100 %** des cas testés — vérifie l'effet effectif du changement.
- **SC-007** : Un membre légitime dont le compte était verrouillé (compteur d'échecs > seuil)
  peut se connecter **immédiatement** après un reset réussi, sans attendre l'expiration du
  verrouillage — vérifie l'intégration avec la feature 003.

## Assumptions

- **Longueur du jeton et forme** : 32 octets aléatoires encodés en base64url — suffisant pour
  résister à toute attaque par devinette. Le lien envoyé par email intègre ce jeton en clair.
- **Format du lien envoyé** : `https://<domaine-spa>/auth/reset-password?token=<jeton>` — l'URL
  cible la SPA Angular ; la SPA extrait le paramètre et appelle l'API de reset. Le domaine
  s'appuie sur la configuration existante (feature 002 — configuration d'email et d'URL de base).
- **Durée de vie par défaut** : 30 minutes. Configurable via `Auth:PasswordResetMinutes`.
  Suffisant pour un usage normal (email reçu, clic, saisie), assez court pour limiter la
  fenêtre d'attaque.
- **Invalidation proactive lors d'une nouvelle demande** : cette itération NE révoque PAS
  automatiquement les tokens antérieurs quand un nouveau est émis pour le même compte. Un
  membre qui fait deux demandes reçoit deux emails ; les deux liens sont utilisables jusqu'à
  leur expiration ou consommation. Justification : simplicité + fenêtre déjà courte (30 min).
  À réévaluer si des cas d'usage réels révèlent un besoin (feature d'évolution).
- **Nettoyage des tokens expirés** : les lignes expirées peuvent rester en base sans dommage
  fonctionnel (elles sont refusées). Un job de purge périodique n'est PAS livré dans cette
  itération — à ajouter ultérieurement si le volume devient significatif.
- **Coexistence complète** avec `/auth/change-password` (utilisateur connecté, feature 003) :
  cette route reste la voie normale pour un utilisateur qui connaît son mot de passe actuel.
- **Coexistence avec le verrouillage anti-force brute** (feature 003) : le verrouillage
  s'applique à `/auth/login` uniquement, pas à `/reset-password`. Cette dernière est protégée
  par (1) l'entropie du jeton et (2) la rareté de la fenêtre. Pas de rate limiting global dans
  cette itération.
- **Anti-timing sur `/forgot-password`** : implémenté par une opération factice (hash de
  l'équivalent d'un token) quand le compte est absent ou sans email, pour égaliser le coût de
  calcul — même stratégie que le hash factice de `/auth/login` (feature 003 F5).
- **Doc d'installation** : le super-admin (feature 005) sera invité à renseigner son email
  dès la première installation. Si l'email n'est pas fourni au moment de l'installation, il
  pourra être ajouté ultérieurement via l'endpoint standard de mise à jour de membre
  (feature 002).
- **Hors périmètre** : SMS, question secrète/OTP, repli CLI, rate limiting HTTP,
  invalidation en cascade, notification par email au propriétaire lors d'un reset réussi
  (« votre mot de passe a été changé ») — pistes possibles pour une itération ultérieure.
