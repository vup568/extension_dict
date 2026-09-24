import { defineConfig, devices, type Project } from "@playwright/test";

const projects: Project[] = [
  {
    name: "chrome",
    use: {
      ...devices["Desktop Chrome"],
      channel: "chrome",
    },
  },
  {
    name: "edge",
    use: {
      ...devices["Desktop Edge"],
      channel: "msedge",
    },
  },
];

const braveExecutable = process.env.BRAVE_EXECUTABLE_PATH;
if (braveExecutable !== undefined && braveExecutable.length > 0) {
  projects.push({
    name: "brave",
    use: {
      ...devices["Desktop Chrome"],
      launchOptions: {
        executablePath: braveExecutable,
      },
    },
  });
}

const firefoxExecutable = process.env.FIREFOX_EXECUTABLE_PATH;
if (firefoxExecutable !== undefined && firefoxExecutable.length > 0) {
  projects.push({
    name: "firefox",
    use: {
      browserName: "firefox",
      launchOptions: {
        executablePath: firefoxExecutable,
      },
    },
  });
}

export default defineConfig({
  testDir: "./tests/browser",
  fullyParallel: true,
  forbidOnly: true,
  retries: 0,
  workers: 2,
  reporter: [
    ["line"],
    ["json", { outputFile: "test-results/browser-results.json" }],
  ],
  use: {
    baseURL: "http://127.0.0.1:4173",
    headless: true,
    trace: "retain-on-failure",
  },
  projects,
  webServer: {
    command: "vite --host 127.0.0.1 --port 4173",
    port: 4173,
    reuseExistingServer: false,
  },
});
