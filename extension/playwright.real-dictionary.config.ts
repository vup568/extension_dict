import { defineConfig } from "@playwright/test";

// Opt-in: requires a separately started backend using a fully imported real-data database.
export default defineConfig({
  testDir: "./tests/demo",
  testMatch: "real-dictionary-smoke.test.ts",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 90000,
  outputDir: "test-results/real-dictionary",
  reporter: [["line"]],
  use: { headless: true, trace: "off", video: "off" },
});
