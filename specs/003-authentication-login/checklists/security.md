# Security Review Checklist: Authentification et connexion des membres

**Purpose**: Revue de sécurité de l'implémentation (Constitution Lumineux v1.0.0, Principe IV) — tâche T033
**Created**: 2026-07-03
**Feature**: [spec.md](../spec.md) · Périmètre : API `/auth/*` (US1→US4)

## Anti-énumération (comptes / identifiants)

- [x] Messages `401 générique` identiques pour « référence inconnue » et « mot de passe erroné » (`LoginHandler`, `ActivateAccountHandler`) — SC-002
- [x] Hash factice appliqué même en cas d'inexistence du compte, pour égaliser le coût de calcul et fermer le canal temporel (F5)
- [x] La différenciation « compte déjà activé » (409) sur `/activate` n'est révélée **qu'après** vérification correcte du mot de passe temporaire (F2)
- [x] Le statut `password_change_required` (403) n'est retourné qu'**après** avoir validé le mot de passe (aucune fuite avant vérif.)

## Politique de mot de passe & stockage

- [x] Mot de passe validé côté serveur (FluentValidation : `ChangePasswordValidator`, `ActivateAccountValidator`) — longueur, lettre, chiffre (FR-010)
- [x] Nouveau mot de passe interdit d'être identique au précédent (activate) ou au courant (change)
- [x] Empreintes uniquement (`IPasswordHasher` — Argon2/BCrypt via infra) ; aucun mot de passe en clair n'est stocké, journalisé ni retourné
- [x] Réponses de jeton (`TokenResponse`) ne contiennent aucun champ de secret (vérifié par test API — `raw.Should().NotContain("passwordHash")`)

## Verrouillage anti-force brute (US4)

- [x] Compteur incrémenté sur chaque échec (`MemberAccount.RegisterFailedLogin`) — testé unitairement + intégration
- [x] Verrouillage temporaire à `MaxFailedAttempts` échecs pendant `LockoutMinutes` minutes (défauts 5 / 15) — configurables via `Auth:*`
- [x] Le compteur est remis à zéro sur succès (`RegisterSuccessfulLogin`) — testé
- [x] Cohérence entre `/login` et `/activate` (verrouillage appliqué des deux côtés, mêmes valeurs `AuthOptions`)
- [ ] F4 (accepté, hors périmètre) : `/change-password` n'applique **pas** de compteur d'échecs — l'endpoint exige déjà un jeton valide (risque faible). À réévaluer si durcissement requis.

## Jetons (JWT)

- [x] Émission via port `ITokenIssuer` avec les droits du membre (claims `permission`, `member_id`, `name`) — `JwtTokenIssuer`
- [x] Signature HS256 avec `Jwt:SigningKey` fourni par configuration/secrets (aucun secret en dur)
- [x] Validation côté serveur : issuer, audience, **lifetime**, signature (`ValidateLifetime = true` dans `Program.cs`) — SC-006, FR-014
- [x] Pas de rafraîchissement — un jeton expiré force une nouvelle authentification (FR-006)
- [x] Aucun jeton n'est journalisé en clair (audit consigne uniquement des identifiants non sensibles)

## Autorisation & défense en profondeur

- [x] `/auth/login` et `/auth/activate` explicitement `[AllowAnonymous]` ; `/auth/change-password` explicitement `[Authorize]`
- [x] `ChangePasswordHandler` re-vérifie `ICurrentUser.MemberId` (défense en profondeur — 401 générique si absent)
- [x] Amorçage minimal des droits (`PermissionBootstrapper`) idempotent, encadré par configuration `Auth:Bootstrap:*` — aucun droit accordé automatiquement sans configuration explicite (F1)

## Journalisation & audit (FR-013, FR-019/020)

- [x] `IAuditLogger` consigne succès (`Login`, `Activate`, `ChangePassword`) et refus (identifiants, verrouillage, must-change) — jamais de mot de passe ni jeton
- [x] Événement `Verrouillage déclenché` distinctement journalisé lors de la transition (T032)
- [x] `IsHttpsFromReverseProxy` géré par ASP.NET (dépend du déploiement ; le middleware d'exception ne fuit pas de détails 500)

## Contrats & OpenAPI

- [x] `AuthController` annoté `ProducesResponseType` alignés avec `contracts/openapi.yaml` (200/204/400/401/403/409)
- [x] Swagger déclare `bearerAuth` (`Program.cs`) — cohérent avec la spec

## Points ouverts (hors périmètre US1→US4)

- [ ] Rotation périodique de `Jwt:SigningKey` : procédure à définir hors implémentation
- [ ] Auditabilité côté « permissions par profil » : dépend de la future feature « profils du bureau » (F1)

## Notes

- Les éléments non cochés relèvent d'actions organisationnelles ou de features futures, non de défauts de l'implémentation courante.
- Revue effectuée sur le périmètre API + Domain + Application. Client (Angular/Flutter) : revue séparée
  (stockage local du jeton, timeout d'inactivité, refresh manuel).
