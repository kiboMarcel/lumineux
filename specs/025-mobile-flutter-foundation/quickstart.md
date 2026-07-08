# Quickstart & validation — Feature 025 (app mobile membre, socle & compte)

Guide de mise en route et de validation du **client Flutter** (`mobile/`). Ne contient pas de code
d'implémentation ; il décrit **comment lancer et vérifier** la fonctionnalité.

## Prérequis

- **Flutter SDK** (canal stable ≥ 3.29) installé — *installation à approuver (téléchargement réseau)*.
- Un **émulateur Android** ou **simulateur iOS** (ou un appareil physique).
- L'**API Lumineux** joignable en HTTPS, avec **CORS/accès** OK, et au moins **un membre** provisionné
  (référence + mot de passe défini) et **un membre** avec mot de passe **temporaire** (pour l'activation).
- URL de base de l'API selon la cible :
  - Android émulateur : `https://10.0.2.2:4311`
  - iOS simulateur : `https://localhost:4311`

## Mise en route

```bash
# depuis mobile/
flutter pub get

# Lancer avec le profil de dev (URL de base injectée)
flutter run --dart-define-from-file=env/dev.json
```

> En **dev uniquement**, le certificat auto-signé de l'API est accepté via une exception TLS **ciblée**
> (profil dev). En **prod**, HTTPS strict, aucune exception.

## Scénarios de validation (mappés aux User Stories / SC)

### US1 — Connexion & session (P1, MVP)
1. Lancer l'app → écran **Connexion**. Saisir une référence + mot de passe **valides** → **Accueil**
   affichant l'identité. *(SC-001 partiel)*
2. Fermer/rouvrir l'app (jeton non expiré) → **session restaurée** sans ressaisie. *(SC-005)*
3. Saisir des identifiants **invalides** → message clair, on reste sur Connexion. *(AS-2)*
4. Couper le réseau, tenter la connexion → message **« réseau indisponible »**. *(edge case)*
5. Simuler l'expiration (jeton échu) puis action → **retour Connexion** + message, aucune donnée
   protégée résiduelle. *(SC-004)*

### US2 — Activation 1re connexion (P2)
1. Se connecter avec un **mot de passe temporaire** → bascule automatique **Activation**
   (référence pré-remplie). *(AS-1)*
2. Définir un nouveau mot de passe **conforme** → **Accueil**. *(SC-001)*
3. Saisir un mot de passe **non conforme** → **retour de validation immédiat**, soumission refusée,
   aucun appel réseau. *(SC-007)*

### US3 — Mot de passe oublié / réinitialisation (P3)
1. Depuis Connexion → **Mot de passe oublié**, saisir une référence → **message générique** (identique
   compte existant/inexistant). *(SC-006)*
2. Récupérer le **jeton** dans l'e-mail, écran **Réinitialisation** : jeton + nouveau mot de passe
   conforme → succès → se connecter avec le nouveau. *(SC-002)*
3. Jeton **invalide/expiré** → message d'erreur clair. *(AS-3)*

### US4 — Changement & déconnexion (P4)
1. Connecté → **Changer le mot de passe** (ancien + nouveau conforme) → confirmation.
2. **Déconnexion** → retour Connexion, jeton **effacé** du coffre. *(SC-003)*
3. Relancer l'app → Connexion (aucune session restaurée). *(SC-005)*

## Vérifications de sécurité (SC-003, SC-004, SC-006)
- Inspecter les journaux : **aucun** mot de passe/jeton/mot de passe temporaire en clair.
- Vérifier que le jeton n'existe **que** dans le coffre sécurisé (pas de `shared_preferences` en clair).
- Confirmer le comportement **anti-énumération** identique sur « oublié ».

## Tests automatisés (Principe III)

```bash
# depuis mobile/
flutter analyze                 # statique, zéro avertissement
flutter test                    # unitaires + widget
flutter test integration_test   # parcours de bout en bout (émulateur requis)
```

**Couverture attendue** :
- Unitaires : `SessionController` (restauration, login, `password_change_required`, expiration→purge,
  logout), `PasswordPolicy`, mapping d'erreurs, `AuthApi` (dio moqué), `SecureTokenStore` (moqué).
- Widget : les 5 écrans (états chargement/erreur/succès + gardes).
- Intégration : cycle de vie activation → home → change → logout.
