# Data Model — Console web : Courbe d'évolution (état client)

Aucune persistance côté SPA. Modèles de vue en mémoire, reflet des DTO de l'API 020. **Le client ne
recalcule aucune statistique** : il met à l'échelle et trace.

```mermaid
flowchart LR
    Dash["reports-dashboard\n(période appliquée + antenne)"] -->|from,to,antennaId| Chart["time-series-chart"]
    Chart -->|granularité (Week/Month)| Chart
    Chart -->|GET /reports/.../time-series| API[(API 020)]
    API -->|points continus| SVG["Courbe / aire SVG (proportionnelle)"]
```

## Modèles consommés (vue client — reflet des DTO API 020)

| Modèle | Champs |
|--------|--------|
| `TimeSeriesGranularity` | `'Week' | 'Month'` |
| `TimeSeriesPoint` | `periodStart`, `label` (`AAAA-Sww` / `AAAA-MM`), `validAttendanceCount`, `sessionCount` |
| `AttendanceTimeSeriesResponse` | `from`, `to`, `granularity`, `points: TimeSeriesPoint[]` (continus) |

## Entrées du panneau (contexte fourni par le tableau de bord)

| Entrée | Source |
|--------|--------|
| `from`, `to` | période **appliquée** du tableau de bord (019) |
| `antennaId?` | filtre d'antenne **appliqué** du tableau de bord (019) |
| `granularity` | **sélecteur** propre au panneau (semaine / mois) |

## Présentation (mise en forme, pas de calcul métier)

| Élément | Dérivation (rendu seul) |
|---------|--------------------------|
| Coordonnée X d'un point | position uniforme sur l'axe (index / (n−1) · largeur) |
| Coordonnée Y d'un point | `hauteur − (validAttendanceCount / max) · hauteur` (`max` = plus grande valeur de la série) |
| Aire | polygone fermé à la ligne de base (y = hauteur) |
| Info-bulle d'un point | `label` + `validAttendanceCount` |

## Erreurs (ProblemDetails)

| Statut | Sens | Traitement UI |
|--------|------|---------------|
| `400` | plage / granularité invalide | message clair (l'API 020 valide) |
| `403` | droit `manage_attendance` manquant | module déjà gardé (403 géré) |
| `401` | session expirée | purge + retour connexion (socle) |

## État de vue (transitoire, non persisté)

- **Granularité** courante (Week/Month).
- **Série** reçue (`points`) + indicateur de chargement + état vide + message d'erreur mappé.
- **Point survolé** (pour l'info-bulle).

## Persistance

**Aucune** (côté SPA). L'API 020 reste la source de vérité.
