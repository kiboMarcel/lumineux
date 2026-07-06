/** Modèles de vue (client) du tableau de bord des rapports (feature 019) — reflet des DTO de l'API 018. */

/** Ligne de synthèse d'affluence pour une antenne, sur la période demandée. */
export interface AntennaAttendanceSummaryItem {
  antennaId: number;
  antennaLabel: string;
  sessionCount: number;
  validAttendanceCount: number;
  averageValidPerSession: number;
}

/** Synthèse d'affluence par antenne sur une plage de dates. */
export interface AntennaAttendanceSummaryResponse {
  from: string;
  to: string;
  items: AntennaAttendanceSummaryItem[];
}

/** Taux d'assiduité d'un membre sur une période. `rate` est une fraction 0..1 (affichée en %). */
export interface MemberAttendanceRateResponse {
  memberId: number;
  memberFullName: string;
  from: string;
  to: string;
  validAttendanceCount: number;
  eligibleSessionCount: number;
  rate: number;
}
