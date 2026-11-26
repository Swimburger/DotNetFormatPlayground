import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests/e2e",
  timeout: 90_000,
  outputDir: "./test-results",
  reporter: [["html", { outputFolder: "playwright-report", open: "never" }]],
  use: {
    baseURL: "http://127.0.0.1:8794",
  },
  webServer: {
    command: "./node_modules/.bin/cf dev -- --port 8794",
    url: "http://127.0.0.1:8794",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});
