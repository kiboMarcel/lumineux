import { expect, test } from '@playwright/test';

/**
 * Feature 021 · US1/US2 — courbe d'évolution des présences.
 * Prérequis : API + CORS + compte **manage_attendance** + des présences réparties sur la période.
 * Renseigner E2E_ATT_REFERENCE / E2E_ATT_PASSWORD.
 */
const REFERENCE = process.env['E2E_ATT_REFERENCE'];
const PASSWORD = process.env['E2E_ATT_PASSWORD'];

test.describe('rapports — évolution (courbe)', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_ATT_REFERENCE / E2E_ATT_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
    await page.goto('/reports');
    await page.getByRole('button', { name: 'Afficher' }).click();
  });

  test('affiche une courbe et bascule semaine/mois', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /évolution de l'affluence/i })).toBeVisible();
    // La courbe est un tracé SVG.
    await expect(page.locator('svg polyline').first()).toBeVisible();

    // Bascule de granularité.
    await page.getByRole('button', { name: 'Par semaine' }).click();
    await expect(page.locator('svg').first()).toBeVisible();
  });
});
