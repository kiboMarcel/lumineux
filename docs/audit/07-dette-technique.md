# 07 — Dette technique

## Sommaire

1. [Méthode de classement](#méthode-de-classement)
2. [Critique](#critique)
3. [Majeur](#majeur)
4. [Mineur](#mineur)
5. [Points positifs (dette évitée)](#points-positifs-dette-évitée)
6. [Sources analysées](#sources-analysées)

## Méthode de classement

Constats **factuels** issus de la relecture, classés par impact :
- **Critique** : risque de sécurité, de perte de données ou d'indisponibilité.
- **Majeur** : risque de régression, de dérive ou de coût de maintenance élevé.
- **Mineur** : propreté, cohérence, confort de maintenance.

Aucun `TODO`/`HACK`/`FIXME` n'a été détecté dans le code source .NET relu (le code est
abondamment commenté et référence les specs).

## Statut de remédiation (2026-07-10, commit `1294cc0`)

> Les constats ci-dessous sont conservés **tels que relevés** (registre historique). Le statut de
> traitement à date :

| # | Constat | Statut | Détail |
|---|---------|--------|--------|
| **C1** | Pas de CI .NET | ✅ **Résolu** | `.github/workflows/dotnet-ci.yml` (build + tests .NET **et** web, bloquant sur push/PR) |
| **C2** | Clé JWT committée | ✅ **Résolu** (résiduel) | Clé retirée des `appsettings`, **fail-fast** au démarrage si absente/< 32 o, provisionnée via **user-secrets** (dev) / `Jwt__SigningKey` (prod). ⚠️ **Résiduel** : la clé dev reste dans l'**historique Git** — rotation/`git filter-repo` non effectués (décision à part) |
| **M1** | Référence membre & concurrence | ✅ **Résolu** (minimum) | La violation d'unicité était **déjà traduite** en 409 par `MemberRepository` (pas de 500) ; détection **durcie** via m4. Retry/`SEQUENCE` = amélioration future non retenue |
| **M2** | Doc données obsolète | ✅ **Résolu** | `Database Entities Documentation.md` remplacé par un pointeur vers `03-modele-donnees.md` |
| **M3** | Permissions coexistantes | ✅ **Résolu** (feature 029, commit `71a8f3c`) | Mécanisme hérité **retiré** : table `member_permissions` + entité/config + `HasPermissionAsync`/`AddAsync` supprimés ; source unique = profils ; port renommé `IEffectivePermissionsReader` |
| **M4** | Deux bootstrappers au démarrage | ✅ **Résolu** (feature 029, commit `71a8f3c`) | `PermissionBootstrapper` + `BureauProfilesBootstrapper` **supprimés** (+ config `Auth:Bootstrap`) ; plus aucune migration de droits au démarrage |
| **m1** | `TestTokenIssuer` en prod | ✅ **Résolu** | Enregistré uniquement hors production (`!IsProduction()`) |
| **m2** | Double autorisation | ✅ **Documenté** | Convention de défense en profondeur explicitée (`DEPLOIEMENT.md §5`) |
| **m3** | Clés de durée de jeton | ✅ **Résolu** | Unifié sur `Auth:AccessTokenMinutes` ; `Jwt:ExpirationMinutes` retiré |
| **m4** | Détection unicité par message | ✅ **Résolu** | Helper `DbUniqueViolation` (codes natifs 2601/2627 + repli message), partagé Attendance/Member |
| **m5** | Migrations non documentées | ✅ **Résolu** | `docs/DEPLOIEMENT.md §2` |
| **m6** | Artefacts non nettoyés | ⚪ **Caduc** | `mobile/env/*`, `template_mobile/` désormais **committés intentionnellement** |

Reste ouvert : **C2 résiduel** (clé dev dans l'historique Git — rotation/`git filter-repo`). **M3/M4
résolus** par la feature 029 (`71a8f3c`).
Tests après remédiation : .NET **381 verts** (Domain 64 / App 166 / Infra 23 / Api 128 après retrait des
tests de coexistence obsolètes).

## Critique

### C1 — Aucune CI pour l'API .NET (tests non bloquants)
- **Localisation** : `.github/workflows/` ne contient que `mobile-ci.yml`.
- **Impact** : les **373 tests** backend (et les tests web) ne s'exécutent sur aucun push/PR. Une
  régression sur le cœur métier (auth, présences, droits) peut être fusionnée sans détection. Or c'est
  précisément la couche qui porte toute la logique et la sécurité.
- **Remédiation** : ajouter un workflow `dotnet-ci.yml` (`dotnet build` + `dotnet test Lumineux.slnx`)
  déclenché sur `src/**` et `tests/**`, et un job Angular (`npm ci && npm test`). Rendre le merge bloquant.

### C2 — Clé de signature JWT de développement committée en clair
- **Localisation** : `src/Lumineux.Api/appsettings.Development.json` → `Jwt:SigningKey`.
- **Impact** : la clé est en clair dans l'historique Git. Elle est marquée « dev-only », mais toute
  instance qui la réutiliserait par erreur (ou tout environnement dérivé de ce fichier) permettrait de
  **forger des jetons valides**. Le fichier `.gitignore` ne l'exclut pas.
- **Remédiation** : déplacer la clé de dev vers **user-secrets** (`dotnet user-secrets`), garantir que
  la prod la fournit hors dépôt, et documenter la rotation. Vérifier qu'aucune autre valeur secrète
  n'a été committée dans l'historique.

## Majeur

### M1 — Génération de référence membre sensible à la concurrence
- **Localisation** : `src/Lumineux.Infrastructure/Security/MemberReferenceGenerator.cs`.
- **Impact** : la séquence dérive d'un `COUNT` des membres de l'année. Deux créations simultanées
  peuvent calculer la même séquence ; l'index unique `reference` rejettera la seconde par une
  `DbUpdateException` **non traduite** dans `CreateMemberHandler` (contrairement aux présences), donc
  potentiellement une 500 au lieu d'un message métier, et pas de nouvelle tentative.
- **Remédiation** : soit une séquence/`SEQUENCE` SQL Server dédiée, soit une politique de retry sur
  collision d'unicité de référence, soit un verrou applicatif. Au minimum, traduire l'exception.

### M2 — Documentation de données obsolète et trompeuse
- **Localisation** : `Database Entities Documentation.md` (racine).
- **Impact** : décrit un modèle **TypeORM/TypeScript** (tables `branches`, `zones`, `sponsorships`,
  `ranks`, `provinces`, `continents`…) qui **ne correspond pas** au schéma EF Core réel. Un nouveau
  développeur peut se fier à un modèle qui n'existe pas. Le vrai modèle est dans les `Configuration` EF.
- **Remédiation** : supprimer ou archiver ce fichier, ou le remplacer par la doc de `03-modele-donnees.md`.

### M3 — Deux mécanismes de permissions coexistants et partiellement morts
- **Localisation** : `member_permissions` (table + `MemberPermissionRepository`) vs profils du bureau.
- **Impact** : `GetPermissionsAsync` (source des claims du jeton) lit **uniquement** les profils ;
  `HasPermissionAsync` lit **uniquement** `member_permissions`. Le `PermissionBootstrapper` écrit dans
  `member_permissions` — données qui **n'alimentent plus le jeton** sauf migration par
  `BureauProfilesBootstrapper`. Risque de confusion : un droit ajouté au « mauvais » endroit est sans effet.
- **Remédiation** : acter la source unique (profils), déprécier explicitement `member_permissions`
  et le `PermissionBootstrapper`, ou documenter clairement le rôle de migration transitoire.

### M4 — Deux bootstrappers exécutés à chaque démarrage
- **Localisation** : `PermissionBootstrapper` + `BureauProfilesBootstrapper` (`AddHostedService`).
- **Impact** : logique de migration de données au **démarrage** de l'application (couplage cycle de vie
  applicatif / migration de données). Idempotents, mais fragiles à long terme (ordre, données partielles)
  et opaques en exploitation. Vestige de l'évolution feature 003 → 004.
- **Remédiation** : une fois la migration effectuée sur tous les environnements, retirer ces services
  ou les convertir en tâche de migration explicite (script/commande) hors chemin de démarrage.

## Mineur

### m1 — `TestTokenIssuer` enregistré dans la DI de production
- **Localisation** : `src/Lumineux.Infrastructure/DependencyInjection.cs` (`AddScoped<TestTokenIssuer>()`).
- **Impact** : classe explicitement « dev/tests only » présente dans le conteneur applicatif. Elle
  n'est pas exposée par un endpoint, mais pollue la composition et pourrait être injectée par erreur.
- **Remédiation** : enregistrer conditionnellement (env de test) ou déplacer dans le projet de tests.

### m2 — Contrôle d'autorisation dupliqué (policy + handler)
- **Localisation** : ex. `StartSessionHandler`, `AddManualAttendanceHandler`, `CreateMemberHandler`
  re-vérifient `HasPermission` alors que le controller porte déjà `[Authorize(Policy=…)]`.
- **Impact** : défense en profondeur volontaire, mais duplication à maintenir en cohérence ; un
  changement de policy sans mise à jour du handler (ou l'inverse) crée des divergences subtiles.
- **Remédiation** : documenter la convention, ou centraliser la vérification (attribut/behavior).

### m3 — Incohérence de nommage de la durée du jeton
- **Localisation** : `Jwt:ExpirationMinutes` (utilisé par `TestTokenIssuer`) vs `Auth:AccessTokenMinutes`
  (utilisé par `JwtTokenIssuer` de production). Deux clés distinctes pour la même notion.
- **Impact** : confusion de configuration ; modifier `Jwt:ExpirationMinutes` n'affecte pas les jetons réels.
- **Remédiation** : unifier sur une seule clé, retirer la redondante.

### m4 — Détection de violation d'unicité par inspection de message
- **Localisation** : `AttendanceRepository.IsUniqueViolation` (recherche « UNIQUE »/« duplicate » dans
  le message d'exception).
- **Impact** : fragile (dépend du libellé du fournisseur/langue). Fonctionne SQL Server + SQLite mais
  risque de faux négatifs si le message change.
- **Remédiation** : tester le code d'erreur natif (`SqlException.Number` 2601/2627) plutôt que le texte.

### m5 — Migrations non appliquées automatiquement, non documenté
- **Localisation** : `Program.cs` (absence de `Database.Migrate()`), aucun README d'exploitation.
- **Impact** : oubli possible d'appliquer une migration lors d'un déploiement.
- **Remédiation** : documenter l'étape, ou l'automatiser de façon contrôlée (hors requête chaude).

### m6 — Fichiers de spec/artefacts divers non nettoyés
- **Localisation** : `template_mobile/`, `Starter.md`, fichiers non suivis (`mobile/env/device.json`,
  `usb.json`) visibles dans `git status`.
- **Impact** : bruit de dépôt, ambiguïté sur ce qui fait foi.
- **Remédiation** : ranger dans `docs/` ou ignorer les environnements locaux via `.gitignore`.

## Points positifs (dette évitée)

À souligner car ils réduisent la dette :

- Domaine riche et testé, invariants côté entités (difficiles à contourner).
- Sécurité soignée : anti-énumération, verrouillage, QR TOTP, PBKDF2, jetons reset hachés à usage
  unique, secrets non journalisés, RBAC double, CORS sûr par défaut.
- Central Package Management + `Directory.Build.props` (nullable, analyzers activés).
- Un commentaire du code (`Directory.Packages.props`) documente la **correction d'une vulnérabilité
  transitive** (NU1903 sur `System.Security.Cryptography.Xml`), signe d'une veille sécurité active.
- Couverture de tests importante et miroir des couches.

## Sources analysées

- `.github/workflows/mobile-ci.yml`, `src/Lumineux.Api/appsettings*.json`
- `src/Lumineux.Infrastructure/Security/MemberReferenceGenerator.cs`, `DependencyInjection.cs`,
  `Repositories/{Attendance,MemberPermission}Repository.cs`, `Security/{Permission,BureauProfiles}Bootstrapper.cs`
- `Database Entities Documentation.md`, `git status` initial
</content>
