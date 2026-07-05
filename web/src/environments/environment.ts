/**
 * Configuration d'environnement (dev par défaut). L'URL de base de l'API et la longueur minimale du
 * mot de passe (alignée sur la politique serveur, non autoritaire) sont paramétrées ici.
 */
export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:5001',
  passwordMinLength: 8,
};
