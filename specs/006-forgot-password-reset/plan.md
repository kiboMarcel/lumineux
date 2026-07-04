# Implementation Plan: Mot de passe oublié

**Branch**: `006-forgot-password-reset` | **Date**: 2026-07-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/006-forgot-password-reset/spec.md`

## Summary

Ajouter la **récupération autonome de mot de passe** par un canal email, en deux endpoints
**anonymes** :

1. `POST /api/v1/auth/forgot-password` — demande de réinitialisation par **référence**. Réponse
   **générique** (200) dans tous les cas (anti-énumération, FR-002) ; si le compte est **actif** et
   possède un **email**, génération d'un **jeton à usage unique** (32 octets), persistance de sa
   seule **empreinte** (SHA-256), envoi d'un lien contenant le jeton en clair par email.
2. `POST /api/v1/auth/reset-password` — validation du jeton + nouveau mot de passe conforme à la
   **politique existante** (feature 003). Succès → mise à jour de l'empreinte du mot de passe,
   **consommation** du jeton, **remise à zéro** des compteurs d'échec et **levée du verrouillage**
   (intégration feature 003), réponse 204. Jeton invalide/expiré/consommé → **401 générique**.

Approche technique : extension de la solution **.NET 10 / Onion / SQL Server code-first** existante.
- **Nouvelle entité de domaine** `PasswordResetToken` (table `password_reset_tokens`) avec fabrique
  `Issue`, `IsUsable(now)`, `Consume(now)`.
- **Nouveaux ports** : `IPasswordResetTokenRepository` (persistance) et `IResetTokenService`
  (génération + hachage du jeton) — ce dernier calqué sur l'`IQrTokenService` existant.
- **Réutilisation** des méthodes de domaine `MemberAccount.ChangePassword` (nouvelle empreinte +
  levée de `MustChangePassword`) et `MemberAccount.RegisterSuccessfulLogin` (reset compteurs +
  levée du verrouillage) — aucune nouvelle méthode de compte requise.
- **Extension** du port `IEmailSender` avec `SendPasswordResetAsync` ; les deux implémentations
  existantes (`LoggingEmailSender` dev, `SmtpEmailSender` prod) sont complétées.
- **Extension** de `AuthOptions` : `PasswordResetMinutes` (défaut 30) et `PasswordResetUrlBase`
  (base d'URL de la SPA pour le lien).
- **Anti-timing** sur `/forgot-password` via une opération de hachage factice quand aucun jeton
  n'est émis — même stratégie que le hash factice de `/auth/login` (feature 003).

Aucune modification de `/auth/login`, `/auth/change-password`, ni du verrouillage anti-force brute
(FR-013). Le super-admin (feature 005) emprunte le **même** chemin (FR-014).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (solution existante)

**Primary Dependencies**: ASP.NET Core Web API, EF Core 10 (SQL Server), FluentValidation, Serilog
(existants). Hachage du mot de passe via `IPasswordHasher` (existant, feature 002). Génération/hachage
du jeton via `System.Security.Cryptography` (`RandomNumberGenerator`, `SHA256`) — BCL, comme
`QrTokenService`.

**Storage**: SQL Server — migration **additive** : nouvelle table `password_reset_tokens`
(FK vers `member_accounts`, index unique sur l'empreinte du jeton). Aucune colonne modifiée sur
les tables existantes.

**Testing**: xUnit — unitaires (Domain : cycle de vie du jeton ; Application : demande générique,
reset, anti-rejeu) sans base ; intégration (API) sur le harnais existant (SQLite in-memory).

**Target Platform**: API .NET, consommée par la SPA Angular (page de saisie du nouveau mot de passe)
et potentiellement l'app mobile Flutter.

**Project Type**: Web service (API) — extension de la solution existante.

**Performance Goals**: Parcours complet (demande → email → clic → saisie → connexion) en < 5 min
côté utilisateur (SC-001) ; réponses `/forgot-password` à coût de calcul égalisé (anti-timing).

**Constraints**: Réponse **strictement identique** sur `/forgot-password` quel que soit l'état du
compte (SC-002) ; refus **générique** unique sur `/reset-password` pour jeton
inexistant/expiré/consommé (SC-003) ; **jeton en clair jamais persisté ni journalisé** (SC-004,
FR-009) ; **usage unique** garanti (SC-005) ; entropie ≥ 32 octets (FR-015) ; index unique sur
l'empreinte (FR-016).

**Scale/Scope**: Toute la communauté (tout membre peut demander un reset) ; 2 user stories P1 ;
1 nouvelle entité + 2 nouveaux ports + 2 cas d'usage + 2 endpoints.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion | Cas d'usage dans Application ; cycle de vie du jeton **dans le Domaine** (`PasswordResetToken`) ; génération/hachage du jeton derrière `IResetTokenService` (Infra) ; persistance derrière `IPasswordResetTokenRepository` ; envoi email derrière `IEmailSender`. Dépendances vers l'intérieur. | ✅ PASS |
| II. Code-First & intégrité BD | Migration additive `password_reset_tokens` ; FK vers `member_accounts`, index **unique** sur l'empreinte (FR-016) ; audit hérité (`AuditColumns`). | ✅ PASS — voir `data-model.md` |
| III. Tests en premier (NON-NÉGOCIABLE) | Unitaires Domain (Issue/IsUsable/Consume, expiration, rejeu) + Application (réponse générique, anti-timing, reset, anti-rejeu, levée du verrouillage) ; intégration API (200/204/400/401). | ✅ PASS |
| IV. Sécurité par défaut | Anti-énumération (réponse générique), anti-timing (hash factice), jeton haute entropie, **seule l'empreinte** persistée, jeton/mot de passe **jamais journalisés**, validation serveur du nouveau mot de passe, usage unique + expiration. | ✅ PASS — `research.md` §sécurité |
| V. Contrats d'API explicites | DTO dédiés (`ForgotPasswordRequest`, `ResetPasswordRequest`) ; aucun secret exposé ; REST `/api/v1/auth/*` ; ProblemDetails ; OpenAPI. | ✅ PASS — `contracts/openapi.yaml` |
| VI. Traçabilité & observabilité | Journalisation des événements (demande, émission, envoi, tentative, succès, refus) via `IAuditLogger`, **sans** jeton en clair ni mot de passe (FR-010) ; horodatages via `IClock` serveur. | ✅ PASS |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception (entité de domaine, deux ports, réutilisation
des méthodes `MemberAccount`, migration additive, DTO dédiés) respecte l'ensemble des principes.
**PASS confirmé** — aucune dérogation à justifier.

## Project Structure

### Documentation (this feature)

```text
specs/006-forgot-password-reset/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/openapi.yaml
├── checklists/requirements.md
└── tasks.md   (/speckit-tasks — non créé ici)
```

### Source Code (repository root) — extension de la solution existante

```text
src/
├── Lumineux.Domain/
│   ├── Entities/            # PasswordResetToken (nouvelle : Issue / IsUsable / Consume)
│   │                        # MemberAccount (INCHANGÉ — réutilise ChangePassword + RegisterSuccessfulLogin)
│   └── Abstractions/        # IPasswordResetTokenRepository (nouveau), IResetTokenService (nouveau),
│                            #   IEmailSender (étendu : SendPasswordResetAsync)
├── Lumineux.Application/
│   ├── Auth/                # RequestPasswordResetHandler, ResetPasswordHandler (+ validators)
│   ├── Abstractions/        # AuthOptions (étendu : PasswordResetMinutes, PasswordResetUrlBase)
│   └── Contracts/Auth/      # ForgotPasswordRequest, ResetPasswordRequest (DTO)
├── Lumineux.Infrastructure/
│   ├── Security/            # ResetTokenService (IResetTokenService — RNG 32o + SHA-256)
│   ├── Email/               # LoggingEmailSender + SmtpEmailSender (SendPasswordResetAsync ajouté)
│   ├── Persistence/
│   │   ├── Configurations/  # PasswordResetTokenConfiguration (table + index unique empreinte)
│   │   └── Migrations/      # <timestamp>_PasswordReset (additive)
│   └── Repositories/        # PasswordResetTokenRepository
└── Lumineux.Api/
    └── Controllers/         # AuthController (+ /forgot-password, /reset-password)

tests/
├── Lumineux.Domain.Tests/          # PasswordResetTokenTests (cycle de vie, expiration, rejeu)
├── Lumineux.Application.Tests/     # RequestPasswordReset (générique, anti-timing, email/no-email),
│                                   #   ResetPassword (succès, anti-rejeu, verrouillage levé, mdp faible)
└── Lumineux.Api.Tests/             # AuthForgotPassword / AuthResetPassword (200/204/400/401 génériques)
```

**Structure Decision**: Extension de la solution Onion existante. Le **cycle de vie du jeton**
(validité, consommation) est **dans le Domaine** (`PasswordResetToken`), conformément à la règle de
dépendance (Constitution I). La **génération et le hachage** du jeton sont un port
(`IResetTokenService`, implémenté en Infrastructure — calque de `IQrTokenService`), tout comme la
**persistance** (`IPasswordResetTokenRepository`) et l'**envoi email** (`IEmailSender` étendu). Les
cas d'usage orchestrent dans Application. La mise à jour du compte réutilise les méthodes de domaine
existantes de `MemberAccount` — aucune duplication.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
