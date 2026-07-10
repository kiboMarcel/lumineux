# Contrat préservé — Invariant d'autorisation (feature 029)

**Phase 1** · Ce lot est un **nettoyage interne** : il **ne modifie aucun contrat exposé**. Ce document
décrit l'**invariant à préserver** (base des tests de non-régression), pas une nouvelle interface.

## Ce qui ne change PAS (garanties)

1. **Claims du jeton** — À la connexion (`POST /api/v1/auth/login`) et à l'activation
   (`POST /api/v1/auth/activate`), le jeton d'accès porte des **claims `permission`** dérivés des **profils
   du bureau** du membre. Pour un même jeu de profils/attributions, l'ensemble des droits du jeton est
   **identique** avant et après ce lot.
2. **Décisions d'autorisation** — Les endpoints protégés par `[Authorize(Policy = …)]` renvoient les
   **mêmes** résultats : **autorisé** si le droit est présent (via profil), **403 Forbidden** sinon. Aucun
   endpoint ne change de code de statut ni de politique.
3. **Visibilité de la navigation (SPA)** — Les modules affichés selon les droits (RBAC d'affichage) restent
   identiques (les droits provenant des mêmes profils).
4. **Première installation** (`POST /api/v1/setup/first-admin`) — Crée un administrateur doté de **tous** les
   droits **via un profil « Administrateur »** ; réponse et effet inchangés.
5. **Audit** — Les tentatives d'accès **refusées** (droit manquant) restent **journalisées**.

## Ce qui change (interne, non exposé)

- Le **port** de lecture des droits est renommé `IEffectivePermissionsReader.GetEffectivePermissionsAsync`
  (aucune signature d'API publique concernée).
- Le stockage hérité `member_permissions`, ses opérations et les bootstrappers de migration **disparaissent**.

## Vérification (référence pour quickstart & tests)

| Garantie | Vérifiée par |
|----------|--------------|
| Mêmes droits dans le jeton (via profils) | `LoginTests` / `ActivateAccountTests` (mocks au nouveau nom) ; `MemberPermissionRepositoryUnionTests` (union de profils) |
| Mêmes 403 sur endpoints protégés | Tests d'endpoints existants (attendance/membres/profils) — inchangés |
| Setup admin par profil | `InstallFirstAdminTests` + endpoint setup |
| Aucune régression au démarrage (plus de migration) | Suppression des tests de coexistence ; suite verte |
| Table héritée absente | Migration `RemoveMemberPermissions` + schéma |
