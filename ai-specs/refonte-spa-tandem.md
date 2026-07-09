# Refonte du SPA console — structure « Tandem / Harris », marque Lumineux

**Date** : 2026-07-09 · **Cible** : `web/` (Angular 20) · **Template source** : `C:\Dev\ng\angular-tandem`
(Tandem Admin, Harris Design System, Angular 17).

## Décisions (validées)

1. **Palette** : **marque Lumineux** (indigo `#3B4FCC` + or/terracotta, cohérent avec les logos ajoutés)
   posée sur la **structure Tandem** (sidebar teal → indigo, cartes/champs/boutons tokenisés). On **ne**
   reprend **pas** le teal Harris.
2. **Polices** : **auto-hébergées** (PT Sans display + Inter body) dans `web/public/fonts/`, via `@font-face`
   local — **aucun** appel Google Fonts au runtime.
3. **Périmètre de cette passe** : **shell** (sidebar + topbar) + **tokens globaux** + **restyle des classes
   `lx-*` partagées**. Les 12 zones fonctionnelles **héritent** du nouveau look sans réécriture. Ajustements
   fins des écrans sur-mesure (table membres, rapports, présences) → passe suivante.

## Existant vs cible

| Aspect | Actuel (`lx-*`) | Cible (Tandem/Harris, marque Lumineux) |
|--------|-----------------|-----------------------------------------|
| Layout | Topbar horizontale + nav en ligne | **Sidebar** (250px, fond indigo foncé) + **topbar** (titre/sous-titre + actions) |
| Couleur | `--lx-primary #2563eb` (bleu générique) | Indigo marque `#3B4FCC` (+ hover/press), neutres Harris, accents or/terracotta |
| Typo | `system-ui` | **PT Sans** (titres) + **Inter** (corps), auto-hébergées |
| Tokens | 8 variables | Échelle Harris : couleurs sémantiques, spacing 1–9, radii, ombres, motion |
| Cartes/champs/boutons | `.lx-card/.lx-field/.lx-btn` | Mêmes classes **restylées** (rayon, ombre, focus ring) ; alias vers tokens |
| Impression (feat. 022) | masque `.lx-topbar`, `form`, `.lx-btn` | **préserver** : masquer sidebar + topbar, garder cartes/tableaux/SVG |

## Stratégie technique (faible churn)

```mermaid
flowchart TD
    A[styles.css : tokens Harris + couleurs Lumineux] --> B[Restyle classes lx-* partagées]
    A --> F[@font-face PT Sans + Inter auto-hébergées]
    B --> C[Les 12 écrans héritent<br/>sans édition]
    A --> D[shell.component.ts<br/>topbar → sidebar + topbar]
    D --> E[Print styles MAJ<br/>masquer sidebar+topbar]
    C --> G[Build prod + vérif]
    D --> G
    E --> G
```

- **Point clé** : les écrans consomment `lx-card`, `lx-field`, `lx-btn`, `lx-alert`, `lx-title`… Restyler
  ces classes = reskin quasi gratuit des 12 zones. On ne touche pas les templates métier dans cette passe.
- **Shell** : reconstruire `shell.component.ts` (sidebar avec logo Lumineux, nav filtrée par droits
  **inchangée**, encart utilisateur + déconnexion ; topbar avec titre de page dérivé de la route). Conserver
  la logique `visibleModules()` / permissions (aucune régression RBAC).
- **Écrans auth** (login/activate/forgot/reset/setup) : hors shell, restent sur `.lx-auth-shell` restylé
  (carte centrée) — le logo lockup déjà ajouté au login reste.
- **Impression** : adapter les sélecteurs print (nouveaux `.side-nav`, `.topbar`) pour ne rien casser au
  PDF des rapports (feature 022).

## Mapping tokens (extrait)

| Rôle | Valeur cible |
|------|--------------|
| `--brand-primary` | `#3B4FCC` (indigo Lumineux) ; hover `#2C3AA0` ; press `#242F86` |
| `--brand-accent` | or/terracotta `#D97A3F` (badges, avatar utilisateur) |
| `--bg-inverse` (sidebar) | indigo très foncé `#1B2050` (dérivé marque, pas teal) |
| `--bg-page` / `--bg-surface` | `#F3F6FA` / `#FFFFFF` |
| `--fg-primary` / `--fg-body` / `--fg-muted` | `#1F2430` / `#4A5662` / `#6B7280` |
| Typo | `--font-display: 'PT Sans'` · `--font-body: 'Inter'` |
| Radii / ombres / motion | repris de Harris (md 8, lg 12, focus ring indigo) |

