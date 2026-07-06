import { expect, test } from '@playwright/test';

/**
 * Feature 017 · US2 — création d'une antenne + refus de code dupliqué.
 * Prérequis : API + CORS + compte **manage_referentials** + au moins un district au référentiel.
 */
const REFERENCE = process.env['E2E_REF_REFERENCE'];
const PASSWORD = process.env['E2E_REF_PASSWORD'];

test.describe('antennes — création', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_REF_REFERENCE / E2E_REF_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
    await page.goto('/antennas/new');
  });

  test('crée une antenne puis refuse un code dupliqué', async ({ page }) => {
    const code = `E2E-${Date.now()}`;
    await page.getByLabel(/Code/).fill(code);
    await page.getByLabel('Libellé *').fill('Antenne E2E');
    await page.getByLabel('District *').selectOption({ index: 1 });
    await page.getByRole('button', { name: /créer l'antenne/i }).click();

    await expect(page).toHaveURL(/\/antennas$/);
    await expect(page.getByText(code)).toBeVisible();

    // Recréer le même code → message clair.
    await page.goto('/antennas/new');
    await page.getByLabel(/Code/).fill(code);
    await page.getByLabel('Libellé *').fill('Doublon');
    await page.getByLabel('District *').selectOption({ index: 1 });
    await page.getByRole('button', { name: /créer l'antenne/i }).click();
    await expect(page.getByText(/déjà utilisé/i)).toBeVisible();
  });
});
