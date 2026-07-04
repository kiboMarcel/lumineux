# Security Review Checklist: Installation du premier administrateur

**Purpose**: Revue de sécurité de l'implémentation (Constitution Lumineux v1.0.0, Principe IV) — tâche T019
**Created**: 2026-07-03
**Feature**: [spec.md](../spec.md) · Périmètre : API `/api/v1/setup/first-admin`

## Verrouillage & anti-fuite

- [x] Endpoint anonyme (`[AllowAnonymous]`) **avec verrou métier** : refus dès que
  `IBureauProfileRepository.CountActiveAdministratorsAsync() > 0` (FR-004)
- [x] **Refus prioritaire** : `already_installed` est levé **AVANT** la validation FluentValidation
  du payload (FR-005) — vérifié par le test `Install_after_first_install_with_invalid_payload_still_returns_409_not_400`
- [x] Aucune fuite sur la structure du payload : un tiers ne peut pas énumérer les champs attendus
  en observant les 400/409

## Atomicité & rollback

- [x] Une **seule** `SaveChangesAsync` — vérifié par le test unitaire
  `Install_valid_creates_member_account_profile_assignment_and_returns_token`
- [x] Sur chemin d'erreur (SaveChanges lève) : jeton **non émis**, exception propagée — vérifié par
  `Install_when_save_fails_does_not_issue_token`
- [x] Test d'intégration `Install_with_existing_contact_returns_409_contact_in_use` vérifie
  qu'aucun profil ni compte partiel n'est créé en cas de collision

## Politique de mot de passe & hachage

- [x] Politique **héritée feature 003** (`Auth:PasswordMinLength`, au moins 1 lettre + 1 chiffre)
  via `PasswordRules.ApplyPolicy` — aucune règle spécifique à l'installation
- [x] Hachage via `IPasswordHasher` (Identity/Argon2) — jamais de mot de passe en clair stocké
- [x] Mot de passe fourni utilisé UNE fois : `MemberAccount.Provision(member, hash)` +
  `ChangePassword(sameHash)` + `Activate()` → compte immédiatement utilisable

## Coexistence avec l'amorçage historique

- [x] `Auth:Bootstrap:*` (feature 003) NON modifié — reste opérationnel comme filet d'urgence
- [x] `PermissionBootstrapper` + `BureauProfilesBootstrapper` non altérés
- [x] Test dédié `SetupBootstrapCoexistenceTests` : si les bootstrappers ont déjà créé un admin,
  `/setup/first-admin` refuse (FR-012)

## Idempotence & anti-doublon

- [x] Profil « Administrateur » : si présent (nom insensible à la casse), **réutilisé** sans
  modification de description ni de droits (FR-008/013) — vérifié par test unitaire ET intégration
- [x] Unicité de nom garantie par index EF Core (`bureau_profiles.name_normalized` unique)
- [x] Attribution `(member, profile)` unique par contrainte EF Core (feature 004)

## Journalisation & audit

- [x] `IAuditLogger.Operation("Setup.FirstAdminCreated", { memberId, reference })` sur succès
- [x] `IAuditLogger.Refused("Setup.FirstAdmin", ...)` sur refus (déjà installé, coordonnée en usage)
- [x] **Aucun** mot de passe / jeton dans les journaux — vérifié par revue de code (le `request`
  n'est pas passé tel quel au logger, seuls `memberId` et `reference` sont exposés)
- [x] Test d'intégration assertions `raw.Should().NotContain("password").And.NotContain("passwordHash")`

## Contrats & DTO

- [x] DTO dédiés (`InstallFirstAdminRequest`) — aucune entité EF exposée
- [x] Réponse `TokenResponse` réutilisée de feature 003 — cohérence avec `/auth/login`
- [x] OpenAPI aligné avec l'implémentation (`contracts/openapi.yaml`) : 201/400/409 + codes
  métier `already_installed`, `contact_in_use`, `duplicate_reference`
- [x] Annotations `ProducesResponseType` sur le contrôleur

## Sécurité JWT

- [x] Jeton émis via `ITokenIssuer` (feature 003) — même chaîne d'émission que `/auth/login`
- [x] Signature HS256 avec `Jwt:SigningKey` (secret hors code)
- [x] Durée héritée `Auth:AccessTokenMinutes` (60 min par défaut)

## Points ouverts (hors périmètre)

- [ ] Rate limiting sur `/setup/*` : non requis (verrou naturel + volume attendu = 1 appel réel).
  À réévaluer si l'API devient publique.
- [ ] « Mot de passe oublié » pour le super-admin — feature future distincte.
- [ ] Interface d'installation dédiée dans la SPA Angular — hors périmètre backend.

## Notes

- Aucun secret ni jeton dans les journaux (FR-010, SC-005). L'audit ne consigne que l'identifiant
  et la référence du membre créé.
- La navigation `Member`/`BureauProfile` ajoutée à `MemberBureauProfile` permet à EF Core de
  résoudre les FK dans une seule transaction (SaveChanges unique — SC-003) — aucune migration
  requise, le schéma reste identique.
- Aucune violation de la Constitution constatée. Les 6 principes I–VI respectés.
