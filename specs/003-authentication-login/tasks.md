---
description: "Task list — Authentification et connexion des membres"
---

# Tasks: Authentification et connexion des membres

**Input**: Design documents from `specs/003-authentication-login/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUS — imposés par la Constitution Lumineux v1.0.0, Principe III.
Unitaires Domain/Application ; intégration API (harnais SQLite existant).

**Organization**: Tâches regroupées par user story (US1→US4). Extension de la solution Onion
existante (features 001/002) : `src/Lumineux.Domain|Application|Infrastructure|Api`, `tests/Lumineux.*.Tests`.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US4 (uniquement pour les phases de user story)
- Chaque tâche indique le chemin de fichier exact

---

## Phase 1: Setup

- [x] T001 [P] Ajouter la section de configuration `Auth` (`AccessTokenMinutes`=60, `MaxFailedAttempts`=5, `LockoutMinutes`=15, `PasswordMinLength`=8) dans `src/Lumineux.Api/appsettings.json`

**Checkpoint**: Configuration disponible.

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: Modèle, ports, émission de jeton et persistance requis par toutes les user stories.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [x] T002 Enrichir l'entité `MemberAccount` : champs `FailedAttempts`, `LockoutUntil`, `LastLoginAt` + méthodes `IsLockedOut`, `RegisterFailedLogin`, `RegisterSuccessfulLogin`, `ChangePassword`, `Activate` dans `src/Lumineux.Domain/Entities/MemberAccount.cs`
- [x] T003 [P] Créer l'entité `MemberPermission` (member, permission) dans `src/Lumineux.Domain/Entities/MemberPermission.cs`
- [x] T004 [P] Définir le port `ITokenIssuer` (émission JWT : membre + droits → jeton + expiration) dans `src/Lumineux.Application/Abstractions/ITokenIssuer.cs`
- [x] T005 [P] Définir le port `IMemberPermissionRepository` (`GetPermissionsAsync(memberId)`) dans `src/Lumineux.Domain/Abstractions/IMemberPermissionRepository.cs`
- [x] T006 Étendre `IMemberAccountRepository` : `GetByLoginIdAsync` (avec le membre chargé) et `SaveChangesAsync` dans `src/Lumineux.Domain/Abstractions/IMemberAccountRepository.cs`
- [x] T007 [P] Créer `AuthOptions` + helper `PasswordPolicy` (longueur/complexité) dans `src/Lumineux.Infrastructure/Security/AuthOptions.cs`
- [x] T008 Configuration EF : enrichir `MemberAccountConfiguration` (nouvelles colonnes) + `MemberPermissionConfiguration` (unicité `(member, permission)`) + `DbSet<MemberPermission>` dans `AppDbContext`
- [x] T009 Générer la **migration** (colonnes de sécurité sur `member_accounts` + table `member_permissions`) dans `src/Lumineux.Infrastructure/Persistence/Migrations/`
- [x] T010 [P] Implémenter `JwtTokenIssuer` (`ITokenIssuer`, réutilise `JwtOptions`) dans `src/Lumineux.Infrastructure/Security/JwtTokenIssuer.cs`
- [x] T011 [P] Implémenter `MemberPermissionRepository` et étendre `MemberAccountRepository` (`GetByLoginIdAsync`, `SaveChangesAsync`) dans `src/Lumineux.Infrastructure/Repositories/`
- [x] T012 Enregistrement DI (bind `AuthOptions`, `ITokenIssuer`, `IMemberPermissionRepository`, repos étendus) dans `src/Lumineux.Infrastructure/DependencyInjection.cs`
- [x] T012a **Amorçage minimal des droits (F1)** : au démarrage, accorder de façon **idempotente** les permissions d'un compte bureau initial défini en configuration (`Auth:Bootstrap:MemberReference` + `Auth:Bootstrap:Permissions`), afin que le système soit utilisable de bout en bout avant la future feature « profils du bureau ». Service d'amorçage dans `src/Lumineux.Infrastructure/Security/PermissionBootstrapper.cs` (ne fait rien si non configuré).

**Checkpoint**: Socle prêt — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Connexion et obtention d'un jeton (Priority: P1) 🎯 MVP

**Goal**: Connexion référence + mot de passe → jeton signé/expirant portant les droits ; refus
générique si invalide ; refus si compte non actif ; signal `password_change_required` si activation requise.

**Independent Test**: `POST /auth/login` → 200 + jeton (compte actif) ; 401 générique (invalide) ;
403 `password_change_required` (compte à activer).

### Tests for User Story 1 ⚠️

- [x] T013 [P] [US1] Tests unitaires Domain `MemberAccount` (`RegisterSuccessfulLogin` remet à zéro, `ChangePassword`, `Activate`) dans `tests/Lumineux.Domain.Tests/MemberAccountTests.cs`
- [x] T014 [P] [US1] Tests unitaires `LoginHandler` (valide→jeton+droits ; invalide→401 générique indistinguable ; compte non actif→refus ; `mustChangePassword`→password_change_required) dans `tests/Lumineux.Application.Tests/LoginTests.cs`
- [x] T015 [P] [US1] Test d'intégration `POST /api/v1/auth/login` (200/401/403) dans `tests/Lumineux.Api.Tests/AuthLoginEndpointsTests.cs`

### Implementation for User Story 1

- [x] T016 [P] [US1] DTO `LoginRequest`/`TokenResponse` dans `src/Lumineux.Application/Contracts/Auth/`
- [x] T017 [US1] Cas d'usage `LoginHandler` (recherche par référence — **normalisation : trim + comparaison insensible à la casse** [F5] ; contrôle verrouillage ; vérif. mot de passe + **hash factice anti-énumération** ; statut membre actif ; signal must-change ; `RegisterSuccessfulLogin`/`RegisterFailedLogin` ; émission jeton avec droits ; audit) dans `src/Lumineux.Application/Auth/LoginHandler.cs`
- [x] T018 [US1] `AuthController` : `POST /api/v1/auth/login` (anonyme) dans `src/Lumineux.Api/Controllers/AuthController.cs`

**Checkpoint**: US1 fonctionnelle (connexion + jeton) — MVP.

---

## Phase 4: User Story 2 — Première connexion : activation (Priority: P1)

**Goal**: Endpoint dédié référence + mot de passe temporaire + nouveau → active le compte, lève
l'obligation de changement, délivre un jeton.

**Independent Test**: `POST /auth/activate` → 200 + jeton (compte activé) ; temporaire erroné→401 ;
nouveau non conforme→400 ; compte déjà actif→409.

### Tests for User Story 2 ⚠️

- [x] T019 [P] [US2] Tests unitaires `ActivateAccountHandler` (temporaire valide→activation+jeton ; erroné→401 ; politique→400 ; déjà actif→409) dans `tests/Lumineux.Application.Tests/ActivateAccountTests.cs`
- [x] T020 [P] [US2] Test d'intégration `POST /api/v1/auth/activate` (200/400/401/409) dans `tests/Lumineux.Api.Tests/AuthActivateEndpointsTests.cs`

### Implementation for User Story 2

- [x] T021 [P] [US2] DTO `ActivateAccountRequest` + validator (politique de mot de passe) dans `src/Lumineux.Application/Contracts/Auth/` et `src/Lumineux.Application/Auth/ActivateAccountValidator.cs`
- [x] T022 [US2] Cas d'usage `ActivateAccountHandler` (vérif. temporaire **d'abord** ; politique ; `ChangePassword`+`Activate` ; verrouillage ; émission jeton ; audit). **Anti-énumération (F2)** : ne révéler « compte déjà activé » (409) **qu'après** vérification correcte du mot de passe temporaire ; sinon **401 générique**. Fichier `src/Lumineux.Application/Auth/ActivateAccountHandler.cs`
- [x] T023 [US2] Ajouter `POST /api/v1/auth/activate` (anonyme) à `AuthController`

**Checkpoint**: US1 + US2 — nouveaux membres activables et connectables. ✅ Implémenté et vérifié (build .NET 10 OK, 128 tests verts, migration EF Authentication) le 2026-07-03.

---

## Phase 5: User Story 3 — Changement de mot de passe (Priority: P2)

**Goal**: Un utilisateur connecté change son mot de passe (actuel + nouveau conforme).

**Independent Test**: `POST /auth/change-password` (authentifié) → 204 ; actuel erroné→401 ; nouveau non conforme→400.

### Tests for User Story 3 ⚠️

- [x] T024 [P] [US3] Tests unitaires `ChangePasswordHandler` (actuel correct→changé ; erroné→401 ; politique→400) dans `tests/Lumineux.Application.Tests/ChangePasswordTests.cs`
- [x] T025 [P] [US3] Test d'intégration `POST /api/v1/auth/change-password` (204/400/401) dans `tests/Lumineux.Api.Tests/AuthChangePasswordEndpointsTests.cs`

### Implementation for User Story 3

- [x] T026 [P] [US3] DTO `ChangePasswordRequest` + validator dans `src/Lumineux.Application/Contracts/Auth/` et `src/Lumineux.Application/Auth/ChangePasswordValidator.cs`
- [x] T027 [US3] Cas d'usage `ChangePasswordHandler` (membre courant via `ICurrentUser`, vérif. actuel, politique, `ChangePassword`, audit) dans `src/Lumineux.Application/Auth/ChangePasswordHandler.cs`
- [x] T028 [US3] Ajouter `POST /api/v1/auth/change-password` (authentifié) à `AuthController`

**Checkpoint**: US1 + US2 + US3 fonctionnent.

---

## Phase 6: User Story 4 — Protection contre les tentatives abusives (Priority: P2)

**Goal**: Verrouillage temporaire après N échecs consécutifs pendant D minutes, sur `/login` et `/activate`.

**Independent Test**: Après N échecs, la connexion (même avec le bon mot de passe) est refusée
pendant D ; réautorisée après expiration/succès.

### Tests for User Story 4 ⚠️

- [x] T029 [P] [US4] Tests unitaires Domain `MemberAccount` du verrouillage (`RegisterFailedLogin` incrémente ; seuil→`LockoutUntil` ; `IsLockedOut` ; reset) dans `tests/Lumineux.Domain.Tests/MemberAccountLockoutTests.cs`
- [x] T030 [P] [US4] Test d'intégration verrouillage effectif via `/auth/login` (N échecs → 401 même avec bon mot de passe) dans `tests/Lumineux.Api.Tests/AuthLockoutEndpointsTests.cs`

### Implementation for User Story 4

- [x] T031 [US4] Appliquer le même verrouillage dans `ActivateAccountHandler` et confirmer son application dans `LoginHandler` (cohérence login/activate, valeurs `AuthOptions`) dans `src/Lumineux.Application/Auth/`

**Checkpoint**: Les 4 user stories fonctionnent ; sécurité de connexion complète.

---

## Phase 7: Polish & Cross-Cutting

- [x] T032 [P] Journaliser via `IAuditLogger` les événements d'authentification (succès, échec, verrouillage, activation, changement) **sans** mot de passe ni jeton (FR-013)
- [x] T033 [P] Aligner Swagger/OpenAPI sur `contracts/openapi.yaml` + revue de sécurité dans `specs/003-authentication-login/checklists/security.md`
- [x] T034 [P] Documentation : compléter `src/Lumineux.Api/README.md` (endpoints `/auth`, config `Auth`, migration)
- [x] T035 Exécuter le scénario `quickstart.md` et vérifier SC-001..SC-006, **dont le rejet d'un jeton expiré/malformé sur un endpoint protégé (FR-014, couverture héritée de la validation JWT existante — F3)**

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** BLOQUE toutes les user stories.
- **US1 (P1)** : login — cœur ; après Foundational.
- **US2 (P1)** : activation — réutilise `MemberAccount`/`ITokenIssuer` ; peut suivre US1.
- **US3 (P2)** : changement de mot de passe — indépendant (nouveau handler/endpoint).
- **US4 (P2)** : verrouillage — les méthodes sont en Foundational (T002) ; US1/US2 les appliquent ; US4 = tests dédiés + cohérence activate.
- **Polish (Phase 7)** : après les user stories.

### User Story Dependencies

- **US1** : après Foundational ; introduit `AuthController` + `LoginHandler`.
- **US2** : après Foundational ; ajoute `/activate` (même contrôleur).
- **US3** : après Foundational ; ajoute `/change-password` (même contrôleur).
- **US4** : dépend de la présence des handlers login/activate (US1/US2) pour les tests d'intégration.

### Within Each User Story

- Tests d'abord (doivent échouer) — Constitution III.
- Ordre : entité/ports → config EF/migration → services/repos → cas d'usage → contrôleur.

### Parallel Opportunities

- Foundational : T003, T004, T005, T007, T010, T011 en parallèle (fichiers distincts) après T002/T006/T008.
- Par story : tests `[P]` + DTO/validators `[P]` en parallèle.
- US3 peut avancer en parallèle de US2 (fichiers différents) après Foundational ; attention au fichier
  partagé `AuthController` (endpoints ajoutés séquentiellement).

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Setup → Foundational → US1 (login) → US2 (activation). À ce stade, un nouveau membre peut activer
   son compte et se connecter — parcours d'accès complet et démontrable.

### Incremental Delivery

1. Setup + Foundational → socle prêt.
2. + US1 → connexion + jeton (MVP).
3. + US2 → activation des nouveaux comptes.
4. + US3 → changement de mot de passe.
5. + US4 → verrouillage anti-force brute (tests + cohérence).
6. Polish → journalisation, doc, validation quickstart.

### Notes

- [P] = fichiers différents, aucune dépendance non satisfaite.
- Vérifier que les tests échouent avant d'implémenter (Constitution III).
- Messages génériques + hash factice : aucune distinction « compte inexistant » / « mot de passe erroné ».
- Aucun mot de passe ni jeton journalisé ; jetons signés et expirants (source de temps serveur).
- Migration additive sur `member_accounts` (colonnes à défaut 0/null) — compatible avec les données existantes.
- **F4 (accepté)** : `/change-password` exige déjà un jeton valide ; le verrouillage anti-force brute n'y est pas appliqué dans cette itération (risque faible). À réévaluer si un durcissement est requis (compteur d'échecs sur le mot de passe actuel).
- **F1** : sans configuration d'amorçage (`Auth:Bootstrap:*`), aucun droit n'est accordé automatiquement ; les jetons ne porteront de permissions que pour les membres explicitement dotés (via bootstrap ou future feature de profils).
