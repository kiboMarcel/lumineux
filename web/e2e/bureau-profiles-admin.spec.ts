import { expect, test } from '@playwright/test';

/**
 * US2 — administration des profils. Prérequis : API + CORS + compte administrateur des profils
 * (manage_bureau_profiles). E2E_ADMIN_REFERENCE / E2E_ADMIN_PASSWORD.
 */
const REFERENCE = process.env['E2E_ADMIN_REFERENCE'];
const PASSWORD = process.env['E2E_ADMIN_PASSWORD'];

test.describe('administration des profils', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_ADMIN_REFERENCE / E2E_ADMIN_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
  });

  test('créer un profil puis le supprimer (avec confirmation)', async ({ page }) => {
    await page.goto('/bureau-profiles/new');
    const name = `E2E-${Date.now()}`;
    await page.getByLabel('Nom *').fill(name);
    await page.getByRole('checkbox').first().check();
    await page.getByRole('button', { name: /créer le profil/i }).click();

    await expect(page.getByRole('heading', { name })).toBeVisible();

    page.on('dialog', (d) => d.accept()); // confirmation de suppression
    await page.getByRole('button', { name: /supprimer/i }).click();
    await expect(page).toHaveURL(/\/bureau-profiles$/);
  });
});
