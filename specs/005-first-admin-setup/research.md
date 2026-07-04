# Research — Installation du premier administrateur

**Feature**: 005-first-admin-setup · **Date**: 2026-07-03

Ce document consigne les décisions techniques prises avant la conception détaillée (Phase 1). Aucun
marqueur `NEEDS CLARIFICATION` n'est resté ouvert : les points sensibles ont été arbitrés lors du
cadrage préalable (spec — Assumptions & Clarifications intégrées).

## 1. Détection de l'état « aucun administrateur »

- **Décision** : Réutiliser `IBureauProfileRepository.CountActiveAdministratorsAsync()` (feature 004).
  Si le résultat est `0`, la route est autorisée ; sinon → `409 already_installed`.
- **Rationale** : Méthode déjà éprouvée par les tests de garde-fou (feature 004). Comptabilise les
  membres **actifs** disposant de `manage_bureau_profiles` par attribution de profils — cohérent
  avec la source de vérité (feature 004 FR-006).
- **Alternatives** :
  - *Compter directement les lignes de `member_bureau_profiles` avec le droit admin* — écarté : ré-implémenterait la logique existante.
  - *Ajouter un flag `Setup:Enabled=false` en configuration* — écarté (cadrage utilisateur : le contrôle « 0 admin » suffit).

## 2. Refus prioritaire (anti-fuite d'information)

- **Décision** : Dans le handler, vérifier `CountActiveAdministratorsAsync() == 0` **avant** la
  validation FluentValidation du payload. Si un admin existe → `409 already_installed`,
  quel que soit le contenu du corps.
