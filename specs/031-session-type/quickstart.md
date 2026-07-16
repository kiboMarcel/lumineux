# Quickstart — Validation de bout en bout : Type de session

Guide de vérification. Contrat dans [`contracts/sessions-api.md`](./contracts/sessions-api.md),
modèle dans [`data-model.md`](./data-model.md).

## Prérequis

- API .NET en marche avec la migration `SessionType` appliquée (`dotnet ef database update`).
- Un compte bureau disposant du droit `manage_attendance`.

## Vérifications API (contrat)

1. **Démarrage sans type → AntennaMeeting**
   - `POST /api/v1/attendance-sessions` sans `sessionType`.
   - Attendu : **201**, `sessionType == "AntennaMeeting"`.

2. **Démarrage avec `Teaching`**
   - `POST` avec `"sessionType": "Teaching"`.
   - Attendu : **201**, `sessionType == "Teaching"`.

3. **Type inconnu refusé**
   - `POST` avec `"sessionType": "Party"`.
   - Attendu : **400**, message de validation clair ; aucune session créée.

4. **Lecture**
   - `GET /api/v1/attendance-sessions/{id}` et `GET /api/v1/attendance-sessions/mine/open` →
     `sessionType` présent et cohérent (jamais null).

5. **Sessions préexistantes**
   - Une session créée avant la migration se lit avec `sessionType == "AntennaMeeting"` (défaut en base).

6. **Non-régression**
   - Démarrer, afficher le QR, pointer, ajouter manuellement, clôturer, annuler (session vide) et
     consulter les rapports 018/020 : comportement identique quel que soit le type.

## Vérifications SPA

- L'écran de démarrage fonctionne comme avant (aucun sélecteur de type ajouté) et produit des
  sessions `AntennaMeeting` ; l'interface `SessionResponse` du SPA porte le champ `sessionType`
  (contrat synchronisé, non affiché).

## Tests automatisés attendus (référence pour `/speckit-tasks`)

- **Domain** : `Start` sans type → `AntennaMeeting` ; `Start` avec `Teaching` → conservé ;
  `Close`/`Cancel`/`AutoClose` ne modifient pas le type (immuabilité).
- **Application** : handler démarre avec type explicite ; validator rejette un type inconnu ;
  mapping `SessionResponse.SessionType`.
- **Infrastructure** : la colonne `session_type` persiste/relit la valeur (SQLite) ; défaut appliqué.
- **Api** : `POST` avec/sans type (201 + valeur) ; type inconnu → 400 ; `GET` expose le type.

## Critères de succès (rappel spec)

SC-001 toutes les sessions ont un type · SC-002 défaut AntennaMeeting · SC-003 type inconnu refusé
· SC-004 aucune régression des parcours de session.
