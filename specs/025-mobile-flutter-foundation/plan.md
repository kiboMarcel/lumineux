# Implementation Plan: Application mobile membre — socle & cycle de vie du compte

**Branch**: `025-mobile-flutter-foundation` | **Date**: 2026-07-07 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/025-mobile-flutter-foundation/spec.md`

## Summary

Créer le **premier client mobile** de Lumineux en **Flutter** (dossier `mobile/` du mono-dépôt), destiné
au **membre** (compte simple), et livrer le **cycle de vie complet du compte** : connexion, activation à la
première connexion, mot de passe oublié + réinitialisation, changement de mot de passe, déconnexion — avec
un **socle** technique réutilisable (navigation gardée, client réseau à intercepteurs, coffre sécurisé du
jeton, gestion centralisée des erreurs). L'app **consomme l'API existante** (`/api/v1/auth/*`, features
003/006/007) **sans aucune évolution d'API ni migration**. Approche : architecture en couches
(présentation / application / données / socle), état & DI via Riverpod, routage `go_router` avec
redirection de session, réseau via `dio` + intercepteurs (Bearer, erreurs), jeton persisté dans
`flutter_secure_storage`, tests unitaires + widget + intégration (Principe III).

## Technical Context

**Language/Version** : Dart ≥ 3.7 · Flutter stable ≥ 3.29

**Primary Dependencies** : `flutter_riverpod` (état/DI), `go_router` (navigation + garde), `dio` (HTTP +
intercepteurs), `flutter_secure_storage` (coffre jeton) ; **dev** : `flutter_test`, `mocktail`,
`integration_test`, `flutter_lints`.

**Storage** : coffre sécurisé de l'OS (Keychain iOS / Keystore Android) pour le **jeton** uniquement.
Aucune base de données locale. Aucune base serveur touchée.

**Testing** : `flutter test` (unitaires + widget), `integration_test` (parcours de bout en bout),
`flutter analyze` (statique).

**Target Platform** : Android (API 24+) et iOS (14+), smartphone.

**Project Type** : Application mobile (nouveau client dans le mono-dépôt, dossier `mobile/`).

**Performance Goals** : démarrage à froid < 3 s ; restauration de session (lecture coffre) quasi-instantanée ;
transitions d'écran fluides (60 fps). Objectifs UX standards mobile, non critiques.

**Constraints** : **HTTPS exclusif** (exception TLS dev-only, ciblée) ; **aucun secret** hors coffre
sécurisé / hors journaux ; **aucune règle métier dupliquée** (API autorité) ; français ; aucune évolution
d'API. Réseau incertain (terrain) → états de chargement/erreur explicites, pas de rafraîchissement de jeton.

**Scale/Scope** : ~6 écrans, ~6 endpoints consommés, 1 persona (membre). Petit périmètre, socle extensible
(scan M1 à venir).

## Constitution Check

*GATE : doit passer avant Phase 0 ; re-vérifié après Phase 1.*

| Principe | Applicabilité au client mobile M0 | Verdict |
|----------|-----------------------------------|---------|
| **I. Onion & séparation des couches** | Appliqué **en esprit** au client : couches **présentation** (écrans/widgets) → **application** (contrôleurs Riverpod, politique) → **données** (API auth, coffre) → **socle** (client réseau, config, erreurs). Dépendances vers l'intérieur ; accès réseau/stockage **encapsulés** derrière des abstractions substituables en test. Aucune règle métier dans les widgets. | ✅ PASS |
| **II. Code-First & intégrité BD** | **N/A** : aucune base serveur ni migration ; le seul stockage est le coffre sécurisé de l'appareil (secret opaque). | ✅ N/A |
| **III. Tests en premier (NON-NÉGOCIABLE)** | Logique applicative (session, politique de mot de passe, mapping d'erreurs, client API) couverte par **tests unitaires** ; écrans par **tests widget** ; cycle de vie par **test d'intégration**. Test-first pour la logique. CI verte exigée. | ✅ PASS |
| **IV. Sécurité par défaut** | Jeton au **coffre sécurisé** uniquement, purge à la déconnexion/401 ; **HTTPS** (dev exception ciblée, prod strict) ; **anti-énumération** (message générique oublié) ; **aucun secret journalisé** ; validation serveur **autorité** (client = confort). Entrées transmises telles quelles à l'API qui valide. | ✅ PASS |
| **V. Contrats d'API explicites** | Consomme des contrats **existants et versionnés** (`/api/v1/auth/*`) ; DTO client **miroir** des DTO serveur (documentés dans `contracts/`) ; aucune exposition d'entité ; aucune évolution de contrat. | ✅ PASS |
| **VI. Traçabilité & observabilité** | Journalisation **minimale sans secret** (statut + code métier) ; horodatages métier restent côté serveur (N/A ici) ; erreurs consignées pour diagnostic sans données sensibles. | ✅ PASS |

**Résultat** : aucun écart. Section *Complexity Tracking* laissée vide.

> Note workflow/constitution : l'implémentation nécessitera l'installation du **Flutter SDK** (appel
> réseau / téléchargement) → **approbation explicite requise** avant `/speckit-implement` (action sensible
> selon le guidage runtime). Sans incidence sur `plan`/`tasks`.

## Project Structure

### Documentation (this feature)

```text
specs/025-mobile-flutter-foundation/
├── plan.md              # Ce fichier
├── research.md          # Décisions techniques (Phase 0)
├── data-model.md        # Entités/états client (Phase 1)
├── quickstart.md        # Guide de validation (Phase 1)
├── contracts/           # Contrats consommés + contrats UI (Phase 1)
│   ├── api-consumption.md
│   └── navigation.md
└── tasks.md             # Phase 2 (/speckit-tasks — non créé ici)
```

### Source Code (repository root)

```text
mobile/                              # NOUVEAU client Flutter (mono-dépôt)
├── lib/
│   ├── main.dart                    # bootstrap + ProviderScope
│   ├── app.dart                     # MaterialApp.router, thème FR, localisation
│   ├── core/
│   │   ├── config/env.dart          # API_BASE_URL via --dart-define
│   │   ├── network/dio_client.dart  # dio + intercepteurs (bearer, erreurs)
│   │   ├── network/api_exception.dart
│   │   ├── errors/error_messages.dart   # ProblemDetails/code → message FR
│   │   └── storage/secure_token_store.dart  # flutter_secure_storage
│   ├── routing/
│   │   └── app_router.dart          # go_router + redirection de session
│   └── features/
│       ├── auth/
│       │   ├── data/auth_api.dart           # login/activate/forgot/reset/change/me
│       │   ├── data/auth_dtos.dart          # requêtes + TokenResponse + CurrentUser
│       │   ├── application/session_controller.dart  # Notifier: état de session
│       │   ├── application/password_policy.dart     # validation client (confort)
│       │   └── presentation/
│       │       ├── login_screen.dart
│       │       ├── activate_screen.dart
│       │       ├── forgot_password_screen.dart
│       │       ├── reset_password_screen.dart
│       │       └── change_password_screen.dart
│       └── home/
│           └── presentation/home_screen.dart
├── test/                            # unitaires + widget
│   ├── core/ (error_messages, secure_token_store, dio_client)
│   └── features/auth/ (session_controller, password_policy, auth_api, *screens*)
├── integration_test/
│   └── account_lifecycle_test.dart
├── pubspec.yaml
├── analysis_options.yaml            # include: flutter_lints
├── env/                             # profils --dart-define-from-file (dev/prod)
│   ├── dev.json
│   └── prod.json
├── android/ · ios/                  # plateformes générées (flutter create)
└── README.md                        # démarrage, profils, tests
```

**Structure Decision** : nouveau dossier **`mobile/`** à la racine du mono-dépôt (symétrique de `web/`
pour la SPA), architecture **feature-first** avec un **socle transverse** `core/` et un **routage** gardé.
Le back-office bureau reste sur `web/` ; l'API sous `src/` est inchangée.

## Complexity Tracking

*Aucun écart à la constitution — section vide.*
