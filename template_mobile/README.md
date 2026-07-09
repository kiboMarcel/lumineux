# Handoff: Lumineux Mobile — Application de gestion des présences

## Overview
Application mobile Flutter consommant l'API .NET "Lumineux" (gestion des présences d'une
communauté : sessions de réunion, pointage QR, membres, profils du bureau). Un seul compte peut
avoir des droits "membre" ou "bureau" (`manage_attendance`, `manage_members`) — l'app doit adapter
l'UI selon les droits effectifs retournés par `GET /api/v1/auth/me`, pas selon un rôle fixe.

## About the Design Files
Le fichier `Lumineux Mobile.dc.html` est une **référence de design créée en HTML** (prototype
interactif) — ce n'est pas du code à copier tel quel. La tâche consiste à **recréer fidèlement ce
design en Flutter** (widgets natifs, thème Material 3, navigation, gestion d'état — Provider/Riverpod/
Bloc selon convention du projet), en branchant les écrans sur l'API réelle décrite plus bas.

## Fidelity
**Haute fidélité (hifi)** — couleurs, typographie, espacements et textes ci-dessous sont définitifs.
Reproduire pixel-perfect ; ne pas réinterpréter la palette ou la mise en page.

## Design Tokens

### Couleurs
| Token | Hex | Usage |
|---|---|---|
| `bg` | `#FAF7F2` | Fond d'écran |
| `bgOuter` | `#F1ECE3` | Fond neutre secondaire (chips inactifs, barre du bas, exigences) |
| `surface` | `#FFFFFF` | Cartes, champs, feuilles |
| `border` | `#ECE7DE` | Bordures fines (1px) |
| `ink` | `#221F1A` | Texte principal |
| `ink2` | `#6B675F` | Texte secondaire |
| `ink3` | `#ADA89D` | Icônes inactifs (nav bar), chevrons |
| `primary` | `#3B4FCC` | Indigo — boutons primaires, liens, onglet actif, avatars secondaires |
| `primaryDark` | `#2C3AA0` | Hover/pressed du primaire |
| `primarySoft` | `#E7E9FB` | Fond badges "profil du bureau" |
| `accentWarm` | `#D97A3F` | Terracotta — avatars de membres (chaleureux/communautaire) |
| `accentWarmSoft` | `#FBE7D6` | Fond badge rôle "Bureau" |
| `accentWarmText` | `#B85C22` | Texte badge rôle "Bureau" |
| `success` | `#2F8F5B` | Statuts "Actif" / "Validé" |
| `successSoft` | `#E1F3E7` | Fond badges succès |
| `danger` | `#C1483A` | Statuts "Inactif" / "Annulé", boutons destructifs (déconnexion, clôturer) |
| `dangerSoft` | `#FBE4E0` | Fond badges danger |

Pas de dégradés. Pas d'accents supplémentaires.

### Typographie
- Police unique : **Manrope** (Google Fonts), poids 400/500/600/700/800. Fallback système `sans-serif`.
- Titre logo/écran (H1) : 24px / 800 (`Lumineux`, écran de connexion)
- Titre d'écran (H2) : 20px / 800 (ex. "Bonjour, Aline", "Membres", "Scanner")
- Titre de carte / nom (H3) : 15–17px / 700
- Corps : 14–15px / 400–600
- Petit texte / support : 12–13px / 400–600
- Labels de champ : 13px / 600
- `letter-spacing: 0.04em` + majuscules pour les sur-titres de section ("MES PRÉSENCES RÉCENTES", "DROITS")

