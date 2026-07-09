# Implementation Plan: Application mobile membre — capture hors ligne et synchronisation des présences

**Branch**: `027-mobile-offline-sync` | **Date**: 2026-07-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/027-mobile-offline-sync/spec.md`

## Summary

Lot **M2** du client mobile membre (Flutter, `mobile/`) : fiabiliser la présence en zone à connectivité
incertaine. Quand un scan de séance (fonction **M1**) échoue faute de réseau, l'app ne montre plus d'erreur
mais **capture** le scan dans une **file locale persistante** (jeton au **coffre sécurisé**), puis le
**synchronise automatiquement** au retour du réseau, au lancement, et par réessai **backoff** tant que l'app
est active — via l'endpoint de lot **existant** `POST /api/v1/attendance-sessions/{sessionId}/scan/batch`
(corps `{ items:[{ clientOperationId, token, clientArrivalTime }] }`, `[Authorize]` membre), **sans aucune
évolution d'API ni de base**. Le serveur (`SyncOfflineScansHandler`) **re-valide le jeton contre l'heure
d'arrivée** (moment du scan), applique plage horaire / clôture / unicité, et renvoie **par élément** une
issue `Created` / `AlreadyPresent` / `Rejected(raison)`. Le client **réconcilie** : retirer les succès et
les rejets (rejet signalé au membre avec sa raison), **conserver** les seuls échecs **transitoires** dans la
limite d'un **plafond tentatives + âge** (FR-013), au-delà duquel l'élément passe en **échec définitif**
signalé et retiré. Un **indicateur d'état** montre les compteurs en attente / en cours / rejetés. Approche :
étendre le module `features/attendance/` (couches données / application / présentation), réutiliser le socle
réseau/session M0, ajouter la persistance de file (via `flutter_secure_storage` existant) et une abstraction
de connectivité ; tests unitaires + widget + intégration (Principe III).

## Technical Context

**Language/Version** : Dart ≥ 3.7 · Flutter stable ≥ 3.29 (installé : 3.44.5) — **client existant** `mobile/`.

**Primary Dependencies** : existantes — `flutter_riverpod`, `go_router`, `dio`, `flutter_secure_storage`,
`mobile_scanner`, `permission_handler`. **Nouvelle** : `connectivity_plus` (signal « réseau revenu » pour
le déclencheur FR-006 et la cible SC-002 < 30 s). **Aucune** dépendance de base de données locale : la file
est persistée en JSON dans le coffre sécurisé existant. **Dev** : `flutter_test`, `mocktail`,
`integration_test`.

**Storage** : **file locale persistante** de captures hors ligne, sérialisée en JSON et stockée dans
**`flutter_secure_storage`** (Keychain iOS / EncryptedSharedPreferences via Keystore Android) — le **jeton**
reste ainsi **protégé au repos** et **purgé** après issue définitive (FR-009). Les **avis de synchro**
(rejets / échecs définitifs à afficher au membre, **sans jeton**) sont persistés séparément en stockage
applicatif ordinaire. **Aucune** base SQL locale, **aucune** migration serveur.

**Testing** : `flutter test` (unitaires : réconciliation, dédup, validation structurelle, backoff, plafond
FR-013 ; widget : indicateur d'état, confirmation hors ligne, avis de rejet), `integration_test` (parcours
capture→synchro), `flutter analyze`.

**Target Platform** : Android (API 24+) et iOS (14+), smartphone avec caméra.

**Performance Goals** : capture hors ligne **confirmée < 3 s** (SC-001) ; synchro automatique **< 30 s** au
retour de la connectivité (SC-002) ; transitions fluides (60 fps).

**Constraints** : **HTTPS exclusif** (exception TLS dev-only héritée du socle) ; **jeton jamais** affiché /
journalisé, **protégé au repos**, **purgé** après traitement définitif ; **aucune règle métier** dupliquée
(serveur autorité — le client ne fait qu'une **validation structurelle** du QR, FR-001a) ; idempotence par
`clientOperationId` (≤ 64 car.) ; **au plus une** capture en file par séance (FR-014) ; réessai **backoff
exponentiel** borné par le plafond FR-013 ; français, usage tactile.

**Scale/Scope** : file de faible volume (présence **du seul membre**, quelques séances) → 1 store de file
(coffre) + 1 store d'avis ; **1 endpoint** consommé (`.../scan/batch`, existant) ; **0 nouveau contrat
serveur** (DTO miroir du contrat existant) ; ~3 états UI ajoutés (confirmation hors ligne, indicateur de
synchro, avis de rejet) ; 1 persona (membre).

## Constitution Check

*GATE : doit passer avant Phase 0 ; re-vérifié après Phase 1.*

| Principe | Applicabilité à M2 | Verdict |
|----------|--------------------|---------|
| **I. Onion & séparation des couches** | Appliqué **en esprit** : la logique (mise en file, réconciliation d'issues, dédup, backoff, plafond) vit dans la couche **application** ; l'accès plateforme (coffre sécurisé, connectivité, horloge, API) est **encapsulé derrière des abstractions** (ports) substituables en test — comme `scannerFacade`/`cameraPermissionFacade` de M1. Aucune règle métier dans les widgets. Dépendances orientées vers l'intérieur. | ✅ PASS |
| **II. Code-First & intégrité BD** | **N/A** : aucune base ni migration serveur ; la persistance locale est un stockage clé-valeur (coffre), pas un schéma relationnel. Explicitement hors périmètre. | ✅ N/A |
| **III. Tests en premier (NON-NÉGOCIABLE)** | Cœur logique testable **sans dépendance réelle** (coffre/connectivité/horloge/API remplacés par des doubles) : réconciliation Created/AlreadyPresent/Rejected, conservation des seuls échecs transitoires, plafond tentatives+âge → échec définitif, idempotence, dédup par séance, validation structurelle. **Test-first** pour cette logique ; widget + intégration pour l'UX. CI verte bloquante. | ✅ PASS |
| **IV. Sécurité par défaut** | Jeton **protégé au repos** (coffre OS), **jamais** affiché/journalisé, **purgé** à l'issue définitive ; **HTTPS** exclusif ; **serveur seul autorité** (valide jeton contre l'heure de scan, plage, clôture, unicité) — le client ne re-valide **aucune** règle métier ; la validation **structurelle** du QR (FR-001a) rejette un format inconnu sans jamais autoriser ; 401 en cours de synchro → purge session (socle), captures **conservées** en file. | ✅ PASS |
| **V. Contrats d'API explicites** | Consomme le contrat **existant** `.../scan/batch` (inchangé) ; DTO client **miroir** de `OfflineScanBatchRequest`/`OfflineScanBatchResponse`. Aucune évolution de contrat, donc aucun versionnage requis. | ✅ PASS |
| **VI. Traçabilité & observabilité** | La traçabilité métier (opération de synchro, refus, horodatage) reste **serveur** (`IAuditLogger` déjà en place). Côté mobile : journalisation **minimale sans secret** (compteurs, issues) — **jamais** le jeton. L'**heure d'arrivée** métier est l'heure du scan mais **bornée par le serveur** (`arrival ∈ [StartTime, UtcNow]`), conforme au principe « source de temps serveur fait foi ». | ✅ PASS |

**Résultat** : aucun écart. Section *Complexity Tracking* laissée vide.

> Note workflow/constitution : l'implémentation nécessitera l'installation du package **`connectivity_plus`**
> (appel réseau `flutter pub add connectivity_plus` / `flutter pub get`) → **approbation explicite requise**
> avant `/speckit-implement`. Aucune configuration plateforme sensible additionnelle (le coffre et la caméra
> sont déjà configurés en M0/M1). Un repli **sans** `connectivity_plus` (déclencheurs lancement + reprise +
> backoff) reste fonctionnellement possible mais **dégrade SC-002** au retour du réseau ; voir `research.md`.

## Project Structure

### Documentation (this feature)

```text
specs/027-mobile-offline-sync/
├── plan.md              # Ce fichier
├── research.md          # Décisions techniques (Phase 0)
├── data-model.md        # Entités/états client & transitions (Phase 1)
├── quickstart.md        # Guide de validation (Phase 1)
├── contracts/           # Contrats (Phase 1)
│   ├── batch-sync-api-consumption.md # Endpoint /scan/batch consommé (existant)
│   └── offline-sync-ui.md            # Indicateur d'état + confirmation hors ligne + avis de rejet
└── tasks.md             # Phase 2 (/speckit-tasks — non créé ici)
```

### Source Code (repository root)

```text
mobile/                                        # client Flutter existant (M0/M1)
├── lib/
│   ├── core/
│   │   └── time/clock.dart                    # NOUVEAU : abstraction d'horloge (port), substituable en test
│   ├── features/
│   │   └── attendance/
│   │       ├── data/
│   │       │   ├── scan_dtos.dart             # existant (M1)
│   │       │   ├── offline_scan_dtos.dart     # NOUVEAU : miroir OfflineScanBatchRequest/Response + Outcome
│   │       │   ├── attendance_api.dart        # MODIF : + syncBatch(sessionId, items) → results
│   │       │   ├── offline_queue_store.dart   # NOUVEAU : file persistée au coffre (JSON), CRUD + dédup séance
│   │       │   └── sync_notice_store.dart     # NOUVEAU : avis de rejet/échec définitif persistés (sans jeton)
│   │       ├── application/
│   │       │   ├── qr_payload.dart            # existant (validation structurelle réutilisée, FR-001a)
│   │       │   ├── scan_controller.dart       # MODIF : sur échec réseau → capture hors ligne au lieu d'erreur
│   │       │   ├── pending_capture.dart       # NOUVEAU : modèle d'élément en file + états
│   │       │   ├── operation_id.dart          # NOUVEAU : générateur d'identifiant d'opération (≤64, aléatoire sûr)
│   │       │   ├── backoff_policy.dart        # NOUVEAU : backoff exponentiel + plafond tentatives/âge (FR-013)
│   │       │   ├── connectivity_facade.dart   # NOUVEAU : abstraction connectivité (connectivity_plus), substituable
│   │       │   ├── sync_controller.dart       # NOUVEAU : orchestre synchro (groupe/séance, réconciliation, retry)
│   │       │   ├── sync_state.dart            # NOUVEAU : compteurs en attente/en cours/rejetés + avis
│   │       │   └── providers.dart             # MODIF : DI (queueStore, noticeStore, connectivity, clock, syncController)
│   │       └── presentation/
│   │           ├── scanner_screen.dart        # MODIF : bannière/indicateur de synchro + relance manuelle
│   │           ├── scan_result_overlay.dart   # MODIF : variante « enregistrée hors ligne, à synchroniser »
│   │           └── sync_status_banner.dart    # NOUVEAU : compteurs + avis de rejet + bouton « Réessayer »
│   ├── test/ (attendance/*)                   # unitaires + widget (nouveaux fichiers)
│   └── integration_test/offline_sync_test.dart# parcours capture→synchro (skip par défaut : API requise)
└── pubspec.yaml                               # MODIF : + connectivity_plus (approbation requise)
```

**Structure Decision** : **extension** du module feature-first `features/attendance/` du client `mobile/`
(pas de nouveau module de premier niveau), réutilisant le socle réseau/session (M0), le scan et la
validation structurelle du payload (M1), et le **coffre sécurisé existant** pour la persistance de la file.
Les nouveaux ports (`connectivity_facade`, `clock`, `offline_queue_store`) suivent le patron d'abstraction
substituable déjà en place. Le seul changement de socle transverse est l'ajout de `core/time/clock.dart`.
L'API `.NET` sous `src/` est **inchangée** (contrat de lot déjà livré).

## Complexity Tracking

*Aucun écart à la constitution — section vide.*
