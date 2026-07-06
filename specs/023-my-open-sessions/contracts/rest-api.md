# Contrat REST — Mes sessions de présence ouvertes

Endpoint **ajouté** au `AttendanceSessionsController` (feature 001). Exige un jeton Bearer **et** le droit
**`manage_attendance`** (`[Authorize(Policy = manage_attendance)]`). **Lecture seule.** Erreurs au format
**ProblemDetails** (RFC 7807).

## Endpoint

| # | Méthode & chemin | Paramètres | Réponse | Statuts |
|---|------------------|-----------|---------|---------|
| 1 | `GET /api/v1/attendance-sessions/mine/open` | — (identité via jeton) | `200 SessionResponse[]` | 200, 401, 403 |

## Réponse

```text
SessionResponse[] où
SessionResponse { id, antennaId, meetingDate, startTime, endTime?, status, openedByMemberId, closedByMemberId?, attendanceCount }
```

- Ne renvoie que les sessions **ouvertes** (`status = Open`) **de l'utilisateur courant**
  (`openedByMemberId = membre du jeton`).
- `attendanceCount` vaut **0** (cohérent avec `GET /{id}` ; le décompte en direct est obtenu via la
  liste des présences, feature 001).
- **Liste vide** si l'utilisateur n'a aucune session ouverte (200, pas d'erreur).

## Règles

- **`mine/open`** : `mine` n'est pas un entier → **aucun conflit** avec `GET /{sessionId:int}`.
- **Aucun paramètre** : l'identité est déterminée **par le jeton** (jamais un paramètre client) →
  impossible d'obtenir les sessions d'un autre membre.
- **Lecture seule** : aucun effet de bord ; la règle « une session ouverte par antenne/date » et la
  clôture sont **inchangées**.

## RBAC & sécurité

- Droit **`manage_attendance`** (réutilisé). Résultat **strictement limité** à l'utilisateur courant.
  Accès journalisé (request logging). **Aucune écriture, aucune migration.**
