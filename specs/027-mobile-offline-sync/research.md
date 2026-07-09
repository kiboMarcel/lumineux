# Research — Capture hors ligne & synchronisation par lot (M2)

**Feature** : `027-mobile-offline-sync` · **Phase 0** · **Date** : 2026-07-09

Toutes les zones « NEEDS CLARIFICATION » du Technical Context sont résolues ci-dessous. Les questions
produit avaient déjà été verrouillées par `/speckit-clarify` (voir `spec.md › Clarifications`) ; il reste ici
les **choix techniques** de conception.

---

## D1 — Persistance de la file : coffre sécurisé vs base locale chiffrée

- **Décision** : persister la file comme **document JSON unique** dans **`flutter_secure_storage`**
  (Keychain iOS / EncryptedSharedPreferences via Keystore Android), déjà dépendance du projet (M0).
- **Rationale** :
  - Le **jeton** est une donnée sensible qui doit être **protégée au repos** et **jamais** écrite en clair
    (FR-009, Principe IV). Le coffre OS offre cette protection nativement ; une base SQLite non chiffrée
    écrirait le jeton en clair sur disque.
  - Le **volume est faible** : le membre ne gère que **sa propre** présence, soit quelques captures en
    attente au plus. Un document JSON sérialisé est amplement suffisant et évite une base relationnelle.
  - **Aucune nouvelle dépendance** ni approbation d'installation pour la persistance (le coffre est déjà là).
  - Cohérent avec `SecureTokenStore` existant (même mécanisme, mêmes `AndroidOptions`).
- **Alternatives écartées** :
  - **`sqflite` + chiffrement (SQLCipher)** : surdimensionné pour un si petit volume, ajoute une dépendance
    lourde + configuration de clé de chiffrement à gérer ; bénéfice nul ici.
  - **`sqflite` clair** : écrit le **jeton en clair** → viole FR-009/Principe IV. Rejeté.
  - **`shared_preferences` clair** : idem, jeton en clair. Rejeté.
- **Conséquences** :
  - Écritures **sérialisées** via un seul contrôleur (Riverpod `Notifier`) pour éviter les courses.
  - La dédup « au plus une capture par séance » (FR-014) est appliquée à l'écriture dans le store.
  - Les **avis de synchro** (rejets / échecs définitifs à afficher) **ne contiennent pas** de jeton → ils
    peuvent vivre dans un store applicatif ordinaire (voir D6), gardant le coffre réservé aux secrets.

## D2 — Détection du retour de connectivité

- **Décision** : ajouter **`connectivity_plus`** et l'encapsuler derrière un **`ConnectivityFacade`** (port),
  substituable en test. Sur transition « hors ligne → en ligne », **réinitialiser le backoff** et déclencher
  une tentative de synchro immédiate.
- **Rationale** :
  - FR-006 exige un déclencheur **« au retour de la connectivité »** et SC-002 impose **< 30 s**. Un signal
    d'interface réseau (connectivity_plus) fournit une réaction quasi immédiate, là où un simple backoff
    aurait pu être remonté à un intervalle > 30 s.
  - `connectivity_plus` signale l'**état de l'interface**, pas la joignabilité réelle ; c'est acceptable car
    la **tentative de synchro elle-même** confirme la joignabilité (une erreur réseau relance le backoff).
- **Alternatives écartées** :
  - **Repli sans dépendance** (déclencheurs *lancement* + *reprise d'app* via `WidgetsBindingObserver* +
    *backoff* seul) : fonctionne mais peut **dépasser 30 s** au retour du réseau si le backoff a grandi →
    **dégrade SC-002**. Conservé comme **plan B** si l'approbation d'installation est refusée.
  - **Sonde HTTP périodique maison** : réinvente `connectivity_plus`, consomme batterie/réseau inutilement.
- **Contrainte workflow** : `flutter pub add connectivity_plus` est un **appel réseau** → **approbation
  explicite requise** avant `/speckit-implement` (cf. note du plan).

## D3 — Déclencheurs de synchro & réessai (backoff)

