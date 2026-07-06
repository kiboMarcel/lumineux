# Data Model — Console web : Export PDF des rapports

**Aucune donnée nouvelle, aucune persistance, aucun appel réseau.** L'export imprime l'**état déjà
chargé** du tableau de bord (features 019/021).

```mermaid
flowchart LR
    Dash["reports-dashboard (état affiché)\nsynthèse · courbe · taux membre?"] --> Btn["Bouton « Exporter en PDF »"]
    Btn -->|window.print()| Print["Dialogue d'impression navigateur"]
    Print -->|@media print| Layout["Mise en page : en-tête + rapports\n(nav/boutons/champs masqués)"]
```

## Contexte imprimé (repris de l'état du tableau de bord)

| Élément | Source (déjà en mémoire) | Présence à l'impression |
|---------|--------------------------|-------------------------|
| En-tête : période (début/fin) | champs `from`/`to` (appliqués) | toujours |
| En-tête : filtre d'antenne | `antennaId` → libellé (référentiel) ou « Toutes » | toujours |
| En-tête : date de génération | horloge locale au moment de l'export | toujours |
| Synthèse par antenne (tableau + barres) | `summary()` (feature 019) | toujours (ou état vide) |
| Courbe d'évolution | `time-series-chart` (feature 021) | toujours (ou état vide) |
| Taux d'un membre | `member-rate` (feature 019) | **si** un membre est affiché |

## Éléments masqués à l'impression (`@media print`)

| Élément | Règle |
|---------|-------|
| Barre de navigation du shell | masquée |
| Boutons (`.lx-btn`, granularité, Afficher, Exporter CSV/PDF…) | masqués |
| Champs de formulaire (`input`, `select`) | masqués |
| En-tête d'impression (`.lx-print-only`) | **affiché** (masqué à l'écran) |

## Persistance

**Aucune**. Impression locale du DOM courant ; l'API 018/020 reste la source des chiffres déjà affichés.
