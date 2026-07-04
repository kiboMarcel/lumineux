# Data Model — Profil de l'utilisateur courant (auth/me)

## Vue d'ensemble

Cette fonctionnalité **n'introduit aucune entité persistée**, **aucune table**, **aucune migration**.
Elle expose une **représentation en lecture seule dérivée de la session** (le jeton JWT courant).
Elle n'est donc pas concernée par le Principe II (Code-First) au sens d'une évolution de schéma.

```mermaid
flowchart LR
    JWT["Jeton de session (JWT)\nclaims: member_id, name, permission[]"]
    ICU["ICurrentUser (port Application)\nMemberId · UserName · Permissions[] · HasPermission"]
    H["GetCurrentUserHandler (Application)"]
    DTO["CurrentUserResponse (DTO)\nmemberId · displayName · permissions[]"]

    JWT -->|résolu par CurrentUser (API)| ICU
    ICU -->|mapping| H
    H --> DTO
```

## Contrat de sortie — `CurrentUserResponse` (DTO, lecture seule)

| Attribut | Type | Source (claim) | Description |
|----------|------|----------------|-------------|
| `memberId` | entier | `member_id` | Identifiant technique du membre connecté. |
| `displayName` | chaîne | `ClaimTypes.Name` (= nom complet) | Libellé d'affichage pour l'interface (FR-004). |
| `permissions` | liste de chaînes | claims `permission` | Droits fonctionnels **effectifs de la session** ; liste éventuellement **vide**, sans doublon, ordre non significatif (FR-005/006). |

- **Aucun** champ secret ou sensible superflu (pas d'empreinte, pas de jeton, pas de mot de passe) —
  FR-007.
- Représentation **entièrement dérivée** du jeton : aucune lecture en base, aucun effet de bord
  (FR-008).

## Extension du port `ICurrentUser` (Application) — additive

`ICurrentUser` expose déjà `MemberId`, `UserName`, `IsAuthenticated`, `HasPermission(string)`. On
**ajoute** l'énumération des droits de la session :

| Membre ajouté | Type | Sémantique |
|---------------|------|------------|
| `Permissions` | `IReadOnlyCollection<string>` | Droits portés par la session courante (claims `permission`). **Vide** si aucun droit. Jamais `null`. |

- Implémentation API `CurrentUser` : énumère les claims de type `permission` du `ClaimsPrincipal`
  courant.
- Extension **sans rupture** : les doubles de test existants (`Substitute.For<ICurrentUser>()`)
  restent valides.

## Invariants / règles observables

- `permissions` reflète **exactement** l'ensemble des claims `permission` du jeton (ni sur-ensemble,
  ni sous-ensemble) — FR-006, SC-002.
- Deux appels successifs avec le **même** jeton renvoient la **même** réponse (idempotence de lecture)
  — FR-008.
- Un contexte **authentifié mais sans `member_id`** exploitable (jeton malformé) → refus 401
  générique journalisé (garde défensive), pas de réponse partielle.

## Migration

**Aucune.** Rejouable sur base vierge sans impact (rien à créer/modifier).
