# Tasks: Application mobile membre — socle & cycle de vie du compte

**Input**: Design documents from `/specs/025-mobile-flutter-foundation/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-consumption.md, contracts/navigation.md, quickstart.md

**Tests**: INCLUS — le Principe III de la constitution (« Tests en premier ») est **NON-NÉGOCIABLE**. Les tâches de test précèdent leur implémentation et doivent **échouer** avant d'être satisfaites.

**Organization**: Tâches groupées par user story (P1→P4) pour une implémentation et un test indépendants. Racine du client : `mobile/`.

## Format: `[ID] [P?] [Story] Description`

- **[P]** : parallélisable (fichiers différents, aucune dépendance en attente)
- **[Story]** : user story rattachée (US1–US4)
- Chemins de fichiers exacts inclus dans chaque description

## Path Conventions

Nouveau client Flutter dans `mobile/` (mono-dépôt, symétrique de `web/`). Code dans `mobile/lib/`, tests dans `mobile/test/`, tests d'intégration dans `mobile/integration_test/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Amorçage du projet Flutter et structure de base.

> ⚠️ **Prérequis outillage** : le **Flutter SDK** (canal stable ≥ 3.29) doit être installé avant T001 (téléchargement réseau → **approbation explicite requise**, cf. plan.md).

- [X] T001 Amorcer l'app Flutter dans `mobile/` via `flutter create` (génère `android/`, `ios/`, `pubspec.yaml`, `lib/main.dart`), organisation `mobile/` (org : com.lumineux)
- [X] T002 Déclarer les dépendances dans `mobile/pubspec.yaml` : `flutter_riverpod`, `go_router`, `dio`, `flutter_secure_storage` ; **dev** : `mocktail`, `integration_test` (SDK), `flutter_lints` ; puis `flutter pub get`
- [X] T003 [P] Configurer `mobile/analysis_options.yaml` (`include: package:flutter_lints/flutter.yaml`, règles projet)
- [X] T004 [P] Créer les profils d'environnement `mobile/env/dev.json` et `mobile/env/prod.json` (clé `API_BASE_URL` ; dev Android `https://10.0.2.2:4311`, dev iOS `https://localhost:4311`)
- [X] T005 [P] Créer la config d'environnement `mobile/lib/core/config/env.dart` (lecture `API_BASE_URL` via `String.fromEnvironment`, indicateur `isDev`)
- [X] T006 [P] Créer l'arborescence feature-first vide selon plan.md (`lib/core/{config,network,errors,storage}`, `lib/routing`, `lib/features/auth/{data,application,presentation}`, `lib/features/home/presentation`, `test/...`, `integration_test/`)
- [X] T007 [P] Rédiger `mobile/README.md` (démarrage, profils `--dart-define-from-file`, commandes de test)

**Checkpoint**: Projet Flutter compilable et lançable (écran vide), lint configuré.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Socle transverse (réseau, coffre, erreurs, DTO, contrôleur de session, routage gardé) partagé par **toutes** les user stories.

**⚠️ CRITICAL**: Aucune user story ne peut démarrer avant la fin de cette phase.

### Tests du socle (écrire d'abord — doivent échouer)

- [X] T008 [P] Test unitaire du coffre à jeton dans `mobile/test/core/secure_token_store_test.dart` (écriture/lecture/purge, moqué)
- [X] T009 [P] Test unitaire du mapping d'erreurs dans `mobile/test/core/error_messages_test.dart` (401/403+`code`/400 ProblemDetails/network/5xx → messages FR)
- [X] T010 [P] Test unitaire des intercepteurs `dio` dans `mobile/test/core/dio_client_test.dart` (ajout Bearer sur routes protégées, absence sur anonymes, conversion en `ApiException`, purge sur 401)
- [X] T011 [P] Test unitaire d'`AuthApi` dans `mobile/test/features/auth/auth_api_test.dart` (6 routes, `dio` moqué, sérialisation DTO, extraction `extensions.code`)
- [X] T012 [P] Test unitaire de `PasswordPolicy` dans `mobile/test/features/auth/password_policy_test.dart` (longueur min, ≥1 lettre, ≥1 chiffre)
- [X] T013 [P] Test unitaire du `SessionController` (socle) dans `mobile/test/features/auth/session_controller_test.dart` (restauration au démarrage : jeton présent+valide+/me OK → authenticated ; absent/expiré/401 → anonymous+purge)

