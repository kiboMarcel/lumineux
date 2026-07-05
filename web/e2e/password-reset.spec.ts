import { expect, test } from '@playwright/test';

/**
 * US3 — mot de passe oublié (anti-énumération, SC-006). Prérequis : API démarrée.
 * Le message affiché doit être identique quelle que soit la référence saisie.
 */
test('mot de passe oublié : message générique identique', async ({ page }) => {
  const submitAndRead = async (reference: string) => {
    await page.goto('/auth/forgot-password');
    await page.getByLabel('Référence').fill(reference);
    await page.getByRole('button', { name: /envoyer le lien/i }).click();
    return (await page.getByRole('status').textContent())?.trim();
  };

  const existing = await submitAndRead('LUM-EXISTE-PEUT-ETRE');
  const missing = await submitAndRead('LUM-INEXISTANT-XYZ');

  expect(existing).toBeTruthy();
  expect(missing).toBe(existing);
});

test('réinitialisation : jeton manquant signalé', async ({ page }) => {
  await page.goto('/auth/reset-password');
  await expect(page.getByText('Lien invalide : jeton manquant.')).toBeVisible();
});
