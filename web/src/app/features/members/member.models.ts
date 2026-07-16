/** Modèles de vue du module Membres (reflet des DTO de l'API feature 002). */

/** Élément de liste (recherche). */
export interface MemberListItem {
  id: number;
  reference: string;
  lastName: string;
  firstName: string;
  mobile?: string | null;
  email?: string | null;
  antennaId?: number | null;
  status: string;
}

/** Résultat paginé de recherche. */
export interface MemberListResponse {
  page: number;
  pageSize: number;
  total: number;
  items: MemberListItem[];
}

/** Fiche complète d'un membre (aucun secret). */
export interface MemberResponse {
  id: number;
  reference: string;
  entryDate: string;
  lastName: string;
  firstName: string;
  gender: string;
  mobile?: string | null;
  email?: string | null;
  antennaId?: number | null;
  civilityId?: number | null;
  birthDate?: string | null;
  birthPlaceId?: number | null;
  birthCityId?: number | null;
  address?: string | null;
  districtId?: number | null;
  nationalityId?: number | null;
  introducerId?: number | null;
  profession?: string | null;
  status: string;
  accountActivationState: string;
}

/** Requête de création. `confirmDuplicate` gère l'homonymie (FR-007). */
export interface CreateMemberRequest {
  lastName: string;
  firstName: string;
  gender: string;
  antennaId: number;
  mobile?: string | null;
  email?: string | null;
  civilityId?: number | null;
  birthDate?: string | null;
  birthPlaceId?: number | null;
  birthCityId?: number | null;
  address?: string | null;
  districtId?: number | null;
  nationalityId?: number | null;
  introducerId?: number | null;
  profession?: string | null;
  confirmDuplicate?: boolean;
}

/** Requête de correction (sans `confirmDuplicate` ; la référence n'est pas modifiable). */
export type UpdateMemberRequest = Omit<CreateMemberRequest, 'confirmDuplicate'>;

/** Modes de remise des identifiants initiaux. */
export const CredentialsDelivery = {
  EmailSent: 'EmailSent',
  BureauHandout: 'BureauHandout',
} as const;

/** Réponse de création. `temporaryPassword` présent uniquement si remise bureau (affiché une fois). */
export interface MemberCreatedResponse {
  member: MemberResponse;
  loginId: string;
  credentialsDelivery: string;
  temporaryPassword?: string | null;
}
