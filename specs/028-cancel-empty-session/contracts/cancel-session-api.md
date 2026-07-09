# Contrat API — Annulation d'une session (`POST /attendance-sessions/{id}/cancel`)

**Feature** : `028-cancel-empty-session` · **Phase 1** · Statut : **NOUVEAU (additif)**

Miroir de `POST /attendance-sessions/{id}/close`. Aucun DTO nouveau : `SessionResponse` est réutilisé.

## Endpoint

```
POST /api/v1/attendance-sessions/{sessionId}/cancel
Authorization: Bearer <jeton bureau>        # [Authorize] Policy = manage_attendance
```

- Pas de corps de requête.
- HTTPS exclusif.

## Réponse — `200 OK` `SessionResponse`

```jsonc
{
  "id": 42,
  "antennaId": 3,
  "meetingDate": "2026-07-09T00:00:00Z",
  "startTime": "2026-07-09T14:00:00Z",
  "endTime": null,
  "status": "Cancelled",        // <-- l'état passe à Cancelled
  "openedByMemberId": 7,
  "closedByMemberId": null
  // (champs exacts = record SessionResponse existant)
}
```

## Codes d'erreur

| Statut | Condition | Message (ProblemDetails, FR) |
|--------|-----------|------------------------------|
| **200** | Session ouverte **et** 0 présence valide | Session annulée |
| **404** | Session introuvable | « Session introuvable. » |
| **409** | Session **non ouverte** (déjà clôturée/annulée) | « La session n'est pas ouverte : annulation impossible. » |
| **409** | Session ouverte mais **≥ 1 présence valide** | « La session contient des présences et ne peut pas être annulée. » |
| **403** | Droit `manage_attendance` manquant | (refus standard) — tentative **consignée** |

- Les deux cas **409** portent des messages **distincts** (diagnostic client).
- **404** via `NotFoundException`, **409** via `ConflictException`, **403** via la policy — mapping HTTP
  existant réutilisé.

## Sémantique serveur (autorité)

- Vérifie l'état **Ouverte** (garde de domaine `AttendanceSession.Cancel`).
- **Re-vérifie** `CountValidBySessionAsync(sessionId) == 0` **au moment de l'annulation**, dans la même
  transaction que la mise à jour d'état (FR-004/SC-003) — aucune présence ajoutée entre-temps n'est perdue.
- Bascule `Status = Cancelled`, renseigne `CancelledByMemberId` (utilisateur courant) et `CancelledAt`
  (heure serveur), **sans** toucher aux présences (FR-008).
- **Audit** (`IAuditLogger`) : opération d'annulation (session, auteur, horodatage) et tentatives refusées
  pour droit manquant (FR-009).
- **Idempotence pratique** : ré-annuler une session déjà `Cancelled` → **409** (« n'est pas ouverte »),
  aucun effet destructeur (FR-010).

## Miroir client (SPA)

`web/src/app/core/api/attendance-sessions-api.ts` :
```
cancel(sessionId: number): Observable<SessionResponse>
  → POST `${base}/${sessionId}/cancel`, {}
```
Voir `contracts/cancel-session-ui.md`.
