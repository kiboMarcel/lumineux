# Quickstart — Console web : Présences (Lot 4)

Guide de validation du module **Présences** de la SPA Angular (`web/`). Prouve les scénarios
bout-en-bout et mappe les critères de succès **SC-001..SC-007**.

## Prérequis

- API Lumineux démarrée en **HTTPS** (`https://localhost:4311`) avec sessions/présences (001),
  référentiel antennes (010) et **recherche membre allégée** (015).
- SPA lancée : `ng serve` (`http://localhost:4200`) — `environment.apiBaseUrl = https://localhost:4311`.
- Compte de test disposant du droit **`manage_attendance`** (et un compte **sans** ce droit pour
  vérifier le masquage).
- **Bibliothèque QR** installée (dépendance npm — approuvée à l'implémentation).
- Au moins une **antenne** au référentiel et quelques **membres** actifs (pour le lookup).

## Commandes

```powershell
# API (depuis la racine)
dotnet run --project src/Lumineux.Api

# SPA (depuis web/)
npm run start        # ng serve
ng test --no-watch   # unitaires Vitest
npx playwright test  # e2e (API live requise)
```

## Scénarios de validation

### A. Démarrer une session + QR rotatif — US1 (SC-001, SC-005)

1. Se connecter (droit `manage_attendance`) → **« Présences »** visible dans la navigation.
2. Ouvrir `/attendance`, choisir une **antenne** (référentiel), la **date**, un **pas de rotation** →
   **Démarrer**.
3. Redirection vers `/attendance/sessions/:id` : un **QR** s'affiche en grand.
4. Attendre un cycle : le QR **se régénère** avant expiration (**SC-001**).
5. Vérifier (DOM / réseau) que le **jeton n'apparaît jamais en clair** ni n'est persisté (**SC-005**).

### B. Suivi temps réel — US2 (SC-002)

1. Session ouverte : provoquer une présence (scan simulé ou ajout manuel ci-dessous).
2. La **liste** et le **décompte des valides** se mettent à jour **sans action manuelle** (**SC-002**).
3. Basculer le **filtre** Valides / Annulées / Toutes → la liste reflète le statut demandé.

### C. Ajout manuel via lookup — US3 (SC-003)

1. Ouvrir l'**ajout manuel**, saisir une **référence/nom** → résultats du **lookup** (015) (champs
   minimaux : référence, nom, statut).
2. Sélectionner un membre → **Ajouter** → il apparaît dans la liste (**SC-003**).
3. Réajouter le **même** membre → **pas de doublon** (idempotent).

### D. Annulation — US3 (SC-007)

1. Sur une présence, **Annuler** → **confirmation** demandée (**SC-007**).
2. Confirmer → la présence passe en **Annulée**, le décompte des valides décroît.

### E. Clôture — US4 (SC-004, SC-006, SC-007)

1. **Clôturer** la session → **confirmation** (**SC-007**).
2. Après clôture : **QR masqué**, actions d'écriture (ajout/annulation) **indisponibles** (**SC-006**).
3. Tenter une écriture sur session close (accès direct/route) → **409** restitué en message clair
   (**SC-004**).

### F. Contrôle d'accès (sécurité)

1. Se connecter **sans** `manage_attendance` → entrée **« Présences » absente** ; accès direct à
   `/attendance` **refusé** (garde) ; l'API répond **403** si sollicitée.
2. Laisser expirer la session → **401** → purge + retour connexion (socle).

## Résultat attendu

Tous les scénarios A→F passent ; unitaires (rotation QR, polling, lookup, confirmations, masquage
post-clôture) et e2e au vert. Aucun jeton QR affiché/persisté.
