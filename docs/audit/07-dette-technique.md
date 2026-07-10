# 07 — Dette technique

> Constats **factuels**, issus de la lecture du code au moment de l'audit. Chacun
> indique localisation, impact concret et piste de remédiation. Classement :
> Critique / Majeur / Mineur.

## Sommaire

- [Points forts constatés](#points-forts-constatés)
- [Critique](#critique)
- [Majeur](#majeur)
- [Mineur](#mineur)
- [Quick wins](#quick-wins)
- [Sources analysées](#sources-analysées)

## Points forts constatés

À contre-courant d'une dette lourde, la base est saine sur plusieurs axes — à
préserver :

- **Aucun secret committé** ; garde-fou de démarrage sur la clé JWT (`Program.cs`).
- **Anti-énumération** et verrouillage sur l'authentification (`LoginHandler.cs`).
- **Secrets hachés** (mots de passe, jetons de reset, secret QR jamais exposé).
- **Couverture de tests réelle** (~83 fichiers backend, ~40 web, ~48 mobile) avec CI
  bloquante.
- **Versionnement NuGet centralisé** et analyseurs .NET activés.
- Commentaires de code traçant les décisions (features, FR-xxx) et corrections de
  vulnérabilités transitives (`System.Security.Cryptography.Xml` forcé).

## Critique

Aucun constat de niveau **Critique** (secret en dur, injection, faille
d'authentification manifeste) n'a été relevé lors de la lecture.

> Réserve : l'audit est statique. Aucune analyse dynamique (pentest, scan de
> dépendances à jour, revue des permissions SQL réelles) n'a été menée.

## Majeur

### MAJ-1 — Absence de pipeline de déploiement et de doc d'exploitation

- **Localisation** : `.github/workflows/` (CI seulement) ; `docs/DEPLOIEMENT.md`
  **supprimé** (statut git) mais toujours référencé dans `appsettings.Development.json`.
- **Impact** : pas de procédure reproductible de mise en production (migrations,
  secrets, hébergement) ; lien de doc mort ; risque d'écart entre environnements.
- **Remédiation** : ajouter un job CD (ou un runbook), reconstituer la doc de
  déploiement, décider où/quand appliquer les migrations EF.

### MAJ-2 — Double mécanisme d'autorisation à la cohérence variable

- **Localisation** : controllers vs handlers. Certains endpoints reposent sur
  `[Authorize(Policy=…)]` **et** `_user.HasPermission(...)` (attendance, members) ;
  d'autres sur `[Authorize]` seul + contrôle applicatif interne (bureau-profiles,
  member-bureau-profiles).
- **Impact** : la matrice réelle des droits n'est pas lisible d'un coup d'œil ; risque
  qu'un futur endpoint oublie le contrôle applicatif là où la policy n'est pas posée.
- **Remédiation** : documenter/normaliser la stratégie (policy **ou** garde
  applicative systématique), idéalement centraliser dans un filtre unique. Ajouter des
  tests d'autorisation pour chaque endpoint sensible.

### MAJ-3 — Provider SQLite référencé sans usage runtime explicite

- **Localisation** : `Directory.Packages.props` (`EntityFrameworkCore.Sqlite`) ;
  `DependencyInjection.cs` n'enregistre que `UseSqlServer`.
- **Impact** : ambiguïté sur la portabilité ; les filtres d'index sont écrits « sans
  quoting » pour rester compatibles SQLite (tests), ce qui contraint le SQL sans que
  la cible SQLite soit visible dans le runtime.
- **Remédiation** : confirmer que SQLite ne sert qu'aux tests d'intégration et le
  documenter ; sinon expliciter la sélection de provider par configuration.

## Mineur

### MIN-1 — Encapsulation hétérogène des entités du domaine

- **Localisation** : `Member.cs` (setters publics) vs `MemberAccount.cs`,
  `AttendanceSession.cs`, `Attendance.cs` (setters privés).
- **Impact** : `Member` peut être muté hors des fabriques, contournant les
  invariants ; anémie partielle. Choix assumé/commenté mais dette latente.
- **Remédiation** : progressivement encapsuler `Member` derrière des méthodes de
  mutation (`UpdateContact`, `Archive`, …).

### MIN-2 — Logique de règle métier hors du domaine

- **Localisation** : comptage « session vide » dans `CancelSessionHandler`/
  `CloseSessionHandler` ; propagation d'heure de fin orchestrée par le handler.
- **Impact** : la règle vit dans la couche Application (le domaine n'a pas le
  décompte). Acceptable mais fuit hors du modèle.
- **Remédiation** : envisager un agrégat `AttendanceSession` chargeant ses présences,
  ou un service de domaine dédié (voir 08).

### MIN-3 — Génération de référence membre potentiellement sujette aux courses

- **Localisation** : `MemberReferenceGenerator.cs` — la séquence dérive d'un
  `COUNT(*)` des membres de l'année.
- **Impact** : deux créations quasi simultanées peuvent calculer la même séquence ;
  l'index unique `members.reference` rejette alors la seconde (erreur plutôt que
  reprise). Le filet existe mais l'expérience peut être un échec transitoire.
- **Remédiation** : séquence base de données dédiée, ou retry sur collision.

### MIN-4 — `Database Entities Documentation.md` obsolète conservé à la racine

- **Localisation** : racine du dépôt.
- **Impact** : bien que marqué obsolète et transformé en redirection, un document de
  modèle faux subsiste au niveau le plus visible.
- **Remédiation** : le supprimer ou le déplacer sous `docs/legacy/`.

### MIN-5 — Répertoires de travail hétérogènes et gabarits résiduels

- **Localisation** : `template_mobile/`, `ai-specs/`, `specs/` (29 dossiers),
  `.specify/`.
- **Impact** : bruit dans le dépôt ; un lecteur découvrant le code peut confondre
  gabarits/specs et code livré.
- **Remédiation** : clarifier dans le README racine ce qui est source de vérité
  (code) vs intention (specs) vs gabarit.

### MIN-6 — Absence de recherche de marqueurs TODO/HACK exploitables

- **Constat** : aucune revue exhaustive des `TODO`/`HACK`/`FIXME` n'a été réalisée
  au-delà de la lecture ciblée ; les commentaires rencontrés sont des **décisions
  documentées**, pas des dettes ouvertes.
- **Remédiation** : lancer un scan `TODO|HACK|FIXME` en CI et suivre le compte.

## Quick wins

1. Reconstituer/lier `docs/DEPLOIEMENT.md` (corrige le lien mort de MAJ-1).
2. Supprimer/déplacer `Database Entities Documentation.md` (MIN-4).
3. Ajouter un scan `TODO|HACK|FIXME` + un scan de vulnérabilités NuGet/npm/pub dans
   la CI (`dotnet list package --vulnerable`, `npm audit`, `flutter pub outdated`).
4. Documenter en une page la matrice endpoint → droit requis (adresse MAJ-2).
5. Confirmer et documenter le rôle de SQLite (MAJ-3).

## Sources analysées

- `src/Lumineux.Api/Controllers/*.cs`, `Program.cs`
- `src/Lumineux.Domain/Entities/Member.cs` (et autres entités)
- `src/Lumineux.Application/AttendanceSessions/CancelSessionHandler.cs`, `CloseSessionHandler.cs`
- `src/Lumineux.Infrastructure/Security/MemberReferenceGenerator.cs`
- `Directory.Packages.props`, `.github/workflows/*.yml`
- `appsettings.Development.json`, statut git du dépôt
</content>
