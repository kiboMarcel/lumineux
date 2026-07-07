# Contrat — Endpoints consommés par la reprise de session (vue client)

Endpoints **existants** (mes sessions ouvertes 023, référentiels 010). **Aucune modification d'API.**
Requièrent un jeton Bearer ; les sessions exigent le droit **`manage_attendance`**.

## Mes sessions ouvertes (`manage_attendance`, feature 023)

| # | Méthode & chemin | Requête | Réponse | Statuts notables |
|---|------------------|---------|---------|------------------|
| 1 | `GET /api/v1/attendance-sessions/mine/open` | — (identité via jeton) | `200 SessionResponse[]` | `401`, `403` |

## Antennes (feature 010 — authentifié)

| # | Méthode & chemin | Réponse |
|---|------------------|---------|
| 2 | `GET /api/v1/reference/antennas` | `200 ReferenceItem[]` (libellé d'antenne) |

## Démarrage (existant, feature 014) — pour la reprise sur conflit

| # | Méthode & chemin | Réponse | Statuts notables |
|---|------------------|---------|------------------|
| 3 | `POST /api/v1/attendance-sessions` | `201 SessionResponse` | `409` (session déjà ouverte pour antenne/date) |

## Notes

- **(1)** ne renvoie **que** les sessions ouvertes de l'utilisateur courant (l'API filtre par jeton) →
  le client **ne re-filtre pas** par membre.
- **Encart** : construit à partir de (1) ; libellé d'antenne via (2).
- **Reprise sur conflit** : sur **409** de (3), rechercher dans (1) la session où `antennaId` = antenne
  choisie et `meetingDate` (jour) = date choisie → proposer la reprise ; sinon message de conflit.
- Mapping via le socle (`messageForError`) ; 401 gérés globalement (purge + reconnexion).
