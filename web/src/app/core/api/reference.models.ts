/** Modèles des données de référence (feature 010) pour les listes de sélection. */

/** Entrée générique (antenne, civilité, ville, district). */
export interface ReferenceItem {
  id: number;
  code: string;
  label: string;
}

/** Pays / nationalité : libellés distincts. */
export interface Country {
  id: number;
  code: string;
  country: string;
  nationality: string;
}
