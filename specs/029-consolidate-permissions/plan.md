# Implementation Plan: Consolidation du RBAC sur les profils du bureau (retrait du mécanisme hérité)

**Branch**: `029-consolidate-permissions` | **Date**: 2026-07-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/029-consolidate-permissions/spec.md`

## Summary

Nettoyage de dette **M3/M4** : retirer **entièrement** le mécanisme hérité de « permissions directes » pour
ne conserver **qu'une seule source de vérité** des droits — les **profils du bureau** (features 004/011). Le
comportement d'autorisation est **inchangé** : la lecture des droits effectifs
(`GetPermissionsAsync` → renommé `IEffectivePermissionsReader.GetEffectivePermissionsAsync`) lit **déjà et
uniquement** les profils (vérifié : `MemberBureauProfiles ⋈ BureauProfilePermissions`, aucune union avec
`member_permissions`). Sont **supprimés** : l'entité `MemberPermission` + sa config EF + la table
`member_permissions` (**migration de suppression**), les opérations `HasPermissionAsync`/`AddAsync` (et
`SaveChangesAsync`) du port, le `PermissionBootstrapper` et le `BureauProfilesBootstrapper` (aucune donnée
héritée à migrer — projet en dev), et la configuration `Auth:Bootstrap`. Le port est **renommé** pour lever
le dernier nom ambigu. L'**amorçage de l'admin initial** (setup 005) crée déjà un profil « Administrateur »
et reste inchangé. Tests adaptés (retrait des tests de coexistence/migration, mise à jour des mocks au
nouveau nom). Aucun changement de contrat d'API (mêmes claims, mêmes 403).

## Technical Context

**Language/Version** : C# / .NET (API `src/`, architecture Onion) + xUnit (`tests/`).

**Primary Dependencies** : existantes — EF Core (SQL Server, code-first + migrations). **Aucune** nouvelle
dépendance. **Aucun** changement SPA/mobile (les clients ne consomment que les claims, inchangés).

**Storage** : SQL Server. Évolution **destructive-contrôlée** : **suppression** de la table
`member_permissions` (migration EF). Aucune autre table touchée. Aucune donnée à préserver (dev).

**Testing** : xUnit (Domain + Application + Api + Infrastructure). Objectif : suite **verte** et reflétant la
source unique ; les tests s'appuyant sur l'ancien mécanisme sont **retirés** ou **adaptés** (droits via
profils).

**Target Platform** : API .NET (serveur). Pas de client impacté.

**Performance Goals** : neutre (retrait de code/tables) ; un service de moins au démarrage.

**Constraints** : **comportement d'autorisation inchangé** (mêmes droits dans le jeton, mêmes 403) ; **zéro
vestige** (code/config/schéma) ; **setup admin préservé** (profil « Administrateur ») ; audit des refus
conservé.

**Scale/Scope** : ~1 entité + 1 config EF + 1 DbSet + 1 migration (drop) + 1 port renommé/réduit + 1 repo
renommé + 3 appelants mis à jour + 2 bootstrappers supprimés + 1 section de config retirée + ~5 fichiers de
tests retirés/adaptés.

## Constitution Check

*GATE : doit passer avant Phase 0 ; re-vérifié après Phase 1.*

| Principe | Applicabilité | Verdict |
|----------|---------------|---------|
| **I. Onion & séparation des couches** | Le **port** (Domain.Abstractions) est renommé/réduit ; l'implémentation (Infra) et les appelants (Application) suivent. Suppression d'infra (bootstrappers, config EF) sans logique métier déplacée. Règle de dépendance préservée. | ✅ PASS |
| **II. Code-First & intégrité BD** | **Suppression** de table via **migration** versionnée, déterministe/rejouable ; aucune autre contrainte impactée. | ✅ PASS |
| **III. Tests en premier (NON-NÉGOCIABLE)** | Non-régression garantie par les tests **conservés** (Login/Activate/Setup/Union de profils) mis à jour au nouveau nom ; les tests de **coexistence/migration** (obsolètes) sont retirés. CI verte bloquante. | ✅ PASS |
| **IV. Sécurité par défaut** | Autorisation **inchangée** (mêmes claims, mêmes 403) ; **réduction de la surface** (code mort retiré) ; audit des refus conservé. | ✅ PASS |
| **V. Contrats d'API explicites** | **Aucun** changement de contrat d'API (droits identiques, mêmes réponses). Le renommage est **interne** (non exposé). | ✅ PASS |
| **VI. Traçabilité & audit** | Les refus d'accès restent tracés ; aucun horodatage métier concerné. | ✅ PASS |

**Résultat** : aucun écart. Section *Complexity Tracking* laissée vide.

## Project Structure

### Documentation (this feature)

```text
specs/029-consolidate-permissions/
├── plan.md              # Ce fichier
├── research.md          # Décisions techniques (Phase 0)
├── data-model.md        # Entité/table supprimée & source unique (Phase 1)
├── quickstart.md        # Guide de validation (Phase 1)
├── contracts/           # Invariant d'autorisation préservé (Phase 1)
│   └── authorization-invariant.md
└── tasks.md             # Phase 2 (/speckit-tasks — non créé ici)
```

### Source Code (repository root)

```text
src/
├── Lumineux.Domain/
│   ├── Entities/MemberPermission.cs               # SUPPRIMÉ
│   └── Abstractions/IMemberPermissionRepository.cs # RENOMMÉ → IEffectivePermissionsReader
│                                                    #   (garde GetEffectivePermissionsAsync ; retire
│                                                    #    HasPermissionAsync/AddAsync/SaveChangesAsync)
├── Lumineux.Application/
│   ├── Auth/LoginHandler.cs                        # MODIF : port renommé + GetEffectivePermissionsAsync
│   ├── Auth/ActivateAccountHandler.cs              # MODIF : idem
│   ├── Setup/InstallFirstAdminHandler.cs           # MODIF : idem (setup admin par profils inchangé)
│   └── Abstractions/AuthOptions.cs                 # MODIF : retrait de Bootstrap + BootstrapOptions
├── Lumineux.Infrastructure/
│   ├── Repositories/MemberPermissionRepository.cs  # RENOMMÉ → EffectivePermissionsReader (requête profils)
│   ├── Persistence/AppDbContext.cs                 # MODIF : retrait du DbSet MemberPermissions
│   ├── Persistence/Configurations/MemberPermissionConfiguration.cs # SUPPRIMÉ
│   ├── Persistence/Migrations/<ts>_RemoveMemberPermissions.cs       # NOUVEAU : drop table
│   ├── Security/PermissionBootstrapper.cs          # SUPPRIMÉ
│   ├── Security/BureauProfilesBootstrapper.cs      # SUPPRIMÉ
│   └── DependencyInjection.cs                      # MODIF : registration renommée ; retrait des 2 bootstrappers
└── Lumineux.Api/appsettings.json                  # MODIF : retrait de la section Auth:Bootstrap

tests/
├── Lumineux.Api.Tests/
│   ├── BureauProfilesBootstrapperTests.cs          # SUPPRIMÉ (bootstrapper retiré)
│   ├── SetupBootstrapCoexistenceTests.cs           # SUPPRIMÉ (coexistence disparue)
│   ├── MemberPermissionRepositoryUnionTests.cs     # MODIF : renommer refs (teste l'union de PROFILS — conservé)
│   └── Infrastructure/ApiTestFixture.cs            # MODIF : retirer la purge de MemberPermissions
└── Lumineux.Application.Tests/
    ├── LoginTests.cs / ActivateAccountTests.cs / InstallFirstAdminTests.cs # MODIF : mocks au nouveau nom
```

**Structure Decision** : suppression **transverse** mais bornée d'un vestige technique, sans nouveau module.
Le point pivot est le **port renommé** (`IEffectivePermissionsReader`) et la **migration de drop**. Aucun
client (SPA/mobile) impacté ; le contrat d'autorisation est explicitement **préservé** (voir contracts/).

## Complexity Tracking

*Aucun écart à la constitution — section vide.*
