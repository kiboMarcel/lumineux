# Security Review Checklist: Profils du bureau

**Purpose**: Revue de sécurité de l'implémentation (Constitution Lumineux v1.0.0, Principe IV) — tâche T048
**Created**: 2026-07-03
**Feature**: [spec.md](../spec.md) · Périmètre : API `/bureau-profiles`, `/members/{id}/bureau-profiles`, `/permissions`

## Autorisation

- [x] Écritures (POST/PUT/DELETE) **exclusivement** protégées par le droit `manage_bureau_profiles`
  (nouveau, feature 004), vérifié dans chaque handler + par la policy ASP.NET (défense en profondeur)
- [x] Lectures (GET) autorisées à `manage_bureau_profiles` **OU** `manage_members` — décision
  centralisée dans `ReadAccess.HasReadAccess(ICurrentUser)`
- [x] Aucun endpoint anonyme (`[Authorize]` par défaut sur les 3 contrôleurs)
- [x] Défense en profondeur : même quand la policy est présente, chaque handler re-vérifie le droit
  au niveau du cas d'usage

## Garde-fou anti-verrouillage (FR-012, SC-004)

- [x] Compte des administrateurs actifs délégué à `IBureauProfileRepository.CountActiveAdministratorsAsync`
  — testé unitairement via `BureauProfileRepositoryTests` (0 admin, N admins, `excludeProfileId`,
  `excludeMemberId`, membre inactif ignoré)
- [x] Porte 1 (**révocation**, `RevokeProfileHandler`) : refus 409 `last_administrator` si la
  révocation laisserait 0 admin
- [x] Porte 2 (**modification**, `UpdateBureauProfileHandler`) : refus si le retrait de
  `manage_bureau_profiles` d'un profil laisserait 0 admin
- [x] Porte 3 (**suppression**, `DeleteBureauProfileHandler`) : ordre d'évaluation FR-003 puis FR-012c
  (documenté dans `research.md §4`) ; refus 409 `profile_in_use` ou `last_administrator`
- [x] Tests d'intégration dédiés `BureauProfilesLastAdministratorTests` couvrant les 3 portes

## Validation & référentiel figé

- [x] Nom de profil validé (non vide, ≤ 80 caractères) au niveau Domain (`BureauProfile.Rename`) et
  Application (`BureauProfileWriteValidator`)
- [x] Description ≤ 255 caractères (Domain + validator)
- [x] Droits validés contre `IPermissionCatalog` (référentiel figé côté serveur : `manage_attendance`,
  `manage_members`, `manage_bureau_profiles`) — droit inconnu → 400
- [x] Unicité de nom insensible à la casse (`nameNormalized` + index unique EF) → 409 `duplicate_name`

## Anti-fuite d'informations (FR-016)

- [x] DTO dédiés — aucune entité EF exposée directement
- [x] `MemberRef` limité à `id`, `reference`, `fullName`, `status` (aucun `email`, `mobile`,
  `passwordHash`)
- [x] Tests d'intégration `BureauProfilesQueryEndpointsTests` assertent explicitement
  `raw.Should().NotContain("passwordHash").And.NotContain("mobile").And.NotContain("email")`

## Migration au déploiement (FR-013)

- [x] `BureauProfilesBootstrapper` créé le profil « Amorçage » uniquement si `member_permissions`
  contient des droits présents dans le catalogue figé (protection contre données pathologiques)
- [x] Idempotent : ne fait rien si le profil « Amorçage » existe déjà
- [x] Attribution : membre référencé par `Auth:Bootstrap:MemberReference` ; à défaut, tous les
  porteurs historiques
- [x] Aucune ligne `member_permissions` n'est modifiée (traçabilité rétroactive)
- [x] Tests d'intégration `BureauProfilesBootstrapperTests` couvrent : cas nominal, idempotence
  double-lancement, absence de source

## Authentification & effets sur les jetons (FR-006, FR-007)

- [x] `MemberPermissionRepository.GetPermissionsAsync` refactoré : lit désormais l'union des droits
  via `member_bureau_profiles × bureau_profile_permissions` (contrat inchangé)
- [x] Test dédié `MemberPermissionRepositoryUnionTests` (union sans doublon) — SC-005
- [x] Effet vérifié bout-en-bout dans `AssignBureauProfileEndpointsTests` (attribution → login →
  jeton porte le nouveau droit) et `RevokeBureauProfileEndpointsTests` (révocation → login → jeton
  ne porte plus le droit → 403 sur endpoint protégé)
- [x] Aucun jeton en cours de validité n'est révoqué activement — comportement documenté et cohérent
  avec la feature 003 (pas de rafraîchissement)

## Journalisation (FR-010, SC-006)

- [x] `IAuditLogger` invoqué sur tous les cas d'usage : create/update/delete profile,
  assign/revoke, list/get profile, list/get member profiles, list permissions
- [x] Refus systématiquement journalisés (droit manquant, dernier admin, membre inactif, doublon)
- [x] Aucun mot de passe / jeton dans les journaux — validé par revue de code

## Idempotence & doublons (FR-005)

- [x] Attribution : vérification `GetAssignmentAsync` avant insertion — nouvel appel = 204 sans
  duplication (`AssignBureauProfileEndpointsTests.Assign_idempotent_returns_204_twice`)
- [x] Contrainte d'unicité `(member, bureau_profile)` matérialisée en base

## Points ouverts (hors périmètre)

- [ ] Rotation périodique de `Jwt:SigningKey` (partagé avec feature 003)
- [ ] Interface d'affectation en masse — non prévue dans cette itération
- [ ] Révocation active des jetons (blacklist) — non prévue

## Notes

- La table héritée `member_permissions` n'est **plus lue** par la chaîne d'authentification après
  T013 ; conservée pour la migration (BureauProfilesBootstrapper) et le repli PermissionBootstrapper.
- Aucune violation de la Constitution constatée. Tous les principes I–VI respectés.
