import { expect, test } from '@playwright/test';

/**
 * US1 — recherche/consultation et RBAC. Prérequis : API + CORS + compte de test.
 * Renseigner E2E_REFERENCE / E2E_PASSWORD (compte manage_members) pour le parcours authentifié.
 */
const REFERENCE = process.env['E2E_REFERENCE'];
const PASSWORD = process.env['E2E_PASSWORD'];

test('accès direct à /members sans session → redirection connexion', async ({ page }) => {
  await page.goto('/members');
  await expect(page).toHaveURL(/\/login/);
});

test.describe('parcours authentifié (manage_members)', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_REFERENCE / E2E_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
  });

  test('l\'entrée Membres est visible et mène à la liste', async ({ page }) => {
    await page.getByRole('link', { name: 'Membres' }).click();
    await expect(page).toHaveURL(/\/members/);
    await expect(page.getByRole('heading', { name: 'Membres' })).toBeVisible();
  });

  test('recherche puis ouverture d\'une fiche', async ({ page }) => {
    await page.goto('/members');
    await page.getByPlaceholder('Nom, référence ou contact…').fill('LUM');
    await page.getByRole('button', { name: /rechercher/i }).click();
    // Ouvre la première référence si présente
    const firstRef = page.locator('tbody tr td a').first();
    if (await firstRef.count()) {
      await firstRef.click();
      await expect(page.getByRole('link', { name: /modifier/i })).toBeVisible();
    }
  });
});
