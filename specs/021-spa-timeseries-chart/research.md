# Research — Console web : Courbe d'évolution des présences (SPA)

Extension du tableau de bord Rapports (019) consommant l'API série temporelle (020). Décisions figées
avant conception. **Aucune dépendance npm.**

## 1. Extension du service (`ReportsApi.timeSeries`)

- **Décision** : ajouter `timeSeries(from, to, granularity, antennaId?)` à `core/api/reports-api.ts`
  (miroir de `GET /reports/attendance/time-series`, 020). Aucun appel HTTP hors du service (Principe I).
- **Rationale** : cohérent avec les autres méthodes de `ReportsApi` ; testable isolément.
- **Alternatives écartées** : appel dans le composant (couplage, non testable).

## 2. Tracé SVG maison (courbe/aire)

- **Décision** : un composant `time-series-chart` calcule les coordonnées d'une **polyline SVG** : x =
  position de l'intervalle (répartie uniformément), y = `hauteur − (valeur / max) · hauteur`. Option
  **aire** (polygone fermé à la ligne de base). **Points** (cercles) porteurs d'une **info-bulle** (via
  `<title>` SVG ou état de survol) pour lire la valeur. Libellés d'intervalle en repères sous l'axe.
- **Rationale** : décision PO 2026-07-06 ; suffisant et **zéro dépendance** (pas d'appel réseau), poids
  minimal, cohérent avec les barres CSS/SVG de la synthèse (019).
- **Alternatives écartées** : bibliothèque de graphiques (dépendance/poids superflus) ; barres (choix
  « courbe » retenu).

## 3. Aucun calcul statistique côté client

- **Décision** : les points (présences valides par intervalle) proviennent **intégralement** de l'API
  020 ; le client se limite à la **mise à l'échelle** géométrique (coordonnées) et au **tracé**. Le
  `max` pour l'échelle est le maximum des valeurs de la série reçue (pur rendu).
- **Rationale** : FR-008/Principe I ; évite toute divergence avec l'API (source de vérité).
- **Alternatives écartées** : agrégations/recomputations client (risque d'incohérence).

## 4. Série continue & états

- **Décision** : la série de l'API étant **continue** (zéros inclus), la courbe **redescend à 0** sur les
  intervalles vides — aucun traitement client de « trous ». États **chargement**, **vide** (série sans
  point / période sans donnée) et **erreur** mappée (`messageForError`) gérés.
- **Rationale** : FR-005/FR-009, SC-003 ; fidélité à la série 020.
- **Alternatives écartées** : masquer les zéros (romprait la continuité).

## 5. Réactivité au contexte (période / antenne / granularité)

- **Décision** : le panneau reçoit du tableau de bord la **période appliquée** et le **filtre d'antenne**
  (contexte **validé**, pas la saisie en cours) ; il porte son **sélecteur de granularité**. Un **effet**
  déclenche un nouvel appel `timeSeries` à chaque changement de ces entrées/du signal de granularité.
- **Rationale** : FR-006/FR-007, SC-004 ; met à jour sans recharger, sans requête à chaque frappe (on
  s'appuie sur le contexte **appliqué** du tableau de bord).
- **Alternatives écartées** : appel à chaque frappe de date (bruyant) ; sélection de dates propre au
  panneau (double saisie, incohérent avec 019).

## 6. Intégration & sécurité

- **Décision** : le panneau est **intégré** au `reports-dashboard` (aucune nouvelle route) ; le module
  Rapports est déjà gardé `permissionGuard('manage_attendance')`. L'API reste l'autorité (403 géré) ;
  401 → purge/reconnexion (socle).
- **Rationale** : FR-001/SC-005 ; réutilise la garde existante, pas de duplication.
- **Alternatives écartées** : route dédiée `/reports/evolution` (superflu ; le panneau vit dans le
  tableau de bord).
