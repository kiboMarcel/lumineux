import { expect, test } from '@playwright/test';

/**
 * Feature 014 · US2 — suivi des présences en temps réel (liste + décompte + filtre).
 * Prérequis : API + CORS + compte **manage_attendance** + une antenne active.
 * Renseigner E2E_ATT_REFERENCE / E2E_ATT_PASSWORD.
 */
const REFERENCE = process.env['E2E_ATT_REFERENCE'];
const PASSWORD = process.env['E2E_ATT_PASSWORD'];

test.describe('présences — suivi temps réel', () => {
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

  test('affiche le décompte des valides et permet de filtrer par statut', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /valide\(s\)/i })).toBeVisible();
    await page.getByRole('button', { name: 'Toutes' }).click();
    await page.getByRole('button', { name: 'Valides' }).click();
    await expect(page.getByRole('heading', { name: /valide\(s\)/i })).toBeVisible();
  });
});
