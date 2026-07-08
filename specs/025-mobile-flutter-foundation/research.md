# Research — Application mobile membre (socle & compte) · Feature 025

**Date** : 2026-07-07 · **Contexte** : premier client mobile Flutter (`mobile/`), consommant l'API
Lumineux existante. Aucune évolution d'API. Cible : membre (compte simple).

Ce document tranche les choix techniques du socle et les inconnues relevées au `plan`. Format par
décision : **Décision / Rationale / Alternatives écartées**.

---

## 1. Plateforme & langage

- **Décision** : **Flutter** (canal stable, Flutter ≥ 3.29 / Dart ≥ 3.7), cibles **Android** et **iOS**
  (smartphone). Un seul code base.
- **Rationale** : imposé par la constitution (« Mobile : Flutter »). Un unique code base couvre les deux
  OS et le futur scan de présence (caméra).
- **Alternatives écartées** : natif iOS/Android séparés (double effort) ; React Native / MAUI (hors
  constitution).

## 2. Navigation & garde d'authentification

- **Décision** : **`go_router`** (routeur déclaratif officiel) avec une **redirection globale** :
  destination `/login` si aucune session valide, `/home` si session établie, `/auth/activate` quand
  l'API impose le changement de mot de passe. Routes : `/login`, `/auth/activate`, `/auth/forgot`,
  `/auth/reset`, `/home`, `/account/change-password`.
- **Rationale** : la redirection centralisée matérialise la garde de session (équivalent mobile des
  `guards` Angular du socle SPA 008) sans dupliquer la logique par écran. `refreshListenable` branché sur
  l'état de session redirige automatiquement à l'expiration/déconnexion.
- **Alternatives écartées** : `Navigator` impératif (garde dispersée, fragile) ; auto_route (codegen
  superflu pour ~6 écrans).

## 3. État applicatif & injection de dépendances

- **Décision** : **`flutter_riverpod`** — un `SessionController` (`Notifier`) porte l'état de session
  (inconnu / en restauration / authentifié / activation requise / anonyme) ; les services (client
  réseau, coffre sécurisé, API auth) sont exposés en providers pour une **substitution en test**.
- **Rationale** : testabilité et séparation nettes (aligné Principe I « en esprit ») ; pas de codegen ;
  `ProviderScope` avec overrides simplifie les tests unitaires et widget.
- **Alternatives écartées** : `provider` + `ChangeNotifier` (viable mais DI moins ergonomique en test) ;
  `bloc` (cérémonie excessive pour ce périmètre).

## 4. Accès réseau (client HTTP + intercepteurs)

- **Décision** : **`dio`** avec deux intercepteurs :
  1. **Bearer** : ajoute `Authorization: Bearer <token>` aux appels protégés (lit le jeton en mémoire
     de session) ; ignore les routes anonymes.
  2. **Erreurs** : convertit toute réponse en `ApiException` typée — `unauthorized` (401),
     `forbidden` (403, avec extraction du `code` métier `password_change_required`),
     `validation` (400 + `ProblemDetails`), `network` (hors ligne / timeout), `server` (5xx). Sur
     **401**, déclenche la **purge de session** (retour connexion).
- **Rationale** : le modèle d'intercepteurs de `dio` répond directement à FR-003/004/005 ; centralise le
  mapping `ProblemDetails` (RFC 7807) comme le fait l'`errorInterceptor` du SPA.
