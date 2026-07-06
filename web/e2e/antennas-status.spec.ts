import { expect, test } from '@playwright/test';

/**
 * Feature 017 · US4 — désactivation (confirmée) / réactivation d'une antenne.
 * Prérequis : API + CORS + compte **manage_referentials** + au moins une antenne active sans session
 * ouverte. Renseigner E2E_REF_REFERENCE / E2E_REF_PASSWORD.
 */
const REFERENCE = process.env['E2E_REF_REFERENCE'];
const PASSWORD = process.env['E2E_REF_PASSWORD'];

test.describe('antennes — statut', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_REF_REFERENCE / E2E_REF_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
    await page.goto('/antennas');
  });

  test('désactive (avec confirmation) puis réactive une antenne', async ({ page }) => {
    // Désactivation confirmée.
    page.once('dialog', (d) => d.accept());
    await page.getByRole('button', { name: 'Désactiver' }).first().click();
    await expect(page.getByRole('button', { name: 'Réactiver' }).first()).toBeVisible();

    // Réactivation (sans confirmation).
    await page.getByRole('button', { name: 'Réactiver' }).first().click();
    await expect(page.getByRole('button', { name: 'Désactiver' }).first()).toBeVisible();
  });
});
