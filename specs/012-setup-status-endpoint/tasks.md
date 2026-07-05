---
description: "Task list — Statut d'installation (setup/status)"
---

# Tasks: Statut d'installation (setup/status)

**Input**: Design documents from `specs/012-setup-status-endpoint/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUS — Constitution Lumineux v1.0.0, Principe III. Unitaire Application ; intégration API
(harnais SQLite existant).

**Organization**: Une seule user story (P1). Extension de la solution Onion existante, **lecture
seule**, **anonyme**, **sans migration**. Réutilise le décompte du verrou (feature 005).

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1 (phase de user story)
- Chaque tâche indique le chemin de fichier exact

---

## Phase 1: Setup

**Aucune tâche** : extension d'une solution existante ; aucune dépendance, configuration ou migration.

---

## Phase 2: Foundational

**Aucune tâche** : le décompte `IBureauProfileRepository.CountActiveAdministratorsAsync` (features
004/005) est **réutilisé tel quel** ; rien à préparer avant la user story.

---

## Phase 3: User Story 1 — Savoir si l'instance est installée (Priority: P1) 🎯 MVP

**Goal**: Endpoint anonyme `GET /api/v1/setup/status` renvoyant `{ installed }` = (≥1 administrateur
actif), aligné exactement sur le verrou d'installation.

**Independent Test**: Sur une base vierge, `GET setup/status` (sans jeton) → `{ installed: false }` ;
après installation du premier administrateur → `{ installed: true }` ; jamais 401/403 ; réponse
strictement booléenne.

### Tests for User Story 1 ⚠️ (écrire d'abord)

- [X] T001 [P] [US1] Tests unitaires `GetSetupStatusTests` (`installed = false` si `CountActiveAdministratorsAsync` = 0 ; `true` si ≥ 1) dans `tests/Lumineux.Application.Tests/GetSetupStatusTests.cs`
- [X] T002 [P] [US1] Tests d'intégration `SetupStatusEndpointTests` (`GET /api/v1/setup/status` **anonyme** → 200 `{ installed:false }` sur base sans admin ; **bascule** à `true` après `POST /setup/first-admin` ; réponse ne contient **que** `installed`) dans `tests/Lumineux.Api.Tests/SetupStatusEndpointTests.cs`

### Implementation for User Story 1

- [X] T003 [US1] Ajouter le DTO `SetupStatusResponse(bool Installed)` dans `src/Lumineux.Application/Contracts/Setup/SetupDtos.cs`
- [X] T004 [US1] Implémenter `GetSetupStatusHandler` (`installed = await _profiles.CountActiveAdministratorsAsync(ct) > 0` ; lecture seule) dans `src/Lumineux.Application/Setup/GetSetupStatusHandler.cs`
- [X] T005 [US1] Enregistrer `GetSetupStatusHandler` dans le DI Application (`AddScoped`) dans `src/Lumineux.Application/DependencyInjection.cs`
- [X] T006 [US1] Ajouter l'endpoint `GET setup/status` (`[AllowAnonymous]`, renvoie `SetupStatusResponse`, `[ProducesResponseType]` 200) dans `src/Lumineux.Api/Controllers/SetupController.cs`

**Checkpoint**: US1 fonctionnelle — le statut d'installation est consultable anonymement (débloque la découvrabilité SPA).

---

## Phase 4: Polish & Cross-Cutting Concerns

- [X] T007 [P] Vérifier la cohérence des annotations Swagger (`ProducesResponseType` 200, endpoint anonyme) avec `specs/012-setup-status-endpoint/contracts/openapi.yaml` dans `src/Lumineux.Api/Controllers/SetupController.cs`
- [X] T008 Exécuter la validation `quickstart.md` (scénarios A→E) et confirmer SC-001..SC-004
- [X] T009 [P] Revue sécurité : réponse **strictement booléenne** (aucune énumération, SC-003) ; accès **anonyme** (SC-001) ; **verrou d'installation intact** — `POST setup/first-admin` toujours refusé si installé (SC-004)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup / Foundational** : aucune tâche.
- **US1 (Phase 3)** : T001/T002 (tests) avant T003–T006. T003 (DTO) et T004 (handler) avant T006
  (endpoint) ; T005 (DI) avant T006.
- **Polish (Phase 4)** : après US1.

### Parallel Opportunities

- **US1** : T001 et T002 (tests, fichiers distincts) en parallèle avant l'implémentation.
- T003 (DTO) et T004 (handler) modifient des fichiers distincts → parallélisables, mais T004 dépend
  du DTO seulement pour le type de retour ; séquencer T003 → T004 par simplicité.
- **Polish** : T007 et T009 en parallèle.

---

## Implementation Strategy

### MVP (US1)

1. Tests (T001/T002) — rouge.
2. DTO → handler → DI → endpoint (T003–T006) — vert.
3. **STOP & VALIDATE** : `GET setup/status` anonyme reflète le verrou.

### Notes

- [P] = fichiers différents, aucune dépendance ; [Story] = traçabilité US.
- Lecture seule, anonyme ; **ne pas** modifier le verrou d'installation ni `InstallFirstAdminHandler`.
- Vérifier que les tests échouent avant d'implémenter ; commit après chaque tâche ou groupe logique.
