# Contrat — Comportement d'affichage (connexion) & statut consommé

## Endpoint consommé (existant, feature 012)

| Méthode & chemin | Accès | Réponse |
|------------------|-------|---------|
| `GET /api/v1/setup/status` | Anonyme | `200 { "installed": boolean }` |

Aucune modification d'API. Aucun autre endpoint consommé par ce lot (l'installation elle-même utilise
`POST /api/v1/setup/first-admin`, déjà intégrée en feature 008).

## Comportement de l'écran de connexion

| État du statut | `showSetupLink` | Lien « Première installation » |
|----------------|-----------------|--------------------------------|
| `installed = false` | `true` | **Affiché** → `/setup/first-admin` |
| `installed = true` | `false` | **Masqué** |
| **Échec** (réseau/API indisponible) | `false` | **Masqué** (défaut sûr) — la connexion reste utilisable |

## Route (existante, réutilisée)

| Chemin | Écran | Notes |
|--------|-------|-------|
| `/setup/first-admin` | Installation du premier administrateur (feature 008) | Inchangé ; accessible directement par URL ; auto-bloqué (409 `already_installed`) côté API. |

## Invariants

- L'appel de statut est **anonyme** et n'altère pas la capacité de se connecter (FR-005/008).
- Le lien n'apparaît **jamais** sur une instance déjà installée (FR-003, SC-002).
- Aucune donnée sensible n'est affichée (un simple booléen alimente une décision d'affichage).