## Impact par zone fonctionnelle (cette passe)

| Zone | Héritage automatique | Ajustement fin (passe suivante) |
|------|----------------------|----------------------------------|
| login/activate/forgot/reset/setup | ✅ (auth-shell + card + field + btn) | — |
| home | ✅ | encarts « session ouverte » / stats |
| members (list/detail/form) | ✅ (cards, champs) | table dense, filtres, pagination |
| bureau-profiles | ✅ | listes de droits |
| attendance (start/run) | ✅ | QR panel, suivi temps réel |
| reports (dashboard) | ✅ | barres/aires SVG, print PDF |
| antennas (list/form) | ✅ | table |

## Risques & garde-fous

- **RBAC** : ne pas altérer `visibleModules()` ni les gardes → nav filtrée identique.
- **Impression PDF (feat. 022)** : re-tester le masquage ; adapter les sélecteurs au nouveau shell.
- **Tests Vitest** : certains tests ciblent des libellés/roles, pas les classes CSS → risque faible ;
  vérifier ceux du shell (`shell.component.spec.ts`) après refonte du markup.
- **Polices** : téléchargement unique (approuvé) ; fallback `Calibri/Segoe UI/Arial` dans la stack.
- **Responsive** : sidebar rétractable/masquée < 900px (topbar conserve un bouton menu) — sinon la console
  n'est plus utilisable sur tablette.

## Étapes d'implémentation

1. Auto-héberger PT Sans (400/700) + Inter (var ou 400–700) → `web/public/fonts/` + `@font-face`.
2. Réécrire `web/src/styles.css` : tokens Harris structurés, **couleurs Lumineux**, restyle `lx-*`.
3. Reconstruire `shell.component.ts` : sidebar (logo + nav droits + user/logout) + topbar (titre de route).
4. MAJ styles d'impression (masquer sidebar+topbar ; préserver contenu).
5. `ng build` + revue visuelle ; corriger le spec du shell si le markup change les assertions.

## Passe 2 — avancement (finitions écran par écran)

Primitives ajoutées au design system (`styles.css`) : `.lx-page-head`, `.lx-toolbar`,
`.lx-table-wrap`/`.lx-table`, `.lx-pill` (+ success/muted/warn/info/plain), `.lx-row-actions`,
`.lx-btn-sm`, `.lx-empty`, `.lx-tags`.

| Écran | Statut | Détail |
|-------|--------|--------|
| Accueil (`home`) | ✅ | En-tête de page + droits en pastilles (au lieu d'une liste brute) |
| Liste membres | ✅ | En-tête, barre d'outils de recherche, table stylée, statut en pastille |
| Liste antennes | ✅ | En-tête, table stylée, statut en pastille, actions de ligne compactes |
| Liste profils | ✅ | En-tête, table stylée, compteurs droits/titulaires en pastilles |
| Détails membre & profil | ✅ | En-tête `.lx-page-head` (fin du hack `lx-topbar`) |
| Présences (session-run) | ✅ | En-têtes `.lx-page-head` + table `.lx-table` (liste des présents) |
| Rapports (dashboard, time-series) | ✅ | Tableau synthèse `.lx-table` + en-tête du panneau Évolution |
| **Champs mot de passe** | ✅ | Composant réutilisable **`app-password-field`** (toggle afficher/masquer accessible, CVA ReactiveForms) appliqué à login + activation + changement + réinitialisation + installation (9 champs) |
| Formulaire membre | ✅ | Grille **2 colonnes** (`.lx-form-grid`), adresse/référence pleine largeur |
| member-rate & manual-add | ✅ | Recherche en `.lx-toolbar` + résultats en `.lx-list` (fin des styles inline) |
| qr-panel | ✅ | Déjà propre (QR centré), inchangé |
| antenna/profile-form | ✅ | Héritent des classes (3 champs) ; laissés simples |

Primitives finales ajoutées : `.lx-form-grid`, `.lx-list`, `.lx-error` (global).
Build OK · **142 tests Vitest verts** (dont 4 pour `app-password-field`). **Passe 2 complète.**

## Hors périmètre (cette passe)

Restyle détaillé des tableaux/graphes/formulaires métier ; thème sombre ; animations avancées ;
composants Tandem non utilisés (dashboard stats, analytics tabs) — ils serviront de référence plus tard.
