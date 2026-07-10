# Audit technique — Solution Lumineux

> Documentation d'audit produite par relecture du code source. Chaque affirmation
> structurante référence sa source (`chemin/fichier.cs`). Les déductions sans preuve
> directe sont marquées `⚠️ Hypothèse — à confirmer`. Les éléments non analysés sont
> listés en fin de document (« Angles morts »).

## Sommaire

1. [Périmètre](#périmètre)
2. [Méthodologie](#méthodologie)
3. [Index des documents](#index-des-documents)
4. [Synthèse express](#synthèse-express)
5. [Angles morts](#angles-morts)
6. [Sources analysées](#sources-analysées)

## Périmètre

L'audit couvre le mono-dépôt **Lumineux**, qui contient trois briques :

| Brique | Emplacement | Stack | Rôle |
|--------|-------------|-------|------|
| **API** (cœur métier) | `src/`, `tests/` | .NET 10 / C# / EF Core 10 / SQL Server | Source de vérité : membres, présences, droits, auth |
| **Console web bureau** | `web/` | Angular 20 (SPA) | Back-office du bureau (consomme l'API) |
| **App mobile membre** | `mobile/` | Flutter / Dart | Scan QR de présence par le membre (consomme l'API) |

L'audit **approfondit l'API .NET** (logique métier, données, sécurité) et **cartographie**
les deux clients (web, mobile) sans relire chaque composant en détail — ils ne portent
aucune règle métier (l'API reste l'unique autorité, confirmé par
`web/src/app/core/session/session-store.ts` et `PO_description.md`).

## Méthodologie

- Inventaire complet du dépôt (arborescence, `.slnx`, `.csproj`, `package.json`, `pubspec.yaml`).
- Lecture en profondeur : entités du domaine, handlers applicatifs, configuration EF, services
  de sécurité, middleware, `Program.cs`, migrations.
- Survol : boilerplate (DTO records, mappers, controllers minces), composants Angular/Flutter.
- Aucune modification du code source. Seules écritures : les fichiers de ce dossier.
- Outils : lecture de fichiers et recherche `ripgrep` uniquement (aucune exécution de build/test).

## Index des documents

| Fichier | Contenu |
|---------|---------|
| [01-vue-ensemble.md](01-vue-ensemble.md) | Résumé métier, stack et versions, prérequis, build/lancement/tests, diagramme de contexte |
| [02-architecture.md](02-architecture.md) | Couches Onion/Clean, projets et références, patterns, points de friction |
| [03-modele-donnees.md](03-modele-donnees.md) | Entités, relations (erDiagram), mapping code↔tables, index, migrations |
| [04-logique-metier.md](04-logique-metier.md) | Flux métier (auth, membres, présences, profils, setup), règles, machines à états |
| [05-integrations.md](05-integrations.md) | API exposée, SMTP, JWT, clients web/mobile, comportements en panne |
| [06-configuration-deploiement.md](06-configuration-deploiement.md) | Configuration par environnement, secrets, CI, hébergement |
| [07-dette-technique.md](07-dette-technique.md) | Constats classés Critique/Majeur/Mineur, localisation, remédiation |
| [08-vue-ddd.md](08-vue-ddd.md) | Relecture DDD : bounded contexts, agrégats, langage ubiquitaire, écarts |

## Synthèse express

- Architecture **Clean/Onion** rigoureuse en 4 projets (Domain, Application, Infrastructure, Api),
  avec un **domaine riche** (entités porteuses d'invariants et de transitions d'état).
- Solution **feature-driven** (28 specs numérotées dans `specs/`), très bien documentée dans le code.
- Sécurité soignée : anti-énumération, verrouillage de compte, jetons QR rotatifs type TOTP,
  hachage PBKDF2, jetons de reset à usage unique hachés, RBAC par claims/policies.
- Couverture de tests substantielle : **373 tests** (`[Fact]`/`[Theory]`) sur 4 projets.

## Angles morts

Éléments **non analysés en profondeur** (donc non garantis par cet audit) :

- **Clients web (Angular) et mobile (Flutter)** : structure cartographiée, composants individuels
  non relus ligne à ligne. Aucune règle métier n'y est censée résider, mais les validations UI et
  la robustesse hors ligne côté mobile (`mobile/lib/features/attendance/`) ne sont pas auditées en détail.
- **Migrations EF** (`src/Lumineux.Infrastructure/Persistence/Migrations/`) : seul le modèle courant
  (via les `Configuration` EF) a été lu ; le contenu SQL de chaque migration n'a pas été vérifié pas à pas.
- **Tests** : comptés et localisés, mais leur pertinence/qualité n'a pas été évaluée cas par cas.
- **Fichiers binaires / générés** : `obj/`, `bin/`, `node_modules/`, `mobile/build/`, dossiers `android/`
  et `ios/` (natif), assets — hors périmètre.
- **`C:\Dev\Lumineux\exports\`** : bibliothèque d'assets de marque (SVG, logos) — non pertinent pour la logique.
- **Repositories de lecture/rapports** (`AttendanceReportRepository`, `MemberReadRepository`, etc.) :
  logique d'agrégation SQL survolée via les handlers, non relue intégralement.
- La **`Database Entities Documentation.md`** à la racine décrit un ancien modèle **TypeORM** (obsolète,
  cf. 07-dette-technique) et ne reflète pas le schéma EF Core réel.

## Sources analysées

- Racine : `Lumineux.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `PO_description.md`,
  `Database Entities Documentation.md`, `.github/workflows/mobile-ci.yml`, `specs/` (liste), `.gitignore`.
- `src/Lumineux.Domain/` : toutes les entités, enums, abstractions (survol).
- `src/Lumineux.Application/` : DI, handlers métier clés, validators, options.
- `src/Lumineux.Infrastructure/` : DbContext, configurations EF, repositories clés, sécurité, jobs, email.
- `src/Lumineux.Api/` : `Program.cs`, middleware, controllers (routes/attributs), `CurrentUser`, appsettings.
- `web/` : `package.json`, arborescence `src/app`, routes, session-store, intercepteur.
- `mobile/` : `pubspec.yaml`, arborescence `lib/`, fichiers d'environnement.
</content>
</invoke>
