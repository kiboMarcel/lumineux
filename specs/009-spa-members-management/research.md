# Research — Console web : Gestion des membres (Lot 2)

Extension de l'app Angular (feature 008) consommant les API membres (002) et référentiels (010).
Décisions figées avant conception.

## 1. Distinction homonymie vs conflit de contact (codes 409)

- **Décision** : à la **création**, interpréter le `409` selon l'extension **`code`** du ProblemDetails :
  - `code = "duplicate_name"` → **homonymie confirmable** : afficher un avertissement (des homonymes
    existent — `duplicateMemberIds` fournis), proposer **Confirmer / Annuler** ; la confirmation
    relance `POST /members` avec **`ConfirmDuplicate = true`**.
  - `code = "contact_in_use"` → **erreur bloquante** non confirmable (contact déjà utilisé par un
    membre actif).
- **Rationale** : reflète exactement le comportement de `CreateMemberHandler` (les deux cas sont des
  `DuplicateMemberException` à 409, distingués par `code`). FR-007/FR-008, SC-003/SC-004.
- **Alternatives écartées** : se fier au message texte (fragile) ; traiter tout 409 comme bloquant
  (empêcherait l'homonymie légitime).

## 2. Homonymie à la correction : hors sujet côté API

- **Décision** : le formulaire d'**édition** (`PUT /members/{id}`) **ne gère pas** de confirmation
  d'homonyme ; il traite uniquement le **conflit de contact** (`409 contact_in_use`).
- **Rationale** : `UpdateMemberRequest` **n'a pas** de champ `ConfirmDuplicate` et `UpdateMemberHandler`
  ne lève que `contact_in_use`. Introduire une confirmation d'homonyme à l'édition serait sans effet
  côté serveur. (Écart mineur avec FR-012 de la spec, tranché en faveur du comportement réel de l'API.)
- **Alternatives écartées** : simuler une confirmation côté client (trompeur, aucun effet serveur).

## 3. Remise des identifiants (mot de passe temporaire) — affichage unique

- **Décision** : à la création réussie (`201`), lire `CredentialsDelivery` :
  - `EmailSent` → message « invitation envoyée par e-mail » (aucun secret affiché).
  - `BureauHandout` → panneau affichant **`LoginId`** et **`TemporaryPassword`** **une seule fois**,
    conservés uniquement en **état de vue** (signal), **jamais** persistés/journalisés ; disparaissent
    à la navigation ou au rafraîchissement.
- **Rationale** : FR-009/FR-010, SC-005. Cohérent avec la règle « aucun secret persisté » du socle.
- **Alternatives écartées** : stocker le mot de passe temporaire (localStorage/état durable) —
  interdit ; ré-affichage à la demande — contraire à l'usage unique.

## 4. Champs à clé étrangère via référentiels (feature 010)

- **Décision** : peupler les listes de sélection depuis `GET /reference/*` (antennes, civilités,
  villes, districts, pays). L'**antenne d'origine** (requise) est un **choix dans la liste** ; si la
  liste des antennes est **vide**, la création est **empêchée** avec un message explicite (FR-017).
- **Rationale** : décision PO (option a). Évite la saisie d'identifiants et les valeurs invalides.
- **Alternatives écartées** : saisie manuelle d'identifiants (écartée en spec) ; charger les
  référentiels au démarrage global (chargement au niveau du formulaire suffit).

## 5. Formulaire partagé création/édition

- **Décision** : un **composant de formulaire** unique piloté par un mode (création | édition) : en
  édition, préchargement de la fiche et **référence en lecture seule** ; en création, gestion de la
  confirmation d'homonyme.
- **Rationale** : mêmes champs (hors référence) ; réduit la duplication (DRY) tout en respectant les
  différences de comportement (homonymie = création).
- **Alternatives écartées** : deux composants distincts (duplication des champs et de la validation).

## 6. Recherche & pagination

- **Décision** : recherche par terme (`query`) + **pagination** (`page`, `pageSize`, `total`) via
  `GET /members`. Taille de page par défaut raisonnable (ex. 20) ; navigation page à page ; état
  « aucun résultat » explicite.
- **Rationale** : reflète le contrat `MemberListResponse` ; SC-001 (retrouver un membre rapidement).
- **Alternatives écartées** : chargement complet non paginé (non scalable).

## 7. Mapping d'erreurs & RBAC

- **Décision** : réutiliser `messageForError` (feature 008) pour 400/404/5xx, avec **traitement
  spécifique des 409** par `code` (§1). Garde `permissionGuard('manage_members')` + masquage de
  l'entrée de navigation ; l'API reste l'autorité (403 → message « accès refusé »).
- **Rationale** : cohérence avec le socle (SC-006/SC-007).
- **Alternatives écartées** : nouveau mécanisme d'erreurs (redondant avec le socle).
