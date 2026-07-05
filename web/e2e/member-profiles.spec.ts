import { expect, test } from '@playwright/test';

/**
 * US3 — attribution/révocation des profils d'un membre. Prérequis : API + CORS + compte
 * administrateur des profils + un membre cible. E2E_ADMIN_REFERENCE / E2E_ADMIN_PASSWORD /
 * E2E_MEMBER_ID.
 */
const REFERENCE = process.env['E2E_ADMIN_REFERENCE'];
const PASSWORD = process.env['E2E_ADMIN_PASSWORD'];
const MEMBER_ID = process.env['E2E_MEMBER_ID'];

test.describe('profils & droits d\'un membre', () => {
  test.skip(!REFERENCE || !PASSWORD || !MEMBER_ID, 'Définir E2E_ADMIN_REFERENCE / E2E_ADMIN_PASSWORD / E2E_MEMBER_ID');

  test('attribuer puis révoquer un profil', async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel('Référence').fill(REFERENCE!);
    await page.getByLabel('Mot de passe').fill(PASSWORD!);
    await page.getByRole('button', { name: /se connecter/i }).click();

    await page.goto(`/members/${MEMBER_ID}/profiles`);
    await expect(page.getByRole('heading', { name: /Profils & droits/i })).toBeVisible();

    // Attribuer le premier profil disponible
    await page.locator('select[name="toAssign"]').selectOption({ index: 1 });
    await page.getByRole('button', { name: /attribuer/i }).click();

    // Révoquer (confirmation)
    page.on('dialog', (d) => d.accept());
    const revoke = page.getByRole('button', { name: /révoquer/i }).first();
    if (await revoke.count()) {
      await revoke.click();
    }
  });
});
