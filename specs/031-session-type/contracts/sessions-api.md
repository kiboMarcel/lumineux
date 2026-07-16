# Phase 1 — Contrat d'API : deltas Type de session

Aucune nouvelle route. Deux DTO existants du domaine Session reçoivent le champ `sessionType`.
Ajouts **rétrocompatibles** — pas de nouvelle version d'API (Principe V).

## `POST /api/v1/attendance-sessions` — démarrage

**Requête** (`StartSessionRequest`) — champ ajouté (optionnel) :

```jsonc
{
  "antennaId": 12,
  "meetingDate": "2026-07-16",
  "qrStepSeconds": 30,
  "sessionType": "AntennaMeeting"   // optionnel ; absent → AntennaMeeting
}
```

- `sessionType` absent → session `AntennaMeeting` (comportement actuel inchangé).
- `sessionType` = `"AntennaMeeting"` ou `"Teaching"` → session de ce type.
- `sessionType` = valeur non reconnue → **400 Bad Request**, message de validation clair, aucune
  session créée.

**Réponse** : `SessionResponse` (voir ci-dessous).

## `SessionResponse` — lecture (démarrage, `GET /api/v1/attendance-sessions/{id}`, `GET .../mine/open`)

Champ ajouté :

```jsonc
{
  "id": 87,
  "antennaId": 12,
  "meetingDate": "2026-07-16",
  "startTime": "2026-07-16T14:00:00Z",
  "endTime": null,
  "status": "Open",
  "openedByMemberId": 3,
  "closedByMemberId": null,
  "attendanceCount": 0,
  "sessionType": "AntennaMeeting"   // toujours présent (jamais null)
}
```

## Règles transverses

| Règle | Comportement |
|---|---|
| Autorisation | `manage_attendance` requise au démarrage (inchangé). |
| Immuabilité | `sessionType` fixé au démarrage ; aucun endpoint ne le modifie. |
| Validation | Serveur faisant autorité : ensemble fermé `{AntennaMeeting, Teaching}`. |
| Rétrocompat. | Clients existants (SPA sans le champ, mobile) continuent de fonctionner → défaut `AntennaMeeting`. |
| Comportement | Identique quel que soit le type : QR, pointage, clôture, annulation, auto-clôture, rapports, scan inchangés. |

## Non-objectifs de contrat

- Pas de modification du type après création (pas d'endpoint dédié).
- Pas de filtre par type dans les listes/rapports (hors périmètre).
- Pas de sélecteur de type dans l'écran de démarrage du SPA (API-only ; l'interface envoie
  aujourd'hui sans `sessionType` → `AntennaMeeting`).
