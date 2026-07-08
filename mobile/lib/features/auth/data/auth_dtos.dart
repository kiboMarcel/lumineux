/// DTO **miroir** des contrats serveur (`Lumineux.Application.Contracts.Auth`).
/// Sérialisation manuelle (petit ensemble). Aucun mot de passe n'est stocké
/// ni journalisé au-delà de la durée de l'appel.
library;

class LoginRequest {
  const LoginRequest({required this.reference, required this.password});

  final String reference;
  final String password;

  Map<String, dynamic> toJson() => {
        'reference': reference,
        'password': password,
      };
}

class ActivateRequest {
  const ActivateRequest({
    required this.reference,
    required this.temporaryPassword,
    required this.newPassword,
  });

  final String reference;
  final String temporaryPassword;
  final String newPassword;

  Map<String, dynamic> toJson() => {
        'reference': reference,
        'temporaryPassword': temporaryPassword,
        'newPassword': newPassword,
      };
}

class ForgotPasswordRequest {
  const ForgotPasswordRequest({required this.reference});

  final String reference;

  Map<String, dynamic> toJson() => {'reference': reference};
}

class ResetPasswordRequest {
  const ResetPasswordRequest({required this.token, required this.newPassword});

  final String token;
  final String newPassword;

  Map<String, dynamic> toJson() => {
        'token': token,
        'newPassword': newPassword,
      };
}

class ChangePasswordRequest {
  const ChangePasswordRequest({
    required this.currentPassword,
    required this.newPassword,
  });

  final String currentPassword;
  final String newPassword;

  Map<String, dynamic> toJson() => {
        'currentPassword': currentPassword,
        'newPassword': newPassword,
      };
}

class TokenResponse {
  const TokenResponse({
    required this.accessToken,
    required this.tokenType,
    required this.expiresAt,
  });

  final String accessToken;
  final String tokenType;
  final DateTime expiresAt;

  factory TokenResponse.fromJson(Map<String, dynamic> json) => TokenResponse(
        accessToken: json['accessToken'] as String,
        tokenType: (json['tokenType'] as String?) ?? 'Bearer',
        expiresAt: DateTime.parse(json['expiresAt'] as String),
      );
}

class CurrentUser {
  const CurrentUser({
    required this.memberId,
    required this.displayName,
    required this.permissions,
  });

  final String memberId;
  final String displayName;
  final List<String> permissions;

  factory CurrentUser.fromJson(Map<String, dynamic> json) => CurrentUser(
        memberId: json['memberId'].toString(),
        displayName: (json['displayName'] as String?) ?? '',
        permissions: (json['permissions'] as List<dynamic>? ?? const [])
            .map((e) => e.toString())
            .toList(growable: false),
      );
}
