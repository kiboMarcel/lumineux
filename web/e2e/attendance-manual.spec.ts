import { expect, test } from '@playwright/test';

/**
 * Feature 014 · US3 — ajout manuel via recherche allégée (015) + annulation confirmée.
 * Prérequis : API + CORS + compte **manage_attendance** + une antenne active + au moins un membre
 * recherchable (E2E_ATT_MEMBER_QUERY). Renseigner E2E_ATT_REFERENCE / E2E_ATT_PASSWORD.
 */
const REFERENCE = process.env['E2E_ATT_REFERENCE'];
const PASSWORD = process.env['E2E_ATT_PASSWORD'];
const MEMBER_QUERY = process.env['E2E_ATT_MEMBER_QUERY'];

test.describe('présences — ajout manuel + annulation', () => {
  test.skip(!REFERENCE || !PASSWORD || !MEMBER_QUERY, 'Définir E2E_ATT_REFERENCE / E2E_ATT_PASSWORD / E2E_ATT_MEMBER_QUERY');

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

  test('ajoute un membre trouvé par lookup puis annule la présence (avec confirmation)', async ({ page }) => {
    // Ajout manuel : recherche allégée → sélection → ajout.
    await page.getByPlaceholder('Référence ou nom…').fill(MEMBER_QUERY!);
    await page.getByRole('button', { name: 'Rechercher' }).click();
    await page.getByRole('button', { name: 'Ajouter' }).first().click();

    // Réajout : idempotent (aucune erreur bloquante).
    await page.getByRole('button', { name: 'Ajouter' }).first().click();

    // Annulation : confirmation requise.
    page.once('dialog', (d) => d.accept());
    await page.getByRole('button', { name: 'Annuler' }).first().click();
  });
});
