import { expect, test } from '@playwright/test';

/**
 * Feature 014 · US4 — clôture confirmée puis masquage du QR et des écritures.
 * Prérequis : API + CORS + compte **manage_attendance** + une antenne active.
 * Renseigner E2E_ATT_REFERENCE / E2E_ATT_PASSWORD.
 */
const REFERENCE = process.env['E2E_ATT_REFERENCE'];
const PASSWORD = process.env['E2E_ATT_PASSWORD'];

test.describe('présences — clôture', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_ATT_REFERENCE / E2E_ATT_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
    await page.goto('/attendance');
    await page.getByLabel('Antenne').selectOption({ index: 1 });
    await page.getByLabel('Date de réunion').fill(new Date().toISOString().slice(0, 10));
    await page.getByRole('button', { name: /démarrer la session/i }).click();
    await expect(page).toHaveURL(/\/attendance\/sessions\/\d+/);
  });

  test('clôture (avec confirmation) puis QR et actions d\'écriture disparaissent', async ({ page }) => {
    await expect(page.locator('qrcode')).toBeVisible();

    page.once('dialog', (d) => d.accept());
    await page.getByRole('button', { name: /clôturer la session/i }).click();

    await expect(page.getByText(/session clôturée/i)).toBeVisible();
    await expect(page.locator('qrcode')).toHaveCount(0);
    await expect(page.getByRole('button', { name: /clôturer la session/i })).toHaveCount(0);
    await expect(page.getByText("Ajout manuel d'une présence")).toHaveCount(0);
  });
});
