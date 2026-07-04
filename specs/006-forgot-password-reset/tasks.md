---
description: "Task list — Mot de passe oublié"
---

# Tasks: Mot de passe oublié

**Input**: Design documents from `specs/006-forgot-password-reset/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml

**Tests**: INCLUS — imposés par la Constitution Lumineux v1.0.0, Principe III.
Unitaires Domain/Application ; intégration API (harnais existant).

**Organization**: Tâches regroupées par user story (US1→US2). Extension de la solution Onion
existante (features 001→005) : `src/Lumineux.Domain|Application|Infrastructure|Api`,
`tests/Lumineux.*.Tests`. US1 (demande) et US2 (réinitialisation) forment ensemble le MVP P1 —
US2 dépend fonctionnellement des jetons émis par US1, mais chaque story est testable isolément
(US2 via un jeton semé directement en base).

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US2 (uniquement pour les phases de user story)
- Chaque tâche indique le chemin de fichier exact

---

## Phase 1: Setup

- [X] T001 [P] Ajouter à la section `Auth` les clés `PasswordResetMinutes` (30) et `PasswordResetUrlBase` (`https://localhost:4200/auth/reset-password` en dev) dans `src/Lumineux.Api/appsettings.json` et `src/Lumineux.Api/appsettings.Development.json`

**Checkpoint**: Configuration disponible.

---

## Phase 2: Foundational (Prérequis bloquants)

**Purpose**: Entité de domaine, ports, service de jeton, persistance, migration et extension email
requis par les deux user stories.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [X] T002 [P] Créer l'entité de domaine `PasswordResetToken` (props `AccountId`, `TokenHash`, `ExpiresAt`, `ConsumedAt` ; fabrique `Issue`, méthodes `IsUsable(nowUtc)`, `Consume(nowUtc)`) dans `src/Lumineux.Domain/Entities/PasswordResetToken.cs`
- [X] T003 [P] Définir le port `IResetTokenService` (`Generate()` → `(ClearToken, TokenHash)` ; `Hash(clearToken)`) dans `src/Lumineux.Domain/Abstractions/IResetTokenService.cs`
- [X] T004 [P] Définir le port `IPasswordResetTokenRepository` (`AddAsync`, `GetByTokenHashAsync` avec compte + membre chargés, `SaveChangesAsync`) dans `src/Lumineux.Domain/Abstractions/IPasswordResetTokenRepository.cs`
- [X] T005 Étendre le port `IEmailSender` avec `SendPasswordResetAsync(string? toEmail, string resetLink, CancellationToken)` dans `src/Lumineux.Domain/Abstractions/IEmailSender.cs`
- [X] T006 [P] Étendre `AuthOptions` avec `PasswordResetMinutes` (défaut 30) et `PasswordResetUrlBase` dans `src/Lumineux.Application/Abstractions/AuthOptions.cs`
- [X] T007 [P] Implémenter `ResetTokenService` (`IResetTokenService` : `RandomNumberGenerator.GetBytes(32)` → base64url ; empreinte `SHA256`) dans `src/Lumineux.Infrastructure/Security/ResetTokenService.cs`
- [X] T008 [P] Implémenter `PasswordResetTokenRepository` (`IPasswordResetTokenRepository`, charge le compte + `Member` via navigation) dans `src/Lumineux.Infrastructure/Repositories/PasswordResetTokenRepository.cs`
- [X] T009 Ajouter `SendPasswordResetAsync` aux deux implémentations email (`LoggingEmailSender` — sans logguer le lien ; `SmtpEmailSender` — email de reset, `EmailSendOutcome`) dans `src/Lumineux.Infrastructure/Email/LoggingEmailSender.cs` et `src/Lumineux.Infrastructure/Email/SmtpEmailSender.cs`
- [X] T010 Configuration EF : `PasswordResetTokenConfiguration` (table `password_reset_tokens`, index **unique** sur `token_hash`, index sur `account`, FK cascade vers `member_accounts`, `AuditColumns.Apply`) + `DbSet<PasswordResetToken>` dans `src/Lumineux.Infrastructure/Persistence/Configurations/PasswordResetTokenConfiguration.cs` et `src/Lumineux.Infrastructure/Persistence/AppDbContext.cs`
- [X] T011 Générer la **migration** additive `PasswordReset` (création de `password_reset_tokens` + index unique empreinte + FK) dans `src/Lumineux.Infrastructure/Persistence/Migrations/`
- [X] T012 Enregistrement DI (bind des nouvelles options ; `IResetTokenService` → `ResetTokenService` ; `IPasswordResetTokenRepository` → `PasswordResetTokenRepository`) dans `src/Lumineux.Infrastructure/DependencyInjection.cs`
- [X] T013 [P] Tests unitaires Domain `PasswordResetTokenTests` (`Issue` invariants ; `IsUsable` actif/expiré/consommé ; `Consume` marque + refus double consommation) dans `tests/Lumineux.Domain.Tests/PasswordResetTokenTests.cs`

**Checkpoint**: Socle prêt — les user stories peuvent démarrer.

---

