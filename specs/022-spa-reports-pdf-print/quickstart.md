# Quickstart — Console web : Export PDF des rapports (impression)

Guide de validation. Prouve les scénarios bout-en-bout et mappe **SC-001..006**.

## Prérequis

- API Lumineux démarrée (HTTPS `https://localhost:4311`) avec des présences (rapports 018/020 alimentés).
- SPA lancée : `ng serve` (`http://localhost:4200`).
- Un compte avec le droit **`manage_attendance`** et un compte **sans** ce droit (masquage / 403).

## Commandes

```powershell
dotnet run --project src/Lumineux.Api      # API
npm run start   # SPA (ng serve, depuis web/)
ng test --no-watch                          # unitaires Vitest
npx playwright test                         # e2e (émulation media print)
```

## Scénarios de validation

### A. Exporter en PDF — US1 (SC-001, SC-002, SC-003, SC-005)

1. Se connecter (droit `manage_attendance`), ouvrir `/reports`, choisir une **période** et **Afficher**.
2. Cliquer **« Exporter en PDF »** → le **dialogue d'impression** s'ouvre (**SC-001**) ; choisir
   « Enregistrer en PDF ».
3. Dans l'aperçu : **en-tête** avec **période**, **antenne** (ou « Toutes ») et **date de génération**
   (**SC-003**) ; **synthèse** (tableau + barres) et **courbe** conformes à l'écran (**SC-002**).
4. Sélectionner un **membre** (taux affiché) puis exporter → le **bloc taux** figure ; sans membre, il
   **n'apparaît pas** (**SC-005**).

### B. Mise en page imprimable — US2 (SC-004)

1. Dans l'aperçu d'impression (ou via l'émulation `media: print`), vérifier que la **navigation**, les
   **boutons** et les **champs** sont **masqués** (**SC-004**).
2. Vérifier la **lisibilité** : pas de troncature ; tableaux/graphiques dans la largeur de page.

### C. Sécurité (SC-006)

1. Se reconnecter **sans** le droit `manage_attendance` → module Rapports **absent** / accès direct
   refusé → l'export est inaccessible (**SC-006**).

## Résultat attendu

Tous les scénarios A→C passent ; unitaires (`window.print` déclenché, en-tête présent, bloc taux
conditionnel) et e2e (émulation media print) au vert. Aucune dépendance ajoutée ; aucun appel réseau ;
API inchangée.
