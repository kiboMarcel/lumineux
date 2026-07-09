# Contrat UI — Capture hors ligne, indicateur de synchro & avis de rejet

**Feature** : `027-mobile-offline-sync` · **Phase 1** · Statut : **NOUVEAU (client)**

Contrat d'**expérience** (états visibles, formulations, interactions). Français, usage tactile mobile
(FR-012). Aucune fuite de jeton (FR-009). S'insère dans l'écran Scanner M1 existant.

## 1. Confirmation de capture hors ligne (variante de l'overlay M1)

Sur scan **sans réseau** d'un QR **structurellement valide** (FR-001, FR-001a), l'overlay de résultat M1
affiche une **variante hors ligne** au lieu d'une erreur :

- **Type** : `ScanResultKind.offlineQueued` (nouveau, à ajouter à l'énumération M1).
- **Titre** : « Enregistrée hors ligne »
- **Sous-titre** : « À synchroniser dès le retour du réseau »
- **Ton** : neutre/positif (pas d'erreur) — la présence n'est **pas** perdue.
- **Actions** : « Fermer » / « Scanner à nouveau » (comme M1). La détection reste suspendue jusqu'à fermeture.

Cas dérivés :
- **Re-scan d'une séance déjà en file** (FR-014) → même overlay, titre « Déjà capturée hors ligne ».
- **QR non reconnu hors ligne** (FR-001a) → **pas** de capture ; l'indice M1 « Code non reconnu » suffit,
  aucune confirmation trompeuse.

## 2. Indicateur d'état de synchronisation (`sync_status_banner`)

Bandeau/pastille visible sur l'écran Scanner (et surface d'accueil du membre), reflétant `SyncStatus`
(FR-011, SC-006).

| Élément | Contenu | Source |
|---------|---------|--------|
| Compteur **en attente** | « N à synchroniser » (masqué si 0) | `pendingCount` |
| Compteur **en cours** | « Synchronisation… » + spinner (pendant un cycle) | `inProgressCount` / `lastSyncOutcome==running` |
| **Avis de rejet/échec** | liste des `SyncNotice` non acquittés (voir §3) | `unacknowledgedNotices` |
| Bouton **Réessayer** | déclenche une relance manuelle (FR-006) | action `syncController.retryNow()` |
| Horodatage | « Dernière synchro : hh:mm » (si applicable) | `lastSyncAt` |

États d'affichage :
- **Tout synchronisé** (aucune capture, aucun avis) → bandeau masqué ou état neutre discret.
- **En attente** → compteur + bouton « Réessayer » (actif si réseau présumé).
- **En cours** → indicateur de progression, bouton désactivé.
- **Avis présents** → section d'avis (§3), acquittables.

## 3. Avis de rejet / échec définitif (`SyncNotice`)

Pour **100 %** des rejets et échecs définitifs (SC-004), un avis **clair avec raison** est présenté puis
**acquittable** :

- **Rejet serveur** (`kind = rejected`) : titre « Présence refusée », corps = `reason` serveur (ex. « Jeton
  QR invalide au moment du scan. »), séance concernée.
- **Échec définitif** (`kind = permanentlyFailed`, plafond FR-013) : titre « Non synchronisée », corps
  « Après plusieurs tentatives / capture trop ancienne — présence non enregistrée ».
- **Action** : « J'ai compris » → passe `acknowledged = true` (l'avis disparaît de la liste active).
- **Persistance** : l'avis survit à un redémarrage jusqu'à acquittement (garantit SC-004 même app fermée).

## 4. Interactions & déclencheurs (rappel, FR-006)

| Déclencheur | Effet |
|-------------|-------|
| Retour de connectivité (`ConnectivityFacade`) | Réinitialise le backoff + tente une synchro |
| Lancement / reprise d'app (avec réseau) | Tente une synchro |
| Bouton « Réessayer » | Tente une synchro immédiate |
| Backoff automatique (app active, file non vide) | Réessaie à intervalle croissant, borné (FR-013) |

## 5. Accessibilité & sécurité UI

- **Jeton** : jamais rendu ni copiable ; aucun champ ne l'expose (FR-009/SC-005).
- **Libellés** : intégralement en **français** ; cibles tactiles ≥ 44 dp.
- **Feedback** : chaque état a un libellé lisible (capture hors ligne, en cours, terminée, rejet) — FR-012.
- **Non bloquant** : la capture hors ligne et la synchro n'interrompent jamais le scan suivant.
