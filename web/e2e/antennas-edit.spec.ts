import { expect, test } from '@playwright/test';

/**
 * Feature 017 · US3 — édition d'une antenne (libellé/district) ; code en lecture seule.
 * Prérequis : API + CORS + compte **manage_referentials** + au moins une antenne existante.
 */
const REFERENCE = process.env['E2E_REF_REFERENCE'];
const PASSWORD = process.env['E2E_REF_PASSWORD'];

test.describe('antennes — édition', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_REF_REFERENCE / E2E_REF_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
    await page.goto('/antennas');
  });

  test('modifie le libellé ; le code est en lecture seule', async ({ page }) => {
    await page.getByRole('link', { name: 'Modifier' }).first().click();
    await expect(page).toHaveURL(/\/antennas\/\d+\/edit$/);

    // Le champ code est désactivé (immuable).
    await expect(page.getByLabel(/Code/)).toBeDisabled();

    await page.getByLabel('Libellé *').fill('Libellé modifié E2E');
    await page.getByRole('button', { name: /enregistrer/i }).click();
    await expect(page).toHaveURL(/\/antennas$/);
  });
});
