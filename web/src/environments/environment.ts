/**
 * Configuration d'environnement (dev par défaut). L'URL de base de l'API et la longueur minimale du
 * mot de passe (alignée sur la politique serveur, non autoritaire) sont paramétrées ici.
 */
export const environment = {
  production: false,
  // URL de l'API en dev = applicationUrl de src/Lumineux.Api/Properties/launchSettings.json
  // (https://localhost:4311 ; alternative http://localhost:4312 si le certificat de dev n'est pas approuvé).
  apiBaseUrl: 'https://localhost:4311',
  passwordMinLength: 8,
};
