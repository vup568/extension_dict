import { resolve } from "node:path";
import { defineConfig } from "vite";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [tailwindcss()],
  build: {
    lib: { entry: resolve(import.meta.dirname, "src/content.ts"), formats: ["iife"], name: "JPReadingExtension", fileName: () => "content.js" },
    outDir: "dist/chromium", emptyOutDir: true, sourcemap: false, target: "es2022", minify: true,
  },
});
