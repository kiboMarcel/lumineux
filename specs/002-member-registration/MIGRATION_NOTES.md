# Notes de migration — Ajout d'un nouveau membre (T042)

## Contexte

La migration EF `MemberRegistration` :
- **enrichit** la table `members` (colonnes `reference` unique/requise, `entry_date`, `gender`, contact,
  état civil, rattachements) ;
- crée `member_accounts` et les nomenclatures (`civilities`, `countries`, `cities`, `districts`) ;
- ajoute les index uniques filtrés sur `email`/`mobile` des membres actifs.

## Point d'attention : colonne `reference` (unique + requise)

En code-first, l'ajout d'une colonne **requise et unique** sur une table **contenant déjà des lignes**
échoue (valeurs par défaut non uniques). Deux cas :

### Cas nominal — table `members` vide (attendu)

Aucune gestion de membre n'existait avant cette fonctionnalité : en production, la table `members`
est **vide** au moment d'appliquer la migration. Aucun backfill n'est nécessaire.

### Cas à traiter — membres préexistants

Si des lignes existent (ex. jeu de données importé), appliquer un **backfill de `reference` dans le
même déploiement que la migration**, par exemple :

1. Ajouter la colonne `reference` en **nullable** (migration intermédiaire).
2. Peupler `reference` pour chaque membre existant selon le format `LUM-{année d'entrée}-{séquence}`
   (script de données idempotent, garantissant l'unicité).
3. Rendre la colonne **NOT NULL + index unique** (migration finale).
4. Provisionner un `member_account` pour chaque membre existant (ou le déléguer à la feature d'auth).

> Tant que le cas « membres préexistants » ne se présente pas, la migration actuelle (colonne requise
> d'emblée) convient. Réévaluer avant tout import de données historiques.
