# Phase 0 — Research & Décisions techniques

**Feature**: Gestion de la présence aux réunions · **Date**: 2026-07-02

Ce document consigne les décisions techniques prises pour lever les inconnues du contexte, en
cohérence avec la Constitution Lumineux v1.0.0. Aucune inconnue `NEEDS CLARIFICATION` ne subsiste :
les 3 clarifications métier ont été tranchées dans la spec (jeton rotatif, reporting reporté, file
hors ligne), et les choix ci-dessous couvrent les aspects techniques.

---

## 1. Plateforme & version .NET

- **Décision** : .NET 10 (LTS), C# 14, EF Core 10, ASP.NET Core Web API.
- **Rationale** : version LTS la plus récente au moment du démarrage (support long) ; EF Core 10
  aligné pour le code-first SQL Server ; projet neuf donc aucune contrainte de rétrocompatibilité.
- **Alternatives** : .NET 8 (LTS précédent) — écartée car support plus court pour un projet neuf ;
  versions STS — écartées (fenêtre de support trop courte pour un socle « scalable »).

## 2. Architecture Onion — matérialisation

- **Décision** : 4 assemblies (`Domain`, `Application`, `Infrastructure`, `Api`) + 4 projets de
  tests. Le `Domain` ne référence aucun package externe orienté infrastructure ; l'inversion de
  dépendance passe par des interfaces (ports) définies dans `Domain`/`Application` et implémentées
  en `Infrastructure`.
- **Rationale** : rend la règle de dépendance vérifiable à la compilation (Constitution I) et isole
  le métier des frameworks.
- **Alternatives** : projet unique en couches par dossiers — écartée car la règle de dépendance n'y
  est pas garantie ; Vertical Slice pur — écartée pour rester lisible et conforme à l'attendu Onion.
- **Note** : l'orchestration des cas d'usage se fait par services applicatifs explicites (option
  MediatR possible mais non imposée, pour éviter une dépendance structurante prématurée — YAGNI).

## 3. Code QR à jeton rotatif (anti-fraude — FR-013)

- **Décision** : jeton dérivé façon **TOTP** d'un secret propre à la session.
  - À la création de session, le serveur génère un `QrSecret` aléatoire (≥ 160 bits) stocké chiffré/à
    accès restreint, jamais exposé aux clients.
  - Le QR affiché encode un jeton = `f(QrSecret, fenêtre_temps_courante)` où la fenêtre est un pas de
    temps court (défaut **30 s**, configurable). Le bureau rafraîchit le QR à chaque pas.
  - À la validation d'un scan, le serveur recalcule les jetons pour la fenêtre courante ± 1 pas
    (tolérance de dérive) et accepte si correspondance ; sinon rejet `410 Gone`/`409` avec message.
- **Rationale** : une photo du QR devient invalide en ~30 s, ce qui neutralise le partage à distance
  sans dépendre du GPS ni compliquer l'expérience. Le secret ne quitte jamais le serveur.
- **Alternatives** : QR statique — écarté (partageable par photo) ; QR rotatif + géofencing GPS —
  écarté pour cette itération (droits de localisation, précision, complexité mobile ; réactivable
  plus tard) ; jeton signé JWT court dans le QR — viable mais QR plus volumineux et révocation moins
  simple que TOTP côté session.
- **Sécurité** : le jeton ne porte aucune donnée personnelle ; l'identité du membre provient du
  contexte authentifié du scan, pas du QR.

## 4. Scan hors ligne — file locale & synchronisation (FR-023)

