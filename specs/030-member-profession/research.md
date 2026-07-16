# Phase 0 — Recherche & décisions : Profession du membre

## Décision 1 — Texte libre borné vs référentiel de professions

- **Décision** : champ **texte libre borné**, calqué sur le champ `Member.Address` existant.
- **Rationale** :
  - Le besoin exprimé (« connaître la profession de chaque membre ») est descriptif, pas
    analytique ; aucune statistique par métier n'est demandée à ce stade (YAGNI).
  - Un référentiel fermé impose une charge de conception et de seed (liste de métiers,
    maintenance, gestion des « autres »), non justifiée par le besoin actuel.
  - La cohérence avec `Address` (texte libre nullable borné) minimise la surface de code et
    le risque de régression.
- **Alternatives considérées** :
  - *Référentiel `professions`* (table + FK) : rejeté maintenant (sur-ingénierie). Réversible
    plus tard sans casser le contrat si le besoin de normalisation/statistiques émerge —
    documenté comme évolution possible.
  - *Enum figé* : rejeté (les professions sont ouvertes et culturellement variables ;
    un enum serait constamment incomplet).

## Décision 2 — Longueur maximale

- **Décision** : **150 caractères**.
- **Rationale** : un intitulé de métier (éventuellement composé, « Chargé de clientèle
  particuliers ») tient largement en 150 caractères ; borne suffisamment basse pour éviter le
  stockage abusif (Principe IV) et alignée sur l'esprit des autres champs texte du membre
  (`Address` = 255, noms = 200). 150 est un compromis lisible.
- **Alternatives** : 255 (comme `Address`) — acceptable mais généreux pour un simple intitulé ;
  100 — un peu court pour les libellés composés. 150 retenu.
- **Application** : bornée à la fois par **FluentValidation** (`MaximumLength(150)`, message clair,
  fait autorité côté serveur) et par la **configuration EF** (`HasMaxLength(150)`, matérialise la
  contrainte en base — Principe II).

## Décision 3 — Normalisation (trim, vide → null)

- **Décision** : normaliser dans les handlers de création et de correction :
  `Profession = string.IsNullOrWhiteSpace(request.Profession) ? null : request.Profession.Trim();`
- **Rationale** : la spec (FR-005/FR-006) exige un stockage nettoyé et une saisie « espaces
  seuls » traitée comme absence. Placer la normalisation dans l'Application garantit qu'elle
  s'applique quel que soit le client (SPA aujourd'hui, mobile demain), conformément au Principe IV.
  Cela améliore même le traitement actuel de `Address` (non normalisé) sans le modifier.
- **Alternative** : normaliser dans le domaine via une méthode dédiée — cohérent mais plus lourd
  que le pattern actuel (setters publics simples) ; réservé si d'autres champs adoptent la même
  règle plus tard.

## Décision 4 — Migration additive & données existantes

- **Décision** : migration EF `MemberProfession` ajoutant une colonne `profession`
  **nullable** (`nvarchar(150) NULL`), sans valeur par défaut ni backfill.
- **Rationale** : additivité stricte (Principe II) — les membres préexistants conservent une
  profession vide (`NULL`), aucune donnée inventée (FR-011, SC-004). Migration déterministe et
  rejouable sur base vierge. Le déploiement (`dotnet ef database update`) reste à appliquer aux
  bases dev/prod comme pour les features précédentes.
- **Vérification portabilité** : `nvarchar` nullable + aucun index → compatible SQL Server et
  SQLite (tests d'intégration Infra).

## Décision 5 — Contrat d'API (rétrocompatibilité)

- **Décision** : ajouter `Profession` comme propriété **optionnelle** aux DTO
  `CreateMemberRequest`, `UpdateMemberRequest`, `MemberResponse`. Pas de nouvelle version d'API.
- **Rationale** : un champ optionnel ajouté est rétrocompatible (Principe V) — les clients
  existants (mobile ne consomme pas ces DTO ; SPA sera mis à jour dans le même lot) ne cassent
  pas. Les entités restent non exposées (mapping via DTO dédiés).

## Points sans recherche nécessaire

- Sécurité : pas d'unicité (pas d'énumération), stockage ORM paramétré, valeur non journalisée.
- Autorisation : inchangée — `manage_members` déjà exigée à la création et à la correction.
- Audit : `_audit.Operation` et intercepteur d'audit déjà en place couvrent le changement.
