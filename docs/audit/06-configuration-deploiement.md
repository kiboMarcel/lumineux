# 06 — Configuration et déploiement

## Sommaire

1. [Configuration de l'API](#configuration-de-lapi)
2. [Clés par section](#clés-par-section)
3. [Gestion des secrets](#gestion-des-secrets)
4. [Configuration des clients](#configuration-des-clients)
5. [Intégration continue](#intégration-continue)
6. [Hébergement et déploiement](#hébergement-et-déploiement)
7. [Sources analysées](#sources-analysées)

> ⚠️ Conformément aux règles de l'audit, **aucune valeur secrète n'est recopiée**. Les valeurs
> sensibles sont remplacées par `***`. Seules les clés et les valeurs non sensibles sont listées.

## Configuration de l'API

Configuration standard ASP.NET Core par `appsettings.json` + surcharge
`appsettings.Development.json` + (attendu) variables d'environnement / user-secrets en production.
Fichiers : `src/Lumineux.Api/appsettings.json`, `appsettings.Development.json`.

## Clés par section

| Section | Clé | Valeur par défaut (appsettings.json) | Sensible |
|---------|-----|--------------------------------------|----------|
| `ConnectionStrings` | `Default` | `""` (vide — à fournir) | **oui** `***` |
| `Jwt` | `Issuer` / `Audience` | `Lumineux` / `Lumineux` | non |
| `Jwt` | `SigningKey` | `""` (vide en prod) | **oui** `***` |
| `Jwt` | `ExpirationMinutes` | `60` | non |
| `Auth` | `AccessTokenMinutes` | `60` | non |
| `Auth` | `MaxFailedAttempts` | `5` | non |
| `Auth` | `LockoutMinutes` | `15` | non |
| `Auth` | `PasswordMinLength` | `8` | non |
| `Auth` | `PasswordResetMinutes` | `30` | non |
| `Auth` | `PasswordResetUrlBase` | `https://localhost:4200/auth/reset-password` | non |
| `Auth` | `Bootstrap:MemberReference` / `Permissions` | `""` / `[]` | non |
| `AutoClose` | `Enabled` / `PollingIntervalSeconds` / `MaxOpenHours` / `DefaultDurationHours` | `true` / `300` / `3` / `3` | non |
| `Email` | `Provider` | `Logging` | non |
| `Email` | `FromAddress` | `no-reply@lumineux.example` | non |
| `Email` | `Smtp:Host` / `Port` / `UseStartTls` | `""` / `587` / `true` | non |
| `Email` | `Smtp:User` / `Smtp:Password` | `""` / `""` | **oui** `***` |
| `MemberReference` | `Format` | `LUM-{yyyy}-{seq:00000}` | non |
| `Cors` | `AllowedOrigins` | `[]` (aucune origine autorisée par défaut) | non |
| `AllowedHosts` | — | `*` | non |
| `Serilog` | `MinimumLevel.Default` | `Information` | non |

### Différences en développement (`appsettings.Development.json`)

- `ConnectionStrings:Default` : SQL Server local, authentification Windows intégrée.
- `Jwt:SigningKey` : **clé de développement en clair** (explicitement marquée « dev-only,
  override in production »). Voir 07-dette-technique.
- `Cors:AllowedOrigins` : `["http://localhost:4200"]`.

## Gestion des secrets

- **Comportement sûr par défaut** : `ConnectionStrings:Default` et `Jwt:SigningKey` sont **vides** dans
  `appsettings.json` — l'API ne démarre pas correctement sans qu'ils soient fournis hors dépôt.
- En production, les commentaires du code invitent à fournir `Jwt:SigningKey` via **user-secrets ou
  variables d'environnement**. ⚠️ Hypothèse — à confirmer : aucun coffre externe (Key Vault, etc.)
  n'est câblé dans `Program.cs`.
- Migrations : `AppDbContextFactory` lit la chaîne via la variable d'environnement `LUMINEUX_DB`.
- **CORS** : sans origine configurée, aucune requête cross-origin n'est autorisée ; `AllowCredentials`
  n'est **jamais** activé (auth par en-tête Bearer, `Program.cs`).
- **Point de vigilance** : la clé de signature de dev est **committée en clair** dans le dépôt
  (`appsettings.Development.json`). Acceptable pour du dev local, mais à ne jamais réutiliser en prod.

## Configuration des clients

### Console web

- Environnements Angular : `web/src/environments/environment.ts` (dev) et `environment.prod.ts`
  (URL de l'API par build). Build : `ng build` (configuration `production`).

### App mobile

- Profils d'environnement JSON injectés au build (`--dart-define-from-file`) :
  - `env/dev.json` : `https://10.0.2.2:4311` (émulateur Android).
  - `env/device.json` : `https://192.168.2.12:4311` (appareil sur LAN).
  - `env/usb.json` : `https://localhost:4311`.
  - `env/prod.json` : `https://api.lumineux.example`, `IS_DEV=false`.
- Aucun secret dans ces fichiers (uniquement l'URL de base + drapeau dev).

## Intégration continue

- **Un seul workflow** trouvé : `.github/workflows/mobile-ci.yml`.
  - Déclencheurs : push / PR touchant `mobile/**`.
  - Étapes : `flutter pub get` → `flutter analyze` (zéro avertissement) → `flutter test`.
  - Flutter `3.44.5`, canal stable, cache activé.
- **Aucun pipeline CI pour l'API .NET ni pour la console web** n'a été trouvé dans `.github/`.
  Voir 07-dette-technique (Majeur) : les 373 tests backend et les tests web ne sont pas exécutés en CI.

## Hébergement et déploiement

- ⚠️ Hypothèse — à confirmer : **aucun artefact de déploiement** trouvé (pas de `Dockerfile`,
  `docker-compose`, manifeste IIS `web.config` de déploiement, script PowerShell de release,
  `azure-pipelines.yml`). Le mode d'hébergement de l'API et de la SPA n'est pas versionné dans le dépôt.
- L'API est une application ASP.NET Core autonome (Kestrel) ; le déploiement cible (IIS, service,
  conteneur, PaaS) n'est pas déterminable depuis le code.
- La SPA se build en fichiers statiques (`web/dist/`) à servir par un hébergeur/CDN — modalités non versionnées.
- Étapes minimales de mise en service **déduites** :
  1. Provisionner SQL Server et appliquer les migrations (`dotnet ef database update`).
  2. Fournir `ConnectionStrings:Default`, `Jwt:SigningKey`, `Email:*` (si SMTP), `Cors:AllowedOrigins`,
     `Auth:PasswordResetUrlBase` (origine de prod de la SPA).
  3. Déployer l'API, la SPA (build prod), publier l'app mobile avec `env/prod.json`.

## Sources analysées

- `src/Lumineux.Api/appsettings.json`, `appsettings.Development.json`
- `src/Lumineux.Infrastructure/Persistence/AppDbContextFactory.cs`, `DependencyInjection.cs`
- `.github/workflows/mobile-ci.yml`
- `mobile/env/*.json`, `web/src/environments/` (existence)
</content>
