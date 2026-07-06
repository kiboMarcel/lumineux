import { expect, test } from '@playwright/test';

/**
 * Feature 019 · US1 — synthèse par antenne (tableau + barres).
 * Prérequis : API + CORS + compte **manage_attendance** + des présences sur la période.
 * Renseigner E2E_ATT_REFERENCE / E2E_ATT_PASSWORD.
 */
const REFERENCE = process.env['E2E_ATT_REFERENCE'];
const PASSWORD = process.env['E2E_ATT_PASSWORD'];

test.describe('rapports — synthèse', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_ATT_REFERENCE / E2E_ATT_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
  });

  test('le lien Rapports est visible et la synthèse s\'affiche', async ({ page }) => {
    await page.getByRole('link', { name: 'Rapports' }).click();
    await expect(page).toHaveURL(/\/reports$/);
    await expect(page.getByRole('heading', { name: /rapports de présence/i })).toBeVisible();
    await page.getByRole('button', { name: 'Afficher' }).click();
    // Tableau (en-têtes) présent.
    await expect(page.getByText('Présences valides')).toBeVisible();
  });
});
