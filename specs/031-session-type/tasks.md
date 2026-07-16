# Tasks: Type de session de présence

**Input**: Design documents from `/specs/031-session-type/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/sessions-api.md, quickstart.md

**Tests**: INCLUS — Constitution Principe III (NON-NÉGOCIABLE) : tests Domain/Application au même
changement, écrits avant l'implémentation (rouge → vert).

**Organization**: tâches groupées par user story. La fondation (Phase 2) est bloquante et partagée.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]**: US1 / US2 (voir spec.md)

## Path Conventions

Web app (.NET Onion + SPA Angular) : API sous `src/`, tests sous `tests/`, SPA sous `web/src/`.

---

## Phase 1: Setup

- [X] T001 Vérifier la base verte avant modification : `dotnet test` (4 projets) et `npm test` (`web/`) passent tels quels (référence de non-régression).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: enum, propriété immuable, colonne rétro-remplie et lecture partagée — prérequis de TOUTES les stories.

**⚠️ CRITICAL**: aucune story ne peut être implémentée avant la fin de cette phase.

- [X] T002 [P] Créer l'enum `SessionType { AntennaMeeting = 0, Teaching = 1 }` dans `src/Lumineux.Domain/Enums/SessionType.cs` (doc XML, calqué sur `SessionStatus.cs`).
- [X] T003 Ajouter à `src/Lumineux.Domain/Entities/AttendanceSession.cs` la propriété `public SessionType SessionType { get; private set; }` et un paramètre `SessionType sessionType = SessionType.AntennaMeeting` à la fabrique `Start` (assigné dans l'objet créé). Aucune autre méthode ne le modifie (immuabilité, FR-006).
- [X] T004 Configurer la colonne dans `src/Lumineux.Infrastructure/Persistence/Configurations/AttendanceSessionConfiguration.cs` : `builder.Property(x => x.SessionType).HasColumnName("session_type").HasConversion<string>().HasMaxLength(20).IsRequired().HasDefaultValue(SessionType.AntennaMeeting);` (calqué sur `Status`).
- [X] T005 Générer la migration additive : `dotnet ef migrations add SessionType -p src/Lumineux.Infrastructure -s src/Lumineux.Infrastructure -o Persistence/Migrations` → vérifier qu'elle ne contient qu'un `AddColumn` `session_type nvarchar(20) NOT NULL` avec `defaultValue: "AntennaMeeting"` sur `attendance_sessions` ; commiter le fichier généré.
- [X] T006 [P] Ajouter `string SessionType` au DTO `SessionResponse` dans `src/Lumineux.Application/Contracts/Sessions/SessionDtos.cs` (dernier champ, après `AttendanceCount`).
- [X] T007 [P] Test de mapping (écrire AVANT l'impl T009, doit échouer) : `SessionResponse.SessionType` reflète `AttendanceSession.SessionType` (AntennaMeeting et Teaching) — dans `tests/Lumineux.Application.Tests/StartSessionTests.cs` (ou nouveau `SessionTypeMappingTests.cs`).
- [X] T008 [P] Ajouter `sessionType: string;` à l'interface `SessionResponse` dans `web/src/app/features/attendance/attendance.models.ts` (contrat, non affiché — API-only).
- [X] T009 Mapper le type dans `src/Lumineux.Application/AttendanceSessions/SessionMapping.cs` (`ToResponse` : `s.SessionType.ToString()`) — fait passer T007.

**Checkpoint**: le type existe de bout en bout en lecture ; les stories peuvent démarrer.

---

## Phase 3: User Story 1 - Toute session porte un type, défaut AntennaMeeting (Priority: P1) 🎯 MVP

**Goal**: chaque session (existante et nouvelle sans type) est `AntennaMeeting` et expose son type ; comportement inchangé.

**Independent Test**: démarrer une session sans préciser de type et consulter une session préexistante → les deux `AntennaMeeting` ; le type figure dans les données de session.

### Tests for User Story 1 ⚠️ (écrire AVANT, doivent échouer)

- [X] T010 [P] [US1] Test domaine : `AttendanceSession.Start` sans type → `SessionType.AntennaMeeting` ; `Close`/`Cancel`/`AutoClose` ne modifient pas le type (immuabilité) — dans `tests/Lumineux.Domain.Tests/AttendanceSessionTests.cs`.
- [X] T011 [P] [US1] Test endpoint : `POST /api/v1/attendance-sessions` sans `sessionType` → 201 et `sessionType == "AntennaMeeting"` ; `GET` de la session expose le type — dans `tests/Lumineux.Api.Tests/AttendanceSessionsEndpointsTests.cs`.
- [X] T012 [P] [US1] Test infrastructure : la colonne `session_type` persiste et relit la valeur, défaut `AntennaMeeting` appliqué — dans `tests/Lumineux.Infrastructure.Tests/OpenSessionsByOpenerTests.cs` (ou nouveau `SessionTypePersistenceTests.cs`).

### Implementation for User Story 1

- [X] T013 [US1] Confirmer que `StartSessionHandler` (`src/Lumineux.Application/AttendanceSessions/StartSessionHandler.cs`) passe le défaut à la fabrique quand aucun type n'est fourni (aucun changement de comportement ; ajuster l'appel `AttendanceSession.Start(...)` si nécessaire pour rester compilant après T003).

**Checkpoint**: sessions par défaut et existantes typées `AntennaMeeting`, lisibles.

---

## Phase 4: User Story 2 - Démarrer une session en précisant sa nature (Priority: P2)

**Goal**: démarrer avec un type explicite (réunion / enseignement) parmi l'ensemble fermé ; type inconnu refusé ; type immuable.

**Independent Test**: démarrer avec `Teaching` → conservé ; démarrer avec un type inconnu → refus sans création.

### Tests for User Story 2 ⚠️ (écrire AVANT, doivent échouer)

- [X] T014 [P] [US2] Test handler : `StartSessionHandler` démarre une session avec le type fourni (`Teaching`) et le conserve — dans `tests/Lumineux.Application.Tests/StartSessionTests.cs`.
- [X] T015 [P] [US2] Test validation : `StartSessionValidator` accepte `AntennaMeeting`/`Teaching` (et l'absence) et rejette un type inconnu (message clair) — dans `tests/Lumineux.Application.Tests/StartSessionTests.cs`.
- [X] T016 [P] [US2] Test endpoint : `POST /api/v1/attendance-sessions` avec `"sessionType":"Teaching"` → 201 et type conservé ; avec un type inconnu → 400 sans création — dans `tests/Lumineux.Api.Tests/AttendanceSessionsEndpointsTests.cs`.

### Implementation for User Story 2

- [X] T017 [US2] Ajouter `string? SessionType = null` au DTO `StartSessionRequest` dans `src/Lumineux.Application/Contracts/Sessions/SessionDtos.cs` (paramètre optionnel).
- [X] T018 [US2] Ajouter à `src/Lumineux.Application/AttendanceSessions/StartSessionValidator.cs` une règle : si `SessionType` est fourni, il DOIT correspondre à une valeur d'enum reconnue (`Enum.TryParse<SessionType>` sensible à la casse), sinon message clair « Type de session inconnu ».
- [X] T019 [US2] Dans `src/Lumineux.Application/AttendanceSessions/StartSessionHandler.cs`, convertir le type validé (ou défaut `AntennaMeeting` si absent) et le passer à `AttendanceSession.Start(...)` ; adjoindre le type à la charge d'audit `StartSession` (valeur non sensible).

**Checkpoint**: US1 et US2 fonctionnent indépendamment.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T020 [P] Mettre à jour la doc du modèle de données `docs/audit/03-modele-donnees.md` : ajouter la colonne `session_type` à l'entité ATTENDANCE_SESSIONS + noter la migration `SessionType`.
- [X] T021 [P] Vérifier la non-régression des parcours de session existants (SC-004) : les suites `CloseSession*`, `CancelSession*`, `MyOpenSessions*`, `SessionAutoClose*` restent vertes sans modification (ajouter au besoin une assertion que le type est inchangé après clôture/annulation).
- [X] T022 Exécuter la suite complète : `dotnet test` (Domain/Application/Infrastructure/Api) et `npm test` (`web/`) tous verts.
- [ ] T023 Dérouler `specs/031-session-type/quickstart.md` (vérifications API), y compris `dotnet ef database update` sur la base de dev.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: aucune dépendance.
- **Foundational (Phase 2)**: après Setup — **BLOQUE** toutes les stories. Ordre : T002 (enum) → T003 (entité) → T004 (config EF) → T005 (migration) ; T006 avant T007 (compile) ; **T007 test avant T009 impl** ; T008 indépendant.
- **US1 / US2 (Phases 3–4)**: après Phase 2. Indépendantes ; parallélisables si équipe.
- **Polish (Phase 5)**: après les stories.

### Within Each User Story

- Tests écrits et **en échec** avant l'implémentation (Principe III).
- Enum/entité → config/migration → DTO/validator/handler.
- US1 et US2 modifient toutes deux `StartSessionHandler.cs` (T013 puis T019) et `SessionDtos.cs` (T006 puis T017) → **séquentiel** sur ces fichiers, non `[P]` entre stories.

### Parallel Opportunities

- T002, T006, T007, T008 parallélisables (fichiers distincts) ; T009 après T006/T007.
- Tâches de test `[P]` de chaque story s'écrivent en parallèle.
- Attention : `StartSessionHandler.cs` (T013, T019), `StartSessionValidator.cs` (T018), `SessionDtos.cs` (T006, T017) sont partagés → séquencer.

---

## Implementation Strategy

### MVP (User Story 1 seule)

Phase 1 → Phase 2 (fondation critique) → Phase 3 (US1) → **STOP & VALIDATE** : démarrer une session (défaut AntennaMeeting) + relire une session existante typée.

### Livraison incrémentale

Foundational → US1 (MVP, défaut + lecture) → US2 (type explicite + refus inconnu). Chaque story ajoute de la valeur sans casser la précédente.

---

## Notes

- Migration : **NOT NULL avec défaut `AntennaMeeting`** (rétro-remplit l'existant) — différence clé avec la 030 (profession nullable).
- Sécurité : ensemble fermé validé serveur (Principe IV) ; type non sensible (audit possible) ; immuable (FR-006).
- API-only : SPA reçoit `sessionType` dans le contrat mais **aucun composant modifié** ; l'écran de démarrage produit toujours `AntennaMeeting`.
- Déploiement : appliquer la migration (`dotnet ef database update`) — dette de déploiement habituelle, tracée en T023.
- Commiter après chaque groupe logique ; ne pas pousser sans accord explicite.
