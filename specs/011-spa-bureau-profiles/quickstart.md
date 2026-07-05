# Quickstart — Module Profils du bureau & droits (SPA, Lot 3)

Guide de validation. La SPA (`web/`) consomme l'API profils (004), membres (002) et le catalogue de
permissions.

## Prérequis

- API démarrée + **CORS** ; comptes de test : un **administrateur des profils**
  (`manage_bureau_profiles`) et un **gestionnaire des membres** (`manage_members` seul).
- SPA lancée (`cd web && npm start`).

## Scénarios de validation (mappés aux user stories / SC)

### A — Consultation & lecture élargie (US1, SC-001)
1. Se connecter en **gestionnaire des membres** (lecture seule).
2. Ouvrir **Profils du bureau** (entrée visible) → liste ; ouvrir un profil → droits + titulaires.
3. **Attendu** : aucune action **créer/modifier/supprimer** proposée (lecteur sans droit d'écriture).

### B — RBAC sans droit de lecture (US1, SC-004)
1. Se connecter avec un compte **sans** aucun des deux droits.
2. **Attendu** : entrée « Profils du bureau » **absente** ; accès direct refusé.

### C — Administration des profils (US2)
1. Se connecter en **administrateur des profils**.
2. **Créer** un profil : nom + sélection de droits (catalogue) → apparaît dans la liste (SC-002).
3. **Nom déjà utilisé** → erreur bloquante « nom déjà utilisé » (SC-003).
4. **Modifier** un profil ; tenter de **retirer l'administration** du **dernier** profil admin →
   erreur « dernier administrateur » (SC-003).
5. **Supprimer** un profil → **confirmation** requise (SC-007) ; garde-fous restitués (dernier
   administrateur / profil attribué) comme erreurs bloquantes.

### D — Attribution & révocation (US3)
1. Depuis une **fiche membre**, ouvrir **Profils & droits** → profils attribués + **droits effectifs**.
2. **Attribuer** un profil → droits effectifs mis à jour ; **réattribuer** le même → **aucune erreur**
   (idempotence, SC-005).
3. **Révoquer** un profil → **confirmation** ; droits effectifs mis à jour ; révoquer le **dernier
   administrateur** → erreur « dernier administrateur » (SC-003).
4. Vérifier que les **droits effectifs** = **union** des droits des profils attribués (SC-006).

## Vérification finale (checklist SC)

- [ ] SC-001 lecture élargie sans écriture · [ ] SC-002 création < 2 min
- [ ] SC-003 conflits bloquants (nom / dernier admin) · [ ] SC-004 aucune écriture sans droit
- [ ] SC-005 attribution idempotente · [ ] SC-006 droits effectifs = union
- [ ] SC-007 confirmations destructrices
