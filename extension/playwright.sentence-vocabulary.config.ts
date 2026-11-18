import { defineConfig } from "@playwright/test";

// Opt-in: a separate backend must already be serving the real local dictionary.
export default defineConfig({
  testDir: "./tests/demo",
  testMatch: "sentence-vocabulary-smoke.test.ts",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 120000,
  outputDir: "test-results/sentence-vocabulary",
  reporter: [["line"]],
  use: { headless: true, trace: "off", video: "off" },
});