### Implémentation du socle

- [X] T014 [P] Types d'erreurs applicatives `ApiException` (`unauthorized`/`forbidden`/`validation`/`network`/`server` + `code` métier) dans `mobile/lib/core/network/api_exception.dart`
- [X] T015 [P] DTO auth (miroir des contrats) `LoginRequest`, `ActivateRequest`, `ForgotPasswordRequest`, `ResetPasswordRequest`, `ChangePasswordRequest`, `TokenResponse`, `CurrentUser` avec `fromJson`/`toJson` manuels dans `mobile/lib/features/auth/data/auth_dtos.dart`
- [X] T016 [P] `SecureTokenStore` (flutter_secure_storage, `encryptedSharedPreferences: true`) : `save`/`read`/`clear` du jeton+échéance dans `mobile/lib/core/storage/secure_token_store.dart`
- [X] T017 [P] Mapping `ProblemDetails`/`code` → messages FR dans `mobile/lib/core/errors/error_messages.dart`
- [X] T018 Client réseau `dio` + 2 intercepteurs (Bearer, erreurs→`ApiException`, purge sur 401) dans `mobile/lib/core/network/dio_client.dart` — **journalisation sans corps de requête/réponse sensibles** (jamais mot de passe/jeton/mot de passe temporaire ; statut + `code` métier uniquement, FR-010) (dépend de T014, T016)
- [X] T019 `AuthApi` : `login`/`activate`/`forgotPassword`/`resetPassword`/`me`/`changePassword` dans `mobile/lib/features/auth/data/auth_api.dart` (dépend de T015, T018)
- [X] T020 [P] `PasswordPolicy` (validation confort, API autorité) dans `mobile/lib/features/auth/application/password_policy.dart`
- [X] T021 `SessionController` (Riverpod `Notifier`) — machine à états `unknown/restoring/authenticated/passwordChangeRequired/anonymous`, restauration au démarrage, purge sur expiration/401 dans `mobile/lib/features/auth/application/session_controller.dart` (dépend de T016, T019)
- [X] T022 Providers Riverpod (DI : `dioClient`, `secureTokenStore`, `authApi`, `sessionController`) dans `mobile/lib/features/auth/application/providers.dart` (dépend de T016, T018, T019, T021)
- [X] T023 Routage `go_router` + redirection globale (garde de session, `refreshListenable` sur l'état) pour les 6 routes dans `mobile/lib/routing/app_router.dart` (dépend de T021)
- [X] T024 `app.dart` : `MaterialApp.router`, localisation FR, thème tactile, écran splash/chargement pour `unknown`/`restoring` dans `mobile/lib/app.dart` (dépend de T023)
- [X] T025 `main.dart` : bootstrap `ProviderScope` + démarrage de la restauration dans `mobile/lib/main.dart` (dépend de T024)
- [X] T026 Exception TLS **dev-only ciblée** (certificat auto-signé) branchée sur le profil dev dans `mobile/lib/core/network/dio_client.dart` — **HTTPS strict en prod, aucune exception** (dépend de T018)

**Checkpoint**: L'app démarre, restaure/purge la session, route selon l'état ; tous les écrans peuvent maintenant être ajoutés.

---

## Phase 3: User Story 1 - Se connecter et accéder à un espace authentifié (Priority: P1) 🎯 MVP

**Goal**: Installer/lancer l'app, saisir des identifiants valides → écran d'accueil identifié ; session restaurée au relancement ; purge à l'expiration.

**Independent Test**: identifiants valides → Accueil affichant l'identité ; fermer/rouvrir (jeton valide) → session restaurée ; identifiants invalides → message clair ; réseau coupé → « réseau indisponible » ; jeton expiré → retour Connexion sans donnée résiduelle.

### Tests pour US1 (écrire d'abord — doivent échouer) ⚠️

- [X] T027 [P] [US1] Test unitaire `SessionController.login` (200→authenticated ; 401→message ; network→message) dans `mobile/test/features/auth/session_login_test.dart`
- [X] T028 [P] [US1] Test widget écran de connexion (états chargement/erreur, bouton neutralisé anti double-soumission, message d'erreur non révélateur) dans `mobile/test/features/auth/login_screen_test.dart`
- [X] T029 [P] [US1] Test widget écran d'accueil (affiche l'identité du membre) dans `mobile/test/features/home/home_screen_test.dart`
- [X] T030 [P] [US1] Test d'intégration connexion→accueil→relancement (restauration)→expiration (purge+retour connexion) dans `mobile/integration_test/login_session_test.dart`

### Implémentation pour US1

- [X] T031 [US1] Ajouter `login(reference, password)` au `SessionController` (200→authenticated via `me` ; 401→erreur ; network) dans `mobile/lib/features/auth/application/session_controller.dart`
- [X] T032 [P] [US1] Écran de connexion (référence + mot de passe, états chargement/erreur, lien vers oublié) dans `mobile/lib/features/auth/presentation/login_screen.dart`
- [X] T033 [P] [US1] Écran d'accueil authentifié (identité du membre, point d'accès aux actions compte) dans `mobile/lib/features/home/presentation/home_screen.dart`
- [X] T034 [US1] Enregistrer les routes `/login` et `/home` + vérifier la garde (anonyme→/login, authentifié→/home) dans `mobile/lib/routing/app_router.dart`

