# Research — Mot de passe oublié (feature 006)

Ce document consolide les décisions techniques prises pour lever tous les points ouverts avant la
conception. Aucun `[NEEDS CLARIFICATION]` ne subsiste (la spec avait déjà tranché les 4 arbitrages
majeurs). Les décisions ci-dessous portent sur le **comment** technique.

## 1. Génération et stockage du jeton

**Décision** : jeton = **32 octets** issus de `RandomNumberGenerator.GetBytes(32)`, encodés en
**base64url** (sans padding) pour l'insertion dans l'URL. Côté serveur, on ne persiste que
l'**empreinte SHA-256** (hex ou base64) du jeton ; le jeton en clair n'existe qu'en mémoire le temps
de construire le lien, puis dans l'email envoyé.

**Rationale** :
- 32 octets = 256 bits d'entropie → devinette d'un jeton valide statistiquement impossible (FR-015).
- Le jeton ayant une entropie maximale, un **hachage rapide (SHA-256)** suffit pour l'empreinte : pas
  besoin d'un KDF lent (Argon2/PBKDF2) comme pour les mots de passe, car il n'y a **rien à
  bruteforcer** (pas de dictionnaire, pas de faible entropie). C'est la pratique standard pour les
  jetons de reset à haute entropie.
- Le hachage rend l'empreinte **déterministe** → recherche par index unique en O(1) sur
  `token_hash` (FR-016), sans balayage de table ni comparaison ligne à ligne.
- **Seule l'empreinte** en base ⇒ une fuite de la table ne permet **pas** de reconstruire les liens
  (FR-009, SC-004).

**Alternatives écartées** :
- *Stocker le jeton en clair* : rejeté — viole FR-009 (une fuite BD compromettrait tous les liens).
- *GUID/`Guid.NewGuid()`* : rejeté — 122 bits effectifs et générateur non garanti cryptographique
  selon les plateformes ; en-deçà de l'exigence FR-015.
