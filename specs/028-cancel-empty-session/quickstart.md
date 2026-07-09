# Quickstart — Validation de l'annulation d'une session vide (feature 028)

**Phase 1** · Guide de **validation**, pas d'implémentation. Réfère à `data-model.md` et `contracts/`.

## Prérequis

- API .NET buildable, base SQL Server de dev avec la **migration `CancelSession`** appliquée.
- SPA `web/` buildable ; un membre **bureau** authentifié (droit `manage_attendance`).
- Endpoint existant de démarrage/clôture opérationnel.

## Tests automatisés

```bash
# API (.NET) — depuis la racine
dotnet test               # Domain + Application + Api

# SPA (Angular)
cd web && npm test        # Vitest
```

Attendus (couvrent FR/SC) :

**Domaine** (`AttendanceSessionTests`)
- `Cancel` sur une session **ouverte** → `Status = Cancelled`, `CancelledByMemberId`/`CancelledAt` renseignés.
- `Cancel` sur une session **clôturée** ou **déjà annulée** → `ConflictException`. → FR-005/FR-010.

**Application** (`CancelSessionTests`, doubles de repo)
- Session **introuvable** → `NotFoundException` (404). 
- Session **non ouverte** → `ConflictException` (409). → FR-005.
- Session ouverte **avec ≥ 1 présence valide** (`CountValid` renvoie > 0) → `ConflictException` (409),
  **aucune** présence touchée, session **reste Open**. → FR-002/FR-003/FR-008/SC-002.
- Session ouverte **vide** (`CountValid == 0`) → succès, `Cancel` appelé, `SaveChanges`, **audit** émis. →
  FR-001/FR-006/FR-009/SC-001/SC-005.
- **Concurrence** : `CountValid` = 0 au 1er contrôle mais > 0 à la re-vérification → **refus** (409), pas
  d'annulation. → FR-004/SC-003.
- Présence ajoutée **puis annulée** (`CountValid == 0`) → session **annulable**. → spec Edge Case.

**API** (`SessionEndpointsTests`)
- `POST /{id}/cancel` : **200** (vide) / **404** / **409** (non ouverte, non vide) / **403** (sans droit,
  tentative **consignée**).

**SPA** (`session-run`)
- Bouton **« Annuler la session »** présent **uniquement** quand 0 présent valide ; **absent** dès qu'une
  présence existe. → contrats/cancel-session-ui.md §1.
- Confirmation demandée avant appel ; sur **409**, message serveur affiché sans quitter l'écran ; sur
  **200**, redirection hors du suivi.

## Scénarios manuels (console bureau)

### A — Annuler une session vide (US1, SC-001)
1. Démarrer une session (antenne/date), **ne scanner/ajouter personne**.
2. Sur le suivi, cliquer **« Annuler la session »** → **confirmer**.
3. **Attendu** : la session est annulée (**< 5 s**), on quitte le suivi ; la **reprise « session en cours »**
   ne la propose plus ; elle n'apparaît pas dans les listes/rapports.

### B — Refus si présence (US2, SC-002)
1. Démarrer une session, **ajouter/scanner un membre**.
2. **Attendu** : le bouton **« Annuler la session »** n'est **pas** proposé ; seule la **clôture** l'est.
3. (API) Forcer `POST /{id}/cancel` → **409** « contient des présences » ; la session reste ouverte,
   présence intacte.

### C — Course concurrente (SC-003)
1. Session vide affichée ; un membre **scanne** juste avant l'annulation.
2. Déclencher l'annulation. **Attendu** : **refus 409** (la re-vérification détecte la présence) ; **aucune**
   présence perdue.

### D — Présence annulée puis session annulable (Edge Case)
1. Session avec une présence, **annuler cette présence** (décompte valide → 0).
2. **Attendu** : **« Annuler la session »** redevient disponible ; l'annulation réussit.

### E — Session close / double annulation (FR-005/FR-010)
1. Sur une session **clôturée** (ou déjà annulée), tenter l'annulation via API.
2. **Attendu** : **409** « n'est pas ouverte », aucun effet destructeur.

## Critères de sortie

- [ ] `dotnet test` et `npm test` verts (dont concurrence + refus non-vide).
- [ ] Migration `CancelSession` appliquée et rejouable sur base vierge.
- [ ] Scénarios A→E conformes.
- [ ] Aucune présence perdue/modifiée ; sessions annulées absentes des vues actives ; annulations tracées.
