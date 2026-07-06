# Implementation Plan: Console web — Tableau de bord des rapports de présence (SPA)

**Branch**: `019-spa-attendance-reports` | **Date**: 2026-07-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/019-spa-attendance-reports/spec.md`

## Summary

Ajouter à la console web **Angular** (`web/`) un module **« Rapports »** : **synthèse par antenne** sur
une période (tableau + **barres CSS/SVG**), **export CSV** (téléchargement authentifié) et **taux
d'assiduité par membre** (via **recherche allégée 015**, affiché en **pourcentage** avec jauge légère).
Réservé au droit **`manage_attendance`**. Consomme l'**API 018** (rapports), le **référentiel des
antennes** (010, filtre) et la **recherche membre** (015). Le **socle 008** fournit session, gardes,
intercepteurs, mapping d'erreurs et notifications. **L'API n'est pas modifiée. Aucune dépendance npm.**

Décisions structurantes (spec) :
- **Visualisation légère** en **CSS/SVG** (barres proportionnelles, jauge de taux), **sans** bibliothèque
  de graphiques.
- **Aucun calcul statistique client** : les chiffres viennent de l'API 018 ; le client met en forme (%
  d'affichage, hauteurs de barres proportionnelles à `Math.max` des valeurs).
- **Export CSV** : téléchargement via **requête authentifiée** (`HttpClient` → `Blob`, jeton porté par
  l'intercepteur) puis enregistrement de fichier navigateur (URL d'objet + ancre).
- **Période commune** aux deux rapports ; **RBAC** : nav + routes gardées `permissionGuard('manage_attendance')`.

## Technical Context

**Language/Version**: TypeScript ; **Angular 20** (app `web/` existante — standalone, signals).

**Primary Dependencies**: socle feature 008 (`SessionStore`, intercepteurs, `permissionGuard`,
`messageForError`, notifications) ; `ReferenceApi` (010, antennes) ; `MemberLookupApi` (015, recherche
membre) ; Angular Router, `HttpClient`, Reactive/Template Forms. Nouveau service `ReportsApi` (consomme
l'API 018). **Aucune dépendance npm nouvelle** (barres/jauges en CSS/SVG maison).

**Storage**: aucune persistance client (état de vue transitoire).

**Testing**: **Vitest** (unitaires : service `ReportsApi` — dont téléchargement `Blob` ; composant
synthèse — tableau, barres proportionnelles, filtre, état vide, plage invalide ; export CSV déclenché ;
composant taux membre — sélection via lookup, pourcentage, 0 %/404) + **Playwright** (e2e : synthèse,
export, taux membre).

**Target Platform**: navigateurs modernes (bureau + tablette), HTTPS.

**Project Type**: Web (extension de l'app `web/`). L'API n'est pas modifiée.

**Performance Goals**: consultations ponctuelles ; rendu immédiat des barres/jauges (calcul de
proportion trivial côté client).

**Constraints**: droit **`manage_attendance`** (garde + masquage) ; **aucun calcul statistique client** ;
**pas de dépendance graphique** ; erreurs mappées ; **français** + responsive ; aucun secret journalisé.

**Scale/Scope**: 3 user stories ; ~1 écran tableau de bord (2 panneaux : synthèse+export, taux membre) +
1 service API + intégration navigation.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

> Principes I/VI appliqués dans leur **esprit** au front (séparation composants/services, aucune règle
> métier ni calcul statistique client, aucun secret journalisé).

| Principe | Gate | Statut |
|----------|------|--------|
| I. Architecture Onion & séparation | Accès API encapsulé (`core/api/reports-api.ts`) ; composants de présentation (`features/reports`) ; gardes transverses. **Aucun calcul statistique client** (présentation seule). | ✅ PASS (esprit) |
| II. Code-First & intégrité BD | **Sans objet** (pas de base côté SPA ; API 018 inchangée). | ✅ N/A |
| III. Tests en premier | Unitaires (service dont Blob, synthèse+barres, export, taux membre) + e2e. Rouge → vert. | ✅ PASS |
| IV. Sécurité par défaut | Garde `manage_attendance` + **autorité serveur** (403 géré) ; 401 → purge/reconnexion ; export via requête authentifiée (jeton non exposé dans une URL). | ✅ PASS |
| V. Contrats d'API explicites | Consomme les **contrats versionnés** de l'API 018 via **modèles typés** ; erreurs mappées. | ✅ PASS |
| VI. Traçabilité & observabilité | Accès/opérations journalisés **côté API** ; côté client, aucun secret loggé. | ✅ PASS (esprit) |

**Résultat initial : PASS** — aucune violation, Complexity Tracking non requis.

*Re-check post-conception (Phase 1)* : la conception (service `core/api`, tableau de bord de présentation,
barres/jauges CSS/SVG, téléchargement Blob authentifié) respecte les principes. **PASS confirmé.**

## Project Structure

### Documentation (this feature)

```text
specs/019-spa-attendance-reports/
├── plan.md · research.md · data-model.md · quickstart.md
├── contracts/
│   ├── api-consumption.md   # endpoints rapports (018) + antennes (010) + lookup (015) consommés
│   └── routes.md            # route du module + garde manage_attendance + nav
└── checklists/requirements.md
```

### Source Code (repository root) — extension de l'app front `web/`

```text
web/src/app/
├── core/api/
│   ├── reports-api.ts               # antennaSummary · antennaSummaryCsv (Blob) · memberRate (API 018)
│   └── (reference-api.ts, member-lookup-api.ts existants réutilisés)
├── features/reports/
│   ├── report.models.ts             # AntennaAttendanceSummary(Item/Response), MemberAttendanceRateResponse
│   ├── reports-dashboard/           # US1/US2 : période + filtre antenne + tableau + barres + export CSV
│   └── member-rate/                 # US3 : sélecteur membre (lookup) + jauge de taux
├── shell/                           # nav « Rapports » : lien réel (garde manage_attendance)
└── app.routes.ts                    # route protégée /reports (permissionGuard)

# API (src/) : INCHANGÉE
```

**Structure Decision**: Extension de l'app Angular existante. L'accès réseau est **encapsulé** dans
`core/api/reports-api.ts` (dont l'export CSV en `Blob`). Le **tableau de bord** (`reports-dashboard`)
porte la **période commune** et le **filtre d'antenne** (via `ReferenceApi`), affiche la **synthèse**
(tableau + **barres CSS/SVG** proportionnelles) et déclenche l'**export CSV**. Le panneau **taux membre**
(`member-rate`) réutilise `MemberLookupApi` (015) pour sélectionner un membre et affiche une **jauge**
de pourcentage. Le client **ne recalcule aucune statistique** : il met en forme les valeurs de l'API.
Navigation et route gardées `permissionGuard('manage_attendance')`.

## Complexity Tracking

> Aucune violation de la Constitution Check — section non applicable.
