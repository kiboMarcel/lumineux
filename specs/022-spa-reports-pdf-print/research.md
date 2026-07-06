# Research — Console web : Export PDF des rapports (impression navigateur)

Extension **minimale** du tableau de bord Rapports (019/021). Décisions figées avant conception.
**Aucune dépendance npm, aucun appel réseau, aucune modification d'API.**

## 1. Export = impression navigateur (`window.print`)

- **Décision** : un bouton **« Exporter en PDF »** appelle `window.print()`. L'utilisateur choisit
  « Enregistrer en PDF » (ou une imprimante) dans le dialogue standard du navigateur.
- **Rationale** : décision PO 2026-07-06 ; **zéro dépendance**, **WYSIWYG** (le PDF reprend exactement
  le rendu — tableaux, barres, courbe SVG), aucune génération serveur ni bibliothèque à maintenir.
- **Alternatives écartées** : génération **serveur** (API + NuGet, licence, rendu de graphiques
  complexe) ; **bibliothèque JS** (jsPDF/html2canvas — dépendance/poids).

## 2. Mise en page imprimable via `@media print` (styles globaux)

- **Décision** : ajouter à `web/src/styles.css` un bloc **`@media print`** qui **masque** la barre de
  navigation du shell, les **boutons** (`.lx-btn`) et les **champs de formulaire** (`input`, `select`),
  et **affiche** les éléments marqués « print-only ». Le contenu des rapports reste visible et lisible.
- **Rationale** : FR-007/FR-008, SC-004 ; les styles globaux atteignent le shell (hors du composant
  tableau de bord) ; un seul point de contrôle de l'impression.
- **Alternatives écartées** : styles d'impression par composant (ne peuvent masquer le shell parent) ;
  page/route d'impression dédiée (duplication du rendu).

## 3. En-tête d'impression (période, antenne, date)

- **Décision** : un **en-tête** ajouté au `reports-dashboard`, **masqué à l'écran** et **visible à
  l'impression** (classe utilitaire type `.lx-print-only`). Il affiche la **période** (début/fin), le
  **libellé de l'antenne** filtrée (ou « Toutes ») et la **date de génération**.
- **Rationale** : FR-003/SC-003 ; contextualise le document exporté.
- **Alternatives écartées** : en-tête toujours visible à l'écran (bruit inutile hors impression).

## 4. Contenu conditionnel (taux membre)

- **Décision** : le **bloc taux membre** figure dans le document **seulement si** un membre est affiché
  (déjà conditionnel dans le tableau de bord 019). Aucune section vide à l'impression.
- **Rationale** : FR-006/SC-005 ; évite un bloc vide.
- **Alternatives écartées** : toujours imprimer le bloc (section vide disgracieuse).

## 5. Sécurité & intégration

- **Décision** : le bouton vit dans le tableau de bord, déjà gardé `permissionGuard('manage_attendance')` ;
  aucune nouvelle route/nav. L'impression est **locale** (aucun appel réseau). L'API reste l'autorité des
  données déjà chargées.
- **Rationale** : FR-001/FR-009, SC-006 ; réutilise la garde existante.
- **Alternatives écartées** : endpoint d'export (inutile pour une impression client).
