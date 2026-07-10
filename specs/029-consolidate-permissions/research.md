# Research — Consolidation du RBAC sur les profils (feature 029)

**Phase 0** · **Date** : 2026-07-10. Nettoyage de dette M3/M4. Aucune zone « NEEDS CLARIFICATION » restante.

## D1 — La source des droits du jeton est déjà, exclusivement, les profils

- **Décision** : conserver la lecture des droits effectifs telle quelle (requête **profils uniquement**),
  la **renommer** en `IEffectivePermissionsReader.GetEffectivePermissionsAsync` (clarification verrouillée).
- **Preuve (code vérifié)** : `MemberPermissionRepository.GetPermissionsAsync` fait
  `from mbp in MemberBureauProfiles join bpp in BureauProfilePermissions … select bpp.Permission` —
  **aucune** union avec `member_permissions`. Retirer la table héritée ne change donc **rien** aux droits du
  jeton (FR-001/SC-001).
- **Appelants** : `LoginHandler`, `ActivateAccountHandler`, `InstallFirstAdminHandler` → mise à jour du type
  injecté + nom de méthode.
- **Note** : le test `MemberPermissionRepositoryUnionTests` teste l'**union de plusieurs profils** (dédup),
  pas la table héritée — il **reste valide** (mise à jour du nom de type seulement).

## D2 — Le mécanisme hérité est isolé et mort pour le jeton

- **Décision** : supprimer `HasPermissionAsync` + `AddAsync` (+ `SaveChangesAsync`) du port, la table
  `member_permissions`, l'entité `MemberPermission`, sa config EF, et le `DbSet`.
- **Preuve** : `HasPermissionAsync` (lit `member_permissions`) et `AddAsync` (écrit `member_permissions`) ne
  sont appelés **que** par le `PermissionBootstrapper`. Aucun chemin de requête ne les utilise. Donc leur
  retrait n'a aucun effet fonctionnel hors du bootstrapper (lui-même retiré, D3).

## D3 — Retrait des deux bootstrappers de démarrage (M4)

- **Décision** : supprimer `PermissionBootstrapper` (écrivait `Auth:Bootstrap` dans la table héritée) et
  `BureauProfilesBootstrapper` (migrait la table héritée → profils). Retrait des `AddHostedService`.
- **Rationale** : projet en développement, **aucune donnée héritée à migrer** ; ces services faisaient de la
  migration de données au démarrage (dette M4). L'amorçage de l'admin passe par le **setup 005** (profil).
- **Conséquence** : la configuration `Auth:Bootstrap` (+ `BootstrapOptions`) devient inutile → retirée
  (FR-007). Aucune logique de migration ne s'exécute plus au démarrage (SC-005).

## D4 — Amorçage de l'admin initial préservé (setup 005)

- **Décision** : ne **rien** changer à `InstallFirstAdminHandler` hormis le nom du port/méthode de lecture.
- **Preuve** : le handler crée déjà un **profil « Administrateur »** (`BureauProfile.Create` avec tout le
  catalogue) et l'**attribue** (`MemberBureauProfile`), puis lit les droits effectifs via les profils. Il ne
  dépend **pas** de `member_permissions` pour l'octroi (FR-005/SC-003).

## D5 — Migration de suppression de table

- **Décision** : migration EF `RemoveMemberPermissions` qui **DROP** la table `member_permissions`. Rejouable
  sur base vierge ; `Down()` recrée la table (réversibilité EF standard).
- **Rationale** : Principe II (code-first). Aucune autre table/contrainte impactée.

## D6 — Adaptation des tests (non-régression)

- **Décision** :
  - **Supprimer** `BureauProfilesBootstrapperTests` et `SetupBootstrapCoexistenceTests` (scénarios de
    coexistence/migration désormais sans objet).
  - **Adapter** `ApiTestFixture.ResetInstallationStateAsync` : retirer la purge de `MemberPermissions`.
  - **Adapter** les mocks (`LoginTests`, `ActivateAccountTests`, `InstallFirstAdminTests`) et
    `MemberPermissionRepositoryUnionTests` au **nouveau nom** du port/méthode.
- **Rationale** : les tests conservés couvrent la non-régression (droits du jeton via profils, setup admin,
  refus 403). La suite doit rester **verte** (Principe III, SC-005).

## D7 — Aucun impact client / contrat

- **Décision** : aucun changement d'API ni de DTO ; les **claims** du jeton et les **codes** (403) sont
  identiques. Le renommage est **interne** (non exposé). SPA/mobile inchangés.
- Voir `contracts/authorization-invariant.md`.

---

## Synthèse

| # | Sujet | Décision |
|---|-------|----------|
| D1 | Source des droits | Profils uniquement (déjà le cas) ; lecture **renommée** `IEffectivePermissionsReader.GetEffectivePermissionsAsync` |
| D2 | Port hérité | Retrait `HasPermissionAsync`/`AddAsync`/`SaveChangesAsync` + entité/config/DbSet |
| D3 | Bootstrappers | Retrait `PermissionBootstrapper` + `BureauProfilesBootstrapper` + config `Auth:Bootstrap` |
| D4 | Setup admin | Inchangé (déjà par profils) |
| D5 | Migration | `RemoveMemberPermissions` (DROP table, réversible) |
| D6 | Tests | Retirer coexistence/migration ; adapter fixture + mocks au nouveau nom |
| D7 | Contrat | Aucun changement (mêmes claims/403) |

**Aucune** zone « NEEDS CLARIFICATION » restante. Prêt pour la Phase 1.
