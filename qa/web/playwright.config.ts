import { defineConfig, devices } from '@playwright/test';

const webOrigin = process.env.QA_WEB_ORIGIN ?? 'http://localhost:5173';
const apiOrigin = process.env.QA_API_ORIGIN ?? 'http://127.0.0.1:5080';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list']],
  use: {
    baseURL: webOrigin,
    actionTimeout: 10_000,
    navigationTimeout: 15_000,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    extraHTTPHeaders: {
      origin: webOrigin,
      'x-forwarded-host': webOrigin.replace(/^https?:\/\//, ''),
    },
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1366, height: 900 } },
    },
  ],
});

export const QA_API_ORIGIN = apiOrigin;
