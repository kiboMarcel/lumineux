import { expect, test } from '@playwright/test';

/**
 * US1 — consultation & RBAC lecture. Prérequis : API + CORS + comptes de test.
 * E2E_REFERENCE / E2E_PASSWORD = compte avec un droit de lecture (admin profils ou gestion membres).
 */
const REFERENCE = process.env['E2E_REFERENCE'];
const PASSWORD = process.env['E2E_PASSWORD'];

test('accès direct à /bureau-profiles sans session → connexion', async ({ page }) => {
  await page.goto('/bureau-profiles');
  await expect(page).toHaveURL(/\/login/);
});

test.describe('parcours authentifié (lecture)', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_REFERENCE / E2E_PASSWORD');

  test('l\'entrée Profils du bureau mène à la liste', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
    await page.getByRole('link', { name: 'Profils du bureau' }).click();
    await expect(page).toHaveURL(/\/bureau-profiles/);
    await expect(page.getByRole('heading', { name: 'Profils du bureau' })).toBeVisible();
  });
});
