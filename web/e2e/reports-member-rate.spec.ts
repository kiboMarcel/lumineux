import { expect, test } from '@playwright/test';

/**
 * Feature 019 · US3 — taux d'assiduité d'un membre (sélection via recherche allégée + jauge %).
 * Prérequis : API + CORS + compte **manage_attendance** + un membre recherchable (E2E_ATT_MEMBER_QUERY).
 */
const REFERENCE = process.env['E2E_ATT_REFERENCE'];
const PASSWORD = process.env['E2E_ATT_PASSWORD'];
const MEMBER_QUERY = process.env['E2E_ATT_MEMBER_QUERY'];

test.describe('rapports — taux membre', () => {
  test.skip(!REFERENCE || !PASSWORD || !MEMBER_QUERY, 'Définir E2E_ATT_REFERENCE / E2E_ATT_PASSWORD / E2E_ATT_MEMBER_QUERY');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();
    await page.goto('/reports');
  });

  test('sélectionne un membre et affiche son taux en pourcentage', async ({ page }) => {
    await page.getByPlaceholder('Référence ou nom du membre…').fill(MEMBER_QUERY!);
    await page.getByRole('button', { name: 'Rechercher' }).click();
    await page.getByRole('button', { name: 'Voir le taux' }).first().click();
    // Le taux est affiché en pourcentage.
    await expect(page.getByText('%')).toBeVisible();
  });
});
