# Data Model — Console web : Reprendre une session (état client)

Aucune persistance côté SPA. Modèles de vue en mémoire, reflet des DTO de l'API 023 (sessions) et 010
(antennes). **Le client ne recalcule aucune règle métier.**

```mermaid
flowchart LR
    Start["session-start (chargement)"] -->|GET .../mine/open (023)| Open["Mes sessions ouvertes"]
    Ant["GET /reference/antennas (010)"] --> Start
    Open --> Encart["Encart « session en cours » (libellé antenne, date, heure)"]
    Encart -->|Reprendre| Run["/attendance/sessions/:id (session-run)"]
    Start -->|POST /attendance-sessions| Ok["Nouvelle session"]
    Start -->|409 conflit| Match{"Session ouverte pour antenne+date ?"}
    Match -->|oui| Resume["Bouton « Reprendre la session en cours »"]
    Match -->|non| Msg["Message de conflit clair"]
```

## Modèles consommés (vue client — reflet des DTO existants)

### Sessions ouvertes (`/attendance-sessions/mine/open`, feature 023)

| Modèle | Champs |
|--------|--------|
| `SessionResponse` (existant, 014) | `id`, `antennaId`, `meetingDate`, `startTime`, `endTime?`, `status`, `openedByMemberId`, `closedByMemberId?`, `attendanceCount` |
| Réponse | `SessionResponse[]` (0/1/plusieurs — **uniquement** celles de l'utilisateur) |

### Antennes (`/reference/antennas`, feature 010)

| Modèle | Champs |
|--------|--------|
| `ReferenceItem` (existant) | `id`, `code`, `label` (libellé d'antenne) |

## Présentation (mise en forme, pas de calcul métier)

| Élément | Dérivation (rendu seul) |
|---------|--------------------------|
| Libellé d'antenne | `antennas().find(a => a.id === session.antennaId)?.label` (référentiel 010) |
| Date / heure | `meetingDate` (jour) et `startTime` formatés |
| Reprise | navigation vers `/attendance/sessions/{id}` |
| Correspondance conflit | session de `mine/open` où `antennaId = choisi` et `meetingDate(jour) = choisi` |

## État de vue (transitoire, non persisté)

- **Mes sessions ouvertes** : `SessionResponse[]` + indicateur de **chargement** + éventuel message
  d'erreur (non bloquant).
- **Conflit au démarrage** : session de reprise correspondante (si trouvée) **ou** message de conflit.
- **Formulaire de démarrage** : antenne, date, pas de rotation (existant, feature 014).

## Erreurs (ProblemDetails)

| Statut | Sens | Traitement UI |
|--------|------|---------------|
| `409` (démarrage) | session déjà ouverte pour antenne/date | reprise proposée si correspondance, sinon message clair |
| `401` | session expirée | purge + retour connexion (socle) |
| `403` | droit manquant | module déjà gardé |
| autre | échec vérification / démarrage | message mappé (`messageForError`), non bloquant pour le formulaire |

## Persistance

**Aucune** (côté SPA). L'API 023 reste la source de vérité des sessions ouvertes.
