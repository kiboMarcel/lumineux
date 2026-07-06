# Contrat REST — Gestion des antennes (`/api/v1/antennas`)

Tous les endpoints exigent un jeton Bearer **et** le droit **`manage_referentials`**
(`[Authorize(Policy = manage_referentials)]`). L'API reste l'autorité (401 non authentifié / 403 sans
droit). Erreurs au format **ProblemDetails** (RFC 7807) + extension `code`. La lecture publique
`GET /api/v1/reference/antennas` (feature 010) **n'est pas modifiée**.

## DTO

```text
AntennaResponse       { id, code, label, districtId, status }
CreateAntennaRequest  { code, label, districtId }
UpdateAntennaRequest  { label, districtId }          # code NON accepté (immuable)
```

## Endpoints

| # | Méthode & chemin | Corps | Réponse | Statuts |
|---|------------------|-------|---------|---------|
| 1 | `POST /api/v1/antennas` | `CreateAntennaRequest` | `201 AntennaResponse` (+ `Location`) | 201, 400 (validation), 409 `duplicate_code`, 401, 403 |
| 2 | `GET /api/v1/antennas` | — | `200 AntennaResponse[]` (**inclut les inactives**) | 200, 401, 403 |
| 3 | `GET /api/v1/antennas/{id}` | — | `200 AntennaResponse` | 200, 404, 401, 403 |
| 4 | `PUT /api/v1/antennas/{id}` | `UpdateAntennaRequest` | `200 AntennaResponse` | 200, 400, 404, 401, 403 |
| 5 | `POST /api/v1/antennas/{id}/deactivate` | — | `200 AntennaResponse` (Inactive) | 200, 404, 409 `antenna_has_open_sessions`, 401, 403 |
| 6 | `POST /api/v1/antennas/{id}/activate` | — | `200 AntennaResponse` (Active) | 200, 404, 401, 403 |

## Règles par endpoint

- **1 Créer** : `code` requis (unique, trim), `label` requis, `districtId` existant. Doublon de code →
  **409 `duplicate_code`**. District inconnu → **400** (validation). Statut initial **Active**.
- **2 Lister (gestion)** : renvoie **toutes** les antennes (actives **et** inactives) avec `status` —
  distinct de la lecture publique 010 (actives seules).
- **3 Consulter** : `404` si introuvable.
- **4 Modifier** : met à jour `label` + `districtId` (existant) ; le **`code` est ignoré/immuable** ;
  `404` si introuvable.
- **5 Désactiver** : passe `Inactive`. **Refusé 409 `antenna_has_open_sessions`** si l'antenne porte au
  moins une **session de présence ouverte**. Idempotent si déjà inactive (état cible atteint).
  Préserve les rattachements membres/sessions (aucune suppression).
- **6 Réactiver** : passe `Active`. Idempotent si déjà active.

## Codes d'erreur métier

| `code` | Statut | Sens |
|--------|--------|------|
| `duplicate_code` | 409 | Une antenne avec ce code existe déjà |
| `antenna_has_open_sessions` | 409 | Désactivation refusée : session(s) de présence ouverte(s) |
| (validation) | 400 | Champ requis manquant / district inexistant |
| — | 404 | Antenne introuvable |
| — | 401/403 | Non authentifié / droit `manage_referentials` manquant |

## Traçabilité (Principe VI)

Chaque **création, modification, désactivation, réactivation** est journalisée (auteur + horodatage) ;
les **refus** (droit manquant, `duplicate_code`, `antenna_has_open_sessions`) sont consignés.

## RBAC

- Nouveau droit **`manage_referentials`** ajouté au **catalogue** (attribuable via les profils du
  bureau, feature 011) et déclaré comme **policy** (vérifiée sur le contrôleur).
