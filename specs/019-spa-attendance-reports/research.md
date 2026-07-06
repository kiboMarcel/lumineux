# Research — Console web : Tableau de bord des rapports (SPA)

Extension de l'app Angular (feature 008) consommant l'API de rapports (018), le référentiel des antennes
(010) et la recherche membre allégée (015). Décisions figées avant conception.

## 1. Service d'accès encapsulé (`ReportsApi`)

- **Décision** : un service `core/api/reports-api.ts` expose `antennaSummary(from, to, antennaId?)`,
  `antennaSummaryCsv(from, to, antennaId?)` (réponse **`Blob`**) et `memberRate(memberId, from, to)` —
  miroir des endpoints 018. Aucun appel HTTP hors de ce service (Principe I).
- **Rationale** : cohérent avec les autres services `core/api` ; testable isolément.
- **Alternatives écartées** : appels dispersés dans les composants (couplage, non testable).

## 2. Export CSV : téléchargement authentifié

- **Décision** : l'export passe par `HttpClient.get(..., { responseType: 'blob' })` — l'**intercepteur**
  ajoute le jeton Bearer — puis le composant crée une **URL d'objet** et déclenche le téléchargement
  (ancre `download`), avant de **révoquer** l'URL. Nom de fichier reflétant la période.
- **Rationale** : FR-006 ; un simple `<a href>` vers l'endpoint **ne porterait pas** l'en-tête
  `Authorization` (jeton en mémoire, pas de cookie) → 401. Le Blob authentifié garantit l'accès.
- **Alternatives écartées** : lien direct (échoue en 401) ; jeton en query string (fuite du secret).

## 3. Visualisation légère (CSS/SVG, sans dépendance)

- **Décision** : barres de comparaison par antenne et jauge de taux en **CSS/SVG maison** : hauteur/
  largeur **proportionnelle** à la valeur (`valeur / max`). Aucune bibliothèque de graphiques.
- **Rationale** : décision PO 2026-07-06 ; suffisant pour synthèse + taux ; **zéro npm install** (pas
  d'appel réseau), poids minimal, cohérent avec le socle.
- **Alternatives écartées** : lib de graphiques (ngx-charts/Chart.js) — dépendance/poids superflus pour
  le MVP.

## 4. Aucun calcul statistique côté client

- **Décision** : les chiffres (sessions, présences valides, moyenne, taux) proviennent **intégralement**
  de l'API 018. Le client se limite à la **présentation** : conversion du taux (0..1) en **pourcentage**
  d'affichage et calcul des **proportions de barres** (pur rendu).
- **Rationale** : FR-010/Principe I ; évite toute divergence avec l'API (source de vérité).
- **Alternatives écartées** : recomputations client (risque d'incohérence).

## 5. Période commune & rapports

- **Décision** : une **plage de dates** (début/fin) commune, portée par le tableau de bord. La synthèse
  se met à jour à la validation ; le panneau **taux membre** reçoit la même période et se déclenche sur
  sélection d'un membre. Validation locale minimale (fin ≥ début) **en plus** de la validation serveur
  (l'API 018 reste l'autorité, 400 mappé).
- **Rationale** : FR-002/US1/US3 ; UX cohérente, un seul contexte temporel.
- **Alternatives écartées** : périodes distinctes par rapport (confus).

## 6. Sélecteur de membre via recherche allégée (015)

- **Décision** : le panneau taux réutilise `MemberLookupApi.lookup(query)` (015) pour choisir un membre
  (champs minimaux : référence, nom) ; tant qu'aucun membre n'est choisi, **aucun appel** de taux.
- **Rationale** : FR-007/FR-008 ; réutilise l'existant (déjà employé par l'ajout manuel de présence).
- **Alternatives écartées** : recherche complète (droit `manage_members`, inutile ici) ; saisie d'un id
  numérique (UX pauvre).

## 7. RBAC d'affichage & navigation

- **Décision** : entrée de nav **« Rapports »** (lien réel) visible seulement si `manage_attendance` ;
  route `/reports` gardée `permissionGuard('manage_attendance')`. L'API reste l'autorité (403 géré) ;
  401 → purge/reconnexion (socle).
- **Rationale** : FR-001/SC-005 ; réutilise `visibleModules`/`canSee()` et la garde existante.
- **Alternatives écartées** : accès non gardé (l'API refuserait, mais mauvaise UX).
