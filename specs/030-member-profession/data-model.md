# Phase 1 — Modèle de données : Profession du membre

## Entité modifiée : `Member`

Ajout d'un unique attribut. Aucune relation, aucun index, aucune contrainte d'unicité.

| Attribut | Type | Nullable | Contrainte | Notes |
|---|---|---|---|---|
| `Profession` | `string?` | Oui | Longueur ≤ 150 ; nettoyée (trim) ; vide → `null` | Texte libre. Aucune FK, aucune unicité. |

### Représentation en base

| Colonne | Type SQL | Nullable | Défaut |
|---|---|---|---|
| `profession` | `nvarchar(150)` | `NULL` | aucun |

- Migration : `MemberProfession` — `AddColumn` sur la table `members`. Additive, rejouable.
- Aucun index (pas de recherche ni d'unicité sur ce champ).
- Champs d'audit (`createdt/by`, `updatedt/by`) inchangés, gérés par l'intercepteur existant.

### Règles de validation (FR-005, FR-006, FR-007, FR-009)

1. **Optionnel** : absence de valeur autorisée à la création et à la correction.
2. **Nettoyage** : suppression des espaces de début/fin avant stockage (handler Application).
3. **Vide ⇒ null** : chaîne vide ou composée uniquement d'espaces stockée comme `null`.
4. **Borne** : longueur maximale 150 ; au-delà → rejet avec message clair
   (FluentValidation, serveur faisant autorité) ; matérialisé aussi en base (`HasMaxLength(150)`).
5. **Pas d'unicité** : plusieurs membres peuvent partager la même profession (FR-010).

### Invariants inchangés

- Les invariants de création du membre (référence, nom, prénom, genre, antenne obligatoire selon
  la surcharge) restent identiques ; `Profession` n'entre dans aucune règle obligatoire (FR-002).
- Aucun impact sur `MemberAccount`, les présences (`Attendance`, `AttendanceSession`) ni les
  référentiels.

## Transitions d'état

Aucune — `Profession` est une donnée descriptive libre, sans machine à états. Elle peut être
renseignée, remplacée ou effacée à tout moment via la correction (US2).

## Flux de données (résumé)

```text
SPA member-form ──(profession)──▶ CreateMemberRequest / UpdateMemberRequest
                                          │
                                          ▼
                             Handler (validate → trim/vide→null)
                                          │
                                          ▼
                                   Member.Profession
                                          │
                                          ▼
                              members.profession (nvarchar(150) NULL)
                                          │
                                          ▼
                        MemberResponse.Profession ──▶ fiche membre (SPA)
```
