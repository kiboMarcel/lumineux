import { expect, test } from '@playwright/test';

/**
 * Feature 019 · US2 — export CSV de la synthèse (téléchargement authentifié).
 * Prérequis : API + CORS + compte **manage_attendance** + des présences sur la période.
 */
const REFERENCE = process.env['E2E_ATT_REFERENCE'];
const PASSWORD = process.env['E2E_ATT_PASSWORD'];

test.describe('rapports — export CSV', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_ATT_REFERENCE / E2E_ATT_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
    await page.goto('/reports');
    await page.getByRole('button', { name: 'Afficher' }).click();
  });

  test('le bouton Exporter déclenche un téléchargement CSV', async ({ page }) => {
    const downloadPromise = page.waitForEvent('download');
    await page.getByRole('button', { name: /exporter \(csv\)/i }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/presence-antennes_.*\.csv/);
  });
});
