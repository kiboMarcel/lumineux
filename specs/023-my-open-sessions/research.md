# Research — API : récupérer mes sessions de présence ouvertes

Extension **lecture seule** de l'API de présence (feature 001). Réutilise l'infrastructure existante
(Onion, EF Core, RBAC, DTO `SessionResponse`). **Aucune migration.**

## 1. Filtrage : ouvertes + initiateur = utilisateur courant

- **Décision** : le dépôt renvoie les sessions `Status = Open` **et** `OpenedByMemberId = <membre du
  jeton>`. L'identité provient de `ICurrentUser` (jamais d'un paramètre client).
- **Rationale** : FR-001/006, SC-003 ; l'utilisateur ne récupère que **ses** sessions ouvertes, pour la
  reprise. Sécurité : pas de fuite des sessions d'autrui.
- **Alternatives écartées** : filtre par `antennaId`+`meetingDate` fourni par le client (le client doit
  re-saisir le créneau ; moins direct pour « reprendre ce que j'ai démarré ») ; « toutes les sessions
  ouvertes » (fuite + non ciblé).

## 2. DTO existant `SessionResponse`, décompte à 0

- **Décision** : projeter via `SessionMapping.ToResponse` (comme `GetSession`), donc `AttendanceCount =
  0`. Le SPA récupère le **décompte en direct** par polling des présences ; l'endpoint sert à
  **retrouver l'identifiant/le contexte** de la session, pas le décompte.
- **Rationale** : FR-002 ; cohérence avec `GetSession` ; aucun calcul de compte superflu.
- **Alternatives écartées** : calculer le décompte des présences valides (requête supplémentaire inutile
  pour la reprise).

## 3. Liste (0/1/plusieurs)

- **Décision** : renvoyer une **liste** de `SessionResponse` ; vide si aucune session ouverte.
- **Rationale** : FR-004, edge cases ; robustesse (un membre pourrait avoir ouvert plusieurs sessions
  sur des antennes/dates différentes).
- **Alternatives écartées** : renvoyer une seule session / 404 si aucune (moins robuste, gestion d'erreur
  côté client inutile).

## 4. Extension du port + endpoint

- **Décision** : `IAttendanceSessionRepository.ListOpenByOpenerAsync(int openedByMemberId)` (EF :
  `Where(Status == Open && OpenedByMemberId == id)`, `AsNoTracking`). Endpoint **`GET
  /api/v1/attendance-sessions/mine/open`** sur le contrôleur existant, gardé `manage_attendance`.
  (`mine` n'est pas un entier → aucun conflit de route avec `GET {sessionId:int}`.)
- **Rationale** : Principe I/V ; réutilise le contrôleur et le DTO existants.
- **Alternatives écartées** : nouveau contrôleur (redondant) ; endpoint paramétré par memberId (risque
  de fuite/usurpation — l'identité doit venir du jeton).

## 5. Sécurité & lecture seule

- **Décision** : droit **`manage_attendance`** (policy + garde handler) ; identité **par le jeton** ;
  **aucune écriture** ; règle de conflit au démarrage et clôture **inchangées**.
- **Rationale** : FR-005/006/007, SC-004/005 ; corrige le bug sans toucher aux invariants existants.
- **Alternatives écartées** : assouplir la règle « une session ouverte par antenne/date » (changerait la
  sémantique métier ; non demandé).
