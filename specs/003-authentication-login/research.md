# Phase 0 — Research & Décisions techniques

**Feature**: Authentification et connexion des membres · **Date**: 2026-07-03

Décisions cohérentes avec la Constitution v1.0.0 et la solution existante (features 001/002). Les
3 clarifications métier sont tranchées (jeton d'accès seul, endpoint dédié de 1re connexion,
verrouillage temporaire). Ci-dessous les choix techniques.

---

## 1. Émission du jeton d'accès (JWT)

- **Décision** : port `ITokenIssuer` (Application) ; implémentation `JwtTokenIssuer` (Infrastructure)
  réutilisant les **`JwtOptions` existants** (issuer, audience, clé de signature, durée) et
  `System.IdentityModel.Tokens.Jwt`. Le jeton porte : `member_id`, nom, et un claim `permission` par
  droit du membre. Durée configurable (`AuthOptions:AccessTokenMinutes`, défaut 60).
- **Rationale** : réutilise le socle de validation déjà en place (features 001/002) → cohérence
  signature/validation ; garde le Domaine agnostique.
- **Alternatives** : réutiliser `TestTokenIssuer` en production — écarté (réservé aux tests) ;
  bibliothèque OpenIddict/IdentityServer — surdimensionné pour l'itération.
- **Pas de refresh token** (clarification) : jeton d'accès expirant, reconnexion à l'expiration ;
  aucune persistance de jeton, aucune révocation côté serveur.

## 2. Source des permissions portées par le jeton

- **Décision** : table minimale **`member_permissions`** (`memberId`, `permission`) + port
  `IMemberPermissionRepository.GetPermissionsAsync(memberId)`. À la connexion, les permissions du
  membre sont lues et injectées comme claims.
- **Rationale** : découple la **lecture** des droits (nécessaire au jeton) de leur **attribution**
  (gestion des profils du bureau, hors périmètre). Cohérent avec l'approche « projection minimale »
  des features précédentes.
- **Alternatives** : coder les droits en dur / rôle unique — écarté (non évolutif) ; ACL complète
  (rôles + profils) — écarté (relève d'une feature dédiée). L'**attribution** reste hors périmètre :
  la table est lue ici, alimentée ailleurs (ou amorçage).

## 3. Enrichissement de `MemberAccount` (sécurité de connexion)

- **Décision** : ajouter `FailedAttempts` (int), `LockoutUntil` (datetime nullable), `LastLoginAt`
  (datetime nullable) + méthodes de domaine : `RegisterFailedLogin(now, maxAttempts, lockoutDuration)`,
  `RegisterSuccessfulLogin(now)`, `IsLockedOut(now)`, `ChangePassword(hash)`, `Activate()`.
- **Rationale** : les invariants de sécurité (incrément, seuil, fenêtre de verrouillage, reset)
  appartiennent au Domaine (Constitution I) et sont testables sans base.
- **Alternatives** : logique de verrouillage dans le handler — écarté (fuite de règles hors Domaine).

## 4. Parcours de première connexion (endpoint dédié)

- **Décision** : endpoint `POST /auth/activate` prenant `reference + temporaryPassword + newPassword`
  (sans jeton). Vérifie le mot de passe temporaire, applique la politique, `ChangePassword` + `Activate`
  (PendingActivation→Active, `MustChangePassword`→false), puis délivre un **jeton d'accès** (UX : accès
  immédiat après activation).
- **Rationale** : simple, sans jeton intermédiaire ; la connexion normale reste refusée tant que le
  compte n'est pas activé (FR-007).
- **Alternatives** : jeton restreint « changement requis » — écarté (plus complexe, non nécessaire).
- **Verrouillage** : les échecs de mot de passe temporaire sur `/auth/activate` alimentent le **même**
  compteur de verrouillage que `/auth/login`.

## 5. Anti-force brute — verrouillage temporaire

- **Décision** : après **N** échecs consécutifs (`AuthOptions:MaxFailedAttempts`, défaut 5),
  verrouillage pendant **D** minutes (`AuthOptions:LockoutMinutes`, défaut 15). Toute tentative pendant
  le verrouillage est refusée avec le **message générique** (même en cas de bon mot de passe). Compteur
  remis à zéro au succès ou à l'expiration du verrouillage.
- **Rationale** : protection standard, simple, testable ; valeurs configurables.
- **Alternatives** : limitation de débit par IP — utile contre les attaques distribuées mais reportée
  (suivi IP, proxies) ; verrouillage permanent — écarté (déni de service sur compte tiers).

## 6. Anti-énumération & messages génériques

- **Décision** : un **unique** message/erreur `401` pour « référence inconnue », « mot de passe
  erroné » et « compte verrouillé ». Pour égaliser le temps de réponse, effectuer une **vérification
  de hachage factice** quand la référence est inconnue (comparaison à un hash fictif) afin d'éviter un
  canal temporel. L'obligation de changement de mot de passe n'est signalée **qu'après** vérification
  réussie du mot de passe temporaire.
- **Rationale** : empêche l'énumération des comptes (FR-012, SC-002).
- **Alternatives** : messages distincts — écarté (fuite d'information).

## 7. Politique de mot de passe

- **Décision** : `PasswordPolicy` (config `AuthOptions:PasswordMinLength`, défaut 8 ; au moins une
  lettre et un chiffre) appliquée par des validators FluentValidation à l'activation et au changement.
  Interdire la réutilisation du mot de passe temporaire/actuel comme nouveau mot de passe.
- **Rationale** : exigence de base équilibrée (FR-010) ; extensible.
- **Alternatives** : politique complexe (symboles, historique) — reportée.

## 8. Contrats, erreurs, observabilité (Constitutions V & VI)

- **Décision** : endpoints REST `/api/v1/auth/login|activate|change-password` ; DTO dédiés (aucun
  secret exposé) ; erreurs `ProblemDetails` (401 générique, 403 `password_change_required`, 400
  politique). Journalisation des événements d'auth (succès, échec, verrouillage, activation,
  changement) **sans** mot de passe ni jeton, via le journaliseur d'audit existant.
- **Rationale** : cohérence avec les features 001/002 ; traçabilité de sécurité.
- **Alternatives** : exposer des détails d'erreur — écarté (fuite).

## 9. Stratégie de tests (Constitution III)

- **Décision** : unitaires Domain (`MemberAccount` : incrément/seuil/fenêtre de verrouillage, reset,
  ChangePassword/Activate) et Application (login : succès/échec générique/verrouillage/must-change ;
  activate ; change-password) avec ports mockés (`ITokenIssuer`, repos, `IPasswordHasher`, `IClock`) ;
  intégration API des parcours `/auth/*` (200/401/403, verrouillage effectif, activation).
- **Rationale** : couvre les règles de sécurité sans dépendance externe.
- **Alternatives** : intégration seule — écarté (lente, ne protège pas les invariants).
