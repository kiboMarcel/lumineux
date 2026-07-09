# Contrat UI — Bouton « Annuler la session » (console bureau)

**Feature** : `028-cancel-empty-session` · **Phase 1** · Statut : **NOUVEAU (SPA)**

S'insère dans l'écran de **suivi de session** (`session-run`, feature 014). Français, droit
`manage_attendance`. Le serveur reste l'autorité ; le masquage client est une commodité.

## 1. Affichage du bouton

- Dans l'en-tête d'actions de la session (à côté de **« Clôturer la session »**), ajouter un bouton
  **« Annuler la session »**.
- **Visible/actif uniquement si** :
  - la session est **ouverte** (`status !== 'Closed' && status !== 'Cancelled'`) **et**
  - le **nombre de présents valides** affichés = **0** (aucune présence dans la liste, hors présences déjà
    annulées).
- Si au moins une présence valide existe → le bouton **n'est pas proposé** (le chemin reste **« Clôturer »**).

## 2. Distinction des libellés (éviter la confusion)

| Action | Portée | Libellé |
|--------|--------|---------|
| **Annuler la session** | supprime une **séance vide** (état → Cancelled) | « Annuler la session » |
| **Clôturer la session** | termine une séance **avec présences** | « Clôturer la session » |
| **Annuler** (dans la liste) | annule **une présence** d'un membre (existant) | « Annuler » (par ligne) — **inchangé** |

## 3. Confirmation (action irréversible, FR-007)

Au clic, demander une **confirmation explicite**, ex. :
> « Annuler définitivement cette session vide ? Cette action est irréversible. »

Distincte du texte de clôture (« Clôturer définitivement… »).

## 4. Résultats

| Cas | Comportement |
|-----|--------------|
| **Succès (200)** | La session passe à « Cancelled » ; **rediriger** hors du suivi (retour au démarrage/liste) ; message de confirmation. |
| **409 non vide** (course : une présence est apparue) | Afficher le **message serveur** (`messageForError`) ; **rester** sur l'écran ; rafraîchir la liste (une présence est arrivée). |
| **409 non ouverte** (déjà close/annulée) | Message clair ; rafraîchir l'état. |
| **403** | Message d'accès refusé (rare, l'entrée est gardée). |

## 5. Accessibilité & sécurité UI

- Bouton avec libellé explicite (pas seulement une icône) ; cible tactile suffisante.
- Aucune donnée sensible affichée ; l'action ne supprime **aucune** présence (le bouton n'apparaît que
  lorsque la session est vide, et le serveur re-vérifie).
- Confirmation obligatoire avant l'appel réseau.