**Checkpoint**: MVP démontrable — connexion sûre, accueil identifié, restauration/expiration fonctionnelles.

---

## Phase 4: User Story 2 - Activer mon compte à la première connexion (Priority: P2)

**Goal**: Connexion avec mot de passe temporaire → bascule automatique vers l'activation (référence pré-remplie) → nouveau mot de passe → accueil.

**Independent Test**: se connecter avec un mot de passe temporaire → écran d'activation (référence pré-remplie) ; mot de passe conforme → accueil ; mot de passe non conforme → refus immédiat, aucun appel réseau.

### Tests pour US2 (écrire d'abord — doivent échouer) ⚠️

- [X] T035 [P] [US2] Test unitaire `SessionController` : `login` renvoyant `403 password_change_required` → état `passwordChangeRequired(reference)` ; `activate` → authenticated dans `mobile/test/features/auth/session_activate_test.dart`
- [X] T036 [P] [US2] Test widget écran d'activation (référence pré-remplie lecture seule, validation `PasswordPolicy` immédiate, refus sans appel réseau) dans `mobile/test/features/auth/activate_screen_test.dart`
- [X] T037 [US2] Test d'intégration mot de passe temporaire → activation → accueil dans `mobile/integration_test/activation_test.dart`

### Implémentation pour US2

- [X] T038 [US2] Gérer `403 password_change_required` dans `login` (→ `passwordChangeRequired(reference)`) et ajouter `activate(reference, temporaryPassword, newPassword)` dans `mobile/lib/features/auth/application/session_controller.dart`
- [X] T039 [P] [US2] Écran d'activation (référence pré-remplie, mot de passe temporaire + nouveau, validation immédiate) dans `mobile/lib/features/auth/presentation/activate_screen.dart`
- [X] T040 [US2] Enregistrer la route `/auth/activate` + garde forçant l'activation sur l'état `passwordChangeRequired` dans `mobile/lib/routing/app_router.dart`

**Checkpoint**: US1 + US2 fonctionnelles indépendamment.

---

## Phase 5: User Story 3 - Récupérer l'accès via « mot de passe oublié » (Priority: P3)

**Goal**: Depuis la connexion, demander une réinitialisation (message générique anti-énumération) puis réinitialiser via le jeton reçu par e-mail.

**Independent Test**: demande de réinitialisation → message générique identique (compte existant ou non) ; jeton valide + nouveau mot de passe conforme → succès → connexion ; jeton invalide/expiré → message clair.

### Tests pour US3 (écrire d'abord — doivent échouer) ⚠️