### Espacement & rayons
- Rayon carte / bouton grand : 12–20px (boutons 12px, cartes 14–20px, feuille modale coins hauts 20px)
- Rayon pill / badge : 100px (complètement arrondi)
- Padding écran horizontal standard : 20–24px
- Hauteur champ de saisie / bouton : 44–50px
- Gap vertical entre sections : 12–20px
- Bordure fine standard : `1px solid #ECE7DE` (1.5px pour boutons outline)
- Ombre : aucune ombre portée dans ce design (à part le cadre du device, non applicable à l'app)

## Screens / Views

### 1. Connexion (Login)
**Purpose** : authentification (`POST /api/v1/auth/login`).
**Layout** : colonne centrée verticalement, padding 32/28px.
- Logo : carré 64×64, radius 20, fond `primary`, lettre "L" blanche 28px/800, centré
- Titre "Lumineux" 24px/800 + sous-titre "Gestion des présences" 14px `ink2`
- Champ "Identifiant" (référence membre, ex. `LMX-2024-0187`) — label 13px/600, champ 48px hauteur, radius 12, bordure `border`
- Champ "Mot de passe" — même style, valeur masquée (points)
- Bouton "Se connecter" pleine largeur, 50px, radius 12, fond `primary`, texte blanc 15px/700 ; hover → `primaryDark`
- Lien "Mot de passe oublié ?" centré, `primary`, 13px (→ `POST /api/v1/auth/forgot-password`)
- Séparateur "Première connexion" (ligne + texte `ink3`/12px + ligne)
- Bouton outline "Activer mon compte" 48px, radius 12, bordure 1.5px `primary`, texte `primary` 14px/700 → écran Activation

### 2. Activation de compte
**Purpose** : première connexion, mot de passe temporaire → nouveau mot de passe (`POST /api/v1/auth/activate`).
**Layout** : header avec flèche retour (32×32, radius 10) + titre "Activer votre compte" 18px/700.
- Texte d'intro 14px `ink2`
- 3 champs : "Mot de passe temporaire", "Nouveau mot de passe", "Confirmer le mot de passe" (même style que login)
- Encart règles : fond `bgOuter`, radius 12, padding 12/14px — sur-titre "EXIGENCES" 12px/700 `ink2`, puis 2 lignes avec coche "✓" : "8 caractères minimum", "Une majuscule et un chiffre"
- Bouton "Activer mon compte" (style bouton primaire plein, comme login)
- Valider le formulaire → écran principal (Accueil)

### 3. Accueil (Home)
**Purpose** : vue d'entrée, statut de session (si droit `manage_attendance`) + historique perso.
**Layout** : padding 20px.
- En-tête : "Bonjour, {prénom}" 20px/800 + "{antenne}" 13px `ink2`, à droite un badge pill rôle effectif (ex. "Bureau") fond `accentWarmSoft` texte `accentWarmText`, 12px/700
- **Si droits de gestion des présences** : carte pleine largeur fond `primary`, radius 18, padding 18px, texte blanc :
  - État "session ouverte" : "Session ouverte" 15px/700 + "{antenne} · depuis {heure}" 12px opacité 0.8 + bouton blanc "Afficher le QR" (42px, radius 10, texte `primary`) → écran QR session
  - État "aucune session" : "Aucune session ouverte" + description + bouton blanc "Démarrer une session" (`POST /api/v1/attendance-sessions`)
- Sur-titre "MES PRÉSENCES RÉCENTES" puis liste de cartes (fond blanc, bordure `border`, radius 14, padding 12/14) : libellé session + "{date} · {heure}", badge statut à droite (Validé = vert succès, Annulé = rouge danger) — source : historique de présence du membre courant

### 4. Scanner (onglet)
**Purpose** : pointage par scan QR (`POST /api/v1/attendance-sessions/{id}/scan`), avec file d'attente hors-ligne (`.../scan/batch`) si pas de réseau.
**Layout** : titre "Scanner" 20px/800.
- Viseur caméra : carré 240×240, fond noir `#221F1A`, radius 24, 4 coins en L blancs (28×28, bordure 3px) aux quatre angles — mock de flux caméra réel
- Texte "Placez le code QR de la session dans le cadre" 14px `ink2`
- Bouton "Simuler un scan" (dans le vrai produit : déclenché par la détection caméra) → overlay de succès
- **Overlay succès** (modal centré, fond semi-transparent `rgba(34,31,26,0.55)`) : carte blanche 260px, radius 20 — icône check dans cercle vert clair (56×56), "Présence enregistrée" 16px/700, "{nom} · {heure}" 13px `ink2`, bouton "Fermer" pleine largeur primaire

### 5. Session — QR (bureau)
**Purpose** : afficher le QR rotatif de la session ouverte (`GET /api/v1/attendance-sessions/{id}/qr`, polling), suivre les pointages en direct, clôturer (`POST .../close`).
**Layout** : header retour + "Session — {antenne}" 17px/700 + "Ouverte depuis {heure}" 12px `ink2`.
- Bloc QR : carré 220×220, radius 20, motif hachuré (placeholder — remplacer par le vrai QR encodé), bordure `border`
- Sous le QR : cercle countdown (26×26, bordure 3px `primary`, chiffre au centre = secondes avant renouvellement du jeton) + texte "Renouvellement automatique"
- 2 cartes stats côte à côte (fond blanc, bordure `border`, radius 14, padding 14, texte centré) : "{n} présences" / "{n attendus}"
- Sur-titre "DERNIERS POINTAGES" + liste (avatar rond 34px initiales sur fond `accentWarm`, nom, heure à droite) — dernier arrivé en haut
- Bouton flottant bas "Clôturer la session" — outline danger, fixe en bas de l'écran

### 6. Membres (onglet, droit `manage_members`)
**Purpose** : recherche (`GET /api/v1/members?...`) et détail (`GET /api/v1/members/{id}`, `GET /api/v1/members/{id}/bureau-profiles`).
**Layout** : titre "Membres" 20px/800.
- Champ recherche (44px, radius 12, icône loupe simple + placeholder "Rechercher un membre…")
- Chips filtre antenne, scrollables horizontalement : "Toutes antennes" (actif, fond `primary`) / "Centre" / "Nord" (inactifs, fond blanc bordure `border`)
- Liste : avatar rond 40px initiales (fond `accentWarm`), nom 14px/600, "{antenne} · {statut}" 12px `ink2`, chevron `›` `ink3` à droite
- Tap → feuille modale bas d'écran (bottom sheet)

**Feuille détail membre** : poignée grise centrée, avatar 52px + nom 16px/700 + "{référence} · {antenne}" + badge statut (vert Actif / rouge Inactif) aligné à droite ; barre de progression "Taux de présence {n}%" (piste `bgOuter` 8px, remplissage `primary`) ; chips profils du bureau (fond `primarySoft` texte `primary`, pill) ; bouton "Fermer" (fond `bgOuter`, texte `ink`)

### 7. Profil (onglet)
**Purpose** : identité, droits effectifs (`GET /api/v1/auth/me`), déconnexion.
**Layout** :
- Avatar rond 76px initiales fond `primary`, nom 18px/800, "{référence} · {antenne}" 13px `ink2`
- Carte "Aperçu du rôle (démo)" — **à ne PAS reproduire en prod** : c'était un sélecteur de démonstration pour prévisualiser les deux états d'UI. En production, le rôle vient uniquement de l'API (`/auth/me`), aucun switch utilisateur.
- Carte "Droits" : liste des permissions effectives, chaque ligne avec puce check verte + libellé (ex. "Gérer les présences", "Gérer les membres" ; si aucun droit : "Aucun droit de gestion")
- Bouton "Se déconnecter" (outline danger, pleine largeur) → invalider le token, retour à Connexion

## Navigation
- Barre de navigation basse (4 onglets, visible uniquement sur les écrans principaux, masquée sur Connexion/Activation/QR session) : Accueil, Scanner, Membres, Profil
- Icônes = formes géométriques simples (carré plein, carré à coins, cercle plein, cercle contour) — libre à l'implémentation Flutter d'utiliser des `Icons` Material équivalents (home, qr_code_scanner, people, person) plutôt que de reproduire les formes exactes
- Couleur active `primary`, inactive `ink3`
- Écran "Session QR" est un push par-dessus Accueil (bouton retour), pas un onglet
- L'onglet "Membres" ne doit être visible que si l'utilisateur a le droit `manage_members` ou `manage_attendance` (cf. règles API ci-dessous)
- Le bloc "session" sur Accueil ne doit être visible que si l'utilisateur a le droit `manage_attendance`

## State Management
- Session utilisateur : token JWT (stocker en storage sécurisé), profil courant + droits effectifs via `GET /api/v1/auth/me`
- Session de présence active : `GET /api/v1/attendance-sessions/mine/open` au démarrage pour restaurer l'état "session ouverte"
- Jeton QR : re-fetch périodique (`GET /api/v1/attendance-sessions/{id}/qr`), le compte à rebours affiché doit refléter la durée réelle de validité renvoyée par l'API (le prototype simule 30s fixes)
- File de scans hors-ligne : stocker localement puis `POST /scan/batch` à la reconnexion (FR-023 côté API)
- Écran Membres : pagination/recherche côté serveur (`GET /api/v1/members?...`), ne pas tout charger en mémoire

## Interactions & Behavior
- Transitions d'écran : pas d'animation spécifique prescrite dans le prototype — utiliser les transitions standard Flutter/Material (slide horizontal pour push, fade pour modales)
- Bottom sheet (détail membre) et overlay succès scan : apparition depuis le bas / fade, fermeture par bouton "Fermer" (pas de swipe-to-dismiss prescrit, mais recommandé en Flutter)
- Countdown QR : décrément chaque seconde, reset au renouvellement du jeton
- Champs de saisie : pas de validation visuelle spécifiée dans le prototype au-delà de l'encart "Exigences" sur l'activation — appliquer les règles réelles de `PasswordRules.cs` côté API (8 caractères min, majuscule + chiffre)

## API Mapping (backend .NET existant — `src/Lumineux.Api`)
Base : `/api/v1`. Auth par JWT (`Authorize`), policies par droit (`manage_attendance`, `manage_members`, `manage_bureau_profiles`).

- `POST /auth/login`, `POST /auth/activate`, `POST /auth/forgot-password`, `POST /auth/reset-password`, `GET /auth/me`, `POST /auth/change-password`
- `POST /attendance-sessions` (démarrer), `GET /attendance-sessions/mine/open`, `GET /attendance-sessions/{id}`, `GET /attendance-sessions/{id}/qr`, `POST /attendance-sessions/{id}/close`
- `POST /attendance-sessions/{id}/scan`, `POST /attendance-sessions/{id}/scan/batch`, `POST /attendance-sessions/{id}/attendances` (ajout manuel), `GET /attendance-sessions/{id}/attendances`, `DELETE /attendance-sessions/{id}/attendances/{memberId}`
- `POST /members`, `GET /members`, `GET /members/{id}`, `PUT /members/{id}`
- `GET /permissions`, `GET /bureau-profiles`, `GET /bureau-profiles/{id}`, `GET members/{id}/bureau-profiles`

Voir les contrats OpenAPI dans le dépôt (`specs/00X-*/contracts/openapi.yaml`) pour les schémas de requête/réponse exacts — le README de l'API backend (`src/Lumineux.Api/README.md`) liste tous les endpoints avec droits requis.

## Assets
Aucun asset image/icône externe : tout est composé de formes CSS simples (cercles, rectangles arrondis) et de texte. En Flutter, remplacer par des `Icons` Material (ou une icon font maison) pour les icônes de navigation et de recherche ; générer le vrai QR code côté client avec un package dédié (ex. `qr_flutter`) à partir du jeton renvoyé par l'API.

## Files
- `Lumineux Mobile.dc.html` — prototype interactif complet (référence visuelle et comportementale), à ouvrir dans un navigateur pour naviguer tous les écrans/états décrits ci-dessus.
