# Quickstart & validation — Feature 026 (scan de présence par QR)

Guide de mise en route et de validation du **scan** dans le client Flutter (`mobile/`). Décrit **comment
lancer et vérifier** ; ne contient pas de code d'implémentation.

## Prérequis

- **Flutter SDK** stable ≥ 3.29 (installé : 3.44.5).
- Packages ajoutés (à installer, action réseau à approuver) : `mobile_scanner`, `permission_handler`.
- Configuration plateforme :
  - Android : `<uses-permission android:name="android.permission.CAMERA"/>` dans `AndroidManifest.xml`.
  - iOS : `NSCameraUsageDescription` (FR) dans `ios/Runner/Info.plist`.
- Un **appareil physique** recommandé (caméra réelle) ou un émulateur avec caméra virtuelle.
- L'**API Lumineux** joignable en HTTPS avec **une séance ouverte** ; la **console web** (SPA) mise à jour
  pour projeter le **QR JSON** (`{"v":1,"s":…,"t":…}`) — prérequis in-scope.
- Un **membre** provisionné (compte actif) pour s'authentifier sur le mobile.

## Mise en route

```bash
# depuis mobile/
flutter pub get
flutter run --dart-define-from-file=env/dev.json
```

Se connecter en tant que membre, puis ouvrir l'onglet **Scanner**.

## Scénarios de validation (mappés aux User Stories / SC)

### US1 — Enregistrer sa présence (P1, MVP)
1. Onglet **Scanner** → accorder l'accès caméra → l'aperçu et le cadre de visée s'affichent.
2. Viser le **QR de séance** projeté par le bureau (SPA) → **overlay « Présence enregistrée »** avec
   nom + heure. *(SC-001 < 10 s)*
3. Fermer l'overlay → le scan **reprend**. Re-viser le **même** QR → overlay **« Déjà enregistrée »**,
   **sans doublon**. *(SC-005)*
4. Vérifier qu'à aucun moment le **jeton** n'est affiché ; inspecter les journaux : aucun jeton/payload. *(SC-003)*

### US2 — Échecs de scan (P2)
1. Attendre la **rotation** du jeton puis scanner un **ancien** QR (périmé) → overlay **« Code QR expiré… »**
   (410), re-scan possible. *(SC-002)*
2. Faire **clôturer** la séance côté bureau puis scanner → overlay **« La réunion est terminée… »** (409).
3. Couper le **réseau** puis scanner → overlay **« Réseau indisponible, réessayez »** (aucune file hors ligne).
4. Simuler l'**expiration de session** (jeton échu) puis scanner → **retour à la connexion** + message,
   aucune donnée protégée résiduelle. *(SC-004)*
5. Scanner un **QR étranger** (non Lumineux) ou une charge malformée → overlay **« Code non reconnu »**,
   la caméra **continue** après fermeture.

### US3 — Autorisation caméra (P3)
1. **Refuser** l'accès caméra → message d'orientation + **« Ouvrir les réglages »** ; Accueil/Profil
   restent accessibles. *(SC-006)*
2. Accorder depuis les réglages puis revenir → l'aperçu caméra s'affiche.

## Tests automatisés (Principe III)

```bash
# depuis mobile/
flutter analyze                 # statique, zéro avertissement
flutter test                    # unitaires + widget
flutter test integration_test   # parcours de scan (appareil + API requis ; skip par défaut)
```

**Couverture attendue** :
- Unitaires : `QrPayload.parse` (valide / version inconnue / structure invalide) ; `ScanController`
  (201→créée, 200→déjà présente, 410/409/404/403→erreurs, 401→purge, réseau→erreur, anti double-soumission) ;
  `AttendanceApi.scan` (dio moqué, 201 vs 200, Bearer attaché, corps `{token}`).
- Widget : écran Scanner (permission accordée/refusée, overlay succès/erreur, « Scanner à nouveau ») avec
  scanner **abstrait** (pas de vraie caméra) ; `home_shell` (3e onglet Scanner).
- Intégration : ouverture Scanner → détection simulée → overlay succès → re-scan.

## Côté console web (prérequis)

```bash
# depuis web/
npm test    # le test unitaire de qr-panel vérifie que qrdata = JSON {"v":1,"s":…,"t":…}
```