- [X] T041 [P] [US3] Test widget écran « mot de passe oublié » (message **générique** identique quel que soit le résultat) dans `mobile/test/features/auth/forgot_password_screen_test.dart`
- [X] T042 [P] [US3] Test widget écran de réinitialisation (jeton + nouveau mot de passe, validation immédiate, erreur jeton invalide/expiré 401) dans `mobile/test/features/auth/reset_password_screen_test.dart`
- [X] T043 [US3] Test d'intégration oublié → réinitialisation → connexion avec le nouveau mot de passe dans `mobile/integration_test/forgot_reset_test.dart`

### Implémentation pour US3

- [X] T044 [P] [US3] Écran « mot de passe oublié » (référence → `AuthApi.forgotPassword`, affiche le message générique renvoyé) dans `mobile/lib/features/auth/presentation/forgot_password_screen.dart`
- [X] T045 [P] [US3] Écran de réinitialisation (jeton e-mail + nouveau mot de passe → `AuthApi.resetPassword`, 204=succès) dans `mobile/lib/features/auth/presentation/reset_password_screen.dart`
- [X] T046 [US3] Enregistrer les routes anonymes `/auth/forgot` et `/auth/reset` (exemptées de la garde) + lien depuis la connexion dans `mobile/lib/routing/app_router.dart`

**Checkpoint**: US1–US3 fonctionnelles indépendamment.

---

## Phase 6: User Story 4 - Gérer mon mot de passe et me déconnecter (Priority: P4)

**Goal**: Membre authentifié : changer son mot de passe (ancien + nouveau) et se déconnecter (purge du coffre).

**Independent Test**: changer le mot de passe (ancien + nouveau conforme) → confirmation ; déconnexion → retour Connexion, jeton effacé ; relancement → aucune session restaurée.

### Tests pour US4 (écrire d'abord — doivent échouer) ⚠️

- [X] T047 [P] [US4] Test unitaire `SessionController.logout` (purge coffre → `anonymous`) dans `mobile/test/features/auth/session_logout_test.dart`
- [X] T048 [P] [US4] Test widget écran de changement de mot de passe (ancien + nouveau, validation immédiate, confirmation) dans `mobile/test/features/auth/change_password_screen_test.dart`
- [X] T049 [US4] Test d'intégration changement → déconnexion → relancement (aucune restauration) dans `mobile/integration_test/change_logout_test.dart`

### Implémentation pour US4

- [X] T050 [US4] Ajouter `logout()` (purge coffre + `anonymous`) au `SessionController` dans `mobile/lib/features/auth/application/session_controller.dart`
- [X] T051 [P] [US4] Écran de changement de mot de passe (`AuthApi.changePassword`, 204=succès, validation immédiate) dans `mobile/lib/features/auth/presentation/change_password_screen.dart`
- [X] T052 [US4] Accueil : ajouter navigation vers `/account/change-password` + action de déconnexion dans `mobile/lib/features/home/presentation/home_screen.dart`
- [X] T053 [US4] Enregistrer la route protégée `/account/change-password` dans `mobile/lib/routing/app_router.dart`

**Checkpoint**: Cycle de vie complet du compte livré.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Qualité, sécurité et validation transverses.

- [X] T054 [P] `flutter analyze` — zéro avertissement (Principe III/statique)
- [X] T055 Vérification sécurité : aucun mot de passe/jeton/mot de passe temporaire dans les journaux, jeton présent **uniquement** au coffre (SC-003) ; auditer `dio_client`/`SessionController`
- [X] T056 [P] Tests widget de bord additionnels : réseau indisponible et anti double-soumission (edge cases spec) dans `mobile/test/features/auth/`
- [X] T057 Exécuter la validation `quickstart.md` de bout en bout (US1→US4) sur émulateur/simulateur
- [X] T058 [P] Finaliser `mobile/README.md` (profils dev/prod, exception TLS dev-only, commandes de test) et lien depuis la doc racine
- [X] T059 Confirmer la suite verte : `flutter test` + `flutter test integration_test` (CI verte exigée, Principe III)
- [X] T060 [P] **CI (Principe III)** : câbler `flutter analyze` + `flutter test` (+ intégration si runner dispo) sur `mobile/` dans le pipeline du dépôt, bloquant la fusion si échec (gate mobile)
- [X] T061 **Reprise d'app (edge case verrouillage/arrière-plan)** : re-vérifier l'état de session sur `AppLifecycleState.resumed` (pré-vérif `expiresAt` → purge si expiré, aucune donnée protégée résiduelle) via un observateur de cycle de vie dans `mobile/lib/app.dart` + test widget dans `mobile/test/app_lifecycle_test.dart` (rattaché à US1/SC-004)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** : aucune dépendance — démarrage immédiat (après installation du Flutter SDK).
- **Foundational (Phase 2)** : dépend de Setup — **BLOQUE** toutes les user stories.
- **User Stories (Phase 3–6)** : dépendent de Foundational. Ordre de priorité P1→P4 recommandé ; livrables indépendamment testables.
- **Polish (Phase 7)** : après les user stories souhaitées.

