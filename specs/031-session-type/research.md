# Phase 0 — Recherche & décisions : Type de session

## Décision 1 — Enum applicatif à ensemble fermé

- **Décision** : nouvel enum `SessionType { AntennaMeeting = 0, Teaching = 1 }`, calqué sur
  `SessionStatus`.
- **Rationale** : la nature d'une séance est un ensemble **fermé et petit** ; un enum offre la
  validation la plus stricte (rejet naturel des valeurs inconnues) et s'aligne sur le pattern
  existant (`SessionStatus`, `MemberStatuses`, `Genders`). Extensible par ajout de valeurs.
- **Alternatives** : table de référence `session_types` (FK) — rejetée (sur-ingénierie pour un
  ensemble fermé sans attributs propres ; réversible plus tard si le type devait porter des
  métadonnées). Chaîne libre — rejetée (aucune garantie de valeur, contraire à FR-005).

## Décision 2 — Persistance en chaîne, comme `Status`

- **Décision** : `HasConversion<string>().HasMaxLength(20)`, colonne `session_type`.
- **Rationale** : cohérence stricte avec `Status` (lisibilité en base, robustesse au réordonnancement
  de l'enum). Portable SQL Server / SQLite (tests). `AttendanceResponse`/rapports lisent déjà des
  enums en chaîne côté API.
- **Alternative** : stockage entier — rejeté (moins lisible, fragile si l'ordre de l'enum change ;
  incohérent avec `Status`).

## Décision 3 — Valeur par défaut en base pour rétro-remplir l'existant

- **Décision** : migration `AddColumn` **NOT NULL** avec `defaultValue: "AntennaMeeting"`.
- **Rationale** : le discriminant est obligatoire (FR-001) ; le défaut en base garantit que
  **toutes les sessions existantes** deviennent `AntennaMeeting` sans script de backfill (FR-003,
  SC-001). Diffère volontairement de la feature 030 (profession nullable) : ici la colonne est
  requise, donc un défaut est nécessaire pour les lignes existantes. Rejouable, déterministe.
- **Note** : le défaut au niveau **domaine** (fabrique `Start`, paramètre à valeur par défaut
  `AntennaMeeting`) couvre les nouvelles sessions ; le défaut au niveau **base** couvre les
  anciennes lignes lors de la migration. Les deux sont nécessaires et complémentaires.

## Décision 4 — Type fixé à la création, immuable

- **Décision** : propriété `SessionType` à **setter privé**, assignée uniquement dans la fabrique
  `AttendanceSession.Start` (nouveau paramètre `sessionType` à valeur par défaut `AntennaMeeting`).
  Aucun mutateur, aucune transition ne le modifie.
- **Rationale** : la nature d'une séance est décidée à l'ouverture (FR-006) ; l'immuabilité évite
  toute dérive et reste cohérente avec la conception « entité riche » (invariants portés par le
  domaine). Le paramètre optionnel préserve tous les appelants existants (`StartSessionHandler`,
  tests) sans modification forcée.

## Décision 5 — Validation du type fourni

- **Décision** : dans `StartSessionValidator`, si un type est fourni (chaîne), il DOIT correspondre
  à une valeur d'enum reconnue (parse insensible ? — **sensible** à la casse pour cohérence avec
  `Gender`/`Status`, à trancher au plan/impl mais défaut = correspondance stricte du nom d'enum),
  sinon rejet (message clair). Absence de type → défaut `AntennaMeeting` (pas d'erreur).
- **Rationale** : validation serveur faisant autorité (Principe IV, FR-005). Le handler convertit
  la chaîne validée en enum avant d'appeler la fabrique.
- **Alternative** : accepter et normaliser silencieusement une valeur inconnue vers le défaut —
  rejeté (masquerait une erreur client ; FR-005 exige un refus explicite).

## Décision 6 — Livraison API-only (SPA contrat seulement)

- **Décision** : ajouter `sessionType` à l'interface `SessionResponse` du SPA (contrat en phase),
  **sans** sélecteur ni affichage sur l'écran de démarrage ; l'écran continue de produire des
  `AntennaMeeting` (aucun champ envoyé → défaut serveur).
- **Rationale** : le point ouvert de la spec est tranché par le défaut « API-only » : la
  sélection du type n'a de sens qu'avec le futur domaine des enseignements ; l'ajouter maintenant
  serait une UI sans usage. Garder le contrat TS synchronisé évite une dette silencieuse et ne
  modifie aucun composant (zéro risque de régression SPA).
- **Alternative** : sélecteur de type dès maintenant — différé (cf. Assumptions spec) ; à
  reconsidérer au `/speckit-clarify` si souhaité.

## Points sans recherche nécessaire

- Autorisation inchangée (`manage_attendance` déjà exigée au démarrage).
- Audit : `_audit.Operation("StartSession", …)` déjà en place ; on peut y adjoindre le type
  (non sensible), sans nouveau mécanisme.
- Aucun impact QR/pointage/clôture/annulation/auto-clôture/rapports/scan (FR-008).
