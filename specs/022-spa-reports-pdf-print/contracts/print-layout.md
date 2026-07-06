# Contrat — Mise en page imprimable des rapports

**Aucun contrat d'API** (l'export est une **impression locale** ; aucune requête). Ce document décrit le
contrat de **rendu à l'impression** (`@media print`).

## Déclencheur

- Bouton **« Exporter en PDF »** dans le tableau de bord `/reports` (réservé `manage_attendance`, module
  déjà gardé) → appelle **`window.print()`**.

## Contenu attendu du document

| Section | Règle |
|---------|-------|
| **En-tête** | Période (début → fin), **antenne** filtrée (libellé) ou « Toutes », **date de génération**. Visible **uniquement** à l'impression. |
| **Synthèse par antenne** | Tableau (sessions, présences valides, moyenne) + barres comparatives, pour le contexte courant. |
| **Courbe d'évolution** | La courbe (granularité courante) telle qu'affichée. |
| **Taux d'un membre** | Présent **si et seulement si** un membre est affiché. |

## Éléments masqués à l'impression

- Barre de **navigation** (shell).
- **Boutons** (`.lx-btn` : Afficher, Exporter CSV, Exporter PDF, granularité, actions…).
- **Champs de saisie** (`input`, `select`).

## Règles de lisibilité

- Contenu **non tronqué** ; tableaux/graphiques adaptés à la **largeur de page** (`width:100%` déjà en
  place pour les SVG/tableaux).
- Contraste suffisant (couleurs par défaut lisibles à l'impression).
- Répartition multi-pages acceptable ; éviter la coupe illisible d'un tableau dans la mesure du possible.

## Non-fonctionnel

- **Aucune** dépendance, **aucun** appel réseau, **aucune** modification d'API. Français. Aucun secret
  journalisé.