- **Décision** : quatre déclencheurs cumulés —
  1. **Retour de connectivité** (D2) ;
  2. **Lancement / reprise** de l'app (`WidgetsBindingObserver`, `AppLifecycleState.resumed`) avec réseau ;
  3. **Relance manuelle** (bouton « Réessayer », FR-006) ;
  4. **Réessai automatique** avec **backoff exponentiel** tant que l'app est active et que la file n'est pas
     vide (clarification Q2), **borné** par le plafond FR-013.
- **Paramètres backoff** (valeurs par défaut, ajustables — non contractuelles) :
  - délai initial **2 s**, facteur **×2**, plafond d'intervalle **5 min**, **jitter** ±20 % (évite les pics
    synchronisés). Le backoff est **réinitialisé** sur succès partiel ou sur signal de connectivité.
- **Rationale** : couvre les cas app-ouverte (backoff) et transitions (connectivité/reprise) sans marteler le
  serveur ; le jitter et le plafond d'intervalle protègent l'API.

## D4 — Plafond « échec définitif » (FR-013)

- **Décision** : un élément conservé pour **erreur transitoire** passe en **échec définitif** dès que
  **`attemptCount ≥ maxAttempts`** OU **`now − firstCapturedAt ≥ maxAge`**. **Valeurs retenues (défaut de
  conception, exposées en constante configurable)** : **`maxAttempts = 8`**, **`maxAge = 7 jours`**. Ce
  sont les valeurs **à implémenter** ; tout ajustement ultérieur reste une simple modification de constante.
- **Rationale** :
  - Borne à la fois un serveur durablement indisponible (via `maxAttempts`) et une capture trop ancienne qui
    sera de toute façon rejetée par la plage horaire serveur (via `maxAge`).
  - `maxAge = 7 j` laisse une large fenêtre de reconnexion tout en garantissant SC-004 (aucun élément coincé).
- **Comportement** : à l'atteinte du plafond, l'élément est **retiré** de la file et un **avis d'échec
  définitif** (avec raison « non synchronisé après N tentatives / trop ancien ») est enregistré pour
  affichage (SC-004). Seuls les échecs **réseau/serveur transitoires** (erreur réseau, 5xx) incrémentent le
  compteur ; un **401** (session expirée) **n'incrémente pas** et conserve l'élément (voir D5).

## D5 — Classification des issues de synchro (réconciliation)

Mapping de la réponse serveur (`outcome` par élément) et des erreurs de transport vers l'action client :

