import { defineConfig } from "@playwright/test";

// Explicit opt-in configuration: the default browser suite has no external demo DB dependency.
export default defineConfig({
  testDir: "./tests/demo",
  testMatch: "demo-dictionary-smoke.test.ts",
  fullyParallel: false,
  forbidOnly: true,
  retries: 0,
  workers: 1,
  timeout: 90000,
  outputDir: "test-results/local-demo",
  reporter: [["line"], ["json", { outputFile: "test-results/local-demo/results.json" }]],
  use: { headless: true, trace: "off", video: "off" },
});
