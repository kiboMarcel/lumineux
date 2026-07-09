---
description: "Task list — Annulation d'une session de présence vide (feature 028)"
---

# Tasks: Annulation d'une session de présence vide

**Input**: Design documents from `specs/028-cancel-empty-session/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUS — la Constitution (Principe III, NON-NÉGOCIABLE) impose des tests unitaires sur le Domain
et l'Application, écrits avant/conjointement à l'implémentation (rouge → vert). Tests de domaine, de handler
(dont **concurrence** et **refus non-vide**), d'endpoint et SPA.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, sans dépendance sur une tâche incomplète)
- **[Story]** : US1 / US2 (voir spec.md) — absent en Setup / Foundational / Polish
- Chemins **exacts** (racine repo)

## Path Conventions

API .NET (Onion) sous `src/` + tests sous `tests/` ; SPA Angular sous `web/`. Aucune app mobile.

---

## Phase 1: Setup

**Purpose**: Prérequis (aucune nouvelle dépendance).

- [X] T001 Vérifier la baseline verte avant modifications : `dotnet build` (solution) et `cd web && npm run build` réussissent.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Primitives de domaine + persistance partagées par US1 et US2 (état « annulée », migration).

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [X] T002 Ajouter la valeur `Cancelled = 2` à l'énumération dans `src/Lumineux.Domain/Enums/SessionStatus.cs` (data-model.md §2).
- [X] T003 [P] Écrire les tests de domaine dans `tests/Lumineux.Domain.Tests/AttendanceSessionTests.cs` : `Cancel(cancelledBy, nowUtc)` sur une session **ouverte** → `Status=Cancelled`, `CancelledByMemberId`/`CancelledAt` renseignés ; sur une session **clôturée** ou **déjà annulée** → `ConflictException` (FR-005/FR-010).
- [X] T004 Ajouter au domaine la méthode `Cancel(int cancelledByMemberId, DateTime nowUtc)` (garde `Status == Open`, sinon `ConflictException`) et les propriétés `CancelledByMemberId` (int?) / `CancelledAt` (DateTime?) dans `src/Lumineux.Domain/Entities/AttendanceSession.cs` (data-model.md §1, research.md D1).
- [X] T005 Mapper les deux colonnes d'audit (`CancelledByMemberId`, `CancelledAt`, nullables) dans `src/Lumineux.Infrastructure/Persistence/Configurations/AttendanceSessionConfiguration.cs`.
- [X] T006 Générer la migration EF Core **additive** `CancelSession` (2 colonnes nullables ; l'`int Status` accueille la nouvelle valeur d'enum sans changement de type) sous `src/Lumineux.Infrastructure/Persistence/Migrations/` ; vérifier qu'elle est rejouable sur base vierge (research.md D8, Principe II).

**Checkpoint**: Domaine + schéma prêts — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Annuler une session ouverte vide (Priority: P1) 🎯 MVP

**Goal**: Un membre bureau annule une session **ouverte sans présence valide** (confirmation) ; elle passe à
« annulée », disparaît des vues actives, l'action est tracée.

**Independent Test**: démarrer une session, n'ajouter personne, annuler (avec confirmation) → session annulée
(< 5 s), absente de la reprise/listes/rapports.

### Tests for User Story 1 ⚠️ (écrire d'abord, doivent échouer)

- [X] T007 [P] [US1] Tests du handler (succès) dans `tests/Lumineux.Application.Tests/CancelSessionTests.cs` (doubles de repo) : session **introuvable** → `NotFoundException` ; session **non ouverte** → `ConflictException` ; session **ouverte vide** (`CountValidBySessionAsync`=0) → `Cancel` appelé, `SaveChanges`, **audit** émis (FR-001/FR-005/FR-006/FR-009).
- [X] T008 [P] [US1] Tests d'endpoint (succès) dans `tests/Lumineux.Api.Tests/SessionEndpointsTests.cs` : `POST /attendance-sessions/{id}/cancel` → **200** `SessionResponse` (`status="Cancelled"`) pour une session vide ; **404** si introuvable.
- [X] T009 [P] [US1] Test SPA (succès) dans `web/src/app/features/attendance/session-run/session-run.component.spec.ts` : bouton **« Annuler la session »** présent quand 0 présent valide ; au clic confirmé, appelle `cancel()` et redirige hors du suivi.

### Implementation for User Story 1

- [X] T010 [US1] Créer `CancelSessionHandler` dans `src/Lumineux.Application/AttendanceSessions/CancelSessionHandler.cs` : charger la session (`IAttendanceSessionRepository.GetByIdAsync`, sinon `NotFoundException`) ; `Cancel(_user.MemberId, _clock.UtcNow)` (garde ouverte) ; **contrôle `CountValidBySessionAsync == 0`** ; `SaveChangesAsync` ; `IAuditLogger.Operation("CancelSession", …)`. Retourne `SessionResponse` (via `SessionMapping`). (research.md D2/D4)
- [X] T011 [US1] Enregistrer le handler : `services.AddScoped<CancelSessionHandler>();` dans `src/Lumineux.Application/DependencyInjection.cs`.
- [X] T012 [US1] Ajouter l'endpoint `[HttpPost("{sessionId:int}/cancel")]` (injecter le handler dans le constructeur) dans `src/Lumineux.Api/Controllers/AttendanceSessionsController.cs` → **200** `SessionResponse` (contracts/cancel-session-api.md).
- [X] T013 [P] [US1] Ajouter `cancel(sessionId: number): Observable<SessionResponse>` (`POST {base}/{id}/cancel`) dans `web/src/app/core/api/attendance-sessions-api.ts`.
- [X] T014 [US1] Ajouter le bouton **« Annuler la session »** (visible si session ouverte **et** 0 présent valide), la **confirmation** explicite, l'appel `cancel()` et la **redirection** au succès, dans `web/src/app/features/attendance/session-run/session-run.component.ts` (contracts/cancel-session-ui.md §1/§3/§4).

**Checkpoint**: US1 fonctionnelle — annulation d'une session vide de bout en bout (MVP).

---

## Phase 4: User Story 2 — Empêcher l'annulation d'une session non vide (Priority: P1)

**Goal**: L'annulation est **refusée** dès qu'au moins une présence valide existe, **sans** perte ; garde
robuste face à un ajout **concurrent**.

**Independent Test**: sur une session avec ≥ 1 présence, tenter l'annulation → **refus** avec message clair,
session intacte ; le bouton n'est pas proposé côté UI.

### Tests for User Story 2 ⚠️ (écrire d'abord, doivent échouer)

- [X] T015 [P] [US2] Tests du handler (refus/concurrence) dans `tests/Lumineux.Application.Tests/CancelSessionTests.cs` : session ouverte **avec ≥ 1 présence valide** → `ConflictException` (« contient des présences »), `Cancel` **non** persisté, aucune présence touchée (FR-002/FR-003/FR-008) ; **concurrence** : une présence valide insérée pendant l'annulation (simulation d'échec de sérialisation ou re-lecture > 0) → **refus**, état final cohérent (jamais « annulée avec présence ») (FR-004/SC-003) ; présence **ajoutée puis annulée** (`CountValid`=0) → annulation **autorisée** (Edge Case).
- [X] T016 [P] [US2] Test d'endpoint (refus) dans `tests/Lumineux.Api.Tests/SessionEndpointsTests.cs` : session non vide → **409** (message « contient des présences ») ; session non ouverte → **409** (« n'est pas ouverte ») ; sans droit → **403** (tentative **consignée**).
- [X] T017 [P] [US2] Test SPA (refus) dans `web/src/app/features/attendance/session-run/session-run.component.spec.ts` : bouton **absent** dès qu'une présence valide existe ; sur **409**, message serveur affiché et maintien sur l'écran (+ rafraîchissement).

### Implementation for User Story 2

- [X] T018 [US2] Renforcer `CancelSessionHandler` : exécuter le contrôle **et** la bascule d'état dans une **transaction sérialisable** (`IsolationLevel.Serializable`) — `CountValidBySessionAsync == 0` sous verrou de plage, sinon `ConflictException` distinct « contient des présences » ; gérer un **échec de sérialisation** concurrent (→ 409/refus, pas d'annulation) ; message **distinct** « non ouverte » vs « non vide ». Alternative acceptable : **UPDATE conditionnel atomique** (`WHERE Status=Open AND NOT EXISTS présence valide`). Fichier `src/Lumineux.Application/AttendanceSessions/CancelSessionHandler.cs` (research.md D2, FR-004/SC-003).
- [X] T019 [US2] Tracer les **refus métier** vus par le handler via `_audit.Refused("CancelSession", raison, new { sessionId })` (raisons « non ouverte » / « contient des présences »), sur le modèle de `ScanAttendanceHandler`. NB : le **403 de la policy `[Authorize]`** (droit manquant) survient **avant** le handler → il relève d'un **mécanisme d'audit d'autorisation global** (pipeline), pas de cette tâche : **vérifier** qu'il existe ; s'il est absent, le créer est un **item transverse** hors périmètre de cette feature (à signaler), pas un blocage. Fichier `src/Lumineux.Application/AttendanceSessions/CancelSessionHandler.cs` (FR-009, Principe VI).
- [X] T020 [US2] Masquer/désactiver le bouton **« Annuler la session »** dès qu'au moins une présence valide est présente, et gérer le **409** (afficher `messageForError`, rester sur l'écran, rafraîchir la liste), dans `web/src/app/features/attendance/session-run/session-run.component.ts` (contracts/cancel-session-ui.md §1/§4).

**Checkpoint**: US1 + US2 — annulation possible **uniquement** sur session vide, sans perte, avec garde concurrente.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Vérifications transverses et validation finale.

- [X] T021 [P] Vérifier qu'aucune requête/vue existante n'inclut par erreur les sessions `Cancelled` comme actives (reprise 023, garde d'ouverture, auto-clôture, rapports) — revue de `Status == Open` (research.md D5).
- [X] T022 [P] Documenter l'endpoint dans le contrat OpenAPI (annotations `ProducesResponseType` 200/404/409/403) — cohérence Principe V.
- [X] T023 Exécuter la validation `specs/028-cancel-empty-session/quickstart.md` (scénarios A→E).
- [X] T024 `dotnet test` (Domain + Application + Api) et `cd web && npm test` verts ; `dotnet build` + `npm run build` OK.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance.
- **Foundational (Phase 2)** : dépend du Setup — **BLOQUE** US1 et US2 (état + migration).
- **US1 (Phase 3)** : dépend de la Fondation. Livrable MVP autonome (annulation d'une session vide).
- **US2 (Phase 4)** : dépend de la Fondation ; **réutilise et renforce** `CancelSessionHandler` créé en US1 (T010) — la garde de refus + la transaction concurrente (T018) s'y ajoutent. Testable indépendamment (session pré-remplie d'une présence).
- **Polish (Phase 5)** : après US1 + US2.

### User Story Dependencies

- **US1 (P1)** : après Fondation ; aucune dépendance sur US2.
- **US2 (P1)** : après Fondation ; partage le fichier `CancelSessionHandler.cs` avec US1 (T010 → T018 séquentiels) et le fichier `session-run.component.ts` (T014 → T020 séquentiels).

### Within Each User Story

- Tests écrits d'abord et **en échec** avant implémentation (Principe III).
- Domaine → handler → endpoint → SPA.

### Parallel Opportunities

- Phase 2 : T003 (tests domaine) [P] en parallèle de la lecture ; T004/T005/T006 s'enchaînent (domaine → config → migration).
- US1 : tests T007/T008/T009 [P] ensemble ; T013 (SPA api) [P] ; T010→T012 séquentiels (handler → DI → endpoint), T014 après T013.
- US2 : tests T015/T016/T017 [P] ensemble ; T018 (même fichier que T010) séquentiel ; T020 (même fichier que T014) séquentiel.

---

## Parallel Example: User Story 1

```bash
# Tests US1 d'abord (rouge) :
Task: "T007 handler succès dans CancelSessionTests.cs"
Task: "T008 endpoint 200/404 dans SessionEndpointsTests.cs"
Task: "T009 bouton + succès dans session-run.component.spec.ts"

# Puis briques à fichiers distincts :
Task: "T013 cancel() dans attendance-sessions-api.ts"
```

---

## Implementation Strategy

### MVP First (US1)

1. Setup → 2. Foundational (enum + domaine + migration) → 3. US1 (handler + endpoint + SPA).
4. **STOP & VALIDATE** : annuler une session vide (Scénario A). MVP démontrable.

### Incremental Delivery

1. Fondation prête. 2. US1 → annulation d'une session vide (démo). 3. US2 → garde anti-perte + concurrence (démo).

---

## Notes

- **Empreinte minimale** : `CountValidBySessionAsync` **existe déjà** ; l'exclusion des vues actives est
  **acquise** (filtres `Status == Open`). Seul le schéma évolue (migration additive).
- **Sécurité** : endpoint `manage_attendance` ; serveur autorité ; audit des annulations et refus.
- Distinct de la **clôture** et de l'**annulation d'une présence** (existantes) — ne pas confondre les libellés.
- Commit après chaque tâche ou groupe logique ; s'arrêter aux checkpoints pour valider par story.
