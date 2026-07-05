import { expect, test } from '@playwright/test';

/**
 * US2 — activation / première connexion. Prérequis : API démarrée + compte en attente d'activation.
 * Renseigner E2E_PENDING_REFERENCE / E2E_TEMP_PASSWORD pour le parcours complet.
 */
const REFERENCE = process.env['E2E_PENDING_REFERENCE'];
const TEMP = process.env['E2E_TEMP_PASSWORD'];

test('affiche l\'écran d\'activation avec référence pré-remplie', async ({ page }) => {
  await page.goto('/auth/activate?reference=LUM-2026-00042');
  await expect(page.getByRole('heading', { name: 'Première connexion' })).toBeVisible();
  await expect(page.getByLabel('Référence')).toHaveValue('LUM-2026-00042');
});

test.describe('parcours activation', () => {
  test.skip(!REFERENCE || !TEMP, 'Définir E2E_PENDING_REFERENCE / E2E_TEMP_PASSWORD');

  test('activation avec un nouveau mot de passe conforme', async ({ page }) => {
    await page.goto('/auth/activate');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe temporaire').fill(TEMP!);
    await page.getByLabel('Nouveau mot de passe').fill('Nouveau123');
    await page.getByLabel('Confirmer').fill('Nouveau123');
    await page.getByRole('button', { name: /activer mon compte/i }).click();
    await expect(page.getByRole('button', { name: /se déconnecter/i })).toBeVisible();
  });
});
