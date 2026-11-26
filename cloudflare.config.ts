import { bindings, defineConfig } from "cf/config";

export default defineConfig({
  worker: {
    name: "dotnet-format-playground",
    compatibilityDate: "2026-11-26",
    entrypoint: "./src/worker.ts",
    env: {
      ASSETS: bindings.assets(),
    },
    domains: ["format.swimburger.net"],
    assets: {
      notFoundHandling: "single-page-application",
      runWorkerFirst: ["/_framework/*"],
    },
    observability: {
      enabled: true,
      redactQueryString: true,
      logs: {
        enabled: true,
        headSamplingRate: 1,
      },
      traces: {
        enabled: true,
        headSamplingRate: 0.01,
      },
    },
  },
});
