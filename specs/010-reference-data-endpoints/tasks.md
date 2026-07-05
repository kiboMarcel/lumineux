---
description: "Task list — Endpoints de données de référence (nomenclatures)"
---

# Tasks: Endpoints de données de référence (nomenclatures)

**Input**: Design documents from `specs/010-reference-data-endpoints/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUS — Constitution Lumineux v1.0.0, Principe III. Unitaires Application ; intégration API
(harnais SQLite existant).

**Organization**: Tâches regroupées par user story (US1→US2). Extension de la solution Onion
existante, **lecture seule**, **sans migration**. Le socle partagé (DTO, port, repo, handler, DI) est
en phase Foundational ; US1 (antennes) et US2 (autres nomenclatures) ajoutent chacune leurs endpoints
et tests.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US2 (uniquement pour les phases de user story)
- Chaque tâche indique le chemin de fichier exact

---

## Phase 1: Setup

**Aucune tâche** : extension d'une solution existante ; aucune dépendance, configuration ou migration.

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: Plomberie partagée par les deux user stories : contrats de sortie, port de lecture,
implémentation d'accès aux données, cas d'usage de projection, enregistrement DI.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [X] T001 [P] Créer les DTO `ReferenceItemResponse(int Id, string Code, string Label)` et `CountryResponse(int Id, string Code, string Country, string Nationality)` dans `src/Lumineux.Application/Contracts/Reference/ReferenceDtos.cs`
- [X] T002 [P] Définir le port `IReferenceDataRepository` (méthodes `GetActiveAntennasAsync`, `GetActiveCivilitiesAsync`, `GetActiveCitiesAsync`, `GetActiveDistrictsAsync`, `GetActiveCountriesAsync`, renvoyant les entités actives triées) dans `src/Lumineux.Domain/Abstractions/IReferenceDataRepository.cs`
- [X] T003 Implémenter `ReferenceDataRepository` (`AppDbContext`, `AsNoTracking`, filtre `Status` actif, `OrderBy(Label)` — pays triés par libellé de pays) dans `src/Lumineux.Infrastructure/Repositories/ReferenceDataRepository.cs`
- [X] T004 Implémenter `GetReferenceDataHandler` (une méthode par nomenclature ; projette les entités vers les DTO ; nationalité distincte pour les pays) dans `src/Lumineux.Application/Reference/GetReferenceDataHandler.cs`
- [X] T005 Enregistrement DI : `IReferenceDataRepository` → `ReferenceDataRepository` dans `src/Lumineux.Infrastructure/DependencyInjection.cs` ; `GetReferenceDataHandler` (`AddScoped`) dans `src/Lumineux.Application/DependencyInjection.cs`

**Checkpoint**: Socle prêt — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Lister les antennes (Priority: P1) 🎯 MVP

**Goal**: Endpoint authentifié `GET /api/v1/reference/antennas` renvoyant les antennes **actives**
triées, pour peupler l'antenne d'origine du formulaire membre.

**Independent Test**: Avec un jeton, appeler `/reference/antennas` → 200 + liste `{id,code,label}`
active et triée ; sans jeton → 401.

### Tests for User Story 1 ⚠️ (écrire d'abord)

- [X] T006 [P] [US1] Tests unitaires `GetReferenceDataTests` : projection des antennes (mapping `id/code/label`, entrées actives, ordre par libellé) dans `tests/Lumineux.Application.Tests/GetReferenceDataTests.cs`
- [X] T007 [P] [US1] Tests d'intégration `ReferenceEndpointsTests` : `GET /reference/antennas` → 200 (liste active + triée, aucune donnée secrète) ; **401** sans jeton dans `tests/Lumineux.Api.Tests/ReferenceEndpointsTests.cs`

### Implementation for User Story 1

- [X] T008 [US1] Créer `ReferenceController` (`[Authorize]`, route `api/v1/reference`) avec `GET antennas` (`[ProducesResponseType]` 200 `IEnumerable<ReferenceItemResponse>` / 401) dans `src/Lumineux.Api/Controllers/ReferenceController.cs`

**Checkpoint**: US1 fonctionnelle — l'enrôlement du Lot 2 est débloqué (antenne sélectionnable).

---

## Phase 4: User Story 2 — Lister les autres nomenclatures (Priority: P2)

**Goal**: Endpoints authentifiés pour civilités, villes, districts et pays/nationalités (actifs,
triés), pour les champs optionnels de la fiche membre.

**Independent Test**: Avec un jeton, appeler chaque liste → 200 + entrées actives triées ; pour les
pays, `country` et `nationality` distincts.

### Tests for User Story 2 ⚠️ (écrire d'abord)

- [X] T009 [P] [US2] Étendre `GetReferenceDataTests` : projection des civilités/villes/districts et des **pays** (libellés pays **et** nationalité distincts) dans `tests/Lumineux.Application.Tests/GetReferenceDataTests.cs`
- [X] T010 [US2] Étendre `ReferenceEndpointsTests` : `GET /reference/{civilities|cities|districts|countries}` → 200 (actifs, triés ; pays avec nationalité) ; **semer** civilité/ville/district/pays de test (fixture ou test) dans `tests/Lumineux.Api.Tests/ReferenceEndpointsTests.cs` et `tests/Lumineux.Api.Tests/Infrastructure/ApiTestFixture.cs`

### Implementation for User Story 2

- [X] T011 [US2] Ajouter à `ReferenceController` les `GET civilities`, `cities`, `districts`, `countries` (200 / 401) dans `src/Lumineux.Api/Controllers/ReferenceController.cs`

**Checkpoint**: US1 + US2 — toutes les nomenclatures de la fiche membre sont exposées.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T012 [P] Vérifier la cohérence des annotations Swagger (`ProducesResponseType` 200/401) avec `specs/010-reference-data-endpoints/contracts/openapi.yaml` dans `src/Lumineux.Api/Controllers/ReferenceController.cs`
- [X] T013 Exécuter la validation `quickstart.md` (scénarios A→E) et confirmer SC-001..SC-005
- [X] T014 [P] Revue sécurité : entrées **actives** uniquement (SC-002), **401** sans jeton (SC-003), aucune donnée secrète (SC-005)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune tâche.
- **Foundational (Phase 2)** : **bloque** les user stories. Ordre : T001/T002 (parallélisables) →
  T003 (repo, dépend du port) → T004 (handler, dépend port+DTO) → T005 (DI).
- **US1 (Phase 3)** : après la Phase 2. T006/T007 (tests) avant T008.
- **US2 (Phase 4)** : après la Phase 2 ; réutilise repo/handler existants (déjà complets). T009/T010
  (tests) avant T011. T010/T011 partagent des fichiers avec T007/T008 → séquencer US1 puis US2.
- **Polish (Phase 5)** : après US1 et US2.

### Parallel Opportunities

- **Foundational** : T001 et T002 en parallèle.
- **US1** : T006 et T007 en parallèle (fichiers distincts) avant T008.
- ⚠️ T009/T010/T011 modifient les **mêmes** fichiers que T006/T007/T008 → **non** parallélisables
  entre stories (séquencer US1 → US2).

---

## Implementation Strategy

### MVP (US1)

1. Phase 2 : Foundational (DTO, port, repo, handler, DI) — **bloquant**.
2. Phase 3 : US1 (endpoint antennes + tests) → **STOP & VALIDATE** : antennes listées, enrôlement débloqué.

### Livraison incrémentale

1. Socle + US1 → antennes (débloque la création de membre du Lot 2).
2. US2 → civilités, villes, districts, pays/nationalités.
3. Polish → Swagger, quickstart, revue sécurité.

### Notes

- [P] = fichiers différents, aucune dépendance ; [Story] = traçabilité US.
- Lecture seule : aucun accès en écriture, aucune migration.
- Vérifier que les tests échouent avant d'implémenter ; commit après chaque tâche ou groupe logique.
