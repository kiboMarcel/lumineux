# Quickstart — Console web : Courbe d'évolution des présences

Guide de validation du panneau « Évolution ». Prouve les scénarios bout-en-bout et mappe **SC-001..006**.

## Prérequis

- API Lumineux démarrée (HTTPS `https://localhost:4311`) avec la série temporelle (020) et des
  présences réparties sur **plusieurs semaines/mois** (idéalement avec des intervalles **sans** donnée).
- SPA lancée : `ng serve` (`http://localhost:4200`).
- Un compte avec le droit **`manage_attendance`** et un compte **sans** ce droit (masquage / 403).

## Commandes

```powershell
dotnet run --project src/Lumineux.Api      # API
npm run start   # SPA (ng serve, depuis web/)
ng test --no-watch                          # unitaires Vitest
npx playwright test                         # e2e (API live requise)
```

## Scénarios de validation

### A. Courbe d'évolution — US1 (SC-001, SC-002, SC-003, SC-006)

1. Se connecter (droit `manage_attendance`), ouvrir `/reports`, choisir une **période** valide.
2. Dans le panneau **« Évolution »**, choisir une **granularité** (mois) → une **courbe** relie les
   points (présences valides par intervalle) avec les **libellés** `AAAA-MM` en repères (**SC-001**).
3. Vérifier que la **hauteur** d'un point est **proportionnelle** à sa valeur (**SC-002**).
4. Un mois **sans présence** apparaît à **0** (la courbe redescend) (**SC-003**).
5. Survoler/pointer un point → **lire la valeur** (info-bulle/étiquette) (**SC-006**).

### B. Réactivité au contexte — US2 (SC-004)

1. Basculer la granularité **mois → semaine** → la courbe se recalcule en intervalles hebdomadaires
   (libellés `AAAA-Sww`) (**SC-004**).
2. Changer la **période** (ou l'**antenne**) et re-**Afficher** → la courbe se met à jour (**SC-004**).
3. Saisir une **plage invalide** → **message clair** (l'API 020 valide).

### C. Sécurité & robustesse (SC-005)

1. Se reconnecter **sans** le droit `manage_attendance` → l'entrée « Rapports » est **absente** et
   l'accès direct `/reports` **refusé** ; le panneau est donc inaccessible (**SC-005**).
2. Laisser expirer la session (401) → purge + retour connexion (socle).

## Résultat attendu

Tous les scénarios A→C passent ; unitaires (service `timeSeries`, tracé proportionnel, série continue,
granularité, réactivité, erreurs) et e2e au vert. Aucun calcul statistique dupliqué côté client ; aucune
dépendance graphique ajoutée.