| Signal serveur / transport | Action sur l'élément | Avis membre |
|---|---|---|
| `Created` | **Retirer** (succès) | — |
| `AlreadyPresent` | **Retirer** (succès, idempotence/unicité) | — |
| `Rejected(reason)` | **Retirer** | **Avis de rejet** avec `reason` (SC-004) |
| Erreur **réseau** / **5xx** (transitoire) | **Conserver**, `attemptCount++`, backoff ; plafond → échec définitif | Avis seulement si plafond atteint (D4) |
| **401** (session expirée) | **Conserver** (pas d'incrément) ; le socle purge la session | — (reprise après reconnexion) |
| **400** (validation, ne devrait pas survenir) | **Retirer** | Avis d'échec définitif (« requête invalide ») |

- **Rationale** : aligne le client sur la sémantique **exacte** du `SyncOfflineScansHandler` (Created /
  AlreadyPresent / Rejected) et garantit qu'**aucune** présence ne reste indéfiniment en file (SC-004).
- **Source vérifiée** : `src/Lumineux.Application/Attendances/SyncOfflineScansHandler.cs` et
  `AttendanceDtos.cs` (constantes `OfflineScanOutcome`).

## D6 — Persistance & cycle de vie des avis de synchro

- **Décision** : store d'avis **séparé** (`SyncNotice`), en stockage applicatif ordinaire (**sans jeton**),
  persisté pour survivre à un redémarrage. Un avis est **conservé jusqu'à acquittement** par le membre
  (fermeture/relecture), afin que SC-004/SC-006 tiennent même si le rejet survient app fermée.
- **Rationale** : découple les **secrets** (coffre, purgés vite) des **messages d'état** (non sensibles,
  affichés jusqu'à lecture). Évite de conserver un jeton uniquement pour afficher un message.
- **Contenu d'un avis** : `clientOperationId`, `sessionId`, `kind` (rejeté / échec définitif), `reason`,
  `occurredAt`. **Jamais** de jeton.

## D7 — Identifiant d'opération (idempotence)

- **Décision** : générer localement un identifiant **aléatoire cryptographiquement sûr** (`Random.secure()`,
  32 hex ≈ 128 bits, bien **≤ 64** caractères), **une seule fois** à la capture, **immuable** ensuite.
- **Rationale** : garantit l'unicité et l'**idempotence** serveur (`GetByClientOperationIdAsync`) même en cas
  de réessais multiples ; pas besoin d'ajouter le package `uuid` (unicité assurée sans dépendance).
- **Contrainte** : l'identifiant **ne change jamais** entre tentatives — c'est la clé d'idempotence (FR-008).

## D8 — Groupement par séance à l'envoi

- **Décision** : à chaque cycle de synchro, **grouper** les captures **par `sessionId`** et émettre **un POST
  `.../{sessionId}/scan/batch` par séance** (l'`id` de séance est dans la **route**, pas dans le corps).
- **Rationale** : le contrat serveur porte le `sessionId` en route et traite un lot d'items pour **cette**
  séance ; un envoi par séance est donc la forme naturelle (FR-005).
- **Note** : avec la dédup FR-014 (au plus une capture par séance), chaque lot contiendra typiquement **un
  seul** item, mais la structure de lot est respectée pour rester fidèle au contrat et robuste.

## D9 — Chemin de capture hors ligne (intégration M1)

- **Décision** : ne **pas** dupliquer le parsing du QR — réutiliser `QrPayloadResult.parse` (M1) pour la
  **validation structurelle** (FR-001a). Dans `ScanController._submit`, sur `ApiErrorType.network`
  (et uniquement ce cas), **basculer vers la capture hors ligne** au lieu d'afficher une erreur ; tous les
  autres comportements M1 (succès 201/200, autres erreurs, anti double-soumission) restent **inchangés**
  (FR-004).
- **Rationale** : changement **minimal et ciblé** du seul chemin d'échec réseau, préservant M1 à l'identique.
- **Détail** : un QR **non reconnu** est déjà rejeté **avant** toute soumission (indice « Code non reconnu »
  de M1) → aucune capture hors ligne d'un QR invalide, ce qui satisfait FR-001a sans code supplémentaire.

## D10 — Horloge & heure d'arrivée

- **Décision** : introduire un port **`Clock`** (`core/time/clock.dart`) pour horodater la capture
  (`clientArrivalTime`) et calculer l'âge (plafond FR-013), substituable en test.
- **Rationale** : rend le backoff et le plafond **testables** de façon déterministe (Principe III) ; l'heure
  du scan reste l'heure client mais le **serveur la borne** (`arrival ∈ [StartTime, UtcNow]`), donc une
  horloge fausse mène à un **rejet serveur** signalé, jamais à une présence erronée (Principe VI).

---

## Synthèse des décisions

| # | Sujet | Décision |
|---|-------|----------|
| D1 | Persistance file | JSON dans `flutter_secure_storage` (jeton protégé au repos) |
| D2 | Connectivité | `connectivity_plus` derrière `ConnectivityFacade` (approbation d'install requise) |
| D3 | Déclencheurs/retry | connectivité + lancement/reprise + manuel + backoff exponentiel (2 s ×2, cap 5 min, jitter) |
| D4 | Plafond FR-013 | `maxAttempts=8` **ou** `maxAge=7 j` → échec définitif signalé + retiré |
| D5 | Réconciliation | Created/AlreadyPresent→retirer ; Rejected→retirer+avis ; réseau/5xx→conserver ; 401→conserver |
| D6 | Avis de synchro | store séparé sans jeton, persistant, conservé jusqu'à acquittement |
| D7 | Identifiant d'opération | aléatoire sûr 32 hex (≤64), immuable, sans package `uuid` |
| D8 | Groupement | 1 POST `/scan/batch` par séance (id en route) |
| D9 | Capture (M1) | réutiliser parsing M1 ; bascule hors ligne **uniquement** sur erreur réseau |
| D10 | Horloge | port `Clock` substituable ; serveur borne l'heure d'arrivée |

**Aucune** zone « NEEDS CLARIFICATION » restante. Prêt pour la Phase 1.
