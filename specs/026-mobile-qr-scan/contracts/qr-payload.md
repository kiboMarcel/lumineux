# Contrat — Charge du QR de séance (SPA ⇄ mobile)

Nouveau **contrat inter-clients versionné** introduit par ce lot. **Produit** par la console web (bureau,
feature 014) et **consommé** par l'app mobile membre. **Aucune** implication API/serveur.

## Format

Le contenu encodé dans l'image QR est un **JSON UTF-8** :

```json
{ "v": 1, "s": 123, "t": "<jeton-rotatif-opaque>" }
```

| Clé | Type | Contrainte | Sens |
|-----|------|------------|------|
| `v` | entier | `= 1` (version courante) | Version du format de charge. |
| `s` | entier | `> 0` | Identifiant de la séance (`sessionId`). |
| `t` | chaîne | non vide | Jeton rotatif courant (opaque, éphémère). |

## Producteur — console web (prérequis in-scope)

`web/src/app/features/attendance/session-run/qr-panel/qr-panel.component.ts` :
- **Avant** : `this.qrData.set(res.token)` (jeton seul).
- **Après** : `this.qrData.set(JSON.stringify({ v: 1, s: this.sessionId(), t: res.token }))`.
- Le jeton **n'est jamais rendu en texte** ; seul le contenu de l'image change. Le rythme de rotation
  (`stepSeconds`) et le cycle de vie du composant restent inchangés.

## Consommateur — app mobile

`QrPayload.parse(String raw)` :
1. Décoder le JSON. En cas d'échec → **non reconnu**.
2. Vérifier `v == 1` (toute autre valeur, y compris future → **non reconnu**, pour rejet propre).
3. Vérifier `s` entier `> 0` et `t` non vide → sinon **non reconnu**.
4. Succès → `QrPayload(sessionId = s, token = t)`.

**Règle de sécurité** : cette validation ne fait **pas autorité** ; elle extrait `sessionId`/`token` et
écarte les QR étrangers. La validité réelle (jeton courant, séance ouverte, appartenance) est jugée par le
**serveur** lors de l'appel `.../scan`.

## Évolutivité

Le champ `v` permet d'introduire un format `v: 2` ultérieur sans casser les clients : un mobile ne
connaissant que `v: 1` **rejette proprement** un `v: 2` (« code non reconnu ») plutôt que de mal
l'interpréter.
