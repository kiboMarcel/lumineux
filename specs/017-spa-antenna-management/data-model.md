# Data Model — Console web : Gestion des antennes (état client)

Aucune persistance côté SPA. Modèles de vue en mémoire, reflet des DTO de l'API 016 (antennes) et 010
(districts, lecture pour le sélecteur).

```mermaid
flowchart LR
    Dist["GET /reference/districts (010)"] --> Form["antenna-form (sélecteur district)"]
    List["GET /antennas (016)"] --> ListView["antenna-list (actives + inactives)"]
    Form -->|POST /antennas| List
    Form -->|PUT /antennas/{id}| List
    ListView -->|POST /antennas/{id}/deactivate| List
    ListView -->|POST /antennas/{id}/activate| List
```

## Modèles consommés (vue client — reflet des DTO API)

### Antennes (`/api/v1/antennas`, feature 016)

| Modèle | Champs |
|--------|--------|
| `AntennaResponse` | `id`, `code`, `label`, `districtId`, `status` (`Active`/`Inactive`) |
| `CreateAntennaRequest` | `code`, `label`, `districtId` |
| `UpdateAntennaRequest` | `label`, `districtId` (le **code** n'est pas envoyé — immuable) |

### Districts (`/api/v1/reference/districts`, feature 010)

| Modèle | Champs |
|--------|--------|
| `ReferenceItem` | `id`, `code`, `label` (existant, réutilisé pour le sélecteur) |

## Erreurs métier (ProblemDetails + `code`)

| Statut | `code` | Message UI |
|--------|--------|-----------|
| `409` | `duplicate_code` | « Ce code d'antenne est déjà utilisé. » |
| `409` | `antenna_has_open_sessions` | « Impossible de désactiver : une session est encore ouverte. » |
| `400` | (validation) | messages de champ (code/libellé requis, district) |
| `404` | — | « Antenne introuvable. » |
| `403` | — | « Action non autorisée. » (API autorité) |
| `401` | — | purge + retour connexion (socle) |

## État de vue (transitoire, non persisté)

- **Liste** : `AntennaResponse[]` (actives + inactives) + indicateur de chargement.
- **Formulaire** : `code` / `label` / `districtId` ; en **édition**, `code` **lecture seule** ;
  districts chargés via `ReferenceApi.districts()`.
- **Action de statut** : identifiant ciblé + confirmation (désactivation) ; message d'erreur mappé.

## Persistance

**Aucune** (côté SPA). L'API 016 reste la source de vérité.
