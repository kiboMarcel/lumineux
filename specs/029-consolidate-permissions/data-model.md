# Data Model — Consolidation du RBAC sur les profils (feature 029)

**Phase 1** · **Date** : 2026-07-10. Ce lot **retire** un vestige ; il n'ajoute aucune entité.

## 1. Source de vérité (conservée) — Profils du bureau

Inchangé (features 004/011). Les droits effectifs d'un membre = **union dédupliquée** des droits des profils
qui lui sont attribués.

| Entité | Rôle | Statut |
|--------|------|--------|
| `BureauProfile` | Profil nommé portant un ensemble de droits | **Conservé** |
| `BureauProfilePermission` | Droit ↔ profil | **Conservé** |
| `MemberBureauProfile` | Attribution membre ↔ profil | **Conservé** |

**Lecture des droits effectifs** (port renommé) :

```text
IEffectivePermissionsReader.GetEffectivePermissionsAsync(memberId)
  = SELECT DISTINCT bpp.Permission
    FROM MemberBureauProfiles mbp
    JOIN BureauProfilePermissions bpp ON bpp.BureauProfileId = mbp.BureauProfileId
    WHERE mbp.MemberId = @memberId
```

C'est la **seule** source des droits du jeton (Login/Activate/Setup). Requête **inchangée** ; seul le nom du
port/de la méthode change.

## 2. Entité / table **supprimée**

| Élément | Action |
|---------|--------|
| Entité `MemberPermission` (`member_id`, `permission`) | **Supprimée** |
| Table `member_permissions` | **Supprimée** (migration `RemoveMemberPermissions`, DROP) |
| Config EF `MemberPermissionConfiguration` | **Supprimée** |
| `DbSet<MemberPermission> AppDbContext.MemberPermissions` | **Supprimé** |

Aucune autre table n'a de clé étrangère vers `member_permissions` (droit ↔ membre isolé) → suppression
propre.

## 3. Port (Domain.Abstractions) — renommé et réduit

**Avant** (`IMemberPermissionRepository`) :

| Opération | Devenir |
|-----------|---------|
| `GetPermissionsAsync(memberId)` | **Conservée**, renommée `GetEffectivePermissionsAsync` |
| `HasPermissionAsync(memberId, permission)` | **Supprimée** (lisait `member_permissions`) |
| `AddAsync(MemberPermission)` | **Supprimée** (écrivait `member_permissions`) |
| `SaveChangesAsync()` | **Supprimée** (plus d'écriture directe) |

**Après** (`IEffectivePermissionsReader`) : une seule opération de **lecture** des droits effectifs (profils).

## 4. Configuration retirée

| Clé | Action |
|-----|--------|
| `Auth:Bootstrap` (`MemberReference`, `Permissions`) + classe `BootstrapOptions` | **Supprimée** (alimentait le bootstrapper hérité) |

## 5. Services de démarrage retirés

| Service (`IHostedService`) | Rôle (hérité) | Action |
|----------------------------|---------------|--------|
| `PermissionBootstrapper` | Écrivait `Auth:Bootstrap` dans `member_permissions` | **Supprimé** |
| `BureauProfilesBootstrapper` | Migrait `member_permissions` → profils | **Supprimé** |

## 6. Invariant préservé

Droits effectifs d'un membre **=** droits de ses profils, **avant et après** ce lot (aucune donnée héritée en
dev). Un membre sans profil → **aucun** droit (comportement inchangé).
