# Quickstart — Console web : Tableau de bord des rapports de présence

Guide de validation du module **Rapports**. Prouve les scénarios bout-en-bout et mappe **SC-001..006**.

## Prérequis

- API Lumineux démarrée (HTTPS `https://localhost:4311`) avec les rapports (018), le référentiel des
  antennes (010) et la recherche membre (015) ; des sessions/présences sur une période de test.
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

### A. Accès & synthèse — US1 (SC-001, SC-002, SC-005, SC-006)

1. Se connecter (droit `manage_attendance`) → l'entrée **« Rapports »** est visible.
2. Ouvrir `/reports`, choisir une **plage de dates** valide → un **tableau par antenne** (sessions,
   présences valides, moyenne) et des **barres** comparatives s'affichent (**SC-001**).
3. Vérifier que la **hauteur des barres** est **proportionnelle** aux valeurs (**SC-002**).
4. Appliquer un **filtre d'antenne** → seule cette antenne s'affiche ; période sans donnée → **état
   vide**.
5. Saisir une **plage invalide** (fin avant début) → **message clair**, aucun résultat (**SC-006**).
6. Se reconnecter **sans** le droit → entrée **absente** ; accès direct `/reports` **refusé**
   (**SC-005**).

### B. Export CSV — US2 (SC-003)

1. Depuis une synthèse, cliquer **Exporter (CSV)** → un **fichier CSV** est téléchargé (nom reflétant la
   période).
2. Ouvrir le CSV → ses chiffres **correspondent** au tableau affiché (**SC-003**).

### C. Taux par membre — US3 (SC-004)

1. Dans le panneau **Taux par membre**, rechercher et **sélectionner un membre** (recherche allégée).
2. Afficher le taux → **présences valides**, **sessions éligibles** et **taux en %** (jauge).
3. Membre **sans présence** → **0 %** sans erreur (**SC-004**). Membre introuvable → message clair.
4. Sans membre sélectionné → invite à choisir un membre, **aucun appel** de taux.

### D. Robustesse

1. Laisser expirer la session (401) → purge + retour connexion (socle).
2. Les libellés d'**antenne** et de **membre** sont affichés (pas seulement des identifiants).

## Résultat attendu

Tous les scénarios A→D passent ; unitaires (service dont Blob, synthèse+barres, export, taux membre) et
e2e au vert. Aucun calcul statistique dupliqué côté client ; aucune dépendance graphique ajoutée.
