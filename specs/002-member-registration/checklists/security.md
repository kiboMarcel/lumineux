# Security Review Checklist: Ajout d'un nouveau membre

**Purpose**: Revue de sécurité (Constitution Lumineux v1.0.0, Principe IV) — tâche T041
**Created**: 2026-07-03
**Feature**: [spec.md](../spec.md) · Périmètre : API (US1→US3)

## Validation des entrées

- [x] Toutes les entrées validées côté serveur (FluentValidation : `CreateMemberValidator`, `UpdateMemberValidator`)
- [x] Contact obligatoire (mobile OU e-mail), format e-mail vérifié
- [x] Existence des références (antenne, civilité, pays, ville, district, introducteur) validée (FR-005)
- [x] Accès aux données via EF Core (requêtes paramétrées) — aucune concaténation SQL

## Authentification & autorisation

- [x] Toutes les opérations membres protégées par la policy `manage_members` (distincte de `manage_attendance`)
- [x] Compte provisionné **sans aucun droit de gestion** (moindre privilège, FR-012 — testé)
- [x] Défense en profondeur : le handler revérifie le droit même si l'API applique `[Authorize]`

## Secrets & données sensibles

- [x] Mot de passe stocké **uniquement haché** (`PasswordHasher<T>`, PBKDF2) — jamais en clair (FR-016)
- [x] `passwordHash` **jamais exposé** dans les réponses (vérifié par test d'intégration)
- [x] Mot de passe temporaire **jamais journalisé** (senders + audit consignent sans secret)
- [x] Mot de passe temporaire exposé **uniquement** dans la réponse de création en repli remise-bureau, une seule fois (FR-011 / SC-005)
- [x] Secrets SMTP fournis par configuration/secrets, pas en dur (`Email:Smtp`)
- [ ] ⚠️ En production, fournir `Jwt:SigningKey` et les secrets SMTP via user-secrets / variables d'environnement

## Intégrité des données

- [x] Référence membre unique (index unique) = identifiant de connexion
- [x] Unicité des contacts d'un membre **actif** (index unique filtré e-mail/mobile) — refus applicatif + garantie base (FR-008, testé)
- [x] Homonymes tolérés uniquement après confirmation explicite du bureau (FR-007, testé)
- [x] Création membre + compte **atomique** (une seule transaction, FR-006)

## Observabilité (Constitution VI)

- [x] Création et correction tracées (audit `createdt/by`, `updatedt/by` via intercepteur)
- [x] Refus consignés (droit manquant, doublon, contact déjà utilisé) sans donnée sensible
- [x] Résultat d'envoi d'e-mail journalisé (succès/échec) sans mot de passe

## Dépendances (audit NuGet)

- [ ] ⚠️ **NU1903** persistant sur `System.Security.Cryptography.Xml` (dernière version) — cause probable : source NuGet secondaire `Solution_PME` avec base d'avis obsolète (cf. revue feature 001). `SmtpClient` (System.Net.Mail) marqué obsolète (SYSLIB0014) — acceptable ; envisager MailKit ultérieurement.
- **Recommandations** : configurer le *package source mapping* / une source d'audit unique ; relancer `dotnet list package --vulnerable`.

## Notes

- Les éléments ⚠️ sont des actions **avant mise en production**, non des défauts de l'implémentation.
- Le parcours de connexion / changement de mot de passe (activation du compte) est **hors périmètre**
  (feature d'authentification) — sa propre revue de sécurité sera nécessaire (verrouillage, tentatives, etc.).
