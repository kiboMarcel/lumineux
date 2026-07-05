# Research — Endpoints de données de référence

Petite feature de lecture réutilisant l'existant. Décisions figées avant la conception.

## 1. Accès aux données : port de lecture dédié vs réutilisation

- **Décision** : nouveau port **`IReferenceDataRepository`** (Domain) exposant une méthode de lecture
  par nomenclature (antennes, civilités, villes, districts, pays), implémenté en Infrastructure sur
  `AppDbContext`.
- **Rationale** : respecte la règle de dépendance (Constitution I) — l'Application ne connaît pas EF.
  Même idiome que `IMemberRepository`. Lecture `AsNoTracking` (aucune mutation).
- **Alternatives écartées** : accès direct à `AppDbContext` depuis le handler (violation Onion) ;
  cinq ports séparés (fragmentation inutile pour des lectures homogènes).

## 2. Projection : entités → DTO dans le handler

- **Décision** : le repo renvoie les **entités** actives triées ; le **handler** projette vers des
  **DTO dédiés** (`ReferenceItemResponse`, `CountryResponse`).
- **Rationale** : même pattern que `SearchMembersHandler` (repo → entités → `ToListItem()`), et
  Constitution V (ne jamais exposer les entités). Les pays exposent **libellé de pays** ET **libellé
  de nationalité** distincts.
- **Alternatives écartées** : projeter dans le repo vers des DTO Application (couplerait
  l'Infrastructure aux contrats Application).

## 3. Filtrage et tri

- **Décision** : ne renvoyer que les entrées au **statut actif** (`Status == "Active"`), **triées par
  libellé** (`OrderBy(Label)`), côté base.
- **Rationale** : FR-004 (saisie = valeurs actives), FR-005 (tri stable/prévisible). Filtrage/tri en
  base = efficace et déterministe.
- **Note** : pour les **pays**, le tri se fait sur le **libellé de pays**.
- **Alternatives écartées** : renvoyer tout puis filtrer côté client (fuite d'entrées désactivées,
  contraire à FR-004) ; tri côté client (non déterministe pour la vérification SC-004).

## 4. Autorisation : authentifié sans droit spécifique

- **Décision** : `[Authorize]` sur le contrôleur (tout utilisateur authentifié), **sans** politique de
  droit particulière — même choix que `PermissionsController`.
- **Rationale** : les nomenclatures ne sont pas sensibles ; elles alimentent des formulaires. Exiger
  un droit de gestion compliquerait sans bénéfice (le formulaire membre exige déjà `manage_members`,
  mais d'autres écrans pourront réutiliser ces listes). FR-006 : refus si non authentifié.
- **Alternatives écartées** : anonyme (exposition inutile hors session) ; exiger `manage_members`
  (couplage trop restrictif pour des listes réutilisables).

## 5. Découpage & routage

- **Décision** : un **contrôleur** `ReferenceController` sous `api/v1/reference`, cinq `GET` :
  `antennas`, `civilities`, `cities`, `districts`, `countries`. Un **handler** unique
  `GetReferenceDataHandler` avec une méthode par nomenclature.
- **Rationale** : lectures homogènes → un handler cohérent et testable, un contrôleur clair. US1
  (antennes) livrable indépendamment.
- **Alternatives écartées** : cinq handlers/contrôleurs (verbeux) ; endpoint unique paramétré par type
  (moins explicite pour l'OpenAPI et les clients).

## 6. Pagination

- **Décision** : **pas** de pagination dans cette version — chargement de la liste complète (usage
  liste de sélection).
- **Rationale** : nomenclatures de taille modérée. Une pagination/recherche serveur pourra être
  ajoutée si un volume (villes) le justifie — reporté, hors périmètre (cf. spec Assumptions).
- **Alternatives écartées** : pagination systématique (complexité prématurée).
