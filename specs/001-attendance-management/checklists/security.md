# Security Review Checklist: Gestion de la présence aux réunions

**Purpose**: Revue de sécurité de l'implémentation (Constitution Lumineux v1.0.0, Principe IV) — tâche T072
**Created**: 2026-07-02
**Feature**: [spec.md](../spec.md) · Périmètre : API (US1→US4)

## Validation des entrées (injection, données malformées)

- [x] Toutes les entrées sont validées côté serveur (FluentValidation : `StartSessionValidator`, `ScanRequestValidator`, `OfflineScanBatchValidator`, `ManualAttendanceValidator`)
- [x] Accès aux données via EF Core (requêtes paramétrées) — aucune concaténation SQL à partir d'entrées utilisateur
- [x] Les filtres SQL des index (`status = 'Valid'`, `client_operation_id IS NOT NULL`) sont des littéraux fixes, non issus d'entrées

## Authentification & autorisation

- [x] Authentification JWT Bearer validée côté serveur (issuer, audience, durée, signature)
- [x] Opérations de gestion protégées par la policy `manage_attendance` (démarrage, clôture, ajout manuel, retrait, consultation)
- [x] Scan réservé à un membre authentifié ; contrôle du statut membre actif (FR-025)
- [x] Défense en profondeur : les handlers revérifient le droit/identité même si l'API applique déjà `[Authorize]`
- [x] Principe du moindre privilège : le membre standard ne peut pas gérer les présences (403 vérifié par tests)

## Secrets & données sensibles

- [x] Aucun secret en dur dans le code ; `Jwt:SigningKey` fourni par configuration (user-secrets/env)
- [x] `qrSecret` de session **jamais exposé** dans les DTO/réponses (vérifié par test d'intégration)
- [ ] ⚠️ **Clé JWT de développement** présente dans `appsettings.Development.json` (marquée dev-only) — **la production DOIT fournir `Jwt:SigningKey` via secrets/variables d'environnement**
- [x] Journalisation structurée sans secret ni donnée personnelle superflue (audit = identifiants uniquement)

## Anti-fraude & intégrité

- [x] Jeton QR rotatif (TOTP) : une photo devient invalide après ~`qrStepSeconds` (FR-013)
- [x] Anti-doublon garanti au niveau base (index unique filtré `(session, membre)`) — testé
- [x] Idempotence des scans hors ligne via `clientOperationId` (index unique) — testé
- [x] Heures faisant foi issues de l'horloge serveur (`IClock`) ; heure hors ligne bornée par le serveur

## Gestion des erreurs (fuite d'information)

- [x] Réponses d'erreur homogènes ProblemDetails (RFC 7807)
- [x] Les erreurs 500 sont masquées (message générique) ; détails uniquement dans les journaux serveur
- [x] Codes HTTP explicites (400/401/403/404/409/410) sans divulgation d'information sensible

## Dépendances (audit NuGet)

- [x] `System.Security.Cryptography.Xml` porté à la dernière version disponible (10.0.0) via référence directe
- [ ] ⚠️ **NU1903 persistant** sur `System.Security.Cryptography.Xml` 10.0.0 (dernière version) et `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (tests uniquement)
  - Cause probable : source NuGet secondaire `Solution_PME` (avertissement NU1507 : 2 sources) fournissant une base d'avis obsolète/trop large — la dernière version publiée ne peut être « non corrigée »
  - `SQLitePCLRaw` n'est utilisé qu'en tests (non livré en production)
  - **Recommandations** : configurer le *package source mapping* / une source d'audit unique ; surveiller la sortie d'un correctif ; relancer `dotnet list package --vulnerable` après nettoyage des sources

## Notes

- Les éléments non cochés (⚠️) sont des actions **à traiter avant la mise en production**, non des défauts de l'implémentation fonctionnelle.
- Revue effectuée sur le périmètre API. Les clients (Angular/Flutter) feront l'objet de leurs propres revues (stockage du jeton, permissions caméra/localisation, file hors ligne locale).
