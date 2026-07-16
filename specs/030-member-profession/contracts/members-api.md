# Phase 1 — Contrat d'API : deltas Profession

Aucune nouvelle route. Trois DTO existants du domaine Membre reçoivent un champ **optionnel**
`profession` (texte, ≤ 150 caractères, nullable). Ajout **rétrocompatible** — pas de nouvelle
version d'API (Principe V).

## `POST /api/v1/members` — création

**Requête** (`CreateMemberRequest`) — champ ajouté :

```jsonc
{
  // ... champs existants inchangés (lastName, firstName, gender, antennaId, mobile, email, ...)
  "profession": "Enseignant"   // optionnel ; null/absent/vide accepté
}
```

- `profession` absent, `null`, ou vide/espaces → membre créé sans profession.
- `profession` de plus de 150 caractères → **400 Bad Request**, message de validation clair
  mentionnant la longueur maximale, aucun enregistrement partiel.
- Espaces de bord retirés avant stockage.

**Réponse** (`MemberCreatedResponse.member` = `MemberResponse`) : voir ci-dessous.

## `PUT /api/v1/members/{id}` — correction

**Requête** (`UpdateMemberRequest`) — même champ `profession` ajouté, mêmes règles.

- Renseigner une profession sur un membre qui n'en avait pas → ajout.
- Fournir une valeur différente → remplacement.
- Fournir `null`/vide → effacement (retour à absence).

**Réponse** : `MemberResponse` à jour.

## `MemberResponse` — lecture (création, correction, fiche `GET /api/v1/members/{id}`)

Champ ajouté :

```jsonc
{
  // ... champs existants inchangés
  "profession": "Enseignant"   // null si non renseignée
}
```

## Règles transverses

| Règle | Comportement |
|---|---|
| Autorisation | `manage_members` requise (inchangé, création et correction). |
| Unicité | Aucune — plusieurs membres peuvent avoir la même profession. |
| Validation | Serveur faisant autorité (longueur ≤ 150, normalisation trim/vide→null). |
| Format d'erreur | Format d'erreur de validation homogène existant (non fuitant). |
| Journalisation | Opération déjà tracée (`CreateMember`/`UpdateMember`) ; la **valeur** de profession n'est pas journalisée. |

## Non-objectifs de contrat

- Pas de filtre/recherche par profession dans les endpoints de liste/lookup (hors périmètre).
- Pas d'exposition de la profession dans `MemberListItem` ni `MemberLookupResponse` (champs
  minimaux inchangés).
