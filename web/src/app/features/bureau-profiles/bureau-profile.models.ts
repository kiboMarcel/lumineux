/** Modèles de vue du module Profils du bureau (reflet des DTO de l'API feature 004). */

/** Vue publique d'un membre titulaire (aucune donnée sensible). */
export interface MemberRef {
  id: number;
  reference: string;
  fullName: string;
  status: string;
}

/** Vue résumée d'un profil (liste). */
export interface BureauProfileSummary {
  id: number;
  name: string;
  description?: string | null;
  permissions: string[];
  memberCount: number;
}

/** Vue détaillée d'un profil (avec titulaires). */
export interface BureauProfileDetail extends BureauProfileSummary {
  members: MemberRef[];
}

/** Requête de création/modification d'un profil (droits = codes du catalogue). */
export interface BureauProfileWriteRequest {
  name: string;
  description?: string | null;
  permissions: string[];
}

/** Profils et droits effectifs d'un membre. */
export interface MemberProfilesResponse {
  member: MemberRef;
  profiles: BureauProfileSummary[];
  effectivePermissions: string[];
}
