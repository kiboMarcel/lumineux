# Quickstart — Module Membres (SPA, Lot 2)

Guide de validation. La SPA (`web/`) consomme l'API (membres 002 + référentiels 010).

## Prérequis

- API démarrée avec **CORS** autorisant l'origine du SPA ; au moins **une antenne active** (feature
  010) et un compte disposant du droit **`manage_members`**.
- SPA lancée (`cd web && npm start`), connecté avec un compte `manage_members`.

## Scénarios de validation (mappés aux user stories / SC)

### A — Recherche & consultation (US1)
1. Ouvrir l'entrée **Membres** (visible car droit présent) → liste paginée.
2. Rechercher par nom/référence ; naviguer entre pages ; ouvrir une **fiche**.
3. **Attendu** : identité complète, référence, statut, état d'activation ; **aucun secret** (SC-001,
   FR-004). Une fiche pour un id inexistant → « membre introuvable ».

### B — RBAC (US1, SC-006)
1. Se connecter avec un compte **sans** `manage_members`.
2. **Attendu** : l'entrée **Membres** est **absente** ; l'accès direct à `/members` est refusé/redirigé ;
   un appel API direct renverrait 403.

### C — Enrôlement nominal (US2)
1. Ouvrir **Nouveau membre** ; renseigner nom, prénom, sexe et **antenne** (liste déroulante alimentée
   par `/reference/antennas`), + champs optionnels via listes (civilité, ville, district, nationalité).
2. Valider **avec** e-mail → **création** ; message « invitation envoyée par e-mail » (aucun secret).
3. Valider **sans** e-mail → **remise bureau** : `loginId` + **mot de passe temporaire** affichés **une
   seule fois** ; rafraîchir la page → le secret **n'est plus** affiché (SC-005).

### D — Homonymie confirmable (US2, SC-003)
1. Créer un membre de mêmes nom+prénom qu'un existant.
2. **Attendu** : `409 duplicate_name` → **avertissement** Confirmer/Annuler ; **Confirmer** → réessai
   `confirmDuplicate=true` → création.

### E — Conflit de contact bloquant (US2/US3, SC-004)
1. Créer/corriger un membre avec un mobile/e-mail **déjà utilisé par un membre actif**.
2. **Attendu** : `409 contact_in_use` → **erreur bloquante** (pas de confirmation) ; membre non
   créé/non modifié.

### F — Correction (US3)
1. Ouvrir une fiche → **Modifier** ; changer des champs (la **référence** est en lecture seule) →
   enregistrer.
2. **Attendu** : modifications prises en compte ; un contact en conflit → erreur bloquante (cf. E).

### G — Antenne indisponible (FR-017)
1. Sur une instance **sans antenne active**, ouvrir **Nouveau membre**.
2. **Attendu** : la création est **empêchée** avec un message explicite (pas de soumission avec antenne
   invalide).

## Vérification finale (checklist SC)

- [ ] SC-001 recherche→fiche rapide · [ ] SC-002 création nominale + mode de remise
- [ ] SC-003 homonymie Confirmer/Annuler · [ ] SC-004 contact bloquant
- [ ] SC-005 aucun secret persisté (mot de passe temporaire une fois) · [ ] SC-006 masquage sans droit
- [ ] SC-007 message d'erreur distinct par type
