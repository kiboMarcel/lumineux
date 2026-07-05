/** Modèles typés (vue client) des contrats API consommés — cf. specs/008 contracts/api-consumption.md. */

/** Réponse jeton (login / activate / first-admin). */
export interface TokenResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
}

/** Profil de session (GET /auth/me). */
export interface CurrentUser {
  memberId: number;
  displayName: string;
  permissions: string[];
}

/** Réponse générique anti-énumération (forgot-password). */
export interface GenericMessage {
  message: string;
}

/** Statut d'installation de l'instance (feature 012). `installed = true` si un admin actif existe. */
export interface SetupStatus {
  installed: boolean;
}

/** Format d'erreur RFC 7807 renvoyé par l'API (avec extension `code` éventuelle). */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
}

/** Requêtes. */
export interface LoginRequest { reference: string; password: string; }
export interface ActivateRequest { reference: string; temporaryPassword: string; newPassword: string; }
export interface ForgotPasswordRequest { reference: string; }
export interface ResetPasswordRequest { token: string; newPassword: string; }
export interface ChangePasswordRequest { currentPassword: string; newPassword: string; }
export interface FirstAdminRequest {
  lastName: string;
  firstName: string;
  gender: string;
  password: string;
  email?: string | null;
  mobile?: string | null;
}
