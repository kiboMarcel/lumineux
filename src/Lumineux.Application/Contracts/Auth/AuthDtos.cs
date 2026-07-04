namespace Lumineux.Application.Contracts.Auth;

/// <summary>Requête de connexion (FR-001).</summary>
public sealed record LoginRequest(string Reference, string Password);

/// <summary>Requête d'activation / première connexion (FR-007).</summary>
public sealed record ActivateAccountRequest(string Reference, string TemporaryPassword, string NewPassword);

/// <summary>Requête de changement de mot de passe pour un utilisateur connecté (FR-009).</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Réponse contenant le jeton d'accès. Aucun mot de passe n'est exposé.</summary>
public sealed record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresAt);

/// <summary>Requête de demande de réinitialisation de mot de passe (feature 006, FR-001).</summary>
public sealed record ForgotPasswordRequest(string Reference);

/// <summary>Requête de réinitialisation avec le jeton reçu par email (feature 006, FR-005).</summary>
public sealed record ResetPasswordRequest(string Token, string NewPassword);

/// <summary>Réponse générique anti-énumération (feature 006, FR-002). Aucun détail sur le compte.</summary>
public sealed record GenericMessageResponse(string Message);
