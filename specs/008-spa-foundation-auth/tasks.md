---
description: "Task list — Console web Lumineux (socle & cycle de vie du compte)"
---

# Tasks: Console web Lumineux — socle & cycle de vie du compte (SPA)

**Input**: Design documents from `specs/008-spa-foundation-auth/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (api-consumption, routes)

**Tests**: INCLUS — Constitution III (esprit) + emphase sécurité de la spec. Unitaires **Vitest** ;
bout en bout **Playwright**. Écrire les tests d'une story avant/avec son implémentation.

**Organization**: Tâches regroupées par user story (US1→US5). **Nouveau** projet Angular dans `web/`,
sans impact sur la solution .NET (`src/`, API inchangée). Le socle (Lot 0) est réparti entre Setup +
Foundational (transverse à toutes les stories) ; US1 livre le premier parcours démontrable.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance non satisfaite)
- **[Story]** : US1..US5 (uniquement pour les phases de user story)
- Chemins relatifs à la racine du dépôt (dossier `web/`)

---

## Phase 1: Setup (initialisation du projet front)

- [X] T001 Échafauder l'application Angular (standalone + routing, dernière version stable) dans `web/`
- [X] T002 [P] Configurer les environnements (`apiBaseUrl` dev/prod + `passwordMinLength=8`) dans `web/src/environments/environment*.ts`
- [X] T003 [P] Configurer **Vitest** (runner unitaire) dans `web/` (config + script `test`)
- [X] T004 [P] Configurer **Playwright** (e2e) dans `web/e2e/` (config + script `e2e`)
- [X] T005 [P] Styles de base **responsive** + locale **française** (mise en page bureau/tablette, sans défilement horizontal) dans `web/src/styles.*` et `web/src/app/app.config.ts`

**Checkpoint**: Projet `web/` amorcé, testable, configuré vers l'API.

---

## Phase 2: Foundational (Prérequis bloquants — socle Lot 0)

**Purpose**: Socle transverse consommé par toutes les user stories : état de session, accès API,
intercepteurs (Bearer + erreurs), gardes, validateurs, routage, coquille console.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

- [X] T006 [P] Modèles TypeScript (`CurrentUser`, `TokenResponse`, `ProblemDetails`, modèles de formulaires) dans `web/src/app/core/api/models.ts`
- [X] T007 [P] Service d'API `AuthApi` (login, activate, forgotPassword, resetPassword, changePassword, me) dans `web/src/app/core/api/auth-api.ts`
- [X] T008 [P] Service d'API `SetupApi` (installFirstAdmin) dans `web/src/app/core/api/setup-api.ts`
- [X] T009 `SessionStore` (signals : `accessToken`, `currentUser`, `permissions`, `isAuthenticated` ; `establish(token)` → charge `/auth/me` ; `clear()`) dans `web/src/app/core/session/session-store.ts`
- [X] T010 [P] `authTokenInterceptor` (ajoute l'en-tête Bearer si session) dans `web/src/app/core/http/auth-token.interceptor.ts`
- [X] T011 `NotificationService` + `errorInterceptor` (401 quel que soit le corps → `SessionStore.clear()` + redirection `/login` avec `returnUrl` ; mapping ProblemDetails/`code` : `password_change_required`, 403/404/409/410/400 ; messages non techniques) dans `web/src/app/core/http/error.interceptor.ts` et `web/src/app/shared/notifications/`
- [X] T012 [P] Gardes `authGuard`, `permissionGuard(data.permission)`, `guestOnly`, `setupGuard` dans `web/src/app/core/guards/`
- [X] T013 [P] Validateurs de mot de passe (longueur min, ≥1 lettre, ≥1 chiffre, « différent de ») dans `web/src/app/shared/validators/password.validators.ts`
- [X] T014 Routage `app.routes.ts` (routes publiques vs protégées, cf. `contracts/routes.md`) + bootstrap `provideHttpClient(withInterceptors([...]))` dans `web/src/app/app.routes.ts` et `web/src/app/app.config.ts`
- [X] T015 Coquille console `ShellComponent` (layout + zone de navigation pilotée par un signal de droits) dans `web/src/app/shell/shell.component.ts`
- [X] T016 [P] Tests unitaires **Vitest** du socle (`SessionStore` establish/clear ; `authTokenInterceptor` : en-tête `Authorization: Bearer` ajouté si session, **aucun** en-tête sinon — FR-002 ; `errorInterceptor` mapping 401/403 `code`/400 ; gardes ; validateurs) dans `web/src/app/core/**/*.spec.ts` et `web/src/app/shared/**/*.spec.ts`

**Checkpoint**: Socle prêt (session, API, intercepteurs, gardes, routage, shell) — les stories peuvent démarrer.

---

## Phase 3: User Story 1 — Se connecter et accéder à la console selon ses droits (Priority: P1) 🎯 MVP

**Goal**: Connexion (référence + mot de passe) → session établie → identité/droits via `/auth/me` →
console dont la navigation reflète les droits ; déconnexion ; garde des routes protégées.

**Independent Test**: Se connecter avec un compte valide → console adaptée aux droits ; identifiants
erronés → message non révélateur ; URL protégée sans session → redirection connexion ; déconnexion →
retour connexion ; rechargement ne reconnecte pas.

### Tests for User Story 1 ⚠️ (écrire d'abord)

- [X] T017 [P] [US1] Tests unitaires `LoginComponent` (soumission, message d'échec **non révélateur**) et rendu de navigation masquée selon droits dans `web/src/app/features/login/login.component.spec.ts` et `web/src/app/shell/shell.component.spec.ts`
- [X] T018 [P] [US1] Test e2e **Playwright** (connexion valide → console adaptée ; mauvais identifiants → erreur générique ; URL protégée → redirection ; déconnexion → connexion ; rechargement ≠ reconnexion) dans `web/e2e/login.spec.ts`

### Implementation for User Story 1

- [X] T019 [US1] `LoginComponent` (formulaire réactif ; `AuthApi.login` → `SessionStore.establish` → redirection `returnUrl`/console ; erreur générique) dans `web/src/app/features/login/login.component.ts`
- [X] T020 [US1] Navigation de la coquille pilotée par les droits (masquer/désactiver) + action **déconnexion** (`SessionStore.clear()` → `/login`) dans `web/src/app/shell/shell.component.ts`
- [X] T021 [US1] Application de `authGuard` sur les routes protégées + gestion du `returnUrl` (retour à l'URL visée après connexion) dans `web/src/app/app.routes.ts`

**Checkpoint**: US1 fonctionnelle — MVP démontrable (connexion → console adaptée → déconnexion).

---

## Phase 4: User Story 2 — Première connexion / activation (Priority: P2)

**Goal**: Un compte avec mot de passe temporaire définit un nouveau mot de passe conforme et accède
à la console.

**Independent Test**: Connexion avec mot de passe temporaire → détection `password_change_required` →
écran d'activation → nouveau mot de passe conforme → console. Mot de passe non conforme refusé avant envoi.

### Tests for User Story 2 ⚠️

- [X] T022 [P] [US2] Tests unitaires `ActivateComponent` (validation politique, « différent du temporaire », soumission) dans `web/src/app/features/activate/activate.component.spec.ts`
- [X] T023 [P] [US2] Test e2e **Playwright** (login mot de passe temporaire → activation → console) dans `web/e2e/activate.spec.ts`

### Implementation for User Story 2

- [X] T024 [US2] `ActivateComponent` (`AuthApi.activate` → `establish` → console) + bascule automatique vers `/auth/activate` sur `403 password_change_required` (au login) dans `web/src/app/features/activate/activate.component.ts` et `web/src/app/core/http/error.interceptor.ts`

**Checkpoint**: US1 + US2 — comptes provisionnés activables en autonomie.

---

## Phase 5: User Story 3 — Mot de passe oublié et réinitialisation (Priority: P2)

**Goal**: Demande de réinitialisation (message générique) puis redéfinition via le lien reçu par
e-mail (route publique avec jeton).

**Independent Test**: Demande avec référence existante puis inexistante → **message identique** ;
ouverture `/auth/reset-password?token=…` → nouveau mot de passe → retour connexion → accès. Jeton
invalide → échec **générique**.

### Tests for User Story 3 ⚠️

- [X] T025 [P] [US3] Tests unitaires `ForgotPasswordComponent` (message générique) et `ResetPasswordComponent` (jeton lu depuis l'URL, échec générique) dans `web/src/app/features/forgot-password/*.spec.ts` et `web/src/app/features/reset-password/*.spec.ts`
- [X] T026 [P] [US3] Test e2e **Playwright** (oublié : message identique existant/inexistant ; reset via jeton → connexion) dans `web/e2e/password-reset.spec.ts`
- [X] T027 [US3] `ForgotPasswordComponent` (`AuthApi.forgotPassword` ; relais **fidèle** du message générique) dans `web/src/app/features/forgot-password/forgot-password.component.ts`
- [X] T028 [US3] `ResetPasswordComponent` (route publique, lit `?token=` ; `AuthApi.resetPassword` → retour `/login` ; échec générique) dans `web/src/app/features/reset-password/reset-password.component.ts`

**Checkpoint**: US1→US3 — cycle de récupération de compte complet.

---

## Phase 6: User Story 4 — Changer son mot de passe (Priority: P3)

**Goal**: Un utilisateur connecté change son mot de passe (actuel + nouveau conforme et différent).

**Independent Test**: Connecté → changement avec mot de passe actuel + nouveau conforme → succès ;
actuel erroné → erreur ; nouveau non conforme → refus avant envoi.

### Tests for User Story 4 ⚠️

- [X] T029 [P] [US4] Tests unitaires `ChangePasswordComponent` (validation, « différent de l'actuel », gestion erreur actuel erroné) dans `web/src/app/features/change-password/change-password.component.spec.ts`

### Implementation for User Story 4

- [X] T030 [US4] `ChangePasswordComponent` (route protégée `authGuard` ; `AuthApi.changePassword` → confirmation) dans `web/src/app/features/change-password/change-password.component.ts`

**Checkpoint**: US1→US4.

---

## Phase 7: User Story 5 — Installer le premier administrateur (Priority: P3)

**Goal**: Sur une instance non initialisée, créer le premier administrateur ; écran inaccessible une
fois l'instance amorcée.

**Independent Test**: Instance non initialisée → installation → connecté ; instance amorcée → accès
refusé/redirigé (l'API rejette).

### Tests for User Story 5 ⚠️

- [X] T031 [P] [US5] Tests unitaires `SetupComponent` (validation formulaire) + `setupGuard` (blocage si déjà installé, via 409) dans `web/src/app/features/setup/setup.component.spec.ts`

### Implementation for User Story 5

- [X] T032 [US5] `SetupComponent` (`SetupApi.installFirstAdmin` → `establish` → console) + `setupGuard` (gestion du 409 « déjà installé » → redirection) dans `web/src/app/features/setup/setup.component.ts` et `web/src/app/core/guards/setup.guard.ts`

**Checkpoint**: Toutes les user stories livrées.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T033 [P] Passe **responsive & accessibilité** (bureau/tablette, navigation clavier, contrastes, sans défilement horizontal — SC-008) sur `web/src/app/**`
- [ ] T034 Exécuter la validation `quickstart.md` de bout en bout (scénarios A→G) et confirmer SC-001..SC-008 — **EN ATTENTE** : nécessite l'API démarrée + CORS. Validé au niveau **unitaire (35 tests Vitest verts)** et **build applicative** ; les scénarios e2e Playwright (T018/T023/T026) sont **écrits** mais non exécutés contre une API en ligne dans cette session.
- [X] T035 [P] **Revue sécurité** : aucun secret (mot de passe, jeton) dans le stockage navigateur, les URL persistées ou la console (SC-005) ; anti-énumération relayée fidèlement (SC-006) ; jeton **hors** `localStorage` (FR-003)
- [X] T036 [P] `web/README.md` (installation, `apiBaseUrl`, lancement dev/tests/e2e) + **note de dépendance CORS** (l'API doit autoriser l'origine du SPA)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance ; T001 avant tout, puis T002–T005 en parallèle.
- **Foundational (Phase 2)** : dépend du Setup — **bloque** toutes les user stories. Ordre interne :
  modèles/API (T006–T008) → `SessionStore` (T009) → intercepteurs/gardes/validateurs (T010–T013) →
  routage + shell (T014–T015) ; tests socle (T016) avec/juste après.
- **US1 (Phase 3)** : après la Phase 2. Tests (T017/T018) avant/avec T019–T021.
- **US2–US5 (Phases 4→7)** : après la Phase 2 ; peuvent s'enchaîner (ou paralléliser entre équipes)
  car chacune vit dans son propre dossier `features/`. US2 touche aussi `error.interceptor.ts`
  (bascule activation) → séquencer après T011.
- **Polish (Phase 8)** : après les user stories visées.

### Within Each User Story

- Tests écrits avant/avec l'implémentation (rouge → vert).
- Un composant par écran, isolé dans `features/<nom>/` ; l'accès API passe **toujours** par les
  services `core/api` (aucun appel HTTP dans les composants).

### Parallel Opportunities

- **Setup** : T002, T003, T004, T005 en parallèle.
- **Foundational** : T006, T007, T008 en parallèle ; puis T010, T012, T013 en parallèle ; T016 [P].
- **Par story** : le test unitaire [P] et le test e2e [P] en parallèle ; les stories US2–US5 sont
  largement indépendantes (dossiers distincts).
- ⚠️ T011 (error.interceptor) et T024 (bascule activation) touchent le **même** fichier → séquencer.

---

## Parallel Example: Foundational (Phase 2)

```bash
Task: "Modèles TypeScript dans web/src/app/core/api/models.ts"
Task: "AuthApi dans web/src/app/core/api/auth-api.ts"
Task: "SetupApi dans web/src/app/core/api/setup-api.ts"
# puis, après SessionStore :
Task: "authTokenInterceptor dans web/src/app/core/http/auth-token.interceptor.ts"
Task: "Gardes dans web/src/app/core/guards/"
Task: "Validateurs de mot de passe dans web/src/app/shared/validators/password.validators.ts"
```

---

## Implementation Strategy

### MVP (US1)

1. Phase 1 : Setup (échafaudage, config, tests).
2. Phase 2 : Foundational (session, API, intercepteurs, gardes, routage, shell) — **bloquant**.
3. Phase 3 : US1 (connexion + console adaptée + déconnexion) → **STOP & VALIDATE** : parcours
   démontrable de bout en bout.

### Livraison incrémentale

1. Socle + US1 → se connecter et naviguer (MVP).
2. US2 → activation des comptes provisionnés.
3. US3 → récupération de mot de passe.
4. US4 → changement de mot de passe.
5. US5 → installation du premier administrateur.
6. Polish → responsive/accessibilité, quickstart, revue sécurité, README/CORS.

### Notes

- [P] = fichiers différents, aucune dépendance ; [Story] = traçabilité US.
- Aucun appel HTTP dans les composants (toujours via `core/api`) ; aucune règle métier côté client.
- Jeton **en mémoire** uniquement ; **aucun secret** affiché/persisté/loggé.
- Vérifier que les tests échouent avant d'implémenter ; commit après chaque tâche ou groupe logique.
