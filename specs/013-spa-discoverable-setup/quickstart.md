# Quickstart — Installation découvrable (SPA)

Guide de validation. Prérequis : API démarrée + CORS ; SPA lancée (`cd web && npm start`).

## Scénario A — Instance vierge : lien visible → installation (US1)

1. Sur une instance **non initialisée**, ouvrir l'écran de **connexion**.
2. **Attendu** : un lien **« Première installation »** est **visible** (SC-001).
3. Suivre le lien → l'**écran d'installation** (existant) s'affiche ; créer le premier administrateur
   avec des données valides → **connecté** à la console.

## Scénario B — Instance déjà installée : aucun lien (US2, SC-002)

1. Sur une instance **déjà installée** (au moins un administrateur actif), ouvrir la connexion.
2. **Attendu** : **aucun** lien « Première installation » n'est affiché.

## Scénario C — Statut indisponible : défaut sûr (SC-003)

1. Simuler une **indisponibilité** du statut (API injoignable / erreur réseau) à l'ouverture de la
   connexion.
2. **Attendu** : la connexion reste **pleinement utilisable** ; le lien d'installation **n'apparaît
   pas** (masqué par prudence).

## Scénario D — Course avec une autre installation (US2, FR-007)

1. Sur une instance vierge, ouvrir l'écran d'installation ; entre-temps, installer l'admin par un
   autre biais ; soumettre le formulaire.
2. **Attendu** : refus **« déjà installé »** clair, retour connexion, sans état incohérent
   (comportement existant réutilisé).

## Vérification finale (checklist SC)

- [ ] SC-001 lien visible + installation de bout en bout (instance vierge)
- [ ] SC-002 aucun lien sur instance installée
- [ ] SC-003 défaut sûr (statut indisponible → masqué, connexion utilisable)
- [ ] SC-004 aucune donnée sensible ; verrou d'installation effectif
