# Contrat consommé — Synchronisation de lot hors ligne (`/scan/batch`)

**Feature** : `027-mobile-offline-sync` · **Phase 1** · Statut : **EXISTANT, INCHANGÉ**

Ce document décrit le contrat serveur **déjà livré** que le client mobile **consomme**. **Aucune évolution
d'API** dans ce lot (FR-005/FR-010). Source de vérité :
`src/Lumineux.Api/Controllers/AttendancesController.cs`,
`src/Lumineux.Application/Attendances/SyncOfflineScansHandler.cs`,
`src/Lumineux.Application/Contracts/Attendances/AttendanceDtos.cs`,
`src/Lumineux.Application/Attendances/ScanValidators.cs`.

## Endpoint

```
POST /api/v1/attendance-sessions/{sessionId}/scan/batch
Authorization: Bearer <jeton membre>        # [Authorize] ; membre simple autorisé
Content-Type: application/json
```

- Le `sessionId` est porté par la **route** (groupement par séance, D8/FR-005) — **pas** dans le corps.
- HTTPS exclusif (FR-010).

## Corps de requête — `OfflineScanBatchRequest`

```jsonc
{
  "items": [
    {
      "clientOperationId": "a1b2c3…",   // string, non vide, ≤ 64 caractères (unique, idempotence)
      "token": "…",                      // string, non vide ; jeton scanné (sensible)
      "clientArrivalTime": "2026-07-09T14:03:12Z" // date-heure ISO 8601 UTC = heure du scan
    }
  ]
}
```

**Validation serveur** (`OfflineScanBatchValidator`) :
- `items` : **non vide**.
- `clientOperationId` : **non vide**, **≤ 64** caractères.
- `token` : **non vide**.

→ Violation ⇒ **400** (ProblemDetails). Le client doit donc n'émettre que des lots **non vides** et des
`clientOperationId` conformes (garanti par D7).

## Réponse — `200 OK` `OfflineScanBatchResponse`

```jsonc
{
  "results": [
    {
      "clientOperationId": "a1b2c3…",
      "outcome": "Created",       // "Created" | "AlreadyPresent" | "Rejected"
      "reason": null,             // renseigné (string) uniquement si outcome == "Rejected"
      "attendanceId": 4821        // renseigné pour Created/AlreadyPresent ; null si Rejected
    }
  ]
}
```

### Sémantique par `outcome` (source : `SyncOfflineScansHandler`)

| `outcome` | Signification serveur | Réconciliation client (D5) |
|-----------|-----------------------|----------------------------|
| `Created` | Présence enregistrée à l'heure d'arrivée (bornée) | **Retirer** de la file (succès) |
| `AlreadyPresent` | Même `clientOperationId` déjà traité, **ou** membre déjà présent (idempotence/unicité) | **Retirer** de la file (succès) |
| `Rejected` | Refus avec `reason` — jeton invalide au moment du scan, heure hors plage, ou arrivée postérieure à la clôture | **Retirer** + créer un `SyncNotice(rejected, reason)` |

**Raisons de rejet connues** (chaînes serveur, à afficher telles quelles) :
- `« Jeton QR invalide au moment du scan. »`
- `« Heure d'arrivée hors de la plage de la session. »`
- `« Arrivée postérieure à la clôture de la session. »`

## Règles serveur clés (rappel — le client ne les re-valide pas, FR-010)

- **Idempotence** : `GetByClientOperationIdAsync` ⇒ un ré-envoi du même `clientOperationId` renvoie
  `AlreadyPresent`, **jamais** un doublon (FR-008).
- **Jeton validé contre l'heure d'arrivée** : `_qr.Validate(secret, step, token, arrival)` — la capture
  reste valide même synchronisée plus tard (avec tolérance).
- **Bornage temporel** : `arrival ∈ [session.StartTime, UtcNow]` sinon `Rejected`.
- **Post-clôture** : si la séance est close et `arrival >= EndTime` ⇒ `Rejected`.

## Codes d'erreur de transport (mapping client → réconciliation)

| Statut | `ApiErrorType` (client) | Action |
|--------|-------------------------|--------|
| `200` | — | Traiter chaque `result` (tableau ci-dessus) |
| `400` | `validation` | Retirer + `SyncNotice(permanentlyFailed, « requête invalide »)` (ne devrait pas survenir) |
| `401` | `unauthorized` | **Conserver** les éléments ; socle purge la session ; reprise après reconnexion |
| `403` | `forbidden` | Conserver ; membre inactif/inconnu → surfacer (rare) |
| `404` | `notFound` | Séance introuvable → `SyncNotice(permanentlyFailed)` et retrait |
| `5xx` / réseau | `server` / `network` | **Conserver**, `attemptCount++`, backoff ; plafond FR-013 → échec définitif |

## Miroir client (DTO)

`mobile/lib/features/attendance/data/offline_scan_dtos.dart` reproduit **exactement** ces formes :
`OfflineScanItem`, `OfflineScanBatchRequest`, `OfflineScanResult`, `OfflineScanBatchResponse`, et les
constantes d'issue `Created` / `AlreadyPresent` / `Rejected`.
