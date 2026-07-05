# Data Model — Console web : Présences (état client)

Aucune persistance côté SPA. Modèles de vue en mémoire, reflet des DTO de l'API (sessions/présences
feature 001, référentiels 010, lookup 015). **Le jeton QR n'est jamais persisté.**

```mermaid
flowchart LR
    Ant["GET /reference/antennas"] --> Start["SessionStart"]
    Start -->|POST /attendance-sessions| Run["SessionRun (animation)"]
    Run -->|GET .../qr (rotation)| QR["Panneau QR (image générée)"]
    Run -->|GET .../attendances (polling)| List["Liste + décompte"]
    Look["GET /members/lookup"] --> Manual["Ajout manuel"] -->|POST .../attendances| Run
    Run -->|DELETE .../attendances/{memberId}| Run
    Run -->|POST .../close| Closed["Session close"]
```

## Modèles consommés (vue client — reflet des DTO API)

### Session (`/attendance-sessions`)

| Modèle | Champs |
|--------|--------|
| `StartSessionRequest` | `antennaId`, `meetingDate`, `qrStepSeconds?` |
| `SessionResponse` | `id`, `antennaId`, `meetingDate`, `startTime`, `endTime?`, `status`, `openedByMemberId`, `closedByMemberId?`, `attendanceCount` |
| `QrTokenResponse` | `token`, `stepSeconds`, `expiresAt` — **éphémère**, sert au rendu QR, jamais persisté |

### Présences (`/attendance-sessions/{id}/attendances`)

| Modèle | Champs |
|--------|--------|
| `AttendanceResponse` | `id`, `sessionId`, `memberId`, `memberFullName?`, `arrivalTime`, `endTime?`, `source` (Scan/Manuel), `status` (Valid/Cancelled), `originAntennaId?` |
| `AttendanceListResponse` | `sessionId`, `validCount`, `items: AttendanceResponse[]` |
| `ManualAttendanceRequest` | `memberId`, `arrivalTime?` |

### Recherche membre allégée (feature 015)

| Modèle | Champs |
|--------|--------|
| `MemberLookupItem` | `id`, `reference`, `fullName`, `status` |

### Antennes (feature 010)

| Modèle | Champs |
|--------|--------|
| `ReferenceItem` | `id`, `code`, `label` |

## Filtre de statut (liste)

`Valid` | `Cancelled` | `All` (paramètre `status` de la liste).

## Erreurs métier (ProblemDetails + `code`)

| Statut | Sens | Traitement UI |
|--------|------|---------------|
| `400` | validation (démarrage/ajout) | messages de champ |
| `404` | session / présence introuvable | message « introuvable » |
| `409` | **session close** / opération invalide | message bloquant (« session close ») |
| `401` | session expirée | purge + retour connexion (socle) |
| `403` | droit `manage_attendance` manquant | « action non autorisée » (API autorité) |

## État de vue (transitoire, non persisté)

- **Session courante** : `SessionResponse` (statut ouvert/close).
- **QR** : `token`/`stepSeconds`/`expiresAt` **en mémoire**, remplacé à chaque rotation ; **image**
  dérivée du token ; **jamais** affiché en clair ni stocké (FR-005/SC-005).
- **Liste des présences** : items + `validCount` + filtre courant ; rafraîchie par polling.
- **Ajout manuel** : terme de recherche + résultats de lookup + membre sélectionné (transitoire).

## Persistance

**Aucune** (côté SPA). L'API reste la source de vérité.
