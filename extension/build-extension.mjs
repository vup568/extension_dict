import { copyFile, mkdir, writeFile } from "node:fs/promises";
import { resolve } from "node:path";
import { build, loadEnv } from "vite";
import { validateBackendOrigin } from "./src/platform/backendOrigin.ts";

const root = import.meta.dirname;
const environment = loadEnv("production", root, "VITE_");
const origin = validateBackendOrigin(process.env.VITE_API_BASE_URL ?? environment.VITE_API_BASE_URL ?? "http://127.0.0.1:5000");
const define = { "import.meta.env.VITE_API_BASE_URL": JSON.stringify(origin), "process.env.NODE_ENV": JSON.stringify("production") };
await build({ configFile: resolve(root, "vite.extension.config.ts"), define });
await build({ configFile: false, define, build: {
  outDir: "dist/chromium", emptyOutDir: false, sourcemap: false, target: "es2022", minify: true,
  lib: { entry: resolve(root, "src/background.ts"), formats: ["iife"], name: "JPReadingBackground", fileName: () => "background.js" },
} });
await mkdir(resolve(root, "dist/firefox"), { recursive: true });
for (const filename of ["content.js", "background.js"]) {
  await copyFile(resolve(root, "dist/chromium", filename), resolve(root, "dist/firefox", filename));
}
const common = {
  manifest_version: 3, name: "JP Reading Platform", version: "0.1.0",
  description: "Tra cứu tiếng Nhật từ văn bản bôi chọn trên trang.",
  host_permissions: [`${origin}/*`],
  content_scripts: [{ matches: ["http://*/*", "https://*/*"], js: ["content.js"], run_at: "document_idle", all_frames: true }],
};
await writeFile(resolve(root, "dist/chromium/manifest.json"), JSON.stringify({ ...common, background: { service_worker: "background.js" } }, null, 2) + "\n");
await writeFile(resolve(root, "dist/firefox/manifest.json"), JSON.stringify({ ...common, background: { scripts: ["background.js"] },
  browser_specific_settings: { gecko: { id: "jp-reading-platform@local.invalid", data_collection_permissions: { required: ["websiteContent"] } } },
}, null, 2) + "\n");