- **Décision** : contrat de **synchronisation idempotent par lot**.
  - Le mobile met en file chaque scan avec `clientArrivalTime` (heure locale de l'appareil) et un
    identifiant d'opération client (`clientOperationId`, ex. GUID).
  - À la reconnexion, le mobile envoie le lot ; le serveur crée les présences manquantes et renvoie
    un statut par élément (créé / déjà présent / rejeté).
  - L'anti-doublon repose sur l'unicité `(SessionId, MemberId)` **et** sur `clientOperationId` pour
    absorber les rejeux réseau (FR-023a).
- **Heure faisant foi** : l'**heure d'arrivée** conserve l'instant réel du scan. Comme le scan hors
  ligne ne peut être horodaté par le serveur, on retient `clientArrivalTime` **borné** par le serveur
  (rejeté si hors de la plage plausible de la session, p. ex. antérieur au début ou postérieur à la
  synchro), pour concilier fidélité et robustesse face à une horloge décalée (edge case spec).
- **Synchro après clôture (FR-023b)** : accepté si `clientArrivalTime` < heure de clôture, sinon
  rejeté et signalé dans la réponse du lot.
- **Rationale** : robuste en zone à faible couverture (contexte antennes) tout en préservant
  l'intégrité et l'anti-doublon.
- **Alternatives** : refus immédiat hors ligne — écarté par décision métier ; horodatage serveur à
  la synchro — écarté car fausserait l'heure d'arrivée réelle.

## 5. Authentification & autorisation (socle transverse — FR-018, Constitution IV)

- **Décision** : **JWT Bearer** validé côté serveur ; autorisation par **policy** basée sur un droit
  `manage_attendance` porté par les membres du bureau (claim/rôle). Les endpoints de scan exigent un
  membre authentifié ; les endpoints de gestion exigent la policy `manage_attendance`.
- **Rationale** : standard pour une API consommée par SPA + mobile ; découple l'identité du métier.
- **Portée dans cette itération** : mise en place du **socle** de validation JWT et des policies.
  L'émission des jetons / le cycle de vie complet des comptes et des profils de droits relèvent des
  fonctionnalités d'authentification et de gestion du bureau (dépendances déclarées dans la spec).
  Pour permettre le développement et les tests, un mécanisme d'émission minimal (ou un fournisseur de
  jetons de test) est prévu côté `Infrastructure`/tests, sans préjuger de la solution définitive.
- **Alternatives** : sessions à cookies — moins adapté au mobile ; clés d'API — inadapté à des
  utilisateurs finaux.

## 6. Source de temps serveur (FR-009, Constitution VI)

- **Décision** : abstraction `IClock` (port Domain) fournissant l'heure UTC ; implémentation
  `SystemClock` en Infrastructure. Les heures sont stockées en **UTC** et converties à l'affichage.
- **Rationale** : rend le temps injectable/testable et garantit que les heures faisant foi ne
  dépendent pas de l'horloge client.
- **Alternatives** : `DateTime.UtcNow` en dur — écarté (non testable, viole l'inversion de dépendance).

## 7. Concurrence & anti-doublon (FR-010, SC-003)

- **Décision** : contrainte d'unicité SQL `(SessionId, MemberId)` sur les présences **valides** +
  gestion applicative de la violation d'unicité (retour « déjà présent » sans erreur 500). Opérations
  de scan idempotentes ; clôture propageant l'heure de fin en une transaction.
- **Rationale** : garantit l'absence de doublon même sous forte concurrence (affluence d'arrivée).
- **Alternatives** : contrôle applicatif seul (lecture puis écriture) — écarté (fenêtre de course).

## 8. Journalisation & observabilité (Constitution VI)

- **Décision** : Serilog en journalisation structurée ; identifiant de corrélation par requête ;
  journalisation des opérations sensibles (ouverture/clôture, scan, ajout, retrait) et des refus
  (droit manquant, jeton invalide, session close). Aucun secret ni donnée personnelle superflue.
- **Rationale** : diagnostic et sécurité (FR-019, FR-020).
- **Alternatives** : logging console non structuré — écarté (non exploitable à l'échelle).

## 9. Contrats d'API & gestion d'erreurs (Constitution V)

- **Décision** : REST sous `/api/v1`, DTO dédiés, réponses d'erreur homogènes au format
  **ProblemDetails** (RFC 7807), codes HTTP explicites (400 validation, 401/403 auth, 404, 409/410
  conflit/jeton périmé, 422 règle métier selon convention retenue). OpenAPI généré (Swashbuckle) et
  aligné sur `contracts/openapi.yaml`.
- **Rationale** : contrats stables et lisibles pour SPA et mobile, sans fuite d'information sensible.
- **Alternatives** : erreurs ad hoc — écarté (incohérent, risque de fuite).

## 10. Stratégie de tests (Constitution III)

- **Décision** : unitaires sur `Domain` (invariants, transitions d'état de session/présence) et
  `Application` (cas d'usage avec ports mockés) — écrits avant/pendant l'implémentation ; intégration
  sur `Infrastructure` (repositories/migrations, base éphémère) et `Api` (WebApplicationFactory) pour
  les parcours de bout en bout. CI bloquante sur échec.
- **Rationale** : couvre le cœur métier sans base (rapide, déterministe) et sécurise les contrats.
- **Alternatives** : tests d'intégration uniquement — écarté (lents, ne protègent pas les invariants).
