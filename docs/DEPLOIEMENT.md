# Déploiement & exploitation — API Lumineux

Guide opérationnel (issu des remédiations de dette technique de l'audit).

## 1. Secrets — clé de signature JWT (C2)

**Aucune clé de signature n'est committée.** Le démarrage échoue (fail-fast, `Program.cs`) si
`Jwt:SigningKey` est absente ou < 32 octets — pour empêcher toute émission de jetons avec une clé vide.

Fournir la clé **hors du dépôt** :

- **Développement** (user-secrets, par machine) :
  ```bash
  dotnet user-secrets set "Jwt:SigningKey" "<clé aléatoire ≥ 32 octets>" --project src/Lumineux.Api
  # exemple de génération : openssl rand -base64 48
  ```
- **Production / CI** : variable d'environnement `Jwt__SigningKey` (double underscore) ou magasin de
  secrets de la plateforme. Ne jamais la placer dans `appsettings*.json`.
- **Rotation** : changer la clé invalide tous les jetons émis (les clients devront se reconnecter).

Autres secrets à fournir hors dépôt de la même façon : `ConnectionStrings__Default`, SMTP
(`Email__Smtp__User` / `Email__Smtp__Password`) si `Email:Provider = "Smtp"`.

## 2. Base de données — migrations (m5, M4)

Le schéma est **code-first (EF Core)** ; les migrations ne sont **pas** appliquées automatiquement au
démarrage (pas de `Database.Migrate()` sur le chemin chaud). **Appliquer explicitement** à chaque
déploiement, API arrêtée :

```bash
dotnet ef database update --project src/Lumineux.Infrastructure --startup-project src/Lumineux.Infrastructure
```

- Les migrations sont sous `src/Lumineux.Infrastructure/Persistence/Migrations/` et sont rejouables sur
  base vierge.
- Vérifier au préalable qu'aucune donnée existante ne viole une nouvelle contrainte d'unicité.

## 3. Amorçage des droits (M3 / M4 — état transitoire)

Deux `IHostedService` s'exécutent au démarrage pour amorcer/migrer les droits :

- **`PermissionBootstrapper`** — écrit dans la table héritée `member_permissions`. ⚠️ **Ces données
  n'alimentent plus les claims du jeton** : la source de vérité des droits est désormais les **profils du
  bureau** (`GetPermissionsAsync` lit uniquement les profils). `member_permissions` et ce bootstrapper
  sont **conservés à titre transitoire** (migration feature 003 → 004).
- **`BureauProfilesBootstrapper`** — migre l'amorçage vers le modèle de profils (source réelle des droits).

**Recommandation** : une fois la migration confirmée sur **tous** les environnements, retirer
`PermissionBootstrapper` (et déprécier `member_permissions`) ou le convertir en commande de migration
explicite hors chemin de démarrage. À ce jour, **ajouter un droit doit se faire côté profils** — un droit
inscrit dans `member_permissions` reste **sans effet** sur les jetons.

## 4. Intégration continue

- `.github/workflows/dotnet-ci.yml` — build + tests **.NET** (`src/`, `tests/`) et **web** (`web/`),
  bloquants sur push/PR (dette C1).
- `.github/workflows/mobile-ci.yml` — analyse + tests **Flutter** (`mobile/`).

## 5. Rappels de sécurité

- Jeton d'accès : durée unifiée sur `Auth:AccessTokenMinutes` (dette m3).
- `TestTokenIssuer` (émission de jetons dev/tests) n'est **pas** enregistré en production (dette m1).
- CORS : origines autorisées via `Cors:AllowedOrigins` (défaut sûr, pas de wildcard credentials).
- **Convention de défense en profondeur (dette m2)** : les endpoints sensibles portent
  `[Authorize(Policy=…)]` **et** le handler re-vérifie `HasPermission(...)` (double contrôle volontaire).
  Toute modification d'une policy doit être répercutée dans le handler correspondant (et inversement).
