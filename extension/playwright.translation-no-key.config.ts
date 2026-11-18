import { defineConfig } from "@playwright/test";

// Opt-in smoke test: use an already running local backend with no DeepL key.
export default defineConfig({
  testDir: "./tests/demo",
  testMatch: "translation-no-key-smoke.test.ts",
  workers: 1,
  retries: 0,
  timeout: 60000,
  outputDir: "test-results/translation-no-key",
  reporter: [["line"]],
  use: { headless: true, trace: "off", video: "off" },
});
