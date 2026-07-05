# Research — Console web : Présences (Lot 4)

Extension de l'app Angular (feature 008) consommant les API présences (001), référentiels (010) et
recherche membre allégée (015). Décisions figées avant conception.

## 1. Génération du code QR côté client

- **Décision** : utiliser une **bibliothèque cliente de génération de QR** (composant/utilitaire
  Angular, ex. `angularx-qrcode`) qui rend une **image QR** à partir du **jeton** renvoyé par
  `GET /attendance-sessions/{id}/qr`. Le jeton n'est **jamais** affiché en clair ni persisté (FR-005).
- **Rationale** : l'API ne fournit qu'un jeton ; rendre un QR scannable exige une génération côté
  client. Bibliothèque éprouvée = fiabilité du rendu (décision PO 2026-07-05).
- **Dépendance** : ajout d'un paquet npm (installation réseau à **approuver** au moment de
  l'implémentation, comme feature 008).
- **Alternatives écartées** : générateur maison (coût/fiabilité) ; demander une image à l'API (endpoint
  inexistant, hors périmètre).

## 2. Rotation du QR

- **Décision** : après affichage, planifier le **ré-appel** de `GET …/qr` **avant expiration** (au
  rythme `stepSeconds` renvoyé) et **regénérer** l'image. Le cycle s'arrête à la **destruction** du
  composant et à la **clôture** de la session.
- **Rationale** : FR-004/SC-001 ; évite un QR périmé projeté.
- **Alternatives écartées** : rafraîchir uniquement sur action manuelle (le QR expirerait) ; se fier à
  l'`expiresAt` seul sans marge (risque de fenêtre morte) — on rafraîchit **avant** l'expiration.

## 3. Suivi temps réel = polling

- **Décision** : **poller** `GET /attendance-sessions/{id}/attendances?status=…` à intervalle régulier
  (ex. 5 s) tant que la session est ouverte et l'écran affiché ; mettre à jour liste + **décompte des
  valides** ; filtre par statut appliqué à la requête.
- **Rationale** : l'API n'expose pas de flux temps réel ; le polling est la solution simple et robuste
  (FR-007/SC-002). Bornage au cycle de vie du composant (pas de fuite de timers).
- **Alternatives écartées** : websocket/SSE (non fournis par l'API) ; rafraîchissement manuel
  uniquement (dégrade le « temps réel »).

## 4. Ajout manuel : identification du membre via lookup (015)

- **Décision** : le **sélecteur** de l'ajout manuel s'appuie sur `GET /members/lookup?query=…`
  (feature 015, accessible à `manage_attendance`) → l'utilisateur choisit un membre → `POST
  …/attendances { memberId }` (idempotent).
- **Rationale** : évite d'exiger `manage_members` ; expose des champs **minimaux**. FR-009/FR-013,
  SC-003.
- **Alternatives écartées** : réutiliser la recherche complète (`GET /members`, `manage_members`) —
  inaccessible à un opérateur de présence ; saisie d'un id numérique — UX pauvre.

## 5. Modèle d'écran & découpage

- **Décision** : deux écrans — **démarrage** (`session-start` : antenne via référentiel 010 + date +
  pas de rotation) et **animation** (`session-run`) qui compose : **panneau QR** (rendu + rotation),
  **liste temps réel** (polling + filtre + décompte), **ajout manuel** (lookup + POST), **annulation**
  et **clôture** (confirmées). Rechargement possible par identifiant de session (état serveur).
- **Rationale** : sépare les préoccupations ; l'animation est l'écran central du bureau.
- **Alternatives écartées** : un seul écran monolithique (moins testable) ; liste de sessions (hors
  périmètre — la spec cible démarrer/animer/clôturer).

## 6. Confirmations, clôture & mapping d'erreurs

- **Décision** : **annulation** et **clôture** demandent une **confirmation**. Après clôture, le QR et
  les actions d'écriture sont **masqués** ; toute écriture sur session close → **409** restitué via
  `messageForError` (« session close »). Antenne indisponible → démarrage **empêché** (message).
- **Rationale** : FR-010/011/012/016 ; SC-004/SC-007 ; cohérence avec le socle.
- **Alternatives écartées** : actions destructrices sans confirmation (risqué).

## 7. Sécurité du jeton QR

- **Décision** : le jeton n'est **jamais** rendu en texte, ni stocké (localStorage/URL) ; il vit en
  **mémoire** le temps de générer l'image, puis est remplacé au cycle suivant.
- **Rationale** : FR-005/SC-005 ; principe « aucun secret persisté » du socle.
