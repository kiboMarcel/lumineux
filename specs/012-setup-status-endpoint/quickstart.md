# Quickstart — Statut d'installation (setup/status)

Guide de validation. Prérequis : solution buildée.

## Scénario A — Instance vierge → non installé (US1)

1. Sur une base **sans administrateur actif**, appeler `GET /api/v1/setup/status` **sans**
   authentification.
2. **Attendu** : `200` avec `{ "installed": false }`.

## Scénario B — Après installation → installé (US1, cohérence verrou)

1. Installer le premier administrateur (`POST /api/v1/setup/first-admin`).
2. Rappeler `GET /api/v1/setup/status`.
3. **Attendu** : `200` avec `{ "installed": true }` (bascule non installé → installé, SC-002).

## Scénario C — Accès anonyme (US1, SC-001)

1. Appeler `GET /api/v1/setup/status` **sans** en-tête `Authorization`.
2. **Attendu** : `200` (jamais 401/403).

## Scénario D — Réponse minimale, aucune fuite (SC-003)

1. Consulter le statut dans les deux états (installé / non installé).
2. **Attendu** : la réponse ne contient **que** `installed` — aucun comptage, aucune donnée de
   compte/membre.

## Scénario E — Le verrou reste effectif (SC-004)

1. Sur une instance **installée**, tenter `POST /api/v1/setup/first-admin`.
2. **Attendu** : refus **409 already_installed** (le statut n'a rien affaibli).

## Vérification finale (checklist SC)

- [ ] SC-001 statut en un appel anonyme · [ ] SC-002 concordance avec le verrou (bascule)
- [ ] SC-003 réponse strictement booléenne · [ ] SC-004 verrou d'installation toujours effectif
