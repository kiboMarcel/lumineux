# Database Entities Documentation — OBSOLÈTE

> ⚠️ **Ce document est obsolète et ne fait plus foi.**
>
> Il décrivait un ancien modèle **TypeORM/TypeScript** (tables `branches`, `zones`, `sponsorships`,
> `ranks`, `provinces`, `continents`…) qui **ne correspond pas** au schéma réel de l'application.
>
> Le backend est désormais une **API .NET / EF Core (code-first)**. Le modèle de données **réel** est :
>
> - **Documentation d'audit** : [`docs/audit/03-modele-donnees.md`](docs/audit/03-modele-donnees.md)
>   (diagramme entités-relations, mapping code ↔ tables, index/contraintes).
> - **Source de vérité** : les configurations EF Core sous
>   `src/Lumineux.Infrastructure/Persistence/Configurations/` et les migrations sous
>   `src/Lumineux.Infrastructure/Persistence/Migrations/`.
>
> Ne pas se fier à l'ancien contenu (retiré). Conservé comme simple redirection pour éviter les liens morts.
