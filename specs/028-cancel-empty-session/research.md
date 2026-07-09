# Research — Annulation d'une session de présence vide (feature 028)

**Phase 0** · **Date** : 2026-07-09. Les décisions produit sont verrouillées dans `spec.md › Clarifications`.
Ci-dessous les choix **techniques** de conception. Aucune zone « NEEDS CLARIFICATION » ne subsiste.

## D1 — État terminal « annulée » (soft) + champs d'audit

- **Décision** : ajouter `SessionStatus.Cancelled = 2` et deux colonnes d'audit **nullables** sur
  `attendance_sessions` : `CancelledByMemberId` (int?), `CancelledAt` (DateTime? UTC). Méthode de domaine
  `AttendanceSession.Cancel(int cancelledByMemberId, DateTime nowUtc)`.
- **Rationale** : conserve la trace (Principe VI) sans supprimer physiquement (décision clarifiée) ;
  colonnes dédiées (plutôt que réutiliser `ClosedByMemberId`/`EndTime`) pour une sémantique claire et un
  audit non ambigu. `Status` étant déjà un `int`, la nouvelle valeur ne change pas le type de colonne.
- **Garde de domaine** : `Cancel` MUST refuser si `Status != Open` (lève `ConflictException`), fixe
  `Status = Cancelled`, `CancelledByMemberId`, `CancelledAt`. La règle « vide » n'est **pas** dans le domaine
  (il n'a pas le décompte) — elle est portée par le handler (D2).
- **Alternatives écartées** : suppression physique (perte d'audit, écartée en clarify) ; réutiliser les
  champs de clôture (sémantique confuse).

## D2 — Règle « vide » + re-vérification atomique (SC-003)

- **Décision** : le décompte de présences valides utilise **`IAttendanceRepository.CountValidBySessionAsync`
  (déjà existant)**. `CancelSessionHandler` exécute, **dans une transaction** :
  1. charger la session (`GetByIdAsync`) → `NotFoundException` si absente ;
  2. `Cancel(...)` (garde ouverte) ;
  3. **re-vérifier** `CountValidBySessionAsync == 0` **juste avant le commit** ; si > 0 → **rollback** et
     `ConflictException("La session contient des présences et ne peut pas être annulée.")` ;
  4. `SaveChangesAsync` + audit.
- **Rationale** : la re-vérification **au moment de l'annulation** (et non à l'affichage) empêche d'annuler
  une session devenue non vide par un scan concurrent (FR-004/SC-003). La transaction garantit qu'aucune
  présence n'est perdue.
- **Concurrence (garantie forte)** : l'annulation MUST s'exécuter dans une **transaction sérialisable**
  (`IsolationLevel.Serializable`) : le `CountValidBySessionAsync` y pose un **verrou de plage** sur les
  présences de la session, empêchant un scan concurrent d'insérer une présence valide pendant l'annulation
  (l'un des deux échoue par conflit de sérialisation → réessai/refus). **Alternative équivalente** : un
  **UPDATE conditionnel atomique** `SET Status=Cancelled … WHERE Id=@id AND Status=Open AND NOT EXISTS
  (présence valide)` (0 ligne affectée ⇒ refus, à désambiguïser par relecture). Objectif : rendre
  **impossible** l'état incohérent « session annulée **avec** présence valide » (SC-003).

## D3 — Autorité

- **Décision** : endpoint `[Authorize(Policy = Permissions.ManageAttendance)]` — **tout** détenteur du droit
  peut annuler (décision clarifiée), sans restriction à l'auteur de l'ouverture.
- **Rationale** : cohérent avec la clôture ; évite le blocage si l'auteur est indisponible. `ICurrentUser`
  fournit l'identité pour l'audit et `CancelledByMemberId`.

## D4 — Contrat API (additif)

- **Décision** : `POST /api/v1/attendance-sessions/{sessionId}/cancel` → **200** `SessionResponse`
  (`Status = "Cancelled"`). Erreurs : **404** (introuvable), **409** (non ouverte **ou** non vide), **403**
  (droit manquant). Réutilise `SessionResponse` (le champ `Status` expose déjà l'état).
- **Rationale** : miroir de `POST .../{id}/close` (200 `SessionResponse`) ; additif, aucune rupture → pas de
  versionnage (Principe V). Renvoyer la session permet au client de mettre à jour l'UI immédiatement.
- **Distinction des 409** : messages distincts « déjà clôturée / déjà annulée / n'est pas ouverte » vs
  « contient des présences » pour un diagnostic clair côté client.

## D5 — Exclusion des vues actives (acquise)

- **Décision** : aucune vue à modifier pour exclure les annulées — la reprise (feature 023,
  `ListOpenByOpenerAsync`), la garde d'ouverture (`HasOpenSessionAsync`) et l'auto-clôture
  (`ListOpenBeforeAsync`) filtrent déjà `Status == Open`. Les rapports (018/020) portent sur les présences
  **valides** ; une session annulée n'en ayant aucune, elle n'apparaît dans aucun agrégat.
- **Vérification** : s'assurer qu'aucune requête existante ne liste les sessions « non fermées » d'une façon
  qui inclurait `Cancelled` (revue lors de l'implémentation). `IsOpen => Status == Open` reste correct.

## D6 — Côté SPA (console bureau)

- **Décision** : ajouter `AttendanceSessionsApi.cancel(sessionId)` ; sur `session-run`, afficher un bouton
  **« Annuler la session »** (distinct de « Clôturer ») **uniquement** quand la session est **ouverte** et
  que le **nombre de présents valides affichés = 0** ; demander une **confirmation** explicite ; sur **409**,
  afficher le message serveur (`messageForError`) sans quitter l'écran ; sur succès, rediriger hors du suivi.
- **Rationale** : le masquage côté client est une **commodité UX** ; le **serveur reste l'autorité** (re-check
  au moment de l'annulation). Distinguer clairement « Annuler » (séance vide) de « Clôturer » (séance avec
  présences) et de « Annuler » une **présence** (déjà présent dans la liste, libellé inchangé).

## D7 — Synchronisation hors ligne tardive (lien M2)

- **Décision** : aucun traitement nouveau. Un scan hors ligne synchronisé **après** annulation vise une
  session non ouverte → le `SyncOfflineScansHandler` existant le **rejette** (session close/annulée), comme
  tout scan hors plage. La session annulée n'a donc jamais de présence a posteriori.

## D8 — Migration

- **Décision** : migration EF Core **additive** `CancelSession` : ajoute la valeur d'enum (aucune contrainte
  de colonne à changer) et **2 colonnes nullables** (`CancelledByMemberId`, `CancelledAt`). Rejouable sur base
  vierge ; aucune donnée existante impactée (colonnes nullables, valeur d'enum non utilisée rétroactivement).

---

## Synthèse

| # | Sujet | Décision |
|---|-------|----------|
| D1 | État | `Cancelled=2` (soft) + `CancelledByMemberId`/`CancelledAt` + `AttendanceSession.Cancel` |
| D2 | Règle « vide » | `CountValidBySessionAsync==0` **re-vérifié en transaction** au moment de l'annulation |
| D3 | Autorité | `manage_attendance` (tout détenteur) |
| D4 | API | `POST .../{id}/cancel` → 200 `SessionResponse` ; 404/409/403 ; additif |
| D5 | Vues | Exclusion **acquise** (filtres `Status==Open` existants) |
| D6 | SPA | `cancel()` + bouton conditionnel (0 présent) + confirmation + message 409 |
| D7 | Hors ligne | Rejet natif du scan tardif (aucun changement) |
| D8 | Migration | Additive (enum + 2 colonnes nullables) |

**Aucune** zone « NEEDS CLARIFICATION » restante. Prêt pour la Phase 1.