- **Alternatives écartées** : package `http` nu (pas d'intercepteurs → code répété) ; `chopper`
  (codegen inutile).

## 5. Stockage sécurisé du jeton

- **Décision** : **`flutter_secure_storage`** — jeton d'accès + échéance conservés dans le **Keychain
  (iOS)** / **EncryptedSharedPreferences via Keystore (Android)**. Options Android : `encryptedSharedPreferences: true`.
- **Rationale** : FR-007/010 exigent un coffre protégé par l'OS, jamais en clair. Purge à la
  déconnexion et sur 401.
- **Alternatives écartées** : `shared_preferences` (non chiffré — interdit) ; base locale chiffrée
  (surdimensionné pour un seul secret).

## 6. Persistance & restauration de session (vs SPA)

- **Décision** : **persister** le jeton au coffre sécurisé et **restaurer** la session au lancement tant
  que `expiresAt` est dans le futur (pré-vérification locale), l'API restant l'autorité (un 401 ultérieur
  purge). **Pas de jeton de rafraîchissement** (aligné API) : à l'expiration, reconnexion.
- **Rationale** : ergonomie mobile — éviter une reconnexion à chaque lancement (le SPA gardait le jeton
  en mémoire, adapté à un poste bureau, pas à un usage terrain). Décision notée dans la spec (Assumptions).
- **Alternatives écartées** : jeton en mémoire uniquement (reconnexion à chaque ouverture, mauvaise UX
  mobile) ; refresh token (hors périmètre, nécessiterait une évolution API).

## 7. Validation de la politique de mot de passe (côté client)

- **Décision** : un utilitaire `PasswordPolicy` réplique **en confort** les règles publiques de l'API
  (longueur minimale, ≥ 1 lettre et ≥ 1 chiffre) pour un retour immédiat ; **l'API fait foi** (FR-017/018).
- **Rationale** : évite un aller-retour réseau pour une saisie manifestement invalide ; ne duplique pas
  de règle métier « autoritaire » (la validation serveur reste la référence).
- **Alternatives écartées** : aucune validation client (UX dégradée) ; réplication exhaustive des règles
  serveur (risque de dérive — on se limite au strict public documenté).

## 8. Sérialisation JSON des DTO

- **Décision** : **sérialisation manuelle** (`fromJson`/`toJson`) pour le petit ensemble de DTO auth.
- **Rationale** : ~6 DTO seulement ; évite `build_runner`/`json_serializable` et sa chaîne de codegen.
- **Alternatives écartées** : `json_serializable` (outillage lourd pour peu de types).

## 9. Configuration d'environnement (URL de base de l'API)

- **Décision** : URL de base injectée par **`--dart-define`** (`API_BASE_URL`), via un fichier
  `--dart-define-from-file` par profil (dev/prod). Défaut dev documenté.
- **Rationale** : pas de secret en dur dans le binaire ; bascule dev/prod sans recompiler le code.
- **Point d'attention DEV** :
  - **Android émulateur** : `localhost` de l'hôte = `https://10.0.2.2:4311` (pas `localhost`).
  - **iOS simulateur** : `https://localhost:4311` fonctionne.
  - **Certificat dev auto-signé** : en **dev uniquement**, prévoir une exception de confiance TLS
    ciblée (callback de certificat limité au profil dev) ; en **prod**, **HTTPS strict** sans exception
    (FR-019). Ne jamais désactiver la validation TLS en production.
- **Alternatives écartées** : URL codée en dur (non déployable) ; `.env` embarqué (secret dans l'asset).

## 10. Stratégie de tests (Principe III — NON-NÉGOCIABLE)

- **Décision** :
  - **Unitaires** (`flutter_test` + **`mocktail`**) : `SessionController` (restauration, login, activation,
    expiration→purge, logout), `PasswordPolicy`, mapping d'erreurs (`ProblemDetails`/`code`), `AuthApi`
    (avec `dio` moqué via `DioAdapter`/mock), `SecureTokenStore` (moqué).
  - **Widget** : chaque écran (connexion, activation, oublié, réinitialisation, changement) — états
    chargement/erreur/succès, garde de navigation.
  - **Intégration** (`integration_test`) : parcours de bout en bout du cycle de vie (activation →
    home → change → logout), exécuté contre une API accessible (ou doubles).
  - **Analyse statique** : `flutter_lints` + `flutter analyze` sans avertissement.
- **Rationale** : couvre la logique (contrôleurs, politique, mapping) et l'UI ; test-first pour la
  logique métier applicative.
- **Alternatives écartées** : tests manuels seuls (interdit par la constitution).

## 11. Journalisation & secrets (Principe VI)

- **Décision** : journalisation minimale, **jamais** de mot de passe/jeton/mot de passe temporaire dans
  les logs ; les erreurs consignent statut + code métier, pas les corps de requêtes sensibles.
- **Rationale** : FR-010, Principe IV/VI.
- **Alternatives écartées** : logs de requêtes complets (fuite de secrets).

## 12. Décisions différées (hors M0, notées pour la suite)

- **Lien profond (deep link)** depuis l'e-mail de réinitialisation → **M1+** (l'e-mail pointe
  aujourd'hui vers le SPA `:4200`). En M0, saisie/collage manuel du jeton.
- **Payload du QR** (sessionId + token) pour le scan → tranché au `specify` de **M1**.
