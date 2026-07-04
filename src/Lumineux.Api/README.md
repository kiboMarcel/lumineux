# Lumineux — API de gestion de la présence

API .NET 10 (architecture Onion) pour la gestion des présences de la communauté Lumineux :
sessions de réunion, code QR rotatif, pointage par scan, synchronisation hors ligne, ajout manuel,
consultation et clôture avec propagation de l'heure de fin.

## Architecture (Onion)

```
src/
├── Lumineux.Domain          # entités, invariants, ports (interfaces), exceptions métier
├── Lumineux.Application     # cas d'usage, DTO, validators (FluentValidation)
├── Lumineux.Infrastructure  # EF Core (SQL Server), repositories, QR/TOTP, JWT, audit, clôture auto
└── Lumineux.Api             # ASP.NET Core Web API : contrôleurs, middleware, DI, Swagger
```

Règle de dépendance : `Domain` ← `Application` ← `Infrastructure`/`Api`. Voir
[la constitution](../../.specify/memory/constitution.md) et les
[artefacts de conception](../../specs/001-attendance-management/).

## Prérequis

- .NET 10 SDK
- SQL Server (instance locale, LocalDB ou conteneur)
- Outil `dotnet-ef` (`dotnet tool install --global dotnet-ef`)

## Configuration

