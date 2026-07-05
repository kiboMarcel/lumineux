import { expect, test } from '@playwright/test';

/**
 * US1 — connexion et garde de routes. Prérequis : API démarrée + compte de test.
 * Renseigner les identifiants via variables d'environnement E2E_REFERENCE / E2E_PASSWORD.
 */
const REFERENCE = process.env['E2E_REFERENCE'];
const PASSWORD = process.env['E2E_PASSWORD'];

test('affiche l\'écran de connexion', async ({ page }) => {
  await page.goto('/login');
  await expect(page.getByRole('heading', { name: 'Connexion' })).toBeVisible();
  await expect(page.getByLabel('Référence')).toBeVisible();
  await expect(page.getByLabel('Mot de passe')).toBeVisible();
});

test('redirige vers la connexion pour une URL protégée sans session', async ({ page }) => {
  await page.goto('/account/change-password');
  await expect(page).toHaveURL(/\/login/);
});

test('affiche un message non révélateur sur identifiants erronés', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Référence').fill('LUM-INEXISTANT');
  await page.getByLabel('Mot de passe').fill('mauvais');
  await page.getByRole('button', { name: /se connecter/i }).click();
  await expect(page.getByText('Référence ou mot de passe invalide.')).toBeVisible();
});

test.describe('parcours authentifié', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_REFERENCE / E2E_PASSWORD pour ce test');

  test('connexion → console → déconnexion', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();

    await expect(page.getByRole('button', { name: /se déconnecter/i })).toBeVisible();
    await page.getByRole('button', { name: /se déconnecter/i }).click();
    await expect(page).toHaveURL(/\/login/);
  });
});
