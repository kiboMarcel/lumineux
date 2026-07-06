/** Modèles de vue (client) du module Antennes (feature 017) — reflet des DTO de l'API 016. */

/** Statut d'une antenne. */
export type AntennaStatus = 'Active' | 'Inactive' | string;

/** Antenne renvoyée par l'API de gestion (016). */
export interface AntennaResponse {
  id: number;
  code: string;
  label: string;
  districtId: number;
  status: AntennaStatus;
}

/** Requête de création d'une antenne (code unique, libellé, district). */
export interface CreateAntennaRequest {
  code: string;
  label: string;
  districtId: number;
}

/** Requête de modification (libellé + district ; le code est immuable, non envoyé). */
export interface UpdateAntennaRequest {
  label: string;
  districtId: number;
}
