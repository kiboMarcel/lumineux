import { expect, test } from '@playwright/test';

/**
 * Feature 024 · US1 — reprise d'une session de présence en cours.
 * Prérequis : API + CORS + compte **manage_attendance** + une antenne active.
 * Renseigner E2E_ATT_REFERENCE / E2E_ATT_PASSWORD.
 */
const REFERENCE = process.env['E2E_ATT_REFERENCE'];
const PASSWORD = process.env['E2E_ATT_PASSWORD'];

test.describe('présences — reprise de session', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_ATT_REFERENCE / E2E_ATT_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
  });

  test('démarrer, naviguer ailleurs, revenir puis reprendre la session', async ({ page }) => {
    await page.goto('/attendance');
    await page.getByLabel('Antenne').selectOption({ index: 1 });
    await page.getByLabel('Date de réunion').fill(new Date().toISOString().slice(0, 10));
    await page.getByRole('button', { name: /démarrer la session/i }).click();
    await expect(page).toHaveURL(/\/attendance\/sessions\/\d+/);

    // Navigation accidentelle ailleurs puis retour à l'écran de démarrage.
    await page.getByRole('link', { name: 'Accueil' }).click();
    await page.goto('/attendance');

    // L'encart de reprise apparaît → reprendre.
    await expect(page.getByText(/vous avez une session en cours/i)).toBeVisible();
    await page.getByRole('button', { name: 'Reprendre' }).first().click();
    await expect(page).toHaveURL(/\/attendance\/sessions\/\d+/);
  });
});