- *Chiffrer le jeton plutôt que le hacher* : rejeté — introduit une clé à gérer/protéger sans bénéfice
  (on n'a jamais besoin de retrouver le jeton en clair côté serveur).

**Implémentation** : nouveau port `IResetTokenService` dans `Lumineux.Domain.Abstractions`,
implémenté par `ResetTokenService` (Infrastructure/Security), calqué sur `QrTokenService` existant :

```csharp
public interface IResetTokenService
{
    // Retourne le jeton en clair (pour le lien) et son empreinte (pour la base).
    (string ClearToken, string TokenHash) Generate();
    // Recalcule l'empreinte d'un jeton présenté pour la recherche.
    string Hash(string clearToken);
}
```

## 2. Recherche et anti-énumération sur `/reset-password`

**Décision** : à la présentation d'un jeton, on calcule `Hash(clearToken)` puis on recherche la ligne
par `token_hash`. Les trois cas d'échec — **introuvable**, **expiré**, **déjà consommé** — lèvent la
**même** `UnauthorizedException` (→ 401 générique via le middleware existant), sans distinction
(SC-003, FR-008).

**Rationale** : la recherche par empreinte hachée d'un jeton à haute entropie ne présente pas de canal
temporel exploitable (pas de comparaison secret-à-secret sur données à faible entropie). Le refus
unique empêche un attaquant de distinguer « jeton jamais émis » de « jeton expiré/consommé ».

**Note ordre de traitement** : la validation FluentValidation (politique de mot de passe) s'exécute
**avant** la recherche du jeton. Un mot de passe non conforme renvoie donc **400** *sans* consulter ni
consommer le jeton (FR-006, US2 scénario 3) — comportement obtenu « gratuitement » par l'ordre des
étapes du handler.

## 3. Anti-timing sur `/forgot-password`

**Décision** : réponse **toujours 200 générique**. Lorsqu'aucun jeton n'est émis (compte inexistant,
sans email, non actif, verrouillé), le handler exécute une **opération de hachage factice**
(`IResetTokenService.Generate()` dont on jette le résultat, ou un `IPasswordHasher.Hash` d'une valeur
fixe) pour **égaliser le coût de calcul** avec le chemin nominal (génération + persistance + envoi).

**Rationale** : aligne cette feature sur la stratégie déjà en place dans `LoginHandler` (hash factice
quand le compte est absent, feature 003), pour ne pas révéler par le temps de réponse si un compte
existe (Edge Case « attaque par timing », SC-002).

**Alternatives écartées** :
- *Délai fixe artificiel (`Task.Delay`)* : rejeté — fragile (varie selon la charge), gaspille des
  threads, et ne masque pas les variations réelles ; l'opération factice est plus fidèle.

## 4. Mise à jour du compte lors du reset — réutilisation du domaine

**Décision** : le succès du reset appelle **deux méthodes existantes** de `MemberAccount` :
- `ChangePassword(newHash)` → remplace l'empreinte **et** lève `MustChangePassword` (FR-007a) ;
- `RegisterSuccessfulLogin(now)` → remet `FailedAttempts` à 0 **et** annule `LockoutUntil`
  (FR-007c, SC-007 — intégration feature 003).

Aucune nouvelle méthode de compte n'est introduite.

**Rationale** : ces deux méthodes encapsulent déjà exactement les transitions voulues ; les réutiliser
évite la duplication de règles et garantit la cohérence avec `/auth/change-password` et `/auth/login`.
La levée immédiate du verrouillage permet à un membre légitime précédemment bloqué de se reconnecter
sans attendre (SC-007).

**Note FR-013** : `/reset-password` ne touche **pas** à la logique de verrouillage de `/auth/login` ;
il ne fait que **réinitialiser l'état** du compte ciblé via les méthodes de domaine — la protection
anti-force brute de `/auth/login` reste inchangée.

## 5. Cycle de vie du jeton — placement de la règle

**Décision** : la validité et la consommation vivent **dans le Domaine** sur `PasswordResetToken` :
- `IsUsable(DateTime nowUtc)` → `ConsumedAt is null && ExpiresAt > nowUtc` ;
- `Consume(DateTime nowUtc)` → fixe `ConsumedAt = nowUtc` (idempotence défensive : lève une
  `DomainException` si déjà consommé, filet contre un double appel applicatif).

**Rationale** : Constitution I — les invariants métier (un jeton expiré/consommé n'est plus utilisable)
appartiennent au Domaine, pas au handler ni au repository.

## 6. Durée de vie et lien — configuration

**Décision** : extension de `AuthOptions` (section `Auth`) :
- `PasswordResetMinutes` (défaut **30**) → durée de vie du jeton (FR-004).
- `PasswordResetUrlBase` (ex. `https://app.lumineux.example/auth/reset-password`) → base du lien ; le
  handler construit `"{base}?token={clearToken}"`.

Le **handler** (Application) construit le lien à partir de la config et transmet le **lien complet** à
`IEmailSender.SendPasswordResetAsync(email, resetLink)`. L'implémentation email ne manipule que le lien
déjà formé (elle n'a pas connaissance du jeton en tant que tel).

**Rationale** : centralise la construction du lien (et donc la seule manipulation du jeton en clair)
dans le cas d'usage ; l'infrastructure email reste un simple canal. Cohérent avec la config existante
(`AccessTokenMinutes`, `LockoutMinutes` déjà dans `AuthOptions`).

**Alternatives écartées** :
- *Réutiliser une base d'URL de la section `Email`* : rejeté — l'URL cible la **SPA**, pas le serveur
  SMTP ; sémantiquement elle relève de l'authentification (`Auth`).

## 7. Extension du port email

**Décision** : ajouter à `IEmailSender` :

```csharp
Task<EmailSendOutcome> SendPasswordResetAsync(string? toEmail, string resetLink, CancellationToken ct = default);
```

Les deux implémentations existantes sont complétées :
- `LoggingEmailSender` (dev) → journalise l'envoi **sans** le lien complet (le lien contient le jeton) ;
  en dev on peut logguer le lien derrière un niveau `Debug` explicite documenté, mais **jamais** en
  information/production. Décision retenue : ne pas logguer le lien du tout ; le test d'intégration
  capture le jeton via un `IEmailSender` de test (voir quickstart).
- `SmtpEmailSender` (prod) → envoie l'email ; en cas d'échec, retourne `Failed` (journalisé sans lien),
  la réponse API restant **200 générique** (Edge Case « envoi en échec », FR-011).

**Rationale** : réutilise le port et le pattern `EmailSendOutcome` déjà en place (invitation membre,
feature 002). Un seul point d'abstraction pour tous les emails sortants.

## 8. Sécurité — synthèse

| Exigence | Mécanisme |
|----------|-----------|
| Anti-énumération (FR-002, SC-002) | Réponse 200 générique unique + opération factice anti-timing |
| Jeton non devinable (FR-015) | 32 octets `RandomNumberGenerator` |
| Jeton en clair non persisté (FR-009, SC-004) | Seule l'empreinte SHA-256 en base ; jamais journalisé |
| Unicité de l'empreinte (FR-016) | Index unique sur `token_hash` |
| Usage unique (SC-005) | `ConsumedAt` + `IsUsable` + refus 401 au rejeu |
| Expiration (FR-004) | `ExpiresAt` = now + `PasswordResetMinutes` |
| Refus indistinct (FR-008, SC-003) | Même `UnauthorizedException` pour introuvable/expiré/consommé |
| Politique mot de passe (FR-006) | `PasswordRules.ApplyPolicy` réutilisée (feature 003) |
| Levée verrouillage (FR-007c, SC-007) | `RegisterSuccessfulLogin` réutilisée |
| Traçabilité (FR-010) | `IAuditLogger.Operation/Refused` sans secret |
| Membre non actif exclu (FR-012) | Vérif `Member.IsActive` avant émission ; sinon chemin factice |

## 9. Hors périmètre (confirmé)

Invalidation proactive des jetons antérieurs, job de purge des jetons expirés, rate limiting HTTP,
SMS/OTP, notification « votre mot de passe a été changé », repli CLI — tous **différés** (voir
`spec.md` §Assumptions). Aucune dette bloquante : l'expiration courte (30 min) + l'usage unique
couvrent le risque résiduel.
