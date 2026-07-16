# Phase 1 — Modèle de données : Type de session

## Nouvel enum : `SessionType`

| Valeur | Ordinal | Sens |
|---|---|---|
| `AntennaMeeting` | 0 | Réunion d'antenne / prière (défaut — comportement existant) |
| `Teaching` | 1 | Séance d'enseignement (préparé, sans logique métier distincte à ce stade) |

Ensemble **fermé** ; toute valeur hors de cet ensemble est refusée côté serveur.

## Entité modifiée : `AttendanceSession`

| Attribut | Type | Nullable | Contrainte | Notes |
|---|---|---|---|---|
| `SessionType` | `SessionType` | Non | Valeur d'enum reconnue ; défaut `AntennaMeeting` | Setter **privé** ; assigné uniquement par `Start`. Immuable ensuite. |

### Fabrique et immuabilité

- `AttendanceSession.Start(..., SessionType sessionType = SessionType.AntennaMeeting, ...)` :
  nouveau paramètre à valeur par défaut → appelants existants inchangés.
- Aucune méthode (`Close`, `Cancel`, `AutoClose`) ne modifie `SessionType` (FR-006).

### Représentation en base

| Colonne | Type SQL | Nullable | Défaut |
|---|---|---|---|
| `session_type` | `nvarchar(20)` | NOT NULL | `'AntennaMeeting'` |

- Persistance via `HasConversion<string>().HasMaxLength(20)` (comme `status`).
- Migration `SessionType` : `AddColumn` **NOT NULL** avec `defaultValue: "AntennaMeeting"` →
  rétro-remplit toutes les lignes existantes (FR-003). Additive, rejouable.
- Aucun index (pas de filtre/recherche sur ce champ à ce stade).
- Champs d'audit inchangés.

### Règles de validation (FR-001, FR-004, FR-005, FR-009)

1. **Défaut** : absence de type au démarrage → `AntennaMeeting` (pas d'erreur).
2. **Ensemble fermé** : un type fourni doit correspondre à une valeur d'enum reconnue, sinon rejet
   avec message clair, sans création (validation Application, faisant autorité côté serveur).
3. **`Teaching` accepté** structurellement, sans déclencher aucune règle métier distincte.

### Invariants inchangés

- Tous les invariants de `Start` (antenne, membre initiateur, secret QR, pas de rotation) restent
  identiques. `SessionType` n'entre dans aucune autre règle.
- Aucun impact sur `Attendance`, la clôture, l'annulation (028), l'auto-clôture, les rapports
  (018/020), le scan mobile.

## Transitions d'état

Aucune sur `SessionType` : décidé à l'ouverture, immuable. (Le cycle `Open → Closed/Cancelled`
de `Status` est inchangé et orthogonal au type.)

## Flux de données (résumé)

```text
StartSessionRequest.SessionType? ──(validé: reconnu ou absent)──▶ Handler (parse → enum, défaut AntennaMeeting)
                                                                        │
                                                                        ▼
                                                    AttendanceSession.Start(..., sessionType)
                                                                        │
                                                                        ▼
                                                     attendance_sessions.session_type (nvarchar(20) NOT NULL)
                                                                        │
                                                                        ▼
                                              SessionResponse.SessionType (string) ──▶ contrat SPA (non affiché)
```
