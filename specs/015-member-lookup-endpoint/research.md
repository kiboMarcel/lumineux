# Research — Recherche membre allégée (member lookup)

Petite feature de lecture réutilisant l'existant. Décisions figées avant conception.

## 1. Réutiliser `SearchAsync` plutôt qu'une nouvelle requête

- **Décision** : réutiliser `IMemberRepository.SearchAsync(query, page, pageSize)` (feature 002) avec
  **page = 1** et **pageSize = plafond** (ex. 20) ; le handler **projette** les entités `Member` vers
  un **DTO minimal**.
- **Rationale** : la recherche par référence/nom existe déjà ; éviter une nouvelle méthode de
  persistance (moins de surface, cohérence des résultats). La **projection** garantit l'exposition
  minimale (Constitution V).
- **Alternatives écartées** : nouvelle méthode `LookupAsync` dédiée (redondante) ; requête EF dans le
  handler (violation Onion).

## 2. Contrôle d'accès any-of (`manage_attendance` OU `manage_members`)

- **Décision** : le **handler** vérifie via `ICurrentUser` que la session détient
  `manage_attendance` **ou** `manage_members` ; sinon `ForbiddenException` (→ 403). Le **contrôleur**
  est `[Authorize]` (authentifié). Idiome identique à `ReadAccess` (feature 004).
- **Rationale** : le système de **politiques** ASP.NET ne gère que des droits **uniques** ; le any-of
  est porté au niveau du cas d'usage, comme pour la lecture des profils du bureau. FR-005/SC-004.
- **Alternatives écartées** : ajouter une politique « any-of » globale (surdimensionné) ; exiger
  `manage_members` (contraire au besoin — un opérateur de présence n'en dispose pas).

## 3. Contrôleur dédié (pas d'ajout à `MembersController`)

- **Décision** : nouvel endpoint sur un **contrôleur dédié** `MemberLookupController`
  (`[Authorize]`, route `api/v1/members/lookup`).
- **Rationale** : `MembersController` porte `[Authorize(Policy = manage_members)]` **au niveau de la
  classe** ; y ajouter l'action la soumettrait à `manage_members` (les politiques d'action s'**ajoutent**,
  ne remplacent pas), ce qui exclurait les opérateurs de présence. Un contrôleur distinct évite ce
  couplage. La route `.../lookup` ne collisionne pas avec `.../{memberId:int}` (contrainte entière).
- **Alternatives écartées** : action sur `MembersController` (bloquée par la politique de classe) ;
  route `api/v1/member-lookup` (moins lisible que `.../members/lookup`).

## 4. Terme requis, champs minimaux, plafond

- **Décision** : le handler **rejette** un terme vide/blanc (→ 400) ; **projette** uniquement
  `{ id, reference, fullName, status }` ; **plafonne** la taille (page 1, pageSize = 20).
- **Rationale** : FR-002/003/004 ; SC-002/003/005. Empêche l'aspiration de l'annuaire et limite
  l'exposition de données personnelles aux opérateurs de présence.
- **Alternatives écartées** : autoriser un terme vide (listing complet — refusé) ; exposer plus de
  champs (contraire à l'anti-divulgation).

## 5. Anti-divulgation

- **Décision** : le DTO ne contient **jamais** de coordonnée (e-mail/mobile), d'adresse, de date de
  naissance ni de rattachement — uniquement de quoi **identifier** le membre.
- **Rationale** : SC-002 ; principe du moindre privilège (Constitution IV). Un opérateur de présence
  n'a pas besoin des données personnelles complètes.
