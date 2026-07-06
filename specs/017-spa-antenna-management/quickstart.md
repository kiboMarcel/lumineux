# Quickstart — Console web : Gestion des antennes (SPA)

Guide de validation du module **Antennes**. Prouve les scénarios bout-en-bout et mappe **SC-001..007**.

## Prérequis

- API Lumineux démarrée (HTTPS `https://localhost:4311`) avec la gestion des antennes (016) et le
  référentiel des districts (010) ; migration `AntennaCodeUnique` appliquée.
- SPA lancée : `ng serve` (`http://localhost:4200`).
- Un compte de test avec le droit **`manage_referentials`** (attribué via un profil du bureau) et un
  compte **sans** ce droit (pour vérifier le masquage / 403).
- Au moins un **district** au référentiel.

## Commandes

```powershell
dotnet run --project src/Lumineux.Api      # API
npm run start   # SPA (ng serve, depuis web/)
ng test --no-watch                          # unitaires Vitest
npx playwright test                         # e2e (API live requise)
```

## Scénarios de validation

### A. Accès & liste — US1 (SC-002, SC-006)

1. Se connecter (droit `manage_referentials`) → l'entrée **« Antennes »** est visible.
2. Ouvrir `/antennas` : la liste affiche **toutes** les antennes, **inactives incluses**, avec leur
   **statut** (**SC-002**).
3. Se reconnecter **sans** le droit → entrée **absente** ; accès direct à `/antennas` **refusé**
   (**SC-006**).

### B. Créer — US2 (SC-001, SC-003)

1. **Antennes → Nouvelle antenne** : saisir un **code inédit**, un **libellé**, choisir un **district**
   → **Créer** → l'antenne apparaît dans la liste, en **moins d'1 minute**, **sans SQL** (**SC-001**).
2. Recréer avec le **même code** → message clair « code déjà utilisé », rien créé (**SC-003**).
3. Soumettre un formulaire incomplet → validation (champs requis).

### C. Modifier — US3 (SC-005)

1. Éditer une antenne : changer **libellé** + **district** → enregistré et reflété dans la liste.
2. Vérifier que le champ **code** est **en lecture seule** (**SC-005**).

### D. Activer / désactiver — US4 (SC-004, SC-007)

1. **Désactiver** une antenne → **confirmation** demandée (**SC-007**) → statut **inactif** dans la
   liste.
2. **Réactiver** → statut **actif**.
3. Ouvrir une **session de présence** sur une antenne, puis tenter de la **désactiver** → **message
   clair** « une session est encore ouverte », antenne restée active (**SC-004**).

### E. Robustesse

1. Laisser expirer la session (401) → purge + retour connexion (socle).
2. Lecture publique `GET /reference/antennas` (fiche membre / démarrage de session) **inchangée** :
   n'affiche que les actives.

## Résultat attendu

Tous les scénarios A→E passent ; unitaires (service, liste, formulaire, garde, mapping d'erreurs,
confirmation) et e2e au vert. Aucune règle métier dupliquée côté client.
