# Contrat de navigation (UI) — Feature 026

Extension de la coquille membre (M0) : ajout d'un **onglet Scanner** et d'un **overlay de résultat**.

## Barre de navigation basse — 3 onglets

| Position | Onglet | Écran | Accès |
|----------|--------|-------|-------|
| 1 | Accueil | `HomeTab` (existant) | session valide |
| 2 | **Scanner** *(nouveau)* | `ScannerScreen` | session valide |
| 3 | Profil | `ProfileTab` (existant) | session valide |

- Le nouvel onglet est **permanent** (visible pour tout membre authentifié) ; icône Material `qr_code_scanner`.
- Couleur active `primary`, inactive `ink3` (design system existant).
- La coquille `HomeShell` passe de 2 à **3** entrées dans l'`IndexedStack` et la barre.

## Écran Scanner — états

| État | Rendu |
|------|-------|
| Permission inconnue | Chargement bref (résolution du statut caméra). |
| Permission refusée | Message « La caméra est nécessaire pour scanner » + bouton **« Ouvrir les réglages »**. Les autres onglets restent accessibles. |
| Scan actif | Aperçu caméra plein cadre + **cadre de visée** (4 coins en L, cf. design) + consigne « Placez le code QR de la session dans le cadre ». |
| Soumission | Aperçu figé + indicateur ; détection **suspendue**. |

## Overlay de résultat (modal)

Affiché par-dessus l'écran Scanner (fond semi-transparent). Un seul à la fois.

| Cas | Contenu | Action |
|-----|---------|--------|
| **Succès (créée)** | Icône check verte, « Présence enregistrée », `{nom} · {heure}` (ou heure seule si nom absent) | Bouton **« Fermer »** → reprise du scan |
| **Succès (déjà présente)** | « Déjà enregistrée », `{nom} · {heure}` (ou heure seule si nom absent) | Bouton **« Fermer »** → reprise du scan |
| **Erreur serveur/réseau** | Titre + message mappé (410 / 409 / 404 / 403 / réseau) | Bouton **« Scanner à nouveau »** → reprise du scan |

- L'overlay concerne **uniquement** les résultats d'un aller-retour API (succès + erreurs serveur/réseau).
- Le scan **ne reprend qu'à la fermeture** de l'overlay (anti double-soumission, FR-005/FR-014).
- **401** est une exception : purge de session et retour à `/login` (garde du socle), sans overlay persistant.

## Indice « code non reconnu » (non bloquant)

Un **QR non reconnu** (illisible / version inconnue / étranger à Lumineux) **ne déclenche pas** d'overlay :
un **indice transitoire** non bloquant s'affiche (ex. bandeau/snackbar « Code non reconnu ») et la **caméra
continue** de chercher un code valide (spec US2/AS-5). Une **temporisation anti-répétition** évite de
ré-émettre l'indice en boucle sur le même code resté dans le cadre.

## Règles

- L'écran Scanner est un **onglet** (pas un écran poussé) ; pas de bouton retour.
- Aucun secret (jeton/payload) affiché à aucun moment.
- Mise en arrière-plan → caméra libérée ; retour → réactivée (si permission toujours accordée et session valide).
