# Quickstart — Console web : Reprendre une session de présence en cours

Guide de validation. Prouve les scénarios bout-en-bout et mappe **SC-001..006**.

## Prérequis

- API Lumineux démarrée (HTTPS `https://localhost:4311`) avec présences (001), mes sessions ouvertes
  (023) et référentiel antennes (010).
- SPA lancée : `ng serve` (`http://localhost:4200`).
- Un compte avec le droit **`manage_attendance`** et au moins une **antenne** active.

## Commandes

```powershell
dotnet run --project src/Lumineux.Api      # API
npm run start   # SPA (ng serve, depuis web/)
ng test --no-watch                          # unitaires Vitest
npx playwright test                         # e2e (API live requise)
```

## Scénarios de validation

### A. Reprise proactive — US1 (SC-001, SC-002, SC-004)

1. Se connecter (droit `manage_attendance`), ouvrir **Présences** (`/attendance`), **démarrer** une
   session → écran d'animation.
2. Cliquer **« Accueil »** (navigation ailleurs), puis revenir sur **Présences** (`/attendance`).
3. Vérifier l'**encart** « Vous avez une session en cours » : **libellé d'antenne**, **date**, **heure de
   début**, bouton **« Reprendre »** (**SC-002**).
4. Cliquer **« Reprendre »** → l'**écran d'animation** de la session s'ouvre (reprise en < 20 s,
   **SC-001**).
5. **Clôturer** la session, revenir sur `/attendance` → **aucun encart** (**SC-004**).

### B. Reprise sur conflit — US2 (SC-003)

1. Avec une session déjà **ouverte** pour une antenne/date, tenter d'en **démarrer** une nouvelle pour la
   **même** antenne/date → au lieu d'un simple message, un bouton **« Reprendre la session en cours »**
   est proposé (**SC-003**).
2. Cliquer dessus → l'écran d'animation de la session correspondante s'ouvre.
3. Démarrer une session sur une **autre** antenne/date (aucun conflit) → comportement **inchangé**
   (navigation vers la nouvelle session).

### C. Robustesse & sécurité (SC-005, SC-006)

1. Simuler un **échec** de la vérification (API indisponible) → message clair ; le **formulaire de
   démarrage reste utilisable** (**SC-006**).
2. Se connecter **sans** `manage_attendance` → **Présences** absent / `/attendance` refusé → reprise
   inaccessible (**SC-005**).
3. Session expirée (401) → purge + retour connexion (socle).

## Résultat attendu

Tous les scénarios A→C passent ; unitaires (service `myOpenSessions`, encart, reprise, conflit) et e2e
au vert. Aucune règle métier dupliquée côté client ; aucune dépendance ajoutée ; API inchangée.
