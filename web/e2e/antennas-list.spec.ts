import { expect, test } from '@playwright/test';

/**
 * Feature 017 · US1 — liste de gestion des antennes (actives + inactives).
 * Prérequis : API + CORS + compte **manage_referentials**. Renseigner E2E_REF_REFERENCE / E2E_REF_PASSWORD.
 */
const REFERENCE = process.env['E2E_REF_REFERENCE'];
const PASSWORD = process.env['E2E_REF_PASSWORD'];

test.describe('antennes — liste', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_REF_REFERENCE / E2E_REF_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
  });

  test('le lien Antennes est visible et la liste s\'affiche', async ({ page }) => {
    await page.getByRole('link', { name: 'Antennes' }).click();
    await expect(page).toHaveURL(/\/antennas$/);
    await expect(page.getByRole('heading', { name: 'Antennes' })).toBeVisible();
    await expect(page.getByRole('link', { name: /nouvelle antenne/i })).toBeVisible();
  });
});