### User Story Dependencies

- **US1 (P1)** : après Foundational. Aucune dépendance sur une autre story (MVP).
- **US2 (P2)** : après Foundational. Réutilise l'écran de connexion (US1) pour déclencher le `403`, mais testable indépendamment.
- **US3 (P3)** : après Foundational. Routes anonymes autonomes ; indépendante de US1/US2.
- **US4 (P4)** : après Foundational. Nécessite une session (US1) pour un test complet ; logique de logout isolée.

### Fichiers partagés (édités séquentiellement entre stories)

- `session_controller.dart` : T021 (socle) → T031 (US1) → T038 (US2) → T050 (US4).
- `app_router.dart` : T023 (socle) → T034 (US1) → T040 (US2) → T046 (US3) → T053 (US4).
- `home_screen.dart` : T033 (US1) → T052 (US4).

### Parallel Opportunities

- **Setup** : T003–T007 en parallèle après T001/T002.
- **Foundational tests** : T008–T013 tous en parallèle (fichiers distincts).
- **Foundational impl** : T014–T017 et T020 en parallèle ; puis T018 → T019 → T021 → T022 → T023 → T024 → T025 en chaîne ; T026 après T018.
- **Par story** : les tests marqués [P] en parallèle ; les écrans [P] en parallèle (fichiers distincts) ; les tâches touchant `session_controller.dart` / `app_router.dart` restent séquentielles.

---

## Parallel Example: User Story 1

```bash
# Tests US1 en parallèle (écrire d'abord, doivent échouer) :
Task: "Test unitaire SessionController.login dans mobile/test/features/auth/session_login_test.dart"
Task: "Test widget écran connexion dans mobile/test/features/auth/login_screen_test.dart"
Task: "Test widget écran accueil dans mobile/test/features/home/home_screen_test.dart"
Task: "Test d'intégration connexion/session dans mobile/integration_test/login_session_test.dart"

# Écrans US1 en parallèle (après T031) :
Task: "Écran de connexion dans mobile/lib/features/auth/presentation/login_screen.dart"
Task: "Écran d'accueil dans mobile/lib/features/home/presentation/home_screen.dart"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 : Setup (Flutter SDK, scaffold, deps, env).
2. Phase 2 : Foundational (socle réseau/coffre/erreurs/session/routage) — **CRITIQUE**.
3. Phase 3 : US1 (connexion + accueil + restauration/expiration).
4. **STOP & VALIDER** : tester US1 indépendamment (quickstart US1).
5. Démo MVP.

### Incremental Delivery

1. Setup + Foundational → socle prêt.
2. + US1 → tester → démo (MVP).
3. + US2 (activation) → tester → démo.
4. + US3 (oublié/reset) → tester → démo.
5. + US4 (changement/déconnexion) → tester → démo.
6. Polish (analyze, sécurité, quickstart complet).

### Notes

- [P] = fichiers différents, aucune dépendance en attente.
- Écrire les tests d'une story et les voir **échouer** avant implémentation (Principe III).
- Commit après chaque tâche ou groupe logique.
- **HTTPS strict en prod** ; exception TLS uniquement en profil dev.
- Aucune règle métier réimplémentée côté client — l'API reste l'autorité.
