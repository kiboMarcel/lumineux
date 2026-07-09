# Research — App mobile membre : scan de présence par QR · Feature 026

**Date** : 2026-07-09 · **Contexte** : extension du client Flutter `mobile/` (lot M1), consommant l'API
Lumineux existante. Aucune évolution d'API. Cible : membre (compte simple). Décisions tranchées au
`/speckit-clarify` : payload QR **JSON versionné**, résultat en **overlay modal**, **onglet Scanner** permanent.

Format par décision : **Décision / Rationale / Alternatives écartées**.

---

## 1. Composant de scan par caméra

- **Décision** : **`mobile_scanner`** (widget `MobileScanner` + `MobileScannerController`), qui s'appuie
  sur **MLKit Barcode (Android)** et **AVFoundation (iOS)**. Détection de QR en flux, contrôle du cycle de
  vie (start/stop), et `errorBuilder`/`onDetect` exploitables.
- **Rationale** : package le plus maintenu et performant de l'écosystème ; API déclarative Flutter ;
  gère nativement l'aperçu caméra et la détection ; permet de **suspendre** la détection (stop/`onDetect`
  gardé) pour l'anti double-soumission (FR-014).
- **Alternatives écartées** : `qr_code_scanner` (déprécié, non maintenu) ; `google_ml_kit` seul (pas de
  widget d'aperçu intégré, plus de câblage) ; scan « maison » via `camera` + décodage (réinvente MLKit).

## 2. Permission caméra & parcours de refus

- **Décision** : **`permission_handler`** pour lire le **statut** de la permission caméra
  (`Permission.camera.status`), la **demander**, et **ouvrir les réglages** (`openAppSettings()`) en cas de
  refus permanent. L'écran Scanner affiche : aperçu si accordée ; sinon un message d'orientation + bouton
  « Ouvrir les réglages ». Le reste de l'app (Accueil, Profil) reste accessible (FR-011, SC-006).
- **Rationale** : `mobile_scanner` déclenche la demande à l'usage mais n'offre pas un parcours de refus
  explicite ; `permission_handler` donne le contrôle fin exigé par la spec (message clair + réglages).
- **Configuration plateforme** : `AndroidManifest.xml` → `<uses-permission android:name="android.permission.CAMERA"/>` ;
  `ios/Runner/Info.plist` → `NSCameraUsageDescription` (texte FR expliquant l'usage : scanner le QR de séance).
- **Testabilité** : l'accès à `permission_handler` est enveloppé dans une **façade substituable**
  (`CameraPermissionFacade` : `status`/`request`/`openSettings`, exposée en provider), afin de tester les
  états `permissionDenied`/`scanning` sans canal plateforme réel (Principe I/III).
- **Alternatives écartées** : s'appuyer uniquement sur `mobile_scanner` (parcours de refus pauvre) ;
  demander la permission au démarrage de l'app (intrusif, hors contexte) ; appeler `permission_handler`
  directement dans le widget (non testable).

## 3. Contrat de charge du QR (versionné)

- **Décision** : le QR encode un **JSON** `{"v":1,"s":<sessionId:int>,"t":"<token:string>"}`. Côté
  **mobile**, un utilitaire `QrPayload.parse(String)` : décode le JSON, vérifie `v == 1`, `s` entier > 0,
  `t` non vide → `QrPayload(sessionId, token)` ; sinon **échec typé** « code non reconnu » (FR-002/FR-010).
  Côté **SPA (prérequis)**, la `qr-panel` encode `JSON.stringify({ v: 1, s: sessionId, t: token })` au lieu
  de `token` seul.
- **Rationale** : structuré, tolérant à tout caractère du jeton, **évolutif** via `v` (rejet propre des
  versions futures inconnues) ; parsing déterministe des deux côtés.
- **Sécurité** : le parsing client **ne fait pas autorité** — il ne sert qu'à extraire `sessionId`/`token`
  et à rejeter un QR étranger ; c'est **le serveur** qui valide le jeton et l'appartenance (défense en profondeur).
- **Alternatives écartées** : URI custom `lumineux://scan?...` (utile pour un futur deep-link, reporté) ;
  délimité `s|token` (fragile si le jeton contient le séparateur) ; token seul (statu quo — insuffisant,
  le scan a besoin du `sessionId`).

## 4. Consommation de l'API de scan & distinction 201/200

- **Décision** : nouveau `AttendanceApi.scan(int sessionId, String token)` sur le socle `dio` existant :
  `POST /attendance-sessions/{sessionId}/scan` avec `{ "token": ... }`. La réponse **2xx** est acceptée
  (validateStatus 200–299) : **201** → présence **créée**, **200** → **déjà présente**. On lit
  `response.statusCode` pour renseigner `ScanOutcome(created: bool)` et on désérialise `AttendanceResponse`
  (miroir du DTO serveur).
- **Rationale** : reflète exactement le contrat existant (voir `contracts/scan-api-consumption.md`) sans
  évolution d'API ; la distinction 201/200 alimente le libellé de l'overlay (FR-004).
- **Alternatives écartées** : inférer « déjà présent » d'un champ du corps (le statut HTTP est la source
  d'autorité et déjà distinctif).

## 5. Attache du jeton porteur à la route de scan (socle)

- **Décision** : **inverser** la règle de l'intercepteur Bearer (`dio_client.dart`) : attacher
  `Authorization: Bearer` à **toutes** les routes **sauf** les routes d'authentification anonymes connues
  (`/auth/login`, `/auth/activate`, `/auth/forgot-password`, `/auth/reset-password`). Ainsi `/auth/me`,
  `/auth/change-password` **et** `/attendance-sessions/**/scan` (et futurs endpoints protégés) reçoivent le jeton.
- **Rationale** : la liste d'autorisation actuelle (`requiresAuth` = uniquement `me`/`change-password`)
  n'inclut pas le scan ; une **liste de refus** des seules routes anonymes est plus juste et évite d'oublier
  chaque nouvel endpoint protégé. Le 401 déclenche déjà la purge de session (socle).
- **Alternatives écartées** : ajouter `.../scan` à la liste d'autorisation (fonctionne mais fragile à
  chaque nouvel endpoint) ; attacher le jeton à toutes les routes sans exception (fuiterait le jeton aux
  routes anonymes, inutile et moins propre).

## 6. Mapping des erreurs de scan

- **Décision** : réutiliser `ApiException` + `error_messages`. Le serveur renvoie des `ProblemDetails`
  **déjà en français et orientés utilisateur** ; on **affiche le `detail`/`title`** du serveur pour les cas
  métier, avec un contexte de scan :

  | HTTP | Origine serveur | Comportement mobile |
  |------|-----------------|---------------------|
  | 201 / 200 | créé / déjà présent | overlay **succès** (« Présence enregistrée » / « Déjà enregistrée ») |
  | 410 Gone | jeton QR périmé | overlay **erreur** : message serveur (« Code QR expiré… »), re-scan |
  | 409 Conflict | séance close | overlay **erreur** : « La réunion est terminée… » |
  | 404 NotFound | séance introuvable | overlay **erreur** : « Séance introuvable » |
  | 403 Forbidden | membre inactif/inconnu | overlay **erreur** : message serveur |
  | 401 Unauthorized | session expirée | **purge session** → retour connexion (socle) |
  | 400 | jeton vide (ne devrait pas arriver si payload valide) | overlay **erreur** générique |
  | réseau/timeout | hors ligne | overlay **erreur** : « Réseau indisponible, réessayez » |

- **Rationale** : les messages métier serveur sont fiables et localisés ; les afficher évite de dupliquer
  la logique côté client (Principe IV/VIII). On **étend `mapDioException`** pour catégoriser 404/409/410 en
  types dédiés (`notFound`, `conflict`, `gone`) **ou** on conserve le type générique tout en garantissant la
  remontée du `detail` — retenu : ajouter ces catégories pour des messages/UX déterministes et testables.
- **Alternatives écartées** : messages 100 % côté client (risque de désynchronisation avec le serveur) ;
  laisser 404/409/410 en « erreur inconnue » (perte de clarté pour des cas fréquents et attendus).

## 7. Anti double-soumission & cycle de vie de la caméra

- **Décision** : à la **première** détection valide, le `ScanController` passe en `submitting` et le
  Scanner **arrête** la détection (`controller.stop()` / garde `onDetect`). Le **résultat** s'affiche en
  **overlay modal** ; la détection ne **reprend** (`controller.start()`) qu'à la **fermeture** de l'overlay
  (« Fermer » / « Scanner à nouveau »). Sur mise en arrière-plan, la caméra est **libérée** et réactivée au
  retour (via `WidgetsBindingObserver`/lifecycle du contrôleur).
- **Cas « non reconnu » (non bloquant)** : un payload illisible/étranger **ne** passe **pas** par l'overlay
  et **n'arrête pas** la détection ; il émet un **indice transitoire** (bandeau/snackbar « Code non
  reconnu ») avec une **temporisation anti-répétition**, la caméra continuant de chercher (spec US2/AS-5).
  L'overlay modal est **réservé** aux résultats d'un aller-retour API (succès + erreurs serveur/réseau).
