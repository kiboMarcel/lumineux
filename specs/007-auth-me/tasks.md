---
description: "Task list — Profil de l'utilisateur courant (auth/me)"
---

# Tasks: Profil de l'utilisateur courant (auth/me)

**Input**: Design documents from `specs/007-auth-me/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUS — imposés par la Constitution Lumineux v1.0.0, Principe III.
Unitaires Application ; intégration API (harnais existant).

**Organization**: Tâches regroupées par user story (US1→US2). Extension de la solution Onion
existante (features 001→006), **sans persistance ni migration**. US1 (lecture du profil) livre
l'endpoint ; US2 (refus de session) réutilise le même endpoint et couvre le chemin 401. Chaque
story reste testable indépendamment.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US2 (uniquement pour les phases de user story)
- Chaque tâche indique le chemin de fichier exact

---

## Phase 1: Setup

**Aucune tâche** : extension d'une solution déjà initialisée ; aucune nouvelle dépendance, aucune
configuration (`appsettings`/options), aucune migration.

**Checkpoint**: Rien à préparer — passage direct à la phase Foundational.

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: Rendre les droits de la session **énumérables** via l'abstraction de contexte
utilisateur — prérequis commun aux deux user stories.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [X] T001 Étendre le port `ICurrentUser` avec la liste des permissions de la session (`IReadOnlyCollection<string> Permissions { get; }`, jamais `null`) dans `src/Lumineux.Application/Abstractions/ICurrentUser.cs`
- [X] T002 Implémenter `Permissions` dans `CurrentUser` (énumère les claims de type `permission` du `ClaimsPrincipal` ; retourne une collection vide si aucun) dans `src/Lumineux.Api/Security/CurrentUser.cs`

**Checkpoint**: Le contexte de session expose identité + droits — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Connaître mon identité et mes droits de session (Priority: P1) 🎯 MVP

**Goal**: Endpoint authentifié `GET /api/v1/auth/me` qui renvoie `{ memberId, displayName,
permissions[] }` dérivé de la session, sans aucun secret ni accès base.

**Independent Test**: Se connecter, appeler `/auth/me` avec le jeton, vérifier que l'identité et
l'ensemble **exact** des droits du jeton sont renvoyés, sans aucune donnée secrète.

### Tests for User Story 1 ⚠️ (écrire d'abord, doivent échouer)

- [X] T003 [P] [US1] Tests unitaires `GetCurrentUserTests` (mapping identité `memberId`/`displayName` + droits depuis `ICurrentUser` ; liste de droits **vide** gérée ; garde défensive : `MemberId` nul → `UnauthorizedException` + refus journalisé) dans `tests/Lumineux.Application.Tests/GetCurrentUserTests.cs`
- [X] T004 [P] [US1] Tests d'intégration `AuthMeEndpointsTests` (200 avec jeton : corps `memberId`/`displayName`/`permissions` ; **aucun** secret dans le corps `passwordHash`/`token` ; **égalité stricte** des droits annoncés et des droits réellement autorisés par l'API, cf. quickstart §B ; **idempotence (G1)** : deux appels successifs avec le **même** jeton renvoient un corps **identique**, confirmant l'absence d'effet de bord — FR-008) dans `tests/Lumineux.Api.Tests/AuthMeEndpointsTests.cs`

### Implementation for User Story 1

- [X] T005 [P] [US1] Créer le DTO `CurrentUserResponse(int MemberId, string DisplayName, IReadOnlyList<string> Permissions)` dans `src/Lumineux.Application/Contracts/Auth/AuthDtos.cs`
- [X] T006 [US1] Implémenter `GetCurrentUserHandler` (mappe `ICurrentUser` → `CurrentUserResponse` ; garde défensive : contexte authentifié mais `MemberId` nul → `UnauthorizedException` générique + `IAuditLogger.Refused` sans secret ; lecture pure, aucun accès base) dans `src/Lumineux.Application/Auth/GetCurrentUserHandler.cs`
- [X] T007 [US1] Enregistrer `GetCurrentUserHandler` dans le DI Application (`AddScoped`) dans `src/Lumineux.Application/DependencyInjection.cs`
- [X] T008 [US1] Ajouter l'endpoint `GET /api/v1/auth/me` (`[Authorize]`, aucun droit de gestion requis ; `[ProducesResponseType]` 200 `CurrentUserResponse` / 401) dans `src/Lumineux.Api/Controllers/AuthController.cs`

**Checkpoint**: US1 fonctionnelle — un utilisateur connecté lit son identité et ses droits.

---

## Phase 4: User Story 2 — Détecter une session absente ou expirée (Priority: P2)

**Goal**: Toute demande de `/auth/me` sans session valide (jeton absent, invalide ou expiré) est
refusée **uniformément** par un 401, sans divulguer la cause.

**Independent Test**: Appeler `/auth/me` sans jeton puis avec un jeton invalide/expiré ; vérifier un
401 identique dans les deux cas.

> Le comportement 401 est fourni par `[Authorize]` (endpoint d'US1) et la garde défensive du handler
> (T006). US2 n'ajoute **pas** de code de production ; son incrément est la **couverture de test** du
> chemin de refus et la confirmation du caractère uniforme du refus.

### Tests for User Story 2 ⚠️ (écrire d'abord, doivent échouer)

- [X] T009 [US2] Ajouter à `AuthMeEndpointsTests` les cas de refus (401 **sans** en-tête `Authorization` ; 401 avec **jeton invalide/expiré** ; refus **identique** dans les deux cas, sans divulgation de cause — format ProblemDetails) dans `tests/Lumineux.Api.Tests/AuthMeEndpointsTests.cs`

### Implementation for User Story 2

- [X] T010 [US2] Confirmer le refus uniforme : `[Authorize]` sur l'endpoint (T008) + garde défensive du handler (T006) → 401 via `ExceptionHandlingMiddleware` (`UnauthorizedException` → 401) ; vérifier qu'aucune cause précise n'est divulguée dans `src/Lumineux.Api/Controllers/AuthController.cs` et `src/Lumineux.Application/Auth/GetCurrentUserHandler.cs`
- [X] T010b [US2] **Observabilité du refus (D1, FR-009 / Constitution VI)** : confirmer que les 401 du middleware `[Authorize]` (jeton absent/invalide/expiré) sont **journalisés** par le logging HTTP structuré existant (Serilog request logging dans `src/Lumineux.Api/Program.cs`) — comportement **identique** aux autres endpoints protégés. Si le logging de requête n'est pas actif, l'activer ; sinon documenter que la traçabilité du 401 middleware repose sur ce canal (le chemin défensif domaine restant audité via `IAuditLogger`, T006). Aucune divergence introduite par rapport à l'existant.

**Checkpoint**: US1 + US2 opérationnelles — le bootstrap SPA sait « qui je suis » **ou** « je ne suis pas connecté ».

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T011 [P] Vérifier la cohérence des annotations Swagger (`ProducesResponseType` 200/401) avec `specs/007-auth-me/contracts/openapi.yaml` dans `src/Lumineux.Api/Controllers/AuthController.cs`
- [X] T012 Exécuter la validation `quickstart.md` de bout en bout (scénarios A→E) et confirmer les critères SC-001..SC-005
- [X] T013 [P] Revue sécurité : confirmer qu'aucun secret (empreinte, jeton, mot de passe) n'apparaît dans la réponse (SC-004) et que les droits annoncés = droits réellement autorisés (SC-002)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune tâche.
- **Foundational (Phase 2)** : **bloque** les deux user stories (T001 avant T002).
- **US1 (Phase 3)** : démarre après la Phase 2. T003/T004 (tests) avant T005/T006/T008. T005 (DTO) et
  T001 conditionnent T006 ; T006 + T007 avant T008.
- **US2 (Phase 4)** : démarre après US1 (réutilise l'endpoint). T009 (tests) ; T010 = vérification.
- **Polish (Phase 5)** : après US1 et US2.

### Within Each User Story

- Les tests (T003/T004, T009) sont écrits **avant** l'implémentation et doivent échouer d'abord.
- DTO avant handler ; handler + DI avant endpoint.

### Parallel Opportunities

- **US1** : T003, T004, T005 en parallèle (fichiers distincts). T006 dépend de T001 + T005.
- **Polish** : T011 et T013 en parallèle.
- ⚠️ T004 et T009 modifient le **même** fichier (`AuthMeEndpointsTests.cs`) → **non** parallélisables
  entre eux (séquencer US1 puis US2 sur ce fichier).

---

## Parallel Example: User Story 1

```bash
Task: "Tests unitaires GetCurrentUserTests dans tests/Lumineux.Application.Tests/GetCurrentUserTests.cs"
Task: "Tests d'intégration AuthMeEndpointsTests dans tests/Lumineux.Api.Tests/AuthMeEndpointsTests.cs"
Task: "Créer le DTO CurrentUserResponse dans src/Lumineux.Application/Contracts/Auth/AuthDtos.cs"
```

---

## Implementation Strategy

### MVP (US1)

1. Phase 2 : Foundational (extension `ICurrentUser` + implémentation) — **bloquant**.
2. Phase 3 : US1 (DTO, handler, DI, endpoint) → tester la lecture du profil.
3. **STOP & VALIDATE** : `/auth/me` renvoie identité + droits, sans secret.

### Livraison incrémentale

1. Phase 2 → socle prêt.
2. US1 → profil de session lisible (MVP démontrable pour le bootstrap SPA).
3. US2 → couverture du refus 401 (session absente/expirée).
4. Polish → Swagger, quickstart, revue sécurité.

### Notes

- [P] = fichiers différents, aucune dépendance ; [Story] = traçabilité US.
- Vérifier que les tests échouent avant d'implémenter (rouge → vert).
- Aucune donnée secrète ni donnée personnelle superflue dans la réponse (SC-004).
- Commit après chaque tâche ou groupe logique.
