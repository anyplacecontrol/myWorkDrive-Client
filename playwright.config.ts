import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './trace-tools',
  timeout: 30000,
  use: {
    baseURL: 'http://localhost:5173',
    headless: false, // Set to true for CI
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: { browserName: 'chromium' },
    },
  ],
});
