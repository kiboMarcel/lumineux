import { expect, test } from '@playwright/test';

/**
 * US2 — enrôlement. Prérequis : API + CORS + compte manage_members + au moins une antenne active
 * (feature 010). Renseigner E2E_REFERENCE / E2E_PASSWORD.
 */
const REFERENCE = process.env['E2E_REFERENCE'];
const PASSWORD = process.env['E2E_PASSWORD'];

test.describe('création de membre', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_REFERENCE / E2E_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
    await page.goto('/members/new');
  });

  test('création nominale (remise bureau) affiche le mot de passe temporaire une seule fois', async ({ page }) => {
    const unique = `E2E${Date.now()}`;
    await page.getByLabel('Nom *').fill(unique);
    await page.getByLabel('Prénom *').fill('Test');
    await page.getByLabel('Sexe * (M/F)').fill('F');
    await page.getByLabel("Antenne d'origine *").selectOption({ index: 1 });
    await page.getByRole('button', { name: /créer le membre/i }).click();

    await expect(page.getByText('Membre créé')).toBeVisible();
    // Remise bureau : un mot de passe temporaire est présenté. Après navigation, il n'est plus affiché.
    await page.getByRole('link', { name: /retour à la liste/i }).click();
    await expect(page.getByText('Membre créé')).toHaveCount(0);
  });
});
