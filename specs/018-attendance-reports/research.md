# Research — API de rapports & statistiques de présence

Décisions techniques figées avant conception. Couche **analytique en lecture seule** sur les présences
(feature 001). Réutilise l'infrastructure existante (Onion, EF Core, RBAC). **Aucune migration.**

## 1. Port d'agrégation dédié (lecture seule)

- **Décision** : un port `IAttendanceReportRepository` expose les **agrégations** :
  `GetAntennaSummaryAsync(from, to, antennaId?)` → lignes (antennaId, label, sessionCount, validCount) ;
  `GetMemberRateDataAsync(memberId, from, to)` → (memberName, originAntennaId?, validCount,
  eligibleSessionCount). L'implémentation Infrastructure utilise EF (`GroupBy`/`Count`, jointures
  `antennas`/`members`).
- **Rationale** : Principe I (accès données encapsulé, testable) ; garde les requêtes SQL hors des
  handlers.
- **Alternatives écartées** : LINQ dans les handlers sur `DbContext` (couplage, non testable en isolé) ;
  vues SQL matérialisées (migration inutile pour le MVP).

## 2. Décomptes : seules les présences valides

- **Décision** : tous les totaux ne comptent que `AttendanceStatus.Valid` ; les `Cancelled` sont
  exclues (filtre `status = Valid` dans les agrégations).
- **Rationale** : FR-003/SC-002 ; une présence annulée n'est pas une présence.
- **Alternatives écartées** : compter toutes les lignes (fausse l'affluence).

## 3. Sessions comptées & moyenne par antenne

- **Décision** : `sessionCount` = nombre de **sessions** dont `meeting_date` ∈ [from, to] pour
  l'antenne (indépendamment du statut de session). `validCount` = présences valides de ces sessions.
  **Moyenne** = `validCount / sessionCount` (0 si aucune session), calculée dans le **handler** (pas en
  SQL) pour maîtriser l'arrondi.
- **Rationale** : FR-001 ; la moyenne « par séance » est un indicateur de pilotage.
- **Alternatives écartées** : ne compter que les sessions clôturées (exclut des séances réellement
  tenues encore ouvertes) — écarté par défaut, ajustable.

## 4. Taux d'un membre : dénominateur = antenne d'origine

- **Décision** (2026-07-06) : `eligibleSessionCount` = nombre de sessions de **l'antenne d'origine du
  membre** (`members.antenna`) sur la période. `validCount` = présences valides du membre sur la
  période. `taux = validCount / eligibleSessionCount` (0 % si dénominateur 0 — pas de division par
  zéro).
- **Rationale** : FR-005/006, SC-003 ; mesure l'assiduité là où le membre est rattaché.
- **Alternatives écartées** : toutes les sessions (dilue) ; antennes fréquentées (plus complexe).

## 5. Validation de la plage de dates

- **Décision** : `ReportPeriodValidator` (FluentValidation) impose `from` et `to` présents, `to ≥ from`,
  et un **plafond de période** (ex. **366 jours**) pour borner le coût des agrégations. Erreur → 400.
- **Rationale** : FR-010/SC-006 ; sécurité/robustesse (anti-abus).
- **Alternatives écartées** : aucune limite (agrégations coûteuses possibles).

## 6. Export CSV sans dépendance

- **Décision** : le CSV est **généré à la main** (en-têtes + une ligne par antenne, échappement des
  champs, séparateur `;` adapté aux tableurs francophones, encodage UTF-8 avec BOM pour Excel). Le
  handler d'export **réutilise** la synthèse (mêmes chiffres). Réponse `text/csv` + `Content-Disposition`
  (nom de fichier).
- **Rationale** : FR-007/SC-004 ; évite une dépendance npm/NuGet pour un format trivial ; cohérence
  garantie avec la synthèse JSON.
- **Alternatives écartées** : bibliothèque CSV (dépendance superflue) ; génération SQL (illisible).

## 7. Découpage des cas d'usage & endpoints

- **Décision** : handlers `GetAntennaAttendanceSummaryHandler` (US1), `GetMemberAttendanceRateHandler`
  (US2), `ExportAntennaAttendanceCsvHandler` (US3, s'appuie sur US1). Endpoints REST sous
  `/api/v1/reports/attendance/...`, gardés `manage_attendance`. DTO dédiés (pas d'entités exposées).
- **Rationale** : Principe I/V ; un rapport = un cas d'usage testable.
- **Alternatives écartées** : un endpoint générique paramétrable (contrat flou).
