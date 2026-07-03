# Phase 0 — Research & Décisions techniques

**Feature**: Ajout d'un nouveau membre · **Date**: 2026-07-03

Décisions techniques cohérentes avec la Constitution v1.0.0 et la solution existante (feature 001).
Les 4 clarifications métier (identifiant = référence, unicité contact = refus si actif, statut initial
= actif, identifiants par e-mail + repli bureau) sont déjà tranchées dans la spec. Ci-dessous les
choix techniques restants.

---

## 1. Enrichissement de l'entité `Member` existante

- **Décision** : étendre l'entité `Member` (créée en feature 001 comme projection minimale) avec les
  champs métier : `reference`, `entryDate`, `gender`, `civility`, `birthDate`, `birthPlace`,
  `birthCity`, `mobile`, `email`, `address`, `district`, `nationality`, `introducer`, en conservant
  `antenna` (antenne d'origine) et `status`. Migration d'**ajout de colonnes** (pas de recréation).
- **Rationale** : une seule entité Membre cohérente dans tout le système ; réutilise l'existant sans
  rupture (les présences référencent déjà `Member`).
- **Alternatives** : entité séparée « profil membre » — écartée (fragmente le modèle, complexifie les
  jointures présence). Recréer la table — écartée (perte de données/migrations non additive).
- **Note** : les rattachements (`antenna`, `nationality`/pays, `civility`, `birthPlace`/`birthCity`
  villes, `district`, `introducer`→membre) sont des **FK** validées à la création (FR-005).

## 2. Nouvelle entité `MemberAccount` (compte de connexion)

- **Décision** : entité `MemberAccount` (1–1 avec `Member`) : `memberId` (FK unique), `loginId`
  (= référence membre, unique), `passwordHash`, `mustChangePassword` (bool), `activationState`
  (PendingActivation/Active), audit. Le secret n'est **jamais** stocké en clair ni exposé.
- **Rationale** : sépare l'authentification du profil ; l'état d'activation du compte est distinct du
  statut du membre (décision de clarification).
- **Alternatives** : stocker les champs de compte sur `Member` — écarté (mélange profil/sécurité,
  exposition accrue). Table Identity complète (AspNetUsers) — écarté pour cette itération (surdimensionné ;
  le socle auth complet relève d'une feature dédiée).

## 3. Hachage du mot de passe temporaire (Constitution IV)

- **Décision** : port Domain `IPasswordHasher` (Hash/Verify) ; implémentation `PasswordHasher<T>` de
  `Microsoft.Extensions.Identity.Core` (PBKDF2, itérations et sel gérés par le framework).
- **Rationale** : algorithme éprouvé, maintenu, sans réinventer la cryptographie ; le port garde le
  Domain agnostique.
- **Alternatives** : hachage maison (PBKDF2/BCrypt custom) — écarté (risque d'erreur cryptographique) ;
  BCrypt.Net — viable mais dépendance supplémentaire non nécessaire.
- **Mot de passe temporaire** : généré aléatoirement (entropie suffisante), transmis une seule fois
  via le canal choisi, stocké **uniquement** sous forme hachée ; `mustChangePassword = true`.

## 4. Envoi d'e-mail d'invitation (intégration externe, FR-011)

- **Décision** : port Application/Domain `IEmailSender` (envoi d'invitation). Deux implémentations :
  - `LoggingEmailSender` (dev/tests) : journalise l'intention d'envoi **sans** le mot de passe.
  - `SmtpEmailSender` (prod) : paramètres SMTP fournis par **configuration/secrets** (hôte, port,
    identifiants), jamais en dur.
  - Sélection par configuration (`Email:Provider`).
- **Envoi non bloquant (deferred de la spec)** : la création membre+compte est validée et persistée
  **avant** la tentative d'envoi. En cas d'absence d'e-mail ou d'échec d'envoi, le système **n'annule
  pas** la création : il bascule sur le **repli remise-bureau** (les identifiants initiaux sont
  présentés au bureau dans la réponse de création lorsque l'e-mail n'a pas pu être envoyé) et
  journalise l'échec. Le mot de passe temporaire n'apparaît jamais dans les journaux.
- **Rationale** : robustesse (un service e-mail indisponible ne doit pas empêcher l'inscription) et
  inclusivité (membres sans e-mail), conformément à la clarification.
- **Alternatives** : envoi transactionnel bloquant — écarté (couple la création à un service externe) ;
  file d'attente/outbox — envisageable ultérieurement pour la fiabilité, hors périmètre initial.

## 5. Génération de la référence membre (identifiant unique & de connexion)

- **Décision** : port `IMemberReferenceGenerator` ; format configurable (défaut `LUM-{yyyy}-{seq:00000}`),
  séquence garantissant l'unicité. Contrainte d'**unicité** sur `reference` au niveau base.
- **Rationale** : la référence sert à la fois d'identifiant métier et d'identifiant de connexion
  (clarification Q1) ; l'unicité doit être garantie même en concurrence.
- **Alternatives** : GUID — écarté (peu lisible/communicable) ; e-mail comme identifiant — écarté
  (e-mail optionnel).
- **Concurrence** : génération + insertion dans la transaction de création ; contrainte unique en
  filet de sécurité (retry si collision improbable).

## 6. Détection des doublons & unicité des contacts

- **Décision** :
  - **Homonymes (nom+prénom)** : détection **applicative** avant enregistrement → avertissement ; la
    création n'aboutit que si le bureau **confirme** (indicateur explicite dans la requête). **Pas**
    de contrainte d'unicité stricte `(lastName, firstName)` en base (des homonymes distincts sont
    permis après confirmation).
  - **Contacts (e-mail/mobile)** : **refus** si déjà utilisé par un membre **actif** — index unique
    **filtré** `WHERE ... IS NOT NULL AND status = 'Active'` sur `email` et sur `mobile`, doublé d'un
    contrôle applicatif renvoyant un message clair (409).
- **Rationale** : concilie tolérance aux vrais homonymes (décision métier) et intégrité forte des
  coordonnées (décision métier), avec garantie base + message applicatif.
- **Note** : la contrainte unique historique `(lastName, firstName)` de la documentation d'entités
  n'est **pas** appliquée telle quelle, car elle contredit la règle « avertir + confirmer » retenue.

## 7. Atomicité création membre + compte (FR-006)

- **Décision** : création du `Member` et du `MemberAccount` dans une **seule sauvegarde/transaction**
  (même `DbContext`). En cas d'échec de l'un, rien n'est persisté. L'envoi d'e-mail intervient **après**
  la transaction réussie.
- **Rationale** : évite tout état incohérent (membre sans compte / compte orphelin).
- **Alternatives** : deux transactions séparées — écarté (fenêtre d'incohérence).

## 8. Autorisation — droit `manage_members` (Constitution IV)

- **Décision** : nouvelle policy `manage_members` (claim `permission=manage_members`), distincte de
  `manage_attendance`. Le compte provisionné pour un nouveau membre ne reçoit **aucun** droit de
  gestion (moindre privilège, FR-012).
- **Rationale** : séparation des responsabilités du bureau ; réutilise le mécanisme de policies existant.
- **Alternatives** : réutiliser `manage_attendance` — écarté (mélange des responsabilités).

## 9. Contrats, validation et erreurs (Constitution V)

- **Décision** : endpoints REST `/api/v1/members` (POST créer, GET rechercher/consulter, PUT corriger) ;
  DTO dédiés (le `passwordHash` n'est jamais exposé ; le mot de passe temporaire n'est renvoyé que dans
  le cas de **repli bureau**, une seule fois) ; validation FluentValidation ; erreurs ProblemDetails
  (400/401/403/404/409). OpenAPI aligné.
- **Rationale** : cohérence avec la feature 001 ; contrats stables pour la SPA.
- **Alternatives** : exposer l'entité — écarté (fuite de données/sécurité).

## 10. Stratégie de tests (Constitution III)

- **Décision** : unitaires Domain (invariants Member/MemberAccount, provisionnement, génération de
  mot de passe temporaire) et Application (CreateMember : droit, doublon+confirmation, contact refusé,
  atomicité via ports mockés ; UpdateMember ; SearchMembers) ; intégration Infrastructure (unicités
  filtrées, référence, hachage) et API (parcours création/doublon/refus/recherche via SQLite).
- **Rationale** : couvre les règles métier et de sécurité sans dépendance externe (e-mail mocké/loggé).
- **Alternatives** : tests d'intégration seuls — écarté (lents, ne protègent pas les invariants).
