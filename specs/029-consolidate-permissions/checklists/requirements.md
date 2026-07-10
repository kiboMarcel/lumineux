# Specification Quality Checklist: Consolidation du RBAC sur les profils (retrait du mécanisme hérité)

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-07-10
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- **Nettoyage de dette M3/M4** : refonte interne **sans changement de comportement observable** (mêmes
  droits dans le jeton, mêmes 403). La spec insiste donc sur la **non-régression** (US1/US2) autant que sur
  la maintenabilité (US3).
- Faits vérifiés dans le code (aident le `/speckit-plan`) :
  - `GetPermissionsAsync` dérive les droits des **profils** (`MemberBureauProfiles` ⋈ `BureauProfilePermissions`)
    — c'est la source des claims (LoginHandler/ActivateAccountHandler/InstallFirstAdminHandler).
  - `HasPermissionAsync` lit **uniquement** `member_permissions` et n'est appelée **que** par le
    `PermissionBootstrapper` → chemin hérité isolé.
  - `InstallFirstAdminHandler` (feature 005) crée déjà un **profil « Administrateur »** (tous droits du
    catalogue) et l'attribue → **setup admin déjà par profils**, indépendant de l'ancienne table.
  - Cibles de retrait : entité `MemberPermission` + config EF + table `member_permissions` ; méthodes
    `HasPermissionAsync`/`AddAsync` de `IMemberPermissionRepository` ; `PermissionBootstrapper` ;
    `BureauProfilesBootstrapper` ; paramètre `Auth:Bootstrap`.
- Décision à confirmer au `/speckit-clarify` si besoin : **renommer** `IMemberPermissionRepository` /
  `GetPermissionsAsync` (nom désormais ambigu) vs conserver tel quel (l'utilisateur a demandé « inchangé »).
- Prêt pour `/speckit-clarify` (optionnel) puis `/speckit-plan`.
