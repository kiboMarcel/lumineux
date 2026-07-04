# Research — Profils du bureau

**Feature**: 004-bureau-profiles · **Date**: 2026-07-03

Ce document consigne les décisions techniques prises avant la conception détaillée (Phase 1). Aucun
marqueur `NEEDS CLARIFICATION` n'est resté ouvert : les points sensibles ont été arbitrés lors de
`/speckit-clarify` (voir `spec.md § Clarifications`).

## 1. Composition du modèle de droits (profils vs attribution directe)

- **Décision** : Les **profils** deviennent la **source unique de vérité** des droits effectifs.
  L'existante table `member_permissions` (issue de la feature 003 F1, alimentée par le bootstrap)
  est **conservée en lecture** au démarrage pour la migration (création du profil « Amorçage »),
  puis n'est plus lue par la chaîne d'authentification (`IMemberPermissionRepository` refactoré).
- **Rationale** : Simplifie le raisonnement (un seul chemin d'attribution), évite la double gouvernance
  et rend l'audit lisible. La migration idempotente au démarrage garantit qu'aucun droit accordé
  n'est perdu à la mise en production.
- **Alternatives** :
  - *Coexistence permanente (union directs + profils)* — écartée : double surface d'audit, ambiguïté
    de gouvernance, deux endroits à surveiller pour révoquer un droit.
  - *Réinitialisation brutale* — écartée : risquerait de couper l'accès admin si le bootstrap n'est pas
    (re)configuré correctement au redémarrage suivant la migration.

## 2. Cardinalité membre ↔ profil

- **Décision** : Un membre peut détenir **N profils** simultanément. Les droits effectifs sont
  l'**union** des droits (sans doublon).
- **Rationale** : Souplesse de composition (ex. « Gestion des présences » + « Gestion des membres »)
  sans exiger la création d'un profil composite. Alignée avec le modèle claims JWT (multi-valeurs).
- **Alternatives** :
  - *Profil unique par membre* — écartée : oblige à créer un profil pour chaque combinaison, explose
    le catalogue.
  - *Profils N + profil « principal » désigné* — écartée : complexité UX supplémentaire pour un gain
    marginal (l'affichage peut trier par ordre alphabétique/attribution).

## 3. Référentiel des droits fonctionnels

- **Décision** : Le catalogue des droits est **figé côté serveur** (port `IPermissionCatalog`) : il
  contient au moment de la livraison `manage_attendance`, `manage_members` et le nouveau
  `manage_bureau_profiles`. Toute liste soumise en création/modification qui référence un droit
  inconnu est refusée (400).
- **Rationale** : Un droit applicatif implique du code (endpoints protégés, comportement métier) ;
  l'ajout dynamique de droits sans code correspondant serait un anti-pattern.
- **Alternatives** :
  - *Droits libres (chaînes arbitraires)* — écartée : casse la sécurité (des droits fantômes
    inutilisables mais visibles).
  - *Catalogue en base* — écartée pour cette itération : introduit une jointure/entité pour un gain
    nul tant que le référentiel n'évolue qu'avec le code.

## 4. Garde-fou « au moins un administrateur »

- **Décision** : Le garde-fou est appliqué **au niveau des cas d'usage** (Application) au moment de :
  (a) la **révocation** d'une attribution — refus si le compte perdant `manage_bureau_profiles`
  laisserait 0 titulaire actif ;
  (b) la **modification** d'un profil — refus si retirer `manage_bureau_profiles` de la liste des
  droits laisserait 0 titulaire (comptabilise les attributions vivantes du profil) ;
  (c) la **suppression** d'un profil — refus si sa disparition laisserait 0 titulaire.
- **Ordre d'évaluation à la suppression (`DeleteBureauProfileHandler`)** :
  1. Refus **FR-003** (`profile_in_use`, 409) si le profil est encore attribué à ≥ 1 membre.
  2. Sinon, vérification du **garde-fou FR-012c** (`last_administrator`, 409) — pertinente uniquement
     si aucun autre profil actif ne porte `manage_bureau_profiles`. Non attribué + réattribuable
     → suppression autorisée.
  Cet ordre évite un chevauchement conceptuel entre FR-003 et FR-012c et rend le code d'erreur émis
  non ambigu.
- **Rationale** : Éviter un état de verrouillage définitif. La règle est portée par un service unique
  (`IBureauProfileRepository.CountActiveAdministrators(excludeProfileId?, excludeMemberId?)`) exposé
  aux handlers. Un test unitaire de cas d'usage vérifie la couverture des trois portes.
- **Alternatives** :
  - *Garde-fou côté Domain* — écartée : la règle traverse plusieurs agrégats (profils + attributions),
    plus naturelle en cas d'usage.
  - *S'appuyer sur le repli `Auth:Bootstrap:*`* — écartée : fragile (nécessite un redémarrage et une
    configuration correcte pour récupérer d'un verrouillage).

## 5. Effet des changements sur les jetons émis

- **Décision** : Aucune invalidation active des jetons en circulation ; les changements de profils
  s'appliquent à la **prochaine émission** (login/activate). Comportement identique à la
  feature 003 (FR-006).
- **Rationale** : L'API est stateless côté JWT (pas de rafraîchissement, pas de blacklist). Ajouter
  une révocation côté serveur nécessiterait un cache/DB de révocations, hors périmètre.
- **Alternatives** :
  - *Version de droits dans le jeton + vérif serveur* — écartée : ajoute un aller-retour DB par
    requête, contredit le stateless.
  - *Blacklist de jetons* — écartée : lourdeur opérationnelle sans bénéfice tangible pour la taille
    de la communauté.
- **Note d'ergonomie** : les tests d'intégration démontreront le délai « prochaine authentification »
  en émettant un nouveau jeton après attribution ; la doc opérationnelle mentionnera qu'une
  reconnexion peut être nécessaire pour bénéficier immédiatement d'un droit accordé.

## 6. Migration idempotente au démarrage (profil « Amorçage »)

- **Décision** : Un service `BureauProfilesBootstrapper` (Infrastructure) s'exécute au démarrage,
  **après** l'application des migrations EF. Il vérifie l'existence d'un profil nommé « Amorçage »
  (identifiant système ou nom unique réservé). Si absent et si la table `member_permissions`
  contient des lignes, il :
  1. crée le profil « Amorçage » avec l'ensemble des droits présents dans `member_permissions` ;
  2. l'assigne au membre bootstrap (celui référencé par `Auth:Bootstrap:MemberReference`) —
     à défaut, à tous les membres présents dans `member_permissions` ;
  3. journalise l'action (`IAuditLogger`) sans toucher aux lignes `member_permissions` d'origine
     (traçabilité rétroactive) ;
  4. laisse `PermissionBootstrapper` (feature 003) continuer à opérer sur `member_permissions`
     comme **fallback d'urgence** (idempotent) — il n'est plus la source de vérité mais reste utile
     si l'admin des profils disparaît.
- **Rationale** : Zéro coupure de service ; les droits accordés à l'amorçage restent effectifs après
  déploiement, même si le refactor de `IMemberPermissionRepository` a basculé la source de lecture.
- **Alternatives** :
  - *Migration EF avec `Sql()` d'insertion* — écartée : les valeurs (référence membre, droits) sont
    dérivées de la configuration en cours d'exécution, pas connues à la génération de la migration.
  - *Étape manuelle post-deploy* — écartée : sujette à oubli.

## 7. Refactor de `IMemberPermissionRepository`

- **Décision** : Le contrat reste `GetPermissionsAsync(memberId)` : `Task<IReadOnlyCollection<string>>`.
  L'implémentation `MemberPermissionRepository` change : elle **jointe** `member_bureau_profiles →
  bureau_profile_permissions` et renvoie l'ensemble distinct des droits. La table `member_permissions`
  n'est plus lue en dehors de `BureauProfilesBootstrapper`.
- **Rationale** : Le contrat étant stable, `LoginHandler`/`ActivateAccountHandler` et leurs tests
  d'intégration existants continuent de fonctionner sans changement — validation par régression.
- **Alternatives** :
  - *Nouveau port `IEffectivePermissions`* — écartée : dédouble la surface pour un gain nul ; la
    sémantique du port existant est exactement celle recherchée.

## 8. Routes REST

- **Décision** :
  - `POST /api/v1/bureau-profiles` — créer un profil.
  - `GET /api/v1/bureau-profiles` — lister les profils (avec droits + nombre de titulaires).
  - `GET /api/v1/bureau-profiles/{id}` — détail d'un profil (droits + liste des titulaires).
  - `PUT /api/v1/bureau-profiles/{id}` — modifier nom/description/droits.
  - `DELETE /api/v1/bureau-profiles/{id}` — supprimer un profil non attribué (409 sinon).
  - `POST /api/v1/members/{memberId}/bureau-profiles` — attribuer un profil (body : `{ profileId }`).
  - `DELETE /api/v1/members/{memberId}/bureau-profiles/{profileId}` — révoquer une attribution.
  - `GET /api/v1/members/{memberId}/bureau-profiles` — profils d'un membre + droits effectifs.
  - `GET /api/v1/permissions` — référentiel figé des droits fonctionnels (lecture pour toute
    interface d'administration/lecture).
- **Rationale** : Aligné sur les conventions REST existantes (`/members/{id}/…`), pluralisation
  cohérente, verbes HTTP orthogonaux aux opérations métier.
- **Alternatives** :
  - *`PATCH` pour la modification* — non retenu (feature 002/003 utilisent `PUT` complet — cohérence).
  - *`POST /bureau-profiles/{id}/members` inversé* — non retenu (les attributions se lisent
    naturellement depuis la fiche membre côté SPA).

## 9. Sécurité (revue transverse)

- **Autorisation d'écriture** : toutes les mutations exigent `manage_bureau_profiles` (via
  `[Authorize(Policy = "manage_bureau_profiles")]` — nouvelle policy à ajouter dans `Program.cs`).
- **Autorisation de lecture** : `GET /bureau-profiles*` et `GET /members/{id}/bureau-profiles`
  requièrent `manage_bureau_profiles` **OU** `manage_members` (règle appliquée dans le handler pour
  éviter la duplication de policies).
- **Aucune fuite de secret** : DTO sans mot de passe ni hash ; audit sans PII inutile.
- **Anti-verrouillage** : garde-fou triple (§4).
- **Validation stricte** : nom (borné, non vide, unique CI), droits (référentiel figé), assignations
  (membre existant + actif).
- **Cohérence multi-clients** : Angular (SPA) et Flutter (mobile) consomment les mêmes DTO.
- **Code d'erreur « membre inactif » à l'attribution** : `409 Conflict` (code métier
  `member_inactive`), et non `400 Bad Request`. Rationale : la requête est syntaxiquement valide et
  la ressource cible existe, mais elle est dans un état incompatible — cohérent avec les autres
  features (`ConflictException` pour session déjà clôturée, compte déjà activé, contact déjà
  utilisé). `400` reste réservé aux erreurs de validation FluentValidation.

## 10. Tests (couverture attendue)

- **Domain** : invariants `BureauProfile.Create/Rename/UpdatePermissions` (nom, droits reconnus,
  unicité de droit dans la liste). `MemberBureauProfile.Assign` (validation).
- **Application** :
  - `CreateBureauProfileHandler` : succès, nom dupliqué (409), droit inconnu (400), sans droit
    `manage_bureau_profiles` (403).
  - `UpdateBureauProfileHandler` : succès, garde-fou dernier admin (409), nom dupliqué, droit inconnu.
  - `DeleteBureauProfileHandler` : succès (profil non attribué), attribué → 409, garde-fou → 409.
  - `AssignProfileHandler` : succès, idempotence, membre inactif → 400/409, profil inexistant → 404.
  - `RevokeProfileHandler` : succès, dernière révocation admin → 409.
  - `IMemberPermissionRepository` refactoré : union sans doublon (test unitaire de la requête).
  - `BureauProfilesBootstrapper` : création idempotente, aucune duplication en cas de relance.
- **API (intégration)** : matrice des endpoints (200/201/204/400/401/403/404/409).
- **Régression** : les tests d'auth existants (features 001/002/003) doivent rester verts après
  refactor du `MemberPermissionRepository`.

## 11. Décisions non prises (report à des évolutions ultérieures)

- Gestion d'un référentiel de droits **en base** (évolution sans code).
- Interface d'assignation en **masse** (attribuer un profil à N membres en une opération).
- **Blacklist / rafraîchissement** des jetons.
- **Rôles** hiérarchiques (héritage entre profils).
