# Audit technique — Solution Lumineux

> Documentation d'audit produite par lecture directe du code. Chaque affirmation
> structurante référence sa source (`chemin/fichier.cs`). Les déductions sont
> marquées `⚠️ Hypothèse — à confirmer`. Les zones non analysées figurent dans
> « Angles morts ».

## Sommaire

| # | Document | Contenu |
|---|----------|---------|
| — | [README.md](README.md) | Index, périmètre, méthodologie, angles morts |
| 01 | [01-vue-ensemble.md](01-vue-ensemble.md) | Résumé métier, stack, build/lancement, contexte |
| 02 | [02-architecture.md](02-architecture.md) | Couches, projets, dépendances, patterns |
| 03 | [03-modele-donnees.md](03-modele-donnees.md) | Entités, relations, EF Core, migrations |
| 04 | [04-logique-metier.md](04-logique-metier.md) | Flux métier, règles, machines à états |
| 05 | [05-integrations.md](05-integrations.md) | API exposées, e-mail, clients web/mobile |
| 06 | [06-configuration-deploiement.md](06-configuration-deploiement.md) | Config, secrets, CI, hébergement |
| 07 | [07-dette-technique.md](07-dette-technique.md) | Constats classés, risques, quick wins |
| 08 | [08-vue-ddd.md](08-vue-ddd.md) | Relecture Domain-Driven Design |

## Périmètre audité

- **Backend .NET** : solution `Lumineux.slnx` (4 projets `src/` + 4 projets `tests/`).
- **Console web** : SPA Angular sous `web/`.
- **Application mobile** : client Flutter sous `mobile/`.
- **Automatisation** : workflows GitHub Actions sous `.github/workflows/`.

## Méthodologie

1. Cartographie du dépôt (arborescence, fichiers projet, versions cibles).
2. Identification des points d'entrée (`Program.cs`, controllers, `BackgroundService`,
   `main.ts` Angular, `main.dart` Flutter).
3. Lecture en profondeur des zones à forte logique métier : entités du domaine,
   handlers de cas d'usage `Application/`, services de sécurité, configurations EF.
4. Survol du boilerplate (DTOs, mappers, migrations générées).

Les commandes shell n'ont servi qu'à l'inspection (listing, lecture, comptage).
Aucun code source n'a été modifié ; les seules écritures sont ces fichiers de doc.

## Chiffres clés (constatés)

- Backend : 4 projets applicatifs, ~13 controllers, ~50 handlers de cas d'usage.
- Tests backend : ~83 fichiers de tests répartis sur les 4 projets de test
  (`tests/`), exécutés par la CI (`.github/workflows/dotnet-ci.yml`).
- Tests web : ~40 fichiers `*.spec.ts` (`web/src`).
- Tests mobile : ~48 fichiers Dart (`mobile/test`, `mobile/integration_test`).
- 11 migrations EF Core (`src/Lumineux.Infrastructure/Persistence/Migrations/`).
- 29 dossiers de spécifications fonctionnelles versionnées (`specs/001…029`).

## Angles morts (non analysés en profondeur)

- **Fichiers générés / binaires** : `obj/`, `bin/`, `web/dist/`, `mobile/build/`,
  `web/node_modules/` — non lus (artefacts de compilation).
- **Migrations EF `*.Designer.cs` et `AppDbContextModelSnapshot.cs`** : parcourus
  seulement pour recouper le schéma ; le modèle réel a été lu depuis les
  `Configurations/` (source de vérité).
- **Détail exhaustif de la SPA Angular** : routes, gardes, intercepteurs, API
  clients et modèles ont été lus ; le rendu HTML/CSS de chaque composant et les
  `*.spec.ts` n'ont pas été lus ligne à ligne.
- **Détail exhaustif du client Flutter** : architecture (features, contrôleurs
  Riverpod, file hors ligne, sync) lue ; l'intégralité des écrans de présentation
  et la configuration native `android/` `ios/` n'ont pas été lues ligne à ligne.
- **`template_mobile/`** : dossier de gabarit non audité (hors périmètre applicatif).
- **`specs/`, `ai-specs/`, `PO_description.md`** : lus partiellement comme contexte
  métier ; ils ne font pas foi face au code (pris comme intention, pas comme état).
- **Contenu réel de la base** : aucune base n'a été interrogée ; le schéma décrit
  provient du code EF Core, pas d'une instance vivante.

## Sources analysées (transverses)

- `Lumineux.slnx`, `Directory.Build.props`, `Directory.Packages.props`
- `Starter.md`, `PO_description.md`, `Database Entities Documentation.md`
- `.github/workflows/dotnet-ci.yml`, `.github/workflows/mobile-ci.yml`
</content>
</invoke>
