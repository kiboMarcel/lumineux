# Quickstart — Console web Lumineux (socle & cycle de vie du compte)

Guide de mise en route et de validation. La SPA (`web/`) consomme l'API Lumineux (`src/`).

## Prérequis

- API Lumineux démarrée (localement, ex. `dotnet run --project src/Lumineux.Api`) avec **CORS**
  autorisant l'origine de la SPA (dev : `http://localhost:4200`).
- Instance disposant d'au moins un compte actif (via l'installation premier admin, ou un membre
  provisionné + activé).
- Outils front installés dans `web/` (installation des dépendances Angular).

## Démarrage (dev)

1. Configurer `apiBaseUrl` (environnement dev) vers l'API (ex. `https://localhost:5001`).
2. Installer les dépendances puis lancer le serveur de dev de la SPA.
3. Ouvrir l'application ; en l'absence de session, l'écran de **connexion** s'affiche.

## Scénarios de validation (mappés aux user stories / SC)

### A — Connexion et console adaptée aux droits (US1)
1. Se connecter avec un compte valide.
2. **Attendu** : arrivée sur la console ; nom affiché ; navigation limitée aux droits ; les entrées
   liées à un droit absent sont **masquées/désactivées** (SC-003). Se déconnecter → retour connexion ;
   un rechargement ne reconnecte pas.

### B — Garde de routes & retour connexion (US1, FR-006/007)
1. Non connecté, tenter d'ouvrir une URL protégée → redirection **connexion**.
2. Connecté, simuler une session expirée (jeton invalidé) sur une requête → retour **connexion** avec
   message, puis retour à l'URL visée après reconnexion (SC-004).

### C — Première connexion / activation (US2)
1. Se connecter avec un compte en attente (mot de passe temporaire).
2. **Attendu** : détection `password_change_required` → écran d'**activation** ; définir un nouveau
   mot de passe conforme → accès console (SC-001). Un mot de passe non conforme est refusé avant envoi.

### D — Mot de passe oublié + réinitialisation (US3)
1. Depuis la connexion, demander une réinitialisation (référence existante **puis** inexistante).
2. **Attendu** : **message identique** dans les deux cas (SC-006).
3. Ouvrir la route `/auth/reset-password?token=<jeton>` → définir un nouveau mot de passe → retour
   connexion → se connecter avec le nouveau mot de passe (SC-002). Un jeton invalide → **échec
   générique**.

### E — Changement de mot de passe (US4)
1. Connecté, ouvrir le changement de mot de passe ; fournir l'actuel + un nouveau conforme et
   différent → succès. Mot de passe actuel erroné → message d'erreur ; nouveau non conforme → refus
   avant envoi.

### F — Installation du premier administrateur (US5)
1. Sur une instance **non initialisée**, ouvrir `/setup/first-admin`, créer l'admin → connecté.
2. Sur une instance **déjà amorcée**, l'accès est refusé/redirigé (l'API rejette).

### G — Sécurité transverse (SC-005)
- Vérifier qu'**aucun** secret (mot de passe, jeton) n'apparaît dans le stockage du navigateur, les
  URL persistées ou la console de développement, sur l'ensemble des parcours.

## Vérification finale (checklist SC)

- [ ] SC-001 activation de bout en bout · [ ] SC-002 oublié→reset→connexion
- [ ] SC-003 masquage selon droits · [ ] SC-004 retour connexion sur session invalide
- [ ] SC-005 aucun secret observable · [ ] SC-006 message oublié identique
- [ ] SC-007 message d'erreur distinct par type · [ ] SC-008 lisible bureau + tablette
