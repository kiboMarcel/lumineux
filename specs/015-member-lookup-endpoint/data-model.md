# Data Model — Recherche membre allégée (member lookup)

## Vue d'ensemble

**Aucune entité persistée nouvelle**, **aucune table**, **aucune migration**. Projection de lecture
minimale dérivée des membres existants, via la recherche existante.

```mermaid
flowchart LR
    Q["GET /members/lookup?query=…"] --> H["LookupMembersHandler\n(any-of + terme requis)"]
    H -->|SearchAsync(query, page=1, cap)| Repo["IMemberRepository (existant)"]
    Repo -->|Member[]| H
    H -->|projection minimale| DTO["MemberLookupResponse[]"]
```

## Contrat de sortie — `MemberLookupResponse` (DTO, lecture seule)

| Attribut | Type | Source (Member) |
|----------|------|-----------------|
| `id` | entier | `Id` (identifiant à passer à l'ajout manuel) |
| `reference` | chaîne | `Reference` |
| `fullName` | chaîne | `FullName` (prénom + nom) |
| `status` | chaîne | `Status` |

- **Aucun** autre champ : ni e-mail, ni mobile, ni adresse, ni date de naissance, ni rattachement
  (FR-003, SC-002).

## Entrée

| Paramètre | Obligatoire | Règle |
|-----------|-------------|-------|
| `query` | **Oui** | Terme non vide (référence et/ou nom). Vide/blanc → **400** (FR-002). |

## Règles / invariants (observables)

- **Terme requis** ; sinon refus 400 (FR-002/SC-003).
- **Accès** : `manage_attendance` OU `manage_members` ; sinon 403 (non authentifié → 401) — FR-005/SC-004.
- **Champs minimaux** (FR-003/SC-002) ; **résultats plafonnés** (FR-004/SC-005).
- Lecture **répétable**, **sans effet de bord** (FR-006).

## Source (existant, non modifié)

- `IMemberRepository.SearchAsync(query, page, pageSize)` (feature 002) : recherche par nom/prénom/
  référence. Réutilisée avec `page = 1`, `pageSize = plafond` (ex. 20).

## Migration

**Aucune.** Rejouable sur base vierge sans impact.