- **Rationale** : FR-005 exige que le refus prioritaire ne divulgue rien sur la structure attendue
  du payload (empêche un attaquant d'énumérer les champs requis via des messages 400).
- **Alternatives** :
  - *Valider d'abord, refuser ensuite* — écarté : permet à un tiers de sonder la structure du
    payload en observant les 400.
  - *Retourner un 404 générique* — écarté : masque un mécanisme légitime aux opérateurs.

## 3. Atomicité de l'installation

- **Décision** : Toutes les créations (Member, MemberAccount, BureauProfile si absent, MemberBureauProfile)
  passent par les repositories existants **sans appel intermédiaire à `SaveChangesAsync`**, puis un
  **seul** `SaveChangesAsync` final. Les navigations EF garantissent la propagation des clés étrangères
  auto-générées.
- **Rationale** : Une seule transaction implicite → tout-ou-rien (SC-003). Aucune ligne partielle ne
  peut subsister en base en cas d'erreur pendant la séquence.
- **Alternatives** :
  - *`BeginTransactionAsync` explicite* — non nécessaire (une `SaveChangesAsync` unique est déjà
    atomique côté EF/SQL Server).
  - *Sagas / événements* — surdimensionné.

## 4. Idempotence sur le profil « Administrateur »

- **Décision** : Le handler cherche d'abord un profil par nom normalisé (`GetByNameNormalizedAsync`).
  Si présent → réutilisation directe (aucune modification des droits ni de la description).
  Si absent → création avec l'union des `PermissionCatalog.All()` codes.
- **Rationale** : Couvre les scénarios de reprise (feature 004 `BureauProfilesBootstrapper` a pu
  créer un profil, ou un ancien admin archivé a laissé le profil orphelin). Aucun risque d'écraser
  une configuration humaine.
- **Alternatives** :
  - *Vérifier et compléter les droits du profil existant* — écarté : le respect de l'existant
    prime (FR-013).

## 5. Génération de la référence membre

- **Décision** : Réutiliser `IMemberReferenceGenerator` (feature 002, format `LUM-{yyyy}-{seq:00000}`).
- **Rationale** : Cohérence avec les autres membres. Le premier admin n'est pas un citoyen de
  seconde zone — il obtient une référence standard.
- **Alternatives** :
  - *Référence fixe `LUM-ADMIN-0001`* — écarté : casse le format ; peut collisionner en cas de reprise.
  - *Payload fourni par l'opérateur* — écarté : friction inutile (cadrage : payload minimal).

## 6. Contrat du payload et validation

- **Décision** : DTO `InstallFirstAdminRequest(LastName, FirstName, Gender, Password, Email?, Mobile?)`.
  Validator FluentValidation : `LastName`/`FirstName` non vides, `Gender` ∈ {"M","F"},
  `Password` via `PasswordRules.ApplyPolicy` (feature 003), email/mobile facultatifs avec bornes.
- **Rationale** : Payload minimal (cadrage). Politique de mot de passe partagée (FR-003). Genre
  cohérent avec le domaine existant (feature 002).
- **Alternatives** :
  - *Payload complet `CreateMemberRequest` + password* — écarté (cadrage : minimal).
  - *Contact obligatoire* — écarté (edge case : installation ultra-minimale possible).

## 7. Émission du jeton

- **Décision** : Réutiliser `ITokenIssuer.Issue(memberId, fullName, permissions)` avec les droits
  effectifs calculés via `IMemberPermissionRepository.GetPermissionsAsync(memberId)` (feature 004,
  contrat inchangé — l'implémentation lit désormais l'union via profils).
- **Rationale** : Zéro duplication de logique. Le jeton retourné est identique à celui qu'un
  `/auth/login` produirait ensuite pour ce même admin.
- **Alternatives** :
  - *Émettre un jeton spécial « setup »* — écarté : introduit une classe de jetons distincte,
    surface d'attaque accrue.

## 8. Endpoint et sécurité HTTP

- **Décision** : `POST /api/v1/setup/first-admin`, contrôleur `SetupController` avec
  `[AllowAnonymous]` explicite. Réponse `201 Created` avec `TokenResponse` (réutilisé de la
  feature 003). Erreurs via `ExceptionHandlingMiddleware` (RFC 7807 + code métier).
- **Rationale** : Convention REST cohérente (`/setup/` regroupera d'autres routes futures d'installation).
  `TokenResponse` inchangé — la SPA/Postman parse identiquement les réponses de login et de setup.
- **Alternatives** :
  - *`POST /api/v1/auth/setup`* — écarté : le namespace `/auth/` est réservé aux opérations sur un
    compte existant.
  - *`PUT /api/v1/setup/first-admin` (installation = mise en état)* — écarté : `POST` est plus
    naturel pour une création.

## 9. Sécurité (revue transverse)

- **Autorisation** : `[AllowAnonymous]` — verrou métier « 0 admin actif » (FR-004).
- **Anti-fuite** : refus prioritaire (FR-005 — voir §2).
- **Anti-force-brute** : hors périmètre — la route ne divulgue pas d'information exploitable
  (échec = tentative de créer un compte pré-existant), et le verrou naturel rend la répétition
  inutile.
- **Politique de mot de passe** : héritée (feature 003).
- **Audit** : événement dédié `Setup.FirstAdminCreated` sans mot de passe ni jeton (SC-005).
- **Contraintes d'unicité en base** : `LoginId` unique, `(member, profile)` unique,
  `NameNormalized` unique CI — protection en cas de race concurrente.
- **Rate limiting** : non requis (verrou naturel + volume attendu = 1 appel réel).

## 10. Tests (couverture attendue)

- **Application (unitaires, isolés)** :
  - Succès (base vierge) : Member/Account/Profile créés, jeton retourné.
  - `already_installed` prioritaire : même avec payload invalide, retourne 409 sans autre erreur.
  - Idempotence profil : profil « Administrateur » existant réutilisé, droits inchangés.
  - Mot de passe non conforme → 400 (uniquement si aucun admin n'existe).
  - Rollback : `SaveChangesAsync` échouant → aucune ligne partielle.
- **API (intégration SQLite)** :
  - 201 sur base vierge, jeton porte tous les droits, endpoint protégé accessible.
  - 409 `already_installed` après un premier succès.
  - 400 sur payload invalide (base vierge).
  - Le jeton retourné permet d'appeler `/attendance-sessions` (manage_attendance) et
    `/bureau-profiles` (manage_bureau_profiles) sans autre étape.
- **Régression** : la suite existante (features 001–004) reste verte.

## 11. Décisions non prises (report à des évolutions ultérieures)

- « Mot de passe oublié » / reset admin — feature future distincte.
- Interface SPA d'installation (formulaire, redirection auto post-install) — côté client.
- Rate limiting global sur `/setup/*` — non requis pour cette itération.
- Écran de statut d'installation (`GET /setup/status`) — hors périmètre.
