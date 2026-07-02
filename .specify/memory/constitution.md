<!--
SYNC IMPACT REPORT
==================
Version change: (aucune / template) → 1.0.0
Type de changement: MAJOR (ratification initiale — remplacement complet du template)

Principes définis (nouveaux):
  1. Architecture Onion & séparation stricte des couches
  2. Code-First & intégrité de la base de données
  3. Tests en premier & couches métier vérifiables (NON-NÉGOCIABLE)
  4. Sécurité par défaut
  5. Contrats d'API explicites & cohérents
  6. Traçabilité, audit & observabilité

Sections ajoutées:
  - Contraintes technologiques & standards
  - Workflow de développement (Spec-Driven)
  - Governance

Templates / artefacts vérifiés:
  - .specify/templates/plan-template.md ✅ (la « Constitution Check » dérive de ce fichier ;
    aucun placeholder à modifier — les gates seront énumérés lors de /speckit-plan)
  - .specify/templates/spec-template.md ✅ (aligné : la spec reste sans détail d'implémentation)
  - .specify/templates/tasks-template.md ✅ (aligné : phases setup/foundational/polish couvrent
    tests, sécurité et observabilité imposés par les principes)
  - .specify/templates/checklist-template.md ✅ (aucun impact)

Suivi / TODO différés: aucun.
-->

# Constitution Lumineux

## Core Principles

### I. Architecture Onion & séparation stricte des couches

Le code de l'API MUST suivre une architecture en oignon (Onion) avec des couches
explicitement séparées : **Domain** (entités, règles métier, interfaces) au centre,
**Application** (cas d'usage, services applicatifs, DTO, ports), **Infrastructure**
(persistance, EF Core, services externes) et **Presentation/API** (contrôleurs) en périphérie.

Règles non négociables :
- Les dépendances MUST pointer vers l'intérieur uniquement (règle de dépendance) : le Domain
  ne dépend d'aucune couche externe ; l'Infrastructure et l'API dépendent du Domain/Application.
- La logique métier MUST résider dans le Domain/Application, jamais dans les contrôleurs ni dans
  les couches d'accès aux données.
- L'accès aux dépendances externes (base, fichiers, réseau) MUST passer par des abstractions
  (interfaces/ports) définies dans le Domain/Application et implémentées en Infrastructure.
- Chaque couche MUST être un projet/assembly distinct pour rendre la règle de dépendance
  vérifiable à la compilation.

Rationale : garantir la testabilité, l'indépendance vis-à-vis des frameworks et la scalabilité
d'une application appelée à croître (API, SPA, mobile).

### II. Code-First & intégrité de la base de données

La base SQL Server MUST être pilotée en **code-first** : le schéma dérive du modèle de code via
des migrations versionnées et reproductibles.

Règles non négociables :
- Toute évolution de schéma MUST passer par une migration commitée ; aucune modification manuelle
  du schéma en dehors des migrations.
- Les migrations MUST être déterministes et rejouables sur une base vierge (environnement neuf).
- Les contraintes d'intégrité (clés étrangères, unicité, nullabilité, index) déclarées dans la
  documentation des entités MUST être matérialisées dans le schéma.
- Tout enregistrement MUST porter la piste d'audit (`createdt`, `createdby`, `updatedt`,
  `updatedby`) conformément au modèle d'entité de base.

Rationale : un schéma reproductible et traçable est la condition d'une base fiable et d'un
déploiement maîtrisé sur plusieurs environnements.

### III. Tests en premier & couches métier vérifiables (NON-NÉGOCIABLE)

Les couches Domain et Application MUST être couvertes par des tests unitaires, écrits avant ou
conjointement à l'implémentation (approche test-first privilégiée).

Règles non négociables :
- La logique métier (règles, invariants, cas d'usage) MUST avoir des tests unitaires isolés,
  sans dépendance à la base réelle (dépendances externes remplacées par des doubles de test).
- Un cas d'usage nouveau ou modifié MUST être accompagné de ses tests dans le même changement.
- Les tests MUST échouer avant l'implémentation de la fonctionnalité correspondante puis passer
  après (cycle rouge → vert).
- La CI MUST bloquer toute fusion dont les tests échouent.

Rationale : sans filet de tests sur le cœur métier, une application « scalable » devient
rapidement non maintenable ; les tests sont la garantie de non-régression.

### IV. Sécurité par défaut

La sécurité MUST être intégrée dès la conception de chaque fonctionnalité, et non ajoutée après
coup.

Règles non négociables :
- Toute entrée externe (API, mobile, SPA) MUST être validée et assainie côté serveur ; la
  validation côté client ne fait jamais autorité.
- L'accès aux données MUST se faire via des requêtes paramétrées / l'ORM ; aucune concaténation
  de SQL à partir d'entrées utilisateur (protection contre l'injection).
- L'authentification et l'autorisation MUST être vérifiées côté serveur pour chaque opération
  sensible ; les droits (profils du bureau) MUST être contrôlés au niveau de l'action, selon le
  principe du moindre privilège.
- Les secrets (chaînes de connexion, clés, jetons) MUST être stockés hors du code source et hors
  du contrôle de version.
- Les données personnelles des membres MUST être protégées en transit et au repos, et exposées
  au minimum nécessaire.
- Les risques de sécurité identifiés MUST être signalés et traités ; toute exception à ce principe
  MUST être documentée explicitement (ex. contexte PoC).

Rationale : l'application gère des données personnelles d'une communauté ; la confiance et la
conformité en dépendent directement.

### V. Contrats d'API explicites & cohérents

L'API MUST exposer des contrats stables et cohérents, découplés du modèle de persistance.

Règles non négociables :
- Les échanges MUST utiliser des DTO dédiés ; les entités de persistance ne MUST jamais être
  exposées directement.
- Les contrats MUST suivre des conventions REST cohérentes (nommage, codes de statut HTTP,
  format d'erreur homogène et non fuitant).
- Toute évolution incompatible d'un contrat MUST être versionnée pour ne pas casser les clients
  (SPA Angular, application mobile Flutter).
- Les contrats MUST être documentés (ex. OpenAPI) et tenus à jour avec l'implémentation.

Rationale : deux clients (SPA et mobile) consomment l'API ; des contrats clairs et versionnés
évitent les ruptures et facilitent l'évolution parallèle.

### VI. Traçabilité, audit & observabilité

Le comportement du système MUST être observable et les actions sensibles traçables.

Règles non négociables :
- Les opérations sensibles (création/clôture de session, enregistrement/retrait de présence,
  changements de droits) MUST être journalisées avec auteur et horodatage.
- Les tentatives refusées (droit manquant, jeton invalide, session close) MUST être consignées à
  des fins de diagnostic et de sécurité.
- La journalisation MUST être structurée et ne MUST jamais contenir de secrets ni de données
  personnelles superflues.
- Les horodatages métier faisant foi (ex. heure d'arrivée, heure de fin) MUST s'appuyer sur une
  source de temps serveur fiable, pas sur l'horloge du client.

Rationale : la traçabilité est indispensable au diagnostic, à la sécurité et à la confiance dans
les données de présence.

## Contraintes technologiques & standards

- **Base de données** : SQL Server, piloté en code-first (Principe II).
- **API** : .NET Core, architecture Onion en couches séparées avec tests unitaires (Principes I, III).
- **SPA** : Angular — tableau de bord de gestion complet.
- **Mobile** : Flutter — expérience utilisateur (scan de présence) et module tableau de bord réduit.
- **Ordre de mise en œuvre** : l'API est développée en premier ; les clients (SPA, mobile) la
  consomment ensuite via ses contrats.
- **Langue** : la documentation projet (specs, plans, analyses) est rédigée en français ;
  les analyses vont dans `ai-specs/` et les schémas utilisent la syntaxe Mermaid.
- **Conventions de données** : respecter les conventions de nommage et la piste d'audit décrites
  dans la documentation des entités (tables au pluriel, `id` auto-généré, champs d'audit hérités).

## Workflow de développement (Spec-Driven)

- Le développement suit le flux Spec-Driven : `specify` → `clarify` (si besoin) → `plan` →
  `tasks` → `implement`, chaque étape produisant des artefacts versionnés sous `specs/`.
- Une fonctionnalité MUST partir d'une spécification centrée sur le QUOI et le POURQUOI, sans
  détail d'implémentation ; les choix techniques sont arrêtés à l'étape `plan`.
- Chaque plan de fonctionnalité MUST inclure une « Constitution Check » validant le respect des
  principes ci-dessus avant le début de l'implémentation, puis à nouveau après la conception.
- Toute dérogation à un principe MUST être justifiée explicitement dans la section de suivi de
  complexité du plan (raison + alternative plus simple écartée).
- Les actions sensibles (push Git, appels réseau sortants, commandes destructrices) MUST recevoir
  une approbation explicite avant exécution.

## Governance

Cette constitution prévaut sur les autres pratiques du projet. En cas de conflit entre une
pratique et un principe, le principe s'applique jusqu'à amendement formel.

- **Amendements** : toute modification MUST être documentée (motif, portée, impact), validée par
  les responsables du projet, et accompagnée d'une mise à jour des artefacts dépendants
  (templates de plan, spec, tasks) le cas échéant.
- **Versionnage** : la version suit le SemVer. MAJOR pour un retrait/redéfinition incompatible de
  principe ou de gouvernance ; MINOR pour l'ajout d'un principe/section ou une extension
  significative ; PATCH pour des clarifications non sémantiques.
- **Conformité** : chaque plan et chaque revue MUST vérifier le respect des principes ; les
  écarts non justifiés bloquent la fusion.
- **Guidage runtime** : les préférences opérationnelles (langue, `ai-specs/`, Mermaid, contrôle
  des actions sensibles) complètent cette constitution sans la contredire.

**Version**: 1.0.0 | **Ratified**: 2026-07-02 | **Last Amended**: 2026-07-02
