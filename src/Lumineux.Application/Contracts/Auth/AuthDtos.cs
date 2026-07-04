namespace Lumineux.Application.Contracts.Auth;

/// <summary>Requête de connexion (FR-001).</summary>
public sealed record LoginRequest(string Reference, string Password);

/// <summary>Requête d'activation / première connexion (FR-007).</summary>
public sealed record ActivateAccountRequest(string Reference, string TemporaryPassword, string NewPassword);

/// <summary>Requête de changement de mot de passe pour un utilisateur connecté (FR-009).</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Réponse contenant le jeton d'accès. Aucun mot de passe n'est exposé.</summary>
public sealed record TokenResponse(string AccessToken, string TokenType, DateTime ExpiresAt);
