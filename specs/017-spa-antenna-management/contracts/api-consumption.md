# Contrat — Endpoints consommés par le module Antennes (vue client)

Endpoints **existants** (gestion des antennes 016, référentiels 010). **Aucune modification d'API.**
Tous requièrent un jeton Bearer. Les opérations de gestion des antennes exigent le droit
**`manage_referentials`**.

## Gestion des antennes (`manage_referentials`, feature 016)

| # | Méthode & chemin | Requête | Réponse | Statuts notables |
|---|------------------|---------|---------|------------------|
| 1 | `GET /api/v1/antennas` | — | `200 AntennaResponse[]` (**inactives incluses**) | `401`, `403` |
| 2 | `GET /api/v1/antennas/{id}` | — | `200 AntennaResponse` | `401`, `403`, `404` |
| 3 | `POST /api/v1/antennas` | `{ code, label, districtId }` | `201 AntennaResponse` | `400`, `409` `duplicate_code`, `401`, `403` |
| 4 | `PUT /api/v1/antennas/{id}` | `{ label, districtId }` | `200 AntennaResponse` | `400`, `404`, `401`, `403` |
| 5 | `POST /api/v1/antennas/{id}/deactivate` | — | `200 AntennaResponse` (Inactive) | `404`, `409` `antenna_has_open_sessions`, `401`, `403` |
| 6 | `POST /api/v1/antennas/{id}/activate` | — | `200 AntennaResponse` (Active) | `404`, `401`, `403` |

## Districts (feature 010 — authentifié)

| # | Méthode & chemin | Réponse |
|---|------------------|---------|
| 7 | `GET /api/v1/reference/districts` | `200 ReferenceItem[]` (choix du district au formulaire) |

## Notes

- **Liste (1)** : renvoie **toutes** les antennes avec `status` — base des vues actives/inactives ;
  distincte de la lecture publique `GET /reference/antennas` (010, actives seules), **inchangée**.
- **Création (3)** : `duplicate_code` (409) → message « code déjà utilisé » ; district inconnu → `400`.
- **Modification (4)** : n'envoie **pas** le `code` (immuable) ; `404` si introuvable.
- **Désactivation (5)** : **confirmée** côté UI ; refus `antenna_has_open_sessions` (409) → message
  clair, antenne conservée active.
- Mapping via le socle (`messageForError`) + libellés dédiés pour les deux codes 409 ; 401 gérés
  globalement (purge + reconnexion).
