import { expect, test } from '@playwright/test';

/**
 * Feature 014 · US1 — démarrer une session et projeter le QR rotatif.
 * Prérequis : API + CORS + compte **manage_attendance** + au moins une antenne active (feature 010).
 * Renseigner E2E_ATT_REFERENCE / E2E_ATT_PASSWORD.
 */
const REFERENCE = process.env['E2E_ATT_REFERENCE'];
const PASSWORD = process.env['E2E_ATT_PASSWORD'];

test.describe('présences — démarrage + QR', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_ATT_REFERENCE / E2E_ATT_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
  });

  test('le lien Présences est visible et une session démarre avec un QR', async ({ page }) => {
    await page.getByRole('link', { name: 'Présences' }).click();
    await expect(page).toHaveURL(/\/attendance$/);

    await page.getByLabel('Antenne').selectOption({ index: 1 });
    await page.getByLabel('Date de réunion').fill(new Date().toISOString().slice(0, 10));
    await page.getByRole('button', { name: /démarrer la session/i }).click();

    // Écran d'animation : URL de session + image QR projetée.
    await expect(page).toHaveURL(/\/attendance\/sessions\/\d+/);
    await expect(page.locator('qrcode')).toBeVisible();
  });
});
