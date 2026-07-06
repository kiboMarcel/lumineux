import { expect, test } from '@playwright/test';

/**
 * Feature 022 · US1/US2 — export PDF par impression navigateur.
 * Prérequis : API + CORS + compte **manage_attendance**. Renseigner E2E_ATT_REFERENCE / E2E_ATT_PASSWORD.
 */
const REFERENCE = process.env['E2E_ATT_REFERENCE'];
const PASSWORD = process.env['E2E_ATT_PASSWORD'];

test.describe('rapports — export PDF (impression)', () => {
  test.skip(!REFERENCE || !PASSWORD, 'Définir E2E_ATT_REFERENCE / E2E_ATT_PASSWORD');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
    await page.goto('/reports');
    await page.getByRole('button', { name: 'Afficher' }).click();
  });

  test('le bouton Exporter en PDF est présent et déclenche l\'impression', async ({ page }) => {
    // Neutraliser le dialogue d'impression natif pour le test.
    await page.addInitScript(() => { (window as unknown as { print: () => void }).print = () => {}; });
    await page.reload();
    await page.getByRole('button', { name: 'Afficher' }).click();
    await expect(page.getByRole('button', { name: /exporter en pdf/i })).toBeVisible();
    await page.getByRole('button', { name: /exporter en pdf/i }).click();
  });

  test('en mode impression : navigation et boutons masqués, contenu visible', async ({ page }) => {
    await page.emulateMedia({ media: 'print' });
    // La barre de navigation du shell et les boutons sont masqués à l'impression.
    await expect(page.locator('header.lx-topbar')).toBeHidden();
    await expect(page.getByRole('button', { name: 'Afficher' })).toBeHidden();
    // Le contenu du rapport (titre) reste visible.
    await expect(page.getByRole('heading', { name: /rapports de présence/i })).toBeVisible();
  });
});
