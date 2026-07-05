# Research — Statut d'installation (setup/status)

Feature minuscule de lecture réutilisant l'existant. Décisions figées avant conception.

## 1. Source de l'état : réutiliser le décompte du verrou

- **Décision** : `installed = (CountActiveAdministratorsAsync() > 0)` via le **port existant**
  `IBureauProfileRepository`, **le même** décompte utilisé par le verrou d'installation
  (`InstallFirstAdminHandler`, feature 005).
- **Rationale** : garantit **par construction** que le statut est cohérent à 100 % avec le verrou
  (FR-002, SC-002) — impossible de diverger. Aucune nouvelle logique de « comptage admin ».
- **Alternatives écartées** : recompter selon une autre requête (risque de divergence avec le verrou) ;
  recalculer côté Application (couplage EF hors couche).

## 2. Accès anonyme

- **Décision** : endpoint **`[AllowAnonymous]`**, comme `POST setup/first-admin`.
- **Rationale** : le statut sert précisément à décider, **avant toute session**, si l'installation est
  proposée. Exiger un jeton rendrait l'usage impossible sur une instance vierge.
- **Alternatives écartées** : exiger l'authentification (contradictoire avec le besoin) ; restreindre
  par IP/réseau (hors périmètre, sans valeur ici).

## 3. Anti-divulgation : réponse strictement booléenne

- **Décision** : la réponse expose **uniquement** `{ installed: bool }` — aucun comptage détaillé,
  aucune donnée de compte/membre.
- **Rationale** : FR-003, SC-003. Un simple booléen « installé » ne permet aucune énumération ni fuite
  (l'existence d'une instance installée n'est pas une information sensible ; le nombre d'admins, si,
  n'est **pas** exposé).
- **Alternatives écartées** : renvoyer le nombre d'administrateurs (fuite inutile) ; renvoyer des
  détails d'installation (hors périmètre, sensibles).

## 4. Découpage & placement

- **Décision** : un **handler** `GetSetupStatusHandler` (Application/Setup) + **DTO**
  `SetupStatusResponse` (Contracts/Setup) + endpoint sur le **`SetupController`** existant
  (`GET setup/status`).
- **Rationale** : cohérent avec le regroupement « Setup » existant (feature 005) et le pattern
  handler/DTO/contrôleur mince.
- **Alternatives écartées** : logique dans le contrôleur (violation Onion/Constitution I) ; nouveau
  contrôleur dédié (superflu).

## 5. Intangibilité du verrou

- **Décision** : **aucune** modification de `InstallFirstAdminHandler` ni du `POST setup/first-admin`.
  Le statut est une **lecture additive**.
- **Rationale** : FR-005, SC-004 — le refus « déjà installé » reste effectif et prioritaire ; le
  statut ne fait que **lire**.