Fournir les paramètres **hors du code source** (user-secrets en dev, variables d'environnement en prod) :

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=...;Database=Lumineux;..." --project src/Lumineux.Api
dotnet user-secrets set "Jwt:SigningKey" "<clé-de-signature-robuste>" --project src/Lumineux.Api
```

Sections de configuration : `ConnectionStrings:Default`, `Jwt` (Issuer/Audience/SigningKey/ExpirationMinutes),
`AutoClose` (Enabled/PollingIntervalSeconds/MaxOpenHours/DefaultDurationHours),
`Auth` (`AccessTokenMinutes`, `MaxFailedAttempts`, `LockoutMinutes`, `PasswordMinLength`,
`Bootstrap:MemberReference`, `Bootstrap:Permissions`), `Serilog`.

> ⚠️ La clé JWT de `appsettings.Development.json` est réservée au développement. **Ne jamais l'utiliser en production.**

## Base de données (code-first)

```bash
# Appliquer les migrations
dotnet ef database update --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Infrastructure

# Ajouter une migration
dotnet ef migrations add <Nom> --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Infrastructure --output-dir Persistence/Migrations
```

Migrations existantes : `InitialAttendance` (antennes, membres + FK antenne d'origine, sessions),
`AddAttendances` (présences + index uniques filtrés), `MemberRegistration` (enrichissement `members`,
`member_accounts`, nomenclatures civilités/pays/villes/districts, index uniques filtrés contacts),
`Authentication` (colonnes de sécurité sur `member_accounts` + table `member_permissions`),
`BureauProfiles` (feature 004 : tables `bureau_profiles`, `bureau_profile_permissions`,
`member_bureau_profiles` avec index d'unicité).
Voir aussi [MIGRATION_NOTES](../../specs/002-member-registration/MIGRATION_NOTES.md) (backfill `reference`).

## Configuration e-mail (feature 002)

Section `Email` : `Provider` = `Logging` (dev, journalise sans mot de passe) ou `Smtp` (prod).
Les paramètres SMTP (`Email:Smtp:Host/Port/User/Password`) et `MemberReference:Format` sont fournis
par configuration/secrets. Le mot de passe temporaire d'un nouveau membre n'est jamais journalisé ;
en l'absence d'e-mail (ou en cas d'échec d'envoi), il est renvoyé une seule fois dans la réponse de
création pour remise par le bureau.

## Exécution

```bash
dotnet run --project src/Lumineux.Api   # Swagger sur /swagger en développement
```

## Tests

```bash
dotnet test                             # 104 tests (unitaires Domain/Application + intégration Infra/API)
```

## Principaux endpoints (`/api/v1`)

| Méthode | Route | Droit | Rôle |
|---------|-------|-------|------|
| POST | `/attendance-sessions` | `manage_attendance` | Démarrer une session |
| GET | `/attendance-sessions/{id}` | `manage_attendance` | Consulter une session |
| GET | `/attendance-sessions/{id}/qr` | `manage_attendance` | Jeton QR courant |
| POST | `/attendance-sessions/{id}/close` | `manage_attendance` | Clôturer + propager l'heure de fin |
| POST | `/attendance-sessions/{id}/scan` | authentifié | Pointer par scan |
| POST | `/attendance-sessions/{id}/scan/batch` | authentifié | Synchroniser les scans hors ligne |
| POST | `/attendance-sessions/{id}/attendances` | `manage_attendance` | Ajout manuel |
| GET | `/attendance-sessions/{id}/attendances` | `manage_attendance` | Lister les présences |
| DELETE | `/attendance-sessions/{id}/attendances/{memberId}` | `manage_attendance` | Retirer une présence |
| POST | `/members` | `manage_members` | Créer un membre + provisionner le compte |
| GET | `/members` | `manage_members` | Rechercher / lister les membres |
| GET | `/members/{id}` | `manage_members` | Consulter une fiche membre |
| PUT | `/members/{id}` | `manage_members` | Corriger une fiche membre |
| POST | `/setup/first-admin` | anonyme (verrou : 0 admin) | **Installer le premier administrateur** sur base vierge (feature 005) |
| POST | `/auth/login` | anonyme | Connexion + jeton d'accès (verrouillage anti-force brute) |
| POST | `/auth/activate` | anonyme | Première connexion : mot de passe temporaire → nouveau + activation |
| POST | `/auth/change-password` | authentifié | Changer son mot de passe |
| GET | `/permissions` | `manage_bureau_profiles` OU `manage_members` | Référentiel figé des droits |
| GET | `/bureau-profiles` | `manage_bureau_profiles` OU `manage_members` | Lister les profils du bureau |
| GET | `/bureau-profiles/{id}` | `manage_bureau_profiles` OU `manage_members` | Détail d'un profil + titulaires |
| POST | `/bureau-profiles` | `manage_bureau_profiles` | Créer un profil |
| PUT | `/bureau-profiles/{id}` | `manage_bureau_profiles` | Modifier un profil (garde-fou dernier admin) |
| DELETE | `/bureau-profiles/{id}` | `manage_bureau_profiles` | Supprimer un profil non attribué (garde-fou dernier admin) |
| GET | `/members/{id}/bureau-profiles` | `manage_bureau_profiles` OU `manage_members` | Profils d'un membre + droits effectifs |
| POST | `/members/{id}/bureau-profiles` | `manage_bureau_profiles` | Attribuer un profil (idempotent, refus si membre inactif) |
| DELETE | `/members/{id}/bureau-profiles/{profileId}` | `manage_bureau_profiles` | Révoquer une attribution (garde-fou dernier admin) |

Contrats de référence : [présence](../../specs/001-attendance-management/contracts/openapi.yaml),
[membres](../../specs/002-member-registration/contracts/openapi.yaml),
[authentification](../../specs/003-authentication-login/contracts/openapi.yaml),
[profils du bureau](../../specs/004-bureau-profiles/contracts/openapi.yaml),
[installation du premier admin](../../specs/005-first-admin-setup/contracts/openapi.yaml).

Depuis la feature 004, les droits sont attribués **exclusivement via des profils du bureau**. Le
mécanisme `Auth:Bootstrap:*` reste disponible comme filet d'urgence idempotent ; au premier
démarrage, `BureauProfilesBootstrapper` crée automatiquement un profil « Amorçage » à partir des
droits directs historiquement présents dans `member_permissions` (source héritée conservée).

## Sécurité

Voir la [revue de sécurité présence](../../specs/001-attendance-management/checklists/security.md)
et la [revue de sécurité authentification](../../specs/003-authentication-login/checklists/security.md).
Points clés : validation serveur, EF paramétré, JWT + policy, `qrSecret` non exposé, secrets hors code,
messages 401 génériques anti-énumération, verrouillage temporaire configurable, aucun secret ni jeton
en clair dans les journaux.
