# Quickstart — Recherche membre allégée (member lookup)

Guide de validation. Prérequis : solution buildée ; au moins un membre semé (harnais de test).

## Scénario A — Opérateur de présence : recherche minimale (US1)

1. Se connecter avec un compte disposant du droit **gestion des présences** (sans gestion des membres).
2. Appeler `GET /api/v1/members/lookup?query=<nom ou référence>` avec `Authorization: Bearer <jeton>`.
3. **Attendu** : `200` avec une **liste courte** d'objets `{ id, reference, fullName, status }` ;
   **aucune** coordonnée ni donnée personnelle superflue (SC-002).

## Scénario B — Gestionnaire des membres : même vue minimale (US1)

1. Avec un compte **gestion des membres**, appeler le même endpoint.
2. **Attendu** : `200` avec le **même** format minimal (lecture élargie any-of).

## Scénario C — Droit manquant → 403 (SC-004)

1. Avec un compte **sans** gestion des présences ni gestion des membres, appeler l'endpoint.
2. **Attendu** : `403`. Sans jeton → `401`.

## Scénario D — Terme requis → 400 (SC-003)

1. Appeler `GET /api/v1/members/lookup` **sans** `query` (ou `query=` vide).
2. **Attendu** : `400` (un critère est requis).

## Scénario E — Résultats plafonnés (SC-005)

1. Rechercher un terme très commun.
2. **Attendu** : le nombre de résultats ne dépasse **jamais** le plafond (liste courte).

## Vérification finale (checklist SC)

- [ ] SC-001 identification en un appel (présences) · [ ] SC-002 champs minimaux
- [ ] SC-003 terme requis (400) · [ ] SC-004 accès any-of (401/403) · [ ] SC-005 résultats plafonnés
