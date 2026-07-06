# Data Model — Console web : Tableau de bord des rapports (état client)

Aucune persistance côté SPA. Modèles de vue en mémoire, reflet des DTO de l'API 018 (rapports), 010
(antennes) et 015 (recherche membre). **Le client ne recalcule aucune statistique.**

```mermaid
flowchart LR
    Ant["GET /reference/antennas (010)"] --> Dash["reports-dashboard (filtre antenne)"]
    Dash -->|GET /reports/.../antenna-summary| Sum["Tableau + barres CSS/SVG"]
    Dash -->|GET /reports/.../antenna-summary.csv (Blob)| Csv["Téléchargement CSV"]
    Look["GET /members/lookup (015)"] --> MR["member-rate (sélecteur membre)"]
    MR -->|GET /reports/.../member-rate| Gauge["Jauge de pourcentage"]
```

## Modèles consommés (vue client — reflet des DTO API)

### Rapports (`/api/v1/reports/attendance`, feature 018)

| Modèle | Champs |
|--------|--------|
| `AntennaAttendanceSummaryItem` | `antennaId`, `antennaLabel`, `sessionCount`, `validAttendanceCount`, `averageValidPerSession` |
| `AntennaAttendanceSummaryResponse` | `from`, `to`, `items: AntennaAttendanceSummaryItem[]` |
| `MemberAttendanceRateResponse` | `memberId`, `memberFullName`, `from`, `to`, `validAttendanceCount`, `eligibleSessionCount`, `rate` (0..1) |

### Référentiel antennes (010) & recherche membre (015)

| Modèle | Champs |
|--------|--------|
| `ReferenceItem` | `id`, `code`, `label` (filtre d'antenne) |
| `MemberLookupItem` | `id`, `reference`, `fullName`, `status` (sélecteur de membre) |

## Présentation (mise en forme, pas de calcul métier)

| Élément | Dérivation (rendu seul) |
|---------|--------------------------|
| Barre d'antenne | hauteur/largeur = `validAttendanceCount / max(items.validAttendanceCount)` |
| Taux affiché | `Math.round(rate * 100)` % ; jauge = `rate` (0..1) |
| Moyenne | `averageValidPerSession` affichée telle quelle (fournie par l'API) |

## Erreurs (ProblemDetails + `code`)

| Statut | Sens | Traitement UI |
|--------|------|---------------|
| `400` | plage invalide | message clair (validation locale + serveur) |
| `404` | membre introuvable (taux) | message « membre introuvable » |
| `403` | droit `manage_attendance` manquant | « action non autorisée » (API autorité) |
| `401` | session expirée | purge + retour connexion (socle) |

## État de vue (transitoire, non persisté)

- **Période** : `from` / `to` (communs) + validité locale (fin ≥ début).
- **Synthèse** : `items` + antenne filtrée + indicateur de chargement + état vide.
- **Taux membre** : terme de recherche + résultats de lookup + membre sélectionné + réponse de taux.
- **Export** : déclenchement du téléchargement `Blob` (aucun état persistant).

## Persistance

**Aucune** (côté SPA). L'API 018 reste la source de vérité.
