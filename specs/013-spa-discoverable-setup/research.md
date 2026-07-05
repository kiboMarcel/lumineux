# Research — Installation découvrable (SPA)

Petit lot de découvrabilité réutilisant l'existant. Décisions figées avant conception.

## 1. Où consulter le statut

- **Décision** : consulter `GET /setup/status` **à l'ouverture de l'écran de connexion** (au démarrage
  du composant `LoginComponent`).
- **Rationale** : c'est le point d'entrée public de l'application ; un seul appel suffit à décider
  l'affichage du lien (FR-001/002). Cohérent avec le parcours anonyme.
- **Alternatives écartées** : consulter au bootstrap global de l'app (le lien ne concerne que la
  connexion) ; page « premier démarrage » dédiée (hors périmètre — on se limite au lien conditionnel).

## 2. Condition d'affichage & défaut sûr

- **Décision** : `showSetupLink = (status.installed === false)`. En cas d'**échec** de l'appel (réseau/
  API), `showSetupLink = false` (**masqué**) et la connexion **n'est pas bloquée**.
- **Rationale** : FR-002/003/005, SC-002/003. On préfère **ne pas** proposer l'installation à tort ;
  l'écran d'installation reste de toute façon protégé côté serveur (409).
- **Alternatives écartées** : afficher par défaut en cas de doute (risque d'induire en erreur sur une
  instance installée) ; bloquer la connexion tant que le statut n'est pas connu (dégrade l'UX).

## 3. Réutilisation de l'écran d'installation

- **Décision** : le lien pointe vers la **route existante** `/setup/first-admin` (`SetupComponent`,
  feature 008), **non modifiée**. Le refus **409 already_installed** est déjà géré par ce composant.
- **Rationale** : FR-004/006/007. Aucune réimplémentation ; l'URL reste accessible directement.
- **Alternatives écartées** : dupliquer/adapter l'écran d'installation (inutile).

## 4. Intercepteur d'erreurs & statut

- **Décision** : l'appel de statut est un **GET anonyme** ; son échec est traité **localement** dans le
  composant (masquage), sans notification bloquante. Pas de session → l'intercepteur 401 ne s'applique
  pas (aucun jeton en jeu).
- **Rationale** : garder la connexion pleinement utilisable même si le statut échoue (FR-005).
- **Alternatives écartées** : router l'erreur de statut via une notification globale (bruyant et
  inutile pour un simple masquage).
