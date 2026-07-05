# Quickstart — Endpoints de données de référence

Guide de validation. Prérequis : solution buildée ; instance avec au moins une antenne active
(le harnais de test amorce une antenne) et un compte permettant d'obtenir un jeton.

## Scénario A — Lister les antennes (US1, chemin nominal)

1. Se connecter pour obtenir un jeton (`POST /api/v1/auth/login`).
2. Appeler `GET /api/v1/reference/antennas` avec `Authorization: Bearer <jeton>`.
3. **Attendu** : `200` avec une liste d'objets `{ id, code, label }` ; seules les antennes **actives**
   apparaissent ; l'ordre est **stable** (par libellé). Le SPA peut peupler la liste de l'antenne
   d'origine (SC-001).

## Scénario B — Autres nomenclatures (US2)

1. Avec le même jeton, appeler successivement
   `GET /api/v1/reference/{civilities|cities|districts|countries}`.
2. **Attendu** : `200` avec des listes d'entrées **actives** et **triées**.
3. Pour `countries`, chaque entrée fournit `country` (libellé de pays) **et** `nationality` (libellé de
   nationalité) **distincts**.

## Scénario C — Entrées désactivées exclues (FR-004 / SC-002)

1. Sur une base contenant une entrée **désactivée**, appeler la liste correspondante.
2. **Attendu** : l'entrée désactivée **n'apparaît pas**.

## Scénario D — Sans authentification (FR-006 / SC-003)

1. Appeler n'importe quel endpoint `reference/*` **sans** en-tête `Authorization`.
2. **Attendu** : `401 Unauthorized`.

## Scénario E — Tri stable (SC-004)

1. Appeler deux fois de suite la même liste.
2. **Attendu** : le **même ordre** est renvoyé.

## Vérification finale (checklist SC)

- [ ] SC-001 antennes en un appel · [ ] SC-002 actives uniquement · [ ] SC-003 401 sans jeton
- [ ] SC-004 tri stable · [ ] SC-005 aucune donnée secrète
