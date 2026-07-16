# Implementation Plan: Profession du membre

**Branch**: `030-member-profession` | **Date**: 2026-07-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/030-member-profession/spec.md`

## Summary

Ajouter au membre un attribut **profession** optionnel, en texte libre borné. L'approche
est strictement additive et calquée sur le champ `Address` existant : nouvelle propriété
`Member.Profession`, colonne `profession` (migration additive, nullable), extension des DTO
de création/correction/lecture, validation serveur bornée, et ajout d'un champ dans le
formulaire membre du SPA (création + correction) ainsi que sur la fiche. Aucune contrainte
d'unicité, aucune relation, aucun impact sur les présences, comptes ou référentiels.

## Technical Context

**Language/Version**: C# / .NET 10 (API) ; TypeScript / Angular 20.3 (SPA)

**Primary Dependencies**: EF Core 10 (code-first, SQL Server / SQLite en tests), FluentValidation,
Angular Reactive Forms, Vitest (SPA), xUnit (.NET)

**Storage**: SQL Server (prod) piloté code-first ; SQLite en tests d'intégration Infra

**Testing**: xUnit (Domain/Application/Infrastructure/Api) ; Vitest (SPA)

**Target Platform**: API web + console web Angular (back-office bureau)

**Project Type**: Web (API .NET Onion + SPA Angular). Mobile membre hors périmètre.

**Performance Goals**: aucun objectif spécifique — un champ texte de plus sur un flux existant,
sans requête ni index supplémentaire.

**Constraints**: additivité stricte (aucune régression sur membres/présences/référentiels) ;
validation serveur faisant autorité ; migration déterministe et rejouable.

**Scale/Scope**: 1 propriété de domaine, 1 colonne, 1 migration, 3 DTO étendus, 1 mapping,
2 validators, 1 config EF, 3 fichiers SPA (modèles, formulaire, fiche).

## Constitution Check

*GATE: à valider avant Phase 0, re-vérifié après Phase 1.*

| Principe | Statut | Justification |
|---|---|---|
| **I. Onion & couches** | ✅ PASS | La propriété vit dans le Domain ; la validation applicative dans Application ; la persistance en Infrastructure ; l'exposition via DTO en API. Aucune logique métier dans les contrôleurs. |
| **II. Code-first & intégrité** | ✅ PASS | Colonne ajoutée via migration EF versionnée, additive (nullable, aucune donnée existante altérée), rejouable sur base vierge. Champs d'audit inchangés. |
| **III. Tests d'abord (NON-NÉGOCIABLE)** | ✅ PASS | Tests unitaires ajoutés au même changement : normalisation (trim/vide→null), borne de longueur (accepté à la limite, refusé au-delà), mapping DTO, aller-retour création/correction. SPA : tests du formulaire (saisie, effacement). |
| **IV. Sécurité par défaut** | ✅ PASS | Entrée bornée et nettoyée côté serveur (FluentValidation + normalisation handler) indépendamment du client ; stockage via ORM paramétré (aucune concaténation SQL) ; profession = donnée personnelle exposée au strict nécessaire (dans la fiche membre déjà protégée par `manage_members`). Pas d'unicité → pas de fuite par énumération. |
| **V. Contrats d'API explicites** | ✅ PASS | Champ ajouté aux DTO dédiés (`CreateMemberRequest`, `UpdateMemberRequest`, `MemberResponse`) ; ajout **rétrocompatible** (propriété optionnelle) → aucun versionnement de rupture requis. Entités jamais exposées directement. |
| **VI. Traçabilité & audit** | ✅ PASS | Création/correction déjà journalisées (`_audit.Operation`) et horodatées (`updatedt/by` via intercepteur) ; aucun secret ni donnée superflue journalisée (on ne logue pas la valeur de profession). |

**Verdict** : aucun écart. Section *Complexity Tracking* sans objet.

## Project Structure

### Documentation (this feature)

```text
specs/030-member-profession/
├── plan.md              # Ce fichier
├── research.md          # Phase 0 — décisions (longueur, normalisation, texte libre vs référentiel)
├── data-model.md        # Phase 1 — attribut Profession, colonne, contraintes
├── quickstart.md        # Phase 1 — scénarios de validation de bout en bout
├── contracts/
│   └── members-api.md   # Phase 1 — deltas de contrat REST (create/update/read)
└── checklists/
    └── requirements.md  # (créé au /speckit-specify)
```

### Source Code (repository root)

Fichiers **modifiés** (aucune nouvelle couche, aucun nouveau projet) :

```text
src/
├── Lumineux.Domain/
│   └── Entities/Member.cs                         # + propriété Profession (string?)
├── Lumineux.Application/
│   ├── Contracts/Members/MemberDtos.cs            # + Profession dans CreateMemberRequest, MemberResponse
│   ├── Contracts/Members/MemberQueryDtos.cs       # + Profession dans UpdateMemberRequest
│   ├── Members/MemberMapping.cs                   # mappe Profession → MemberResponse
│   ├── Members/CreateMemberHandler.cs             # affecte + normalise Profession (trim, vide→null)
│   ├── Members/UpdateMemberHandler.cs             # idem à la correction
│   ├── Members/CreateMemberValidator.cs           # + règle MaximumLength(profession)
│   └── Members/UpdateMemberValidator.cs           # + règle MaximumLength(profession)
└── Lumineux.Infrastructure/
    ├── Persistence/Configurations/MemberConfiguration.cs   # colonne "profession" HasMaxLength(150)
    └── Persistence/Migrations/<timestamp>_MemberProfession.cs   # migration additive (AddColumn)

web/src/app/features/members/
├── member.models.ts                               # + profession sur les 3 interfaces
└── member-form/member-form.component.ts           # + champ formulaire (création+correction) + fiche

tests (mêmes projets) :
├── Lumineux.Domain.Tests / Application.Tests / Api.Tests   # normalisation, borne, mapping, contrat
└── web (Vitest)                                            # saisie/effacement du champ
```

**Structure Decision** : application web existante (API Onion + SPA Angular). La feature est
un enrichissement transversal d'une entité déjà en place ; elle réutilise intégralement les flux
002 (création/correction/lecture) et 009 (formulaire SPA) sans introduire de module ni de couche.

## Complexity Tracking

Sans objet — la Constitution Check ne relève aucun écart.
