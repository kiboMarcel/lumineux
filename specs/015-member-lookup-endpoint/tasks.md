---
description: "Task list — Recherche membre allégée (member lookup)"
---

# Tasks: Recherche membre allégée (member lookup)

**Input**: Design documents from `specs/015-member-lookup-endpoint/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUS — Constitution Lumineux v1.0.0, Principe III. Unitaire Application ; intégration API
(harnais SQLite existant).

**Organization**: Une seule user story (P1). Extension Onion, **lecture seule**, **sans migration** ;
réutilise `IMemberRepository.SearchAsync`. Contrôle d'accès **any-of** (`manage_attendance` OU
`manage_members`) porté par le handler.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1 (phase de user story)
- Chaque tâche indique le chemin de fichier exact

---

## Phase 1: Setup

**Aucune tâche** : extension d'une solution existante ; aucune dépendance, configuration ou migration.

---

## Phase 2: Foundational

**Aucune tâche** : la recherche `IMemberRepository.SearchAsync` (feature 002) est **réutilisée telle
quelle** ; rien à préparer avant la user story.

---

## Phase 3: User Story 1 — Retrouver un membre pour l'identifier (Priority: P1) 🎯 MVP

**Goal**: Endpoint `GET /api/v1/members/lookup?query=…` renvoyant une liste courte d'entrées minimales
(id, référence, nom complet, statut), accessible à `manage_attendance` OU `manage_members`.

**Independent Test**: Avec un jeton `manage_attendance`, `GET members/lookup?query=Doe` → 200 + entrées
minimales (sans coordonnée) ; sans l'un des deux droits → 403 ; terme absent → 400.

### Tests for User Story 1 ⚠️ (écrire d'abord)

- [X] T001 [P] [US1] Tests unitaires `LookupMembersTests` (refus `ForbiddenException` sans `manage_attendance` NI `manage_members` ; refus `ValidationException`/400 si terme vide ; projection **minimale** (id/référence/nom/statut, aucune coordonnée) ; **plafond** de taille appliqué à `SearchAsync`) dans `tests/Lumineux.Application.Tests/LookupMembersTests.cs`
- [X] T002 [P] [US1] Tests d'intégration `MemberLookupEndpointTests` (`GET /api/v1/members/lookup?query=…` : 200 avec jeton `manage_attendance` renvoyant des champs **minimaux** ; **403** avec un jeton sans aucun des deux droits ; **401** sans jeton ; **400** si `query` absent/vide) dans `tests/Lumineux.Api.Tests/MemberLookupEndpointTests.cs`

### Implementation for User Story 1

- [X] T003 [US1] Ajouter le DTO `MemberLookupResponse(int Id, string Reference, string FullName, string Status)` dans `src/Lumineux.Application/Contracts/Members/MemberQueryDtos.cs`
- [X] T004 [US1] Implémenter `LookupMembersHandler` (contrôle **any-of** via `ICurrentUser` — `manage_attendance` OU `manage_members`, sinon `ForbiddenException` ; **terme requis** sinon refus 400 ; `IMemberRepository.SearchAsync(query, page:1, pageSize:plafond)` ; projection vers `MemberLookupResponse`) dans `src/Lumineux.Application/Members/LookupMembersHandler.cs`
- [X] T005 [US1] Enregistrer `LookupMembersHandler` dans le DI Application (`AddScoped`) dans `src/Lumineux.Application/DependencyInjection.cs`
- [X] T006 [US1] Créer `MemberLookupController` (`[Authorize]`, route `api/v1/members/lookup`, `GET` avec `[FromQuery] string query` ; `[ProducesResponseType]` 200 `IEnumerable<MemberLookupResponse>` / 400 / 401 / 403) dans `src/Lumineux.Api/Controllers/MemberLookupController.cs`

**Checkpoint**: US1 fonctionnelle — un opérateur de présence identifie un membre (débloque l'ajout manuel du Lot 4).

---

## Phase 4: Polish & Cross-Cutting Concerns

- [X] T007 [P] Vérifier la cohérence des annotations Swagger (`ProducesResponseType` 200/400/401/403, paramètre `query` requis) avec `specs/015-member-lookup-endpoint/contracts/openapi.yaml` dans `src/Lumineux.Api/Controllers/MemberLookupController.cs`
- [X] T008 Exécuter la validation `quickstart.md` (scénarios A→E) et confirmer SC-001..SC-005
- [X] T009 [P] Revue sécurité : **champs minimaux** (aucune coordonnée, SC-002) ; **terme requis** (SC-003) ; **accès any-of** serveur (SC-004) ; **résultats plafonnés** (SC-005) ; recherche complète `manage_members` inchangée

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup / Foundational** : aucune tâche.
- **US1 (Phase 3)** : T001/T002 (tests) avant T003–T006. T003 (DTO) et T004 (handler) avant T006 (endpoint) ; T005 (DI) avant T006.
- **Polish (Phase 4)** : après US1.

### Parallel Opportunities

- **US1** : T001 et T002 (tests, fichiers distincts) en parallèle avant l'implémentation.
- **Polish** : T007 et T009 en parallèle.

---

## Implementation Strategy

### MVP (US1)

1. Tests (T001/T002) — rouge.
2. DTO → handler → DI → endpoint (T003–T006) — vert.
3. **STOP & VALIDATE** : recherche allégée accessible aux opérateurs de présence, champs minimaux.

### Notes

- [P] = fichiers différents, aucune dépendance ; [Story] = traçabilité US.
- Lecture seule ; **ne pas** modifier la recherche complète (`MembersController`) ni exposer de données superflues.
- Vérifier que les tests échouent avant d'implémenter ; commit après chaque tâche ou groupe logique.
