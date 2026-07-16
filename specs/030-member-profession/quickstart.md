# Quickstart — Validation de bout en bout : Profession du membre

Guide de vérification manuelle/automatisée. Détails de contrat dans
[`contracts/members-api.md`](./contracts/members-api.md), modèle dans
[`data-model.md`](./data-model.md).

## Prérequis

- API .NET en marche avec la migration `MemberProfession` appliquée
  (`dotnet ef database update` depuis le projet Infrastructure/API).
- Un compte bureau disposant du droit `manage_members`.
- SPA `web/` lancé et connecté à l'API.

## Vérifications API (contrat)

1. **Création avec profession**
   - `POST /api/v1/members` avec `"profession": "Enseignant"` + champs obligatoires.
   - Attendu : **201**, `member.profession == "Enseignant"`.

2. **Création sans profession**
   - Même requête sans `profession` (ou `null`).
   - Attendu : **201**, `member.profession == null`.

3. **Normalisation**
   - `POST` avec `"profession": "  Commerçante  "`.
   - Attendu : `member.profession == "Commerçante"` (espaces de bord retirés).
   - `POST` avec `"profession": "   "` (espaces seuls) → `member.profession == null`.

4. **Borne de longueur**
   - `POST` avec une profession de 151 caractères.
   - Attendu : **400**, message de validation mentionnant la longueur maximale ; aucun membre créé.
   - `POST` avec exactement 150 caractères → **201** (limite acceptée).

5. **Correction (ajouter / modifier / effacer)**
   - Sur un membre sans profession : `PUT /api/v1/members/{id}` avec `"profession": "Infirmier"`
     → **200**, `profession == "Infirmier"`.
   - Rejouer avec `"profession": "Cadre"` → remplacement.
   - Rejouer avec `"profession": null` → `profession == null` (effacée).

6. **Lecture**
   - `GET /api/v1/members/{id}` → `profession` reflète la dernière valeur enregistrée.

7. **Rétrocompatibilité / additivité**
   - Un membre créé avant la migration se lit sans erreur avec `profession == null`.

## Vérifications SPA (console web)

1. Formulaire **création membre** : le champ « Profession » apparaît (facultatif) ; saisir une
   valeur, enregistrer, rouvrir la fiche → la profession s'affiche.
2. Formulaire **correction** : ajouter, modifier puis effacer la profession ; chaque enregistrement
   se reflète à la relecture.
3. **Fiche membre** : profession affichée si renseignée ; absence propre (pas de valeur fictive)
   sinon.

## Tests automatisés attendus (référence pour `/speckit-tasks`)

- **Domain/Application** : normalisation (trim, espaces→null), borne (150 accepté, 151 refusé),
  mapping `MemberResponse.Profession`, aller-retour création puis correction (ajout/remplacement/
  effacement).
- **Infrastructure** : la colonne `profession` persiste et relit la valeur (test d'intégration
  SQLite).
- **SPA (Vitest)** : le contrôle de formulaire est présent, se pré-remplit en édition, envoie
  `null` quand vidé.

## Critères de succès (rappel spec)

SC-001 saisie sans étape supplémentaire · SC-002 renseigner/modifier/effacer reflété · SC-003
hors-borne refusé avec message · SC-004 membres existants intacts (profession vide).