## Phase 3: User Story 1 — Demander la réinitialisation (Priority: P1) 🎯 MVP

**Goal**: Endpoint anonyme `/auth/forgot-password` : réponse **générique** 200 dans tous les cas ;
si compte actif + email → émission d'un jeton usage unique (empreinte persistée) + envoi du lien ;
anti-timing via opération factice sinon.

**Independent Test**: Sur un compte actif+email, la demande envoie un email (capturé par un
`IEmailSender` de test) et persiste **une empreinte** (jamais le clair) ; sur référence inexistante /
sans email / non actif, la réponse est **strictement identique** (200, même corps) et **aucun** email
n'est envoyé.

### Tests for User Story 1 ⚠️ (écrire d'abord, doivent échouer)

- [X] T014 [P] [US1] Tests unitaires `RequestPasswordResetHandlerTests` (réponse générique + email + empreinte persistée sur compte actif+email ; **aucun** email + opération factice sur absent/sans email/non actif/verrouillé ; jeton en clair jamais persisté ; **échec d'envoi email** (`IEmailSender` renvoyant `EmailSendOutcome.Failed`) → réponse **toujours 200 générique**, échec journalisé sans lien, cf. FR-011 / edge case spec) dans `tests/Lumineux.Application.Tests/RequestPasswordResetHandlerTests.cs`
- [X] T015 [P] [US1] Tests d'intégration `AuthForgotPasswordEndpointsTests` (200 + égalité de réponse octet à octet entre actif/inexistant/sans email/archivé ; 400 si référence vide) dans `tests/Lumineux.Api.Tests/AuthForgotPasswordEndpointsTests.cs`

### Implementation for User Story 1

- [X] T016 [P] [US1] Créer le DTO `ForgotPasswordRequest` (`Reference`) et la réponse générique `GenericMessageResponse` dans `src/Lumineux.Application/Contracts/Auth/AuthDtos.cs`
- [X] T017 [P] [US1] Créer `ForgotPasswordValidator` (`Reference` non vide, longueur max 60) dans `src/Lumineux.Application/Auth/PasswordRules.cs`
- [X] T018 [US1] Implémenter `RequestPasswordResetHandler` (recherche compte par `LoginId` ; si actif+email → `IResetTokenService.Generate`, `PasswordResetToken.Issue`, `AddAsync`, construction du lien depuis `PasswordResetUrlBase`, `SendPasswordResetAsync` ; **sinon** opération factice via `IResetTokenService.Generate()` dont le résultat est jeté, pour égaliser le **coût de calcul** — même stratégie que `LoginHandler` feature 003 ; un échec d'envoi (`EmailSendOutcome.Failed`) n'altère pas la réponse (200 générique) ; audit sans secret ; réponse générique toujours). **Note anti-timing (I1)** : cette égalisation couvre le calcul, pas l'I/O (écriture BD + envoi email) du chemin nominal — limite résiduelle assumée, identique à feature 003 ; documenter en commentaire dans le handler. Fichier `src/Lumineux.Application/Auth/RequestPasswordResetHandler.cs`
- [X] T019 [US1] Enregistrer `RequestPasswordResetHandler` dans le DI Application `src/Lumineux.Application/DependencyInjection.cs`
- [X] T020 [US1] Ajouter l'endpoint `POST /api/v1/auth/forgot-password` (`[AllowAnonymous]`, renvoie 200 générique) dans `src/Lumineux.Api/Controllers/AuthController.cs`

**Checkpoint**: US1 fonctionnelle et testable indépendamment — la demande émet/envoie sans rien divulguer.

---

## Phase 4: User Story 2 — Réinitialiser avec le lien reçu (Priority: P1) 🎯 MVP

**Goal**: Endpoint anonyme `/auth/reset-password` : valide le jeton, applique le nouveau mot de passe
(politique feature 003), consomme le jeton (usage unique), remet à zéro les compteurs et lève le
verrouillage, répond 204 ; refus **générique** 401 pour jeton introuvable/expiré/consommé ; 400 sans
consommation si mot de passe non conforme.

**Independent Test**: Avec un jeton valide (semé en base directement, indépendamment d'US1), l'appel
change effectivement le mot de passe (ancien refusé, nouveau accepté sur `/auth/login`) et le même
jeton est refusé au second usage (401).

### Tests for User Story 2 ⚠️ (écrire d'abord, doivent échouer)

- [X] T021 [P] [US2] Tests unitaires `ResetPasswordHandlerTests` (succès : empreinte mise à jour, jeton consommé, `FailedAttempts=0`, `LockoutUntil=null` ; rejeu → 401 ; expiré → 401 ; introuvable → 401 ; mot de passe faible → 400 sans consommation) dans `tests/Lumineux.Application.Tests/ResetPasswordHandlerTests.cs`
- [X] T022 [P] [US2] Tests d'intégration `AuthResetPasswordEndpointsTests` (204 nominal ; ancien mdp refusé / nouveau accepté via `/auth/login` ; rejeu 401 ; jeton inconnu 401 ; mdp faible 400 puis réessai OK ; compte verrouillé → login immédiat après reset, SC-007) dans `tests/Lumineux.Api.Tests/AuthResetPasswordEndpointsTests.cs`

### Implementation for User Story 2

- [X] T023 [P] [US2] Créer le DTO `ResetPasswordRequest` (`Token`, `NewPassword`) dans `src/Lumineux.Application/Contracts/Auth/AuthDtos.cs`
- [X] T024 [P] [US2] Créer `ResetPasswordValidator` (`Token` non vide ; `NewPassword` via `PasswordRules.ApplyPolicy`) dans `src/Lumineux.Application/Auth/PasswordRules.cs`
- [X] T025 [US2] Implémenter `ResetPasswordHandler` (validation politique d'abord → 400 sans toucher au jeton ; `Hash(token)` + `GetByTokenHashAsync` ; `IsUsable` sinon `UnauthorizedException` générique ; sur succès : `MemberAccount.ChangePassword(nouveauHash)` + `RegisterSuccessfulLogin(now)` + `token.Consume(now)` + `SaveChanges` ; audit sans secret ; 204) dans `src/Lumineux.Application/Auth/ResetPasswordHandler.cs`
- [X] T026 [US2] Enregistrer `ResetPasswordHandler` dans le DI Application `src/Lumineux.Application/DependencyInjection.cs`
- [X] T027 [US2] Ajouter l'endpoint `POST /api/v1/auth/reset-password` (`[AllowAnonymous]`, 204/400/401) dans `src/Lumineux.Api/Controllers/AuthController.cs`

**Checkpoint**: US1 + US2 opérationnelles — parcours complet demande → email → reset → connexion.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T028 [P] Mettre à jour le contrat OpenAPI exposé (Swagger) si un fichier agrégé existe, en cohérence avec `specs/006-forgot-password-reset/contracts/openapi.yaml`
- [X] T029 Exécuter la validation `quickstart.md` de bout en bout (scénario nominal + scénarios sécurité A→I) et confirmer les critères SC-001..SC-007
- [X] T030 [P] Revue sécurité : vérifier qu'aucun jeton en clair ni mot de passe n'apparaît dans les logs ou la base (SC-004) ; confirmer l'égalité de réponse `/forgot-password` (SC-002)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance.
- **Foundational (Phase 2)** : dépend du Setup — **bloque** les deux user stories.
- **US1 (Phase 3)** et **US2 (Phase 4)** : démarrent après la Phase 2. US2 est testable via un jeton
  semé en base ; en pratique la démo complète enchaîne US1 → US2.
- **Polish (Phase 5)** : après US1 et US2.

### Within Each User Story

- Les tests (T014/T015, T021/T022) sont écrits **avant** l'implémentation et doivent échouer d'abord.
- DTO/validators avant handler ; handler avant endpoint ; DI avant l'endpoint.

### Parallel Opportunities

- **Phase 2** : T002, T003, T004, T006, T007, T008, T013 en parallèle (fichiers distincts). T005/T009
  touchent l'email ; T010/T011/T012 s'enchaînent (config EF → migration → DI).
- **US1** : T014/T015 (tests) en parallèle ; T016/T017 (DTO/validator) en parallèle avant T018.
- **US2** : T021/T022 (tests) en parallèle ; T023/T024 en parallèle avant T025.
- ⚠️ T016/T023 modifient tous deux `AuthDtos.cs` et T017/T024 modifient `PasswordRules.cs` : ne pas
  paralléliser **entre** stories sur ces fichiers partagés (séquencer US1 puis US2, ou fusionner les ajouts).

---

## Parallel Example: Phase 2 (Foundational)

```bash
Task: "Créer l'entité PasswordResetToken dans src/Lumineux.Domain/Entities/PasswordResetToken.cs"
Task: "Définir IResetTokenService dans src/Lumineux.Domain/Abstractions/IResetTokenService.cs"
Task: "Définir IPasswordResetTokenRepository dans src/Lumineux.Domain/Abstractions/IPasswordResetTokenRepository.cs"
Task: "Étendre AuthOptions dans src/Lumineux.Application/Abstractions/AuthOptions.cs"
Task: "Implémenter ResetTokenService dans src/Lumineux.Infrastructure/Security/ResetTokenService.cs"
Task: "Tests PasswordResetTokenTests dans tests/Lumineux.Domain.Tests/PasswordResetTokenTests.cs"
```

---

## Implementation Strategy

### MVP (US1 + US2 — les deux sont P1)

1. Phase 1 : Setup (config).
2. Phase 2 : Foundational (entité, ports, service jeton, repo, email, migration, DI) — **bloquant**.
3. Phase 3 : US1 (demande) → tester l'anti-énumération et l'envoi.
4. Phase 4 : US2 (réinitialisation) → tester l'usage unique et la levée du verrouillage.
5. Phase 5 : Polish + validation quickstart + revue sécurité.

### Notes

- [P] = fichiers différents, aucune dépendance ; [Story] = traçabilité US.
- Vérifier que les tests échouent avant d'implémenter.
- Commit après chaque tâche ou groupe logique.
- Ne jamais logguer/persister le jeton en clair ni le mot de passe (SC-004).
