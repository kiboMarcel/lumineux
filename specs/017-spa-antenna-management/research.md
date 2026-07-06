# Research — Console web : Gestion des antennes (SPA)

Extension de l'app Angular (feature 008) consommant l'API de gestion des antennes (016) et le
référentiel des districts (010). Décisions figées avant conception.

## 1. Service d'accès encapsulé (`AntennasApi`)

- **Décision** : un service `core/api/antennas-api.ts` expose `list()`, `get(id)`, `create()`,
  `update(id)`, `deactivate(id)`, `activate(id)` — miroir des endpoints de l'API 016. Aucun appel HTTP
  hors de ce service (Principe I).
- **Rationale** : cohérent avec `MembersApi`, `ReferenceApi`, etc. ; testable isolément.
- **Alternatives écartées** : appels HTTP dispersés dans les composants (couplage, non testable).

## 2. Liste de gestion (inactives incluses)

- **Décision** : utiliser `GET /api/v1/antennas` (016) qui renvoie **toutes** les antennes (actives +
  inactives) avec statut — distinct de la lecture publique `GET /reference/antennas` (010, actives
  seules) réservée aux sélecteurs.
- **Rationale** : FR-002/SC-002 ; la gestion doit voir/réactiver les inactives.
- **Alternatives écartées** : réutiliser la lecture publique 010 (n'expose pas les inactives ni le
  statut détaillé).

## 3. Formulaire partagé création/édition, code immuable

- **Décision** : un composant `antenna-form` sert **création** et **édition** (paramètre de route
  `:id`). Le **code** est **éditable en création** et **lecture seule en édition** (l'API ignore tout
  changement de code). Le **district** provient d'un sélecteur alimenté par `ReferenceApi.districts()`.
- **Rationale** : FR-005/008/009, SC-005 ; réduit la duplication (comme `member-form`).
- **Alternatives écartées** : deux composants séparés (duplication) ; code éditable en édition (rejeté
  par l'API, UX trompeuse).

## 4. Mapping des erreurs métier

- **Décision** : s'appuyer sur `messageForError` (socle) pour 400/404/403/401. Pour les 409 avec `code`,
  afficher un message spécifique : **`duplicate_code`** → « ce code d'antenne est déjà utilisé » ;
  **`antenna_has_open_sessions`** → « impossible de désactiver : une session est encore ouverte ».
- **Rationale** : FR-006/011/013 ; messages exploitables sans détail technique.
- **Alternatives écartées** : afficher le `detail` brut de l'API (moins maîtrisé) — on privilégie des
  libellés dédiés pour les deux codes métier connus.

## 5. Désactivation confirmée & rafraîchissement

- **Décision** : la **désactivation** demande une **confirmation** ; à la confirmation, appel
  `deactivate(id)` puis **rafraîchissement** de la liste. La **réactivation** (`activate(id)`) n'exige
  pas de confirmation. Un refus `antenna_has_open_sessions` laisse l'antenne active et affiche le
  message.
- **Rationale** : FR-010/011/012, SC-004/SC-007 ; cohérent avec les confirmations du module Présences.
- **Alternatives écartées** : suppression (inexistante côté API) ; désactivation sans confirmation
  (risque).

## 6. RBAC d'affichage & navigation

- **Décision** : entrée de nav **« Antennes »** (lien réel) visible seulement si
  `manage_referentials` ; routes `/antennas*` gardées `permissionGuard('manage_referentials')`. L'API
  reste l'autorité (403 géré) ; 401 → purge/reconnexion (socle).
- **Rationale** : FR-001/SC-006 ; réutilise le mécanisme `visibleModules`/`canSee()` du shell et la
  garde existante.
- **Alternatives écartées** : regroupement « Référentiels » (prématuré — une seule nomenclature gérée).
