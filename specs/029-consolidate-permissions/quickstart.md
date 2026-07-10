# Quickstart — Validation du retrait du mécanisme hérité (feature 029)

**Phase 1** · Guide de **validation**, pas d'implémentation. Réfère à `data-model.md` et
`contracts/authorization-invariant.md`.

## Prérequis

- API .NET buildable ; base de dev disponible (SQL Server) ; API arrêtée pour appliquer la migration.
- Aucune donnée héritée à préserver (projet en développement).

## Tests automatisés

```bash
dotnet test           # Domain + Application + Api + Infrastructure
```

Attendus (couvrent FR/SC) :

- **Non-régression des droits (SC-001/SC-002)** : `LoginTests`, `ActivateAccountTests`,
  `MemberPermissionRepositoryUnionTests` (union de **profils**) → droits effectifs = ceux des profils,
  **inchangés** ; mocks/refs adaptés au nom `IEffectivePermissionsReader.GetEffectivePermissionsAsync`.
- **Setup admin (SC-003)** : `InstallFirstAdminTests` → admin doté de tous les droits **via profil**.
- **Refus 403 inchangés (SC-002)** : les tests d'endpoints protégés existants restent **verts** sans
  modification.
- **Aucun démarrage-migration (SC-005)** : `BureauProfilesBootstrapperTests` et
  `SetupBootstrapCoexistenceTests` **supprimés** ; plus aucun `IHostedService` de migration de droits.
- **Compilation** : la disparition de `MemberPermission`, `HasPermissionAsync`, `AddAsync`,
  `Auth:Bootstrap` ne laisse **aucune** référence pendante (le build échoue sinon → filet de sécurité).

## Migration (schéma)

```bash
# API arrêtée
dotnet ef migrations add RemoveMemberPermissions --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Infrastructure
dotnet ef database update    --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Infrastructure
```

Attendu : `Up()` **DROP** la table `member_permissions` ; `Down()` la recrée ; rejouable sur base vierge.

## Vérification « zéro vestige » (SC-004)

```bash
# Doit ne renvoyer AUCUNE occurrence (hors dossier specs/ et migrations historiques figées) :
grep -rniE "member_permissions|MemberPermission|IMemberPermissionRepository|PermissionBootstrapper|BureauProfilesBootstrapper|Auth:Bootstrap|BootstrapOptions" src tests
```

> Les **migrations historiques** déjà committées (ex. `20260703174627_Authentication`) conservent des
> références par nature (instantané figé) — elles ne comptent pas comme vestige vivant. Seul le **modèle
> courant** (snapshot régénéré) et le code applicatif doivent être nets.

## Scénarios manuels (facultatif, console/API)

1. **Connexion bureau** → le jeton contient les mêmes droits qu'avant (issus des profils) ; accès aux
   modules autorisés identiques.
2. **Membre sans profil** → aucun droit ; accès aux modules de gestion refusé (403).
3. **Première installation** sur instance vierge → admin créé avec profil « Administrateur » et tous les
   droits.

## Critères de sortie

- [ ] `dotnet test` vert (tests adaptés/supprimés conformes à `research.md` D6).
- [ ] Migration `RemoveMemberPermissions` appliquée et réversible ; table absente du schéma.
- [ ] Recherche « zéro vestige » = 0 occurrence vivante (hors migrations figées).
- [ ] Aucun `IHostedService` de migration de droits au démarrage.
- [ ] Comportement d'autorisation (claims + 403) identique à l'avant-lot.
