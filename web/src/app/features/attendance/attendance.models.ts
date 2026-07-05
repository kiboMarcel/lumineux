/** Modèles de vue (client) du module Présences (Lot 4) — reflet des DTO de l'API présences (001). */

/** Statut d'une session de présence. */
export type SessionStatus = 'Open' | 'Closed' | string;

/** Requête de démarrage de session (POST /attendance-sessions). */
export interface StartSessionRequest {
  antennaId: number;
  meetingDate: string;
  /** Pas de rotation du QR en secondes (optionnel). */
  qrStepSeconds?: number | null;
}

/** Session de présence renvoyée par l'API. */
export interface SessionResponse {
  id: number;
  antennaId: number;
  meetingDate: string;
  startTime: string;
  endTime?: string | null;
  status: SessionStatus;
  openedByMemberId: number;
  closedByMemberId?: number | null;
  attendanceCount: number;
}

/**
 * Jeton QR **éphémère** (GET /attendance-sessions/{id}/qr). Sert uniquement à générer l'image du QR
 * côté client ; jamais affiché en clair ni persisté (FR-005/SC-005).
 */
export interface QrTokenResponse {
  token: string;
  stepSeconds: number;
  expiresAt: string;
}

/** Filtre de statut de la liste des présences. */
export type AttendanceStatusFilter = 'Valid' | 'Cancelled' | 'All';

/** Présence individuelle (vue). */
export interface AttendanceResponse {
  id: number;
  sessionId: number;
  memberId: number;
  memberFullName?: string | null;
  arrivalTime: string;
  endTime?: string | null;
  source: string;
  status: string;
  originAntennaId?: number | null;
}

/** Liste des présences + décompte des valides (GET .../attendances). */
export interface AttendanceListResponse {
  sessionId: number;
  validCount: number;
  items: AttendanceResponse[];
}

/** Requête d'ajout manuel d'une présence (POST .../attendances). */
export interface ManualAttendanceRequest {
  memberId: number;
  arrivalTime?: string | null;
}

/** Entrée de recherche membre allégée (feature 015, GET /members/lookup). */
export interface MemberLookupItem {
  id: number;
  reference: string;
  fullName: string;
  status: string;
}
