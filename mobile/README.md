# Lumineux — Application mobile membre (Flutter)

Client mobile **côté membre** de Lumineux (feature 025, lot M0 de la piste Flutter).
Couvre le **socle** technique et le **cycle de vie du compte** : connexion, activation à
la première connexion, mot de passe oublié + réinitialisation, changement de mot de passe,
déconnexion. Le **scan de présence** (M1) et les modules suivants ne sont pas inclus.

L'app **consomme l'API Lumineux existante** (`/api/v1/auth/*`) sans aucune évolution d'API.

## Prérequis

- **Flutter** stable ≥ 3.29 / Dart ≥ 3.7 (`flutter --version`).
- Un **émulateur Android** (API 24+) ou **simulateur iOS** (14+), ou un appareil physique.
- L'**API Lumineux** joignable en HTTPS.

## Architecture

Architecture en couches (Principe I « en esprit »), feature-first + socle transverse :

```text
lib/
├── main.dart                     # bootstrap ProviderScope
├── app.dart                      # MaterialApp.router, FR, cycle de vie (reprise)
├── core/
│   ├── config/env.dart           # API_BASE_URL via --dart-define
│   ├── network/                  # dio + intercepteurs (Bearer, erreurs), TokenHolder
│   ├── errors/error_messages.dart# ProblemDetails/code → message FR
│   └── storage/secure_token_store.dart  # coffre sécurisé du jeton
├── routing/app_router.dart       # go_router + redirection de session (garde)
└── features/
    ├── auth/{data,application,presentation}   # DTO, AuthApi, SessionController, écrans
    └── home/presentation                      # accueil + splash
```

- **État & DI** : `flutter_riverpod` (`SessionController`, providers substituables en test).
- **Navigation** : `go_router` avec redirection globale pilotée par l'état de session.
- **Réseau** : `dio` (intercepteur Bearer sur routes protégées, mapping d'erreurs centralisé).
- **Jeton** : `flutter_secure_storage` (Keychain iOS / Keystore Android) — **seul** élément persisté.

## Configuration d'environnement

L'URL de base est injectée au lancement via des profils `--dart-define-from-file` :

| Cible | `API_BASE_URL` |
|-------|----------------|
| Android émulateur (dev) | `https://10.0.2.2:4311` |
| iOS simulateur (dev)    | `https://localhost:4311` |
| Production              | HTTPS strict (voir `env/prod.json`) |

> En **dev uniquement** (`IS_DEV: true`), le certificat auto-signé de l'API est accepté via
> une exception TLS **ciblée**. En **prod**, HTTPS strict, aucune exception (FR-019).

## Mise en route

```bash
flutter pub get

# Lancer avec le profil de dev (URL de base injectée)
flutter run --dart-define-from-file=env/dev.json
```

## Tests

```bash
flutter analyze                 # statique — zéro avertissement
flutter test                    # unitaires + widget
flutter test integration_test --dart-define-from-file=env/dev.json  # e2e (émulateur + API requis)
```

Les tests d'intégration (`integration_test/`) sont **écrits** et marqués `skip` : ils
nécessitent un émulateur/simulateur et l'API dev joignable pour s'exécuter réellement.
La CI (`.github/workflows/mobile-ci.yml`) exécute `flutter analyze` + `flutter test`.

## Sécurité

- Le **jeton** n'est jamais journalisé ni affiché ; il vit uniquement au coffre sécurisé et
  est **purgé** à la déconnexion et sur toute réponse `401`.
- **Aucune règle métier** n'est réimplémentée côté client : l'API reste l'autorité.
- Communication **HTTPS** exclusive (exception TLS dev-only).
