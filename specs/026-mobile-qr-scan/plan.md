# Implementation Plan: Application mobile membre — scan de présence par QR

**Branch**: `026-mobile-qr-scan` | **Date**: 2026-07-09 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/026-mobile-qr-scan/spec.md`

## Summary

Ajouter au client mobile membre (Flutter, `mobile/`) la **fonction cœur** de présence : un **onglet
Scanner** ouvrant la caméra pour détecter le **QR rotatif** projeté par le bureau, en extraire la charge
**JSON versionnée** `{"v":1,"s":<sessionId>,"t":"<token>"}`, puis **enregistrer la présence** via
l'endpoint existant `POST /api/v1/attendance-sessions/{sessionId}/scan` (corps `{ token }`, `[Authorize]`
membre) — **sans aucune évolution d'API**. Le résultat s'affiche dans un **overlay modal** (succès
« enregistrée » / « déjà présente », ou erreur mappée), la détection étant **suspendue** jusqu'à sa
fermeture (anti double-soumission). Prérequis **in-scope** côté console web (SPA, feature 014) : la
`qr-panel` encode désormais le **payload JSON** au lieu du jeton seul. Approche : nouveau module
`features/attendance/` (couches données/application/présentation), réutilisant le socle réseau/session du
lot M0 ; scan via `mobile_scanner`, permission caméra via `permission_handler` ; tests unitaires + widget
+ intégration (Principe III).

## Technical Context

**Language/Version** : Dart ≥ 3.7 · Flutter stable ≥ 3.29 (installé : 3.44.5) — **client existant** `mobile/`.

**Primary Dependencies** : existantes — `flutter_riverpod`, `go_router`, `dio`, `flutter_secure_storage`.
**Nouvelles** : `mobile_scanner` (détection QR via caméra, MLKit Android / AVFoundation iOS),
`permission_handler` (statut de permission caméra + ouverture des réglages). **Dev** : `flutter_test`,
`mocktail`, `integration_test`.

**Storage** : **aucune** nouvelle persistance. Le jeton n'est **jamais** persisté ; **pas** de file de
scans hors ligne en M1. Le jeton de session reste au coffre sécurisé (socle M0).

**Testing** : `flutter test` (unitaires + widget), `integration_test` (parcours de scan), `flutter analyze`.

**Target Platform** : Android (API 24+) et iOS (14+), smartphone avec caméra.

**Project Type** : application mobile (extension du client `mobile/`) + **petit prérequis** sur la SPA `web/`.

**Performance Goals** : détection de code **quasi-instantanée** ; parcours ouverture Scanner → confirmation
en **< 10 s** (SC-001) ; transitions fluides (60 fps).

**Constraints** : **HTTPS exclusif** (exception TLS dev-only héritée du socle) ; **jeton jamais**
affiché/journalisé/persisté ; **aucune règle métier** dupliquée (serveur autorité) ; **pas** de capture
hors ligne (M2) ; français ; anti double-soumission via overlay modal suspendant la détection.

**Scale/Scope** : 1 nouvel **onglet** + écran Scanner + overlay de résultat ; **1 endpoint** consommé
(`.../scan`) ; **1 contrat** nouveau (payload QR versionné, partagé SPA↔mobile) ; 1 prérequis SPA
(`qr-panel`) ; 1 persona (membre).

## Constitution Check

*GATE : doit passer avant Phase 0 ; re-vérifié après Phase 1.*

| Principe | Applicabilité à M1 | Verdict |
|----------|--------------------|---------|
| **I. Onion & séparation des couches** | Appliqué **en esprit** : nouveau module `features/attendance/` en couches **présentation** (écran Scanner, overlay) → **application** (contrôleur de scan, parsing/validation du payload) → **données** (client API scan). Accès caméra et réseau **encapsulés** derrière des abstractions substituables en test. Aucune règle métier dans les widgets. | ✅ PASS |
| **II. Code-First & intégrité BD** | **N/A** : aucune base ni migration ; aucun stockage local nouveau. | ✅ N/A |
| **III. Tests en premier (NON-NÉGOCIABLE)** | Logique (parsing/validation payload, contrôleur de scan : succès 201/200, mapping d'erreurs 409/410/404/403/401/réseau, anti double-soumission) couverte par **tests unitaires** ; écran et overlay par **tests widget** ; parcours par **test d'intégration**. Test-first pour la logique. CI verte. | ✅ PASS |
| **IV. Sécurité par défaut** | Jeton **jamais** affiché/journalisé/persisté ; **HTTPS** ; **permission caméra** demandée à l'usage, refus géré ; **serveur autorité** (valide jeton, séance, appartenance, unicité — le client ne re-valide rien) ; 401 → purge session (socle). Entrées (payload QR) validées côté client uniquement pour **rejeter** un format inconnu (défense en profondeur), jamais pour autoriser. | ✅ PASS |
| **V. Contrats d'API explicites** | Consomme le contrat **existant** `.../scan` (inchangé). Introduit **un nouveau contrat inter-clients versionné** : la **charge du QR** `{"v":1,"s","t"}`, documentée dans `contracts/` et **produite** par la SPA (prérequis), **consommée** par le mobile. Champ `v` = évolutivité. | ✅ PASS |
| **VI. Traçabilité & observabilité** | La traçabilité des scans (opération/refus, horodatage serveur) reste **côté serveur** (déjà en place : `IAuditLogger`). Côté mobile : journalisation **minimale sans secret** (statut + issue), jamais le jeton ni le payload. | ✅ PASS |

**Résultat** : aucun écart. Section *Complexity Tracking* laissée vide.

> Note workflow/constitution : l'implémentation nécessitera l'installation des packages **`mobile_scanner`**
> et **`permission_handler`** (appel réseau `flutter pub get`) → **approbation explicite requise** avant
> `/speckit-implement`. La configuration plateforme (permission caméra `AndroidManifest` /
> `NSCameraUsageDescription` iOS) est un ajout local, sans incidence sur `plan`/`tasks`.

## Project Structure

### Documentation (this feature)

```text
specs/026-mobile-qr-scan/
├── plan.md              # Ce fichier
├── research.md          # Décisions techniques (Phase 0)
├── data-model.md        # Entités/états client (Phase 1)
├── quickstart.md        # Guide de validation (Phase 1)
├── contracts/           # Contrats (Phase 1)
│   ├── qr-payload.md          # Charge JSON versionnée (SPA ⇄ mobile)
│   ├── scan-api-consumption.md# Endpoint /scan consommé (existant)
│   └── navigation.md          # 3e onglet + overlay de résultat
└── tasks.md             # Phase 2 (/speckit-tasks — non créé ici)
```

### Source Code (repository root)

```text
mobile/                                  # client Flutter existant (M0)
├── lib/
│   ├── core/
│   │   └── network/dio_client.dart      # MODIF : joindre le Bearer à /attendance-sessions/** (scan)
│   ├── features/
│   │   ├── attendance/                  # NOUVEAU module
│   │   │   ├── data/
│   │   │   │   ├── scan_dtos.dart        # AttendanceResponse (miroir), ScanResult(created)
│   │   │   │   └── attendance_api.dart   # POST /attendance-sessions/{id}/scan
│   │   │   ├── application/
│   │   │   │   ├── qr_payload.dart       # parse+valide {"v":1,"s","t"} → QrPayload | erreur
│   │   │   │   ├── scan_controller.dart  # Notifier: idle/submitting/result ; anti double-soumission
│   │   │   │   └── providers.dart        # DI (attendanceApi, scanController)
│   │   │   └── presentation/
│   │   │       ├── scanner_screen.dart   # caméra (mobile_scanner) + cadre de visée + permission
│   │   │       └── scan_result_overlay.dart # overlay modal succès/erreur (Fermer/Scanner à nouveau)
│   │   └── home/presentation/home_shell.dart # MODIF : 3e onglet « Scanner »
│   ├── test/ (attendance/*, core/*)     # unitaires + widget
│   └── integration_test/scan_test.dart  # parcours de scan (skip par défaut : caméra/API requis)
├── android/app/src/main/AndroidManifest.xml # MODIF : permission CAMERA
└── ios/Runner/Info.plist                # MODIF : NSCameraUsageDescription

web/                                     # console Angular (SPA) — PRÉREQUIS in-scope
└── src/app/features/attendance/session-run/qr-panel/qr-panel.component.ts # MODIF : encoder le JSON
```

**Structure Decision** : nouveau module **feature-first** `features/attendance/` dans le client `mobile/`
existant, réutilisant le socle réseau/session (M0) et le design system (refonte). Une **petite modification
du socle** (`dio_client` : attacher le Bearer à la route de scan) et un **prérequis SPA** (encodage du
payload QR) sont les seuls changements hors du nouveau module. L'API `.NET` sous `src/` est **inchangée**.

## Complexity Tracking

*Aucun écart à la constitution — section vide.*
