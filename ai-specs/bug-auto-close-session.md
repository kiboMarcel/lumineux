# Analyse — Auto-clôture prématurée des sessions & heure de fin non véridique

**Statut** : cause racine identifiée, correctif appliqué (voir §Correctif).
**Composant** : `SessionAutoCloseService` (clôture automatique de secours, FR-024).
**Symptômes signalés** : une session démarrée se clôture d'elle-même après un délai court ; l'heure de
fin enregistrée en base est fausse.

## Observation (table `attendance_sessions`)

| id | start_time | end_time | status | closed_by | updatedby |
|----|-----------|----------|--------|-----------|-----------|
| 1 | 22:45:17 | **03:00:00** | Closed | NULL | **system** |
| 2 | 22:51:37 | 22:52:12 | Closed | 4 | 4 (manuelle) |
| 3 | 22:55:05 | **03:00:00** | Closed | NULL | **system** |

Les sessions 1 et 3 ont été fermées par le **système** (`closed_by = NULL`, `updatedby = system`) avec
`end_time = 03:00:00` — soit **avant** leur propre `start_time` (22:45). La session 2, fermée
manuellement, est correcte.

## Cause racine

### Bug 1 — Expiration calculée sur `MeetingDate` (minuit) au lieu de `StartTime`

```mermaid
timeline
    title Session démarrée à 22:45 le 2026-07-05
    section Réalité
      00:00 : MeetingDate (date seule → minuit)
      22:45 : StartTime (démarrage réel)
    section Règle d'expiration (buguée)
      16:45 : seuil = now(22:45) - MaxOpenHours(6h)
      "MeetingDate(00:00) < seuil(16:45)" : VRAI dès le départ → session jugée expirée
```

- Le SPA envoie une **date seule** (`yyyy-MM-dd`) → `MeetingDate` est stocké à **minuit**.
- `SessionAutoCloseService` liste les sessions ouvertes via
  `ListOpenBeforeAsync(now - MaxOpenHours)`, dont le filtre est `MeetingDate < seuil`.
- Comme `MeetingDate` = minuit, toute session démarrée après 06:00 est déjà « ouverte depuis plus de
  6h » **au moment même du démarrage**, et se fait clôturer au **tick suivant** (`PollingIntervalSeconds
  = 300` → ≤ 5 min ; d'où « un délai court »).
- L'anchor temporel réel d'une session est **`StartTime`**, pas `MeetingDate` (qui n'est qu'une date).

### Bug 2 — Heure de fin par défaut dérivée de `MeetingDate`

```csharp
var endTime = session.MeetingDate.AddHours(options.DefaultDurationHours); // minuit + 3h = 03:00
```

- `end_time = 00:00 + 3h = 03:00`, **antérieure au `StartTime`** (22:45) → donnée incohérente, propagée
  aux présences valides via `ApplyEndTime`.

## Correctif

1. **Repository** `AttendanceSessionRepository.ListOpenBeforeAsync` : filtrer sur **`StartTime`**
   (durée d'ouverture réelle) au lieu de `MeetingDate`. Paramètre renommé `startedBeforeUtc`.
2. **Service** `SessionAutoCloseService` : heure de fin par défaut basée sur **`StartTime`** et
   **jamais dans le futur** :
   ```csharp
   var endTime = session.StartTime.AddHours(options.DefaultDurationHours);
   if (endTime > now) endTime = now;   // borne : jamais après l'instant de clôture
   ```
   Garantit `StartTime < end_time <= now` (cohérent, véridique, jamais avant le démarrage).

### Règle retenue (décision 2026-07-06)

- **`MaxOpenHours = 3`** : une session se clôture automatiquement **~3 h après son démarrage**
  (compté sur `StartTime`), vérifié toutes les 5 min → fermeture entre 3 h 00 et 3 h 05 après le début.
  Ex. session démarrée à 22:45 → auto-clôture vers **01:45**.
- **Heure de fin = `StartTime + DefaultDurationHours` (3 h)**, bornée à `now` — reflète la durée
  nominale de réunion. Avec `MaxOpenHours = DefaultDurationHours = 3`, l'`end_time` ≈ instant de
  fermeture, cohérent (`StartTime < end_time <= now`).

Alternative écartée pour l'heure de fin : `now` littéral (surestime la durée si le job passe tard).

## Nettoyage des données déjà corrompues (optionnel)

Les sessions 1 et 3 ont un `end_time` faux. Correction manuelle possible (à exécuter côté base) :

```sql
-- Recale l'heure de fin des sessions auto-clôturées de façon incohérente (end < start).
UPDATE attendance_sessions
SET end_time = DATEADD(HOUR, 3, start_time), updatedt = SYSUTCDATETIME(), updatedby = N'fix-bug'
WHERE closed_by IS NULL AND end_time < start_time;
-- Propager aux présences correspondantes si nécessaire (selon règle métier retenue).
```

> Purement correctif et ciblé (`WHERE`), non destructif. À lancer manuellement après validation.
