# Implementation Plan: Ajout d'un nouveau membre

**Branch**: `002-member-registration` | **Date**: 2026-07-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-member-registration/spec.md`

## Summary

Permettre au bureau (droit **gestion des membres**) de créer un nouveau membre avec ses informations
de base, en lui attribuant une **référence unique**, une date d'entrée et un statut « actif », puis en
**provisionnant automatiquement un compte** de connexion (identifiant = référence membre, mot de passe
temporaire haché, indicateur « mot de passe à changer »). Les identifiants sont transmis par **e-mail
d'invitation** avec **repli remise-bureau** si le membre n'a pas d'e-mail. Détection des **doublons
d'homonymie** (avertir + confirmer) et **refus** des coordonnées déjà utilisées par un membre actif.
Consultation/recherche et correction des fiches par le bureau.

Approche technique : réutilise la solution **.NET 10 / Onion / SQL Server code-first** de la
fonctionnalité 001. **Enrichit l'entité `Member`** existante (référence, contact, état civil,
rattachements) et ajoute l'entité `MemberAccount`. Nouveaux ports : génération de référence, hachage
de mot de passe, envoi d'e-mail. La création membre + compte est **atomique** ; l'envoi d'e-mail est
**non bloquant** (repli + journalisation). Le parcours de connexion/changement de mot de passe est
**hors périmètre** (dépendance auth).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (solution existante)

**Primary Dependencies**: ASP.NET Core Web API, EF Core 10 (SQL Server), FluentValidation, Serilog,
JWT Bearer (existant). **Ajouts** : hachage de mot de passe via `Microsoft.Extensions.Identity.Core`
(`PasswordHasher<T>`) ; abstraction d'envoi d'e-mail (`IEmailSender`) avec implémentation de
développement journalisée + implémentation SMTP câblée par configuration.

**Storage**: SQL Server — migration d'**enrichissement** de la table `members` + création de
`member_accounts`.

**Testing**: xUnit — unitaires (Domain/Application) sans base ; intégration (Infrastructure/API) sur
SQLite en mémoire, via le harnais existant (`ApiTestFixture`).

**Target Platform**: API .NET (serveur). Consommée par la SPA Angular (bureau) ; la saisie côté
mobile reste possible ultérieurement.

**Project Type**: Web service (API) — extension de la solution existante.

**Performance Goals**: Création d'un membre complet en < 3 min (SC-001) ; recherche de membre
réactive (< 1 s pour un volume communautaire courant).

**Constraints**: Mot de passe **jamais** stocké/journalisé en clair (haché) ; création membre+compte
**atomique** ; envoi d'e-mail **non bloquant** (repli remise-bureau + journalisation) ; secrets SMTP
hors code (configuration/secrets) ; exposition minimale des données personnelles.

**Scale/Scope**: Communauté de plusieurs milliers de membres ; 3 user stories, enrichissement d'1
entité + 1 nouvelle entité + 3 ports.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion | Cas d'usage/entités dans Domain/Application ; e-mail, hachage, référence, persistance derrière des ports implémentés en Infrastructure | ✅ PASS |
| II. Code-First & intégrité BD | Migration d'enrichissement `members` + création `member_accounts` ; FK, unicités (référence, contact actif) ; audit hérité | ✅ PASS — `data-model.md` |
| III. Tests en premier (NON-NÉGOCIABLE) | Tests unitaires Domain/Application (invariants membre, provisionnement, doublons) ; intégration API | ✅ PASS |
| IV. Sécurité par défaut | Mot de passe haché (jamais en clair) ; policy `manage_members` ; validation serveur ; compte à moindre privilège ; secrets SMTP hors code ; pas de secret en journal | ✅ PASS — `research.md` §sécurité |
| V. Contrats d'API explicites | DTO dédiés (jamais l'entité) ; REST `/api/v1/members` ; ProblemDetails ; OpenAPI | ✅ PASS — `contracts/openapi.yaml` |
| VI. Traçabilité & observabilité | Audit création/modification (auteur, horodatage via intercepteur existant) ; refus consignés ; e-mail/échec journalisés sans secret | ✅ PASS |

**Résultat initial : PASS — aucune violation, Complexity Tracking non requise.**

*Re-check post-conception (Phase 1)* : la conception respecte les mêmes principes (ports pour e-mail
et hachage, DTO, migration, atomicité). **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/002-member-registration/
├── plan.md              # Ce fichier
├── research.md          # Phase 0 — décisions techniques
├── data-model.md        # Phase 1 — entités, relations, transitions
├── quickstart.md        # Phase 1 — guide de validation
├── contracts/
│   └── openapi.yaml     # Phase 1 — contrat REST
├── checklists/
│   └── requirements.md  # Checklist qualité (produite)
└── tasks.md             # Phase 2 (/speckit-tasks — NON créé ici)
```

### Source Code (repository root) — extension de la solution existante

```text
src/
├── Lumineux.Domain/
│   ├── Entities/            # Member (enrichie), + MemberAccount (nouvelle)
│   ├── Enums/               # MemberStatus (Active/Archived), Gender si besoin
│   └── Abstractions/        # IMemberRepository, IMemberAccountRepository,
│                            #   IMemberReferenceGenerator, IPasswordHasher, IEmailSender
├── Lumineux.Application/
│   ├── Members/             # CreateMember, SearchMembers, GetMember, UpdateMember (+ validators)
│   └── Contracts/Members/   # DTO d'entrée/sortie (sans secret)
├── Lumineux.Infrastructure/
│   ├── Persistence/         # config EF Member (enrichie) + MemberAccount ; migration
│   ├── Repositories/        # MemberRepository, MemberAccountRepository
│   ├── Security/            # IdentityPasswordHasher (PasswordHasher<T>), MemberReferenceGenerator
│   └── Email/               # IEmailSender : LoggingEmailSender (dev) + SmtpEmailSender (config)
└── Lumineux.Api/
    └── Controllers/         # MembersController ; policy manage_members

tests/
├── Lumineux.Domain.Tests/          # invariants Member/MemberAccount
├── Lumineux.Application.Tests/     # CreateMember/UpdateMember/Search (ports mockés)
├── Lumineux.Infrastructure.Tests/  # unicités (référence, contact actif), référence, hachage
└── Lumineux.Api.Tests/             # endpoints /members (création, doublon, refus, recherche)
```

**Structure Decision**: Extension de la solution Onion existante (feature 001). Aucune nouvelle
solution ; on ajoute des entités, ports, cas d'usage, un contrôleur et une migration. Les ports
`IEmailSender` et `IPasswordHasher` isolent les dépendances externes (SMTP, cryptographie) du Domain,
conformément à la règle de dépendance (Constitution I).

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
