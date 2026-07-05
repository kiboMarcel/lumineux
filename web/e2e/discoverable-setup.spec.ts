import { expect, test } from '@playwright/test';

/**
 * Feature 013 — découvrabilité de l'installation. Prérequis : API + CORS.
 * L'affichage du lien dépend de l'état d'installation de l'instance ; renseigner
 * E2E_INSTANCE_STATE = "virgin" (non initialisée) ou "installed" (déjà installée) pour cibler le cas.
 */
const STATE = process.env['E2E_INSTANCE_STATE'];

test('l\'écran de connexion s\'affiche', async ({ page }) => {
  await page.goto('/login');
  await expect(page.getByRole('heading', { name: 'Connexion' })).toBeVisible();
});

test('instance vierge : le lien « Première installation » est visible et mène à l\'installation', async ({ page }) => {
  test.skip(STATE !== 'virgin', 'Définir E2E_INSTANCE_STATE=virgin');
  await page.goto('/login');
  const link = page.getByRole('link', { name: 'Première installation' });
  await expect(link).toBeVisible();
  await link.click();
  await expect(page).toHaveURL(/\/setup\/first-admin/);
  await expect(page.getByRole('heading', { name: /premier administrateur/i })).toBeVisible();
});

test('instance déjà installée : aucun lien d\'installation', async ({ page }) => {
  test.skip(STATE !== 'installed', 'Définir E2E_INSTANCE_STATE=installed');
  await page.goto('/login');
  await expect(page.getByRole('link', { name: 'Première installation' })).toHaveCount(0);
});
