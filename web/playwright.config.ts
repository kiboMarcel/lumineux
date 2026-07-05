import { defineConfig, devices } from '@playwright/test';

/**
 * Configuration Playwright (feature 008, e2e). Les scénarios s'exécutent contre la SPA servie
 * localement (`npm start` → http://localhost:4200) et supposent une **API Lumineux démarrée** avec
 * CORS autorisant cette origine, ainsi qu'une instance disposant de comptes de test.
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm start',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 120_000,
  },
});