- **Rationale** : réalise FR-005/FR-014 et l'edge case « détections répétées » de façon **déterministe et
  testable** (pas de fenêtre de course), aligné sur le design (overlay + « Fermer ») et sur le
  comportement « la caméra continue » attendu pour un QR étranger.
- **Alternatives écartées** : debounce temporel seul (fenêtre de course possible, tests fragiles) ; reprise
  automatique (risque de re-scan involontaire du même code encore dans le cadre).

## 8. Prérequis SPA (encodage du payload)

- **Décision** : modifier `web/.../qr-panel/qr-panel.component.ts` pour poser
  `this.qrData.set(JSON.stringify({ v: 1, s: this.sessionId(), t: res.token }))` au lieu de `res.token`.
  Le jeton reste **non affiché** (seule l'image change). Mettre à jour le test unitaire du composant.
- **Rationale** : prérequis **in-scope** minimal et localisé ; aucun changement d'API ni d'autre composant ;
  le `sessionId` est déjà disponible dans le composant (`input.required<number>()`).
- **Alternatives écartées** : générer le payload côté serveur (évolution API, hors périmètre) ; laisser le
  SPA inchangé (le mobile ne pourrait pas obtenir le `sessionId`).

## 9. Stratégie de tests (Principe III)

- **Décision** :
  - **Unitaires** (`flutter_test` + `mocktail`) : `QrPayload.parse` (valide / version inconnue / structure
    invalide / champs manquants) ; `ScanController` (201→succès créé, 200→déjà présent, 410/409/404/403→
    erreurs mappées, 401→purge, réseau→erreur, garde anti double-soumission) ; `AttendanceApi.scan`
    (`dio` moqué : 201 vs 200, corps `{token}`, Bearer attaché).
  - **Widget** : écran Scanner (états permission accordée/refusée, cadre de visée, overlay de résultat
    succès/erreur, bouton « Scanner à nouveau ») avec `mobile_scanner` **abstrait** derrière une interface
    substituable (pas de vraie caméra en test) ; `home_shell` (présence du 3e onglet Scanner).
  - **Intégration** (`integration_test`) : parcours ouverture Scanner → détection simulée → overlay succès
    → re-scan ; exécuté sur appareil/émulateur (marqué `skip` par défaut : caméra + API requis).
  - **Analyse statique** : `flutter analyze` sans avertissement.
  - **SPA** : test unitaire de `qr-panel` mis à jour (le `qrdata` est le JSON attendu).
- **Rationale** : couvre la logique (parsing, contrôleur, API) et l'UI ; abstraction du scanner pour la
  testabilité (Principe I/III). Test-first pour la logique.
- **Alternatives écartées** : tests manuels seuls (interdit par la constitution) ; dépendre d'une vraie
  caméra en CI (non déterministe).

## 10. Décisions différées (hors M1)

- **Capture hors ligne** + `POST scan/batch` (idempotence `ClientOperationId`) → **M2**.
- **Historique « mes présences »** membre → **M3** (prérequis API probable).
- **Lien profond** depuis un QR/URL, **torche/flash**, saisie manuelle d'un code → évolutions ultérieures.
