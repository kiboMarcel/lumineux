# Quickstart — Validation de la capture hors ligne & synchronisation (M2)

**Feature** : `027-mobile-offline-sync` · **Phase 1** · Guide de **validation**, pas d'implémentation.

Objectif : prouver de bout en bout que (a) une présence scannée **hors réseau** est **capturée et survit** à
un redémarrage, (b) elle se **synchronise automatiquement** au retour du réseau sans doublon, (c) les
**rejets/échecs** sont signalés puis retirés. Réfère aux détails dans `data-model.md` et `contracts/`.

## Prérequis

- Flutter installé (stable ≥ 3.29 ; testé 3.44.5), Dart ≥ 3.7.
- API Lumineux joignable en HTTPS (dev : exception TLS auto-signé héritée du socle M0) avec l'endpoint
  **existant** `POST /api/v1/attendance-sessions/{id}/scan/batch`.
- Un **membre actif** authentifié (socle M0) et une **séance ouverte** avec QR projeté (SPA feature 014).
- **Approbation** préalable de l'ajout de `connectivity_plus` (`flutter pub add connectivity_plus`) — appel
  réseau soumis à approbation. Config env : `mobile/env/*.json` (déjà en place).

## Installation

```bash
cd mobile
# APRÈS approbation explicite (appel réseau) :
flutter pub add connectivity_plus
flutter pub get
```

## Vérifications statiques & tests automatisés

```bash
cd mobile
flutter analyze
flutter test                     # unitaires + widget (dont la logique de réconciliation/backoff/dédup)
```

Attendus (couvrent les FR/SC) :
- **Réconciliation** : `Created`/`AlreadyPresent` retirent l'élément ; `Rejected` retire + crée un
  `SyncNotice` ; erreur réseau/5xx conserve et incrémente `attemptCount` ; **401** conserve sans incrément.
  → FR-007, D5.
- **Idempotence** : ré-envoyer un `clientOperationId` déjà traité ne crée pas de doublon (mock renvoyant
  `AlreadyPresent`) et l'élément est retiré. → FR-008, SC-003.
- **Plafond FR-013** : au-delà de `maxAttempts` **ou** `maxAge`, l'élément passe en échec définitif (avis +
  retrait). → SC-004.
- **Dédup séance** : deux captures de la même séance ⇒ **une** entrée en file. → FR-014.
- **Validation structurelle** : un QR sans `s`/`t` n'est **jamais** mis en file. → FR-001a.
- **Persistance** : la file relue depuis le coffre après « redémarrage » simulé contient les captures.
  → FR-003, SC-001.

## Scénarios manuels (device / émulateur)

### Scénario A — Capture hors ligne + survie au redémarrage (US1, SC-001)
1. Se connecter en tant que membre ; ouvrir l'onglet **Scanner**.
2. **Couper le réseau** (mode avion).
3. Scanner le **QR d'une séance ouverte**.
4. **Attendu** : overlay **« Enregistrée hors ligne — à synchroniser »** (< 3 s), **sans** message d'échec ;
   l'indicateur montre **1 en attente**.
5. **Fermer et relancer** l'app (toujours hors réseau).
6. **Attendu** : la capture est **toujours** listée « en attente » (persistée).

### Scénario B — Synchronisation automatique au retour du réseau (US2, SC-002/SC-003)
1. Depuis l'état du Scénario A, **rétablir le réseau**.
2. **Attendu** : une synchro se déclenche **automatiquement** (< 30 s) ; l'indicateur passe **en cours** puis
   la capture **disparaît** de la file (issue `Created`).
3. **Rescanner la même séance** en ligne (M1) ou relancer une synchro.
4. **Attendu** : **aucun doublon** — le serveur renvoie `AlreadyPresent` (idempotence).

### Scénario C — Rejet signalé puis retiré (US3, SC-004)
1. Provoquer un **rejet** serveur (ex. horloge appareil avancée hors plage, ou séance close avant synchro).
2. Capturer un scan hors ligne, puis rétablir le réseau.
3. **Attendu** : la capture est **retirée** de la file et un **avis de rejet** clair (avec la **raison**
   serveur) est présenté ; aucun élément « coincé ». L'avis est **acquittable**.

### Scénario D — Relance manuelle & état (FR-006, FR-011, SC-006)
1. Avec des captures en attente et **serveur momentanément coupé**, observer les **réessais backoff**.
2. Appuyer sur **« Réessayer »** → une tentative immédiate a lieu.
3. **Attendu** : à tout moment, l'indicateur affiche le **nombre en attente** et les **avis** ; à
   rétablissement, tout se synchronise.

### Scénario E — Session expirée en cours de synchro (edge case)
1. Invalider la session (jeton expiré) avec des captures en file, réseau présent.
2. **Attendu** : réponse `401` → l'app **purge la session** et revient à la connexion ; les captures
   **restent en file** ; après reconnexion, la synchro reprend et les retire.

## Critères de sortie

- [ ] `flutter analyze` sans erreur ; `flutter test` vert.
- [ ] Scénarios A→E conformes aux attendus.
- [ ] Aucune présence perdue, aucun doublon, aucun élément « coincé » (SC-001..SC-006).
- [ ] Jeton **jamais** visible en clair (logs/affichage) et **purgé** après issue définitive (SC-005).
