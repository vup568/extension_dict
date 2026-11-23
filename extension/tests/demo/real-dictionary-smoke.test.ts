import { test, expect, chromium, type BrowserContext } from "@playwright/test";
import { createServer } from "node:http";
import { mkdtemp, rm } from "node:fs/promises";
import { existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { resolve, join, sep } from "node:path";
import type { AnalysisResponse } from "../../src/api/analysisContract.ts";

// These stable JMdict identifiers and glosses were checked against the locally
// pinned JMdict snapshot, not inferred from the response being tested.
const examples = [
  { text: "食べる", canonical: "dictionary:jmdict:1358280", gloss: "to eat", base: null },
  { text: "食べました", canonical: "dictionary:jmdict:1358280", gloss: "to eat", base: "食べる" },
  { text: "橋", canonical: "dictionary:jmdict:1237410", gloss: "bridge", base: null },
  { text: "箸", canonical: "dictionary:jmdict:1476410", gloss: "chopsticks", base: null },
] as const;
const apiUrl = "http://127.0.0.1:5000/api/analysis";

test("packaged extension looks up JMdict words, KANJIDIC2 and conjugation through live backend", async ({ request }) => {
  expect((await request.get("http://127.0.0.1:5000/health/live")).status()).toBe(200);
  const executablePath = [
    process.env.EXTENSION_SMOKE_EXECUTABLE_PATH,
    process.env.BRAVE_EXECUTABLE_PATH,
    process.env.LOCALAPPDATA ? join(process.env.LOCALAPPDATA, "BraveSoftware/Brave-Browser/Application/brave.exe") : undefined,
    chromium.executablePath(),
  ].find((candidate): candidate is string => candidate !== undefined && existsSync(candidate));
  expect(executablePath, "An installed extension-capable browser is required; this test never downloads one").toBeDefined();
  if (executablePath === undefined) throw new Error("No installed extension-capable browser");

  const server = createServer((_req, res) => {
    res.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
    res.end(`<!doctype html><html><head><meta charset="UTF-8"></head><body>${examples.map((x, i) => `<p id="word-${i}">${x.text}</p>`).join("")}</body></html>`);
  });
  await new Promise<void>((ready, reject) => { server.once("error", reject); server.listen(0, "127.0.0.1", ready); });
  const address = server.address();
  if (address === null || typeof address === "string") throw new Error("Unexpected reading page address");
  const profile = await mkdtemp(join(tmpdir(), "jp-reading-real-smoke-"));
  let context: BrowserContext | undefined;
  try {
    const extensionPath = resolve("dist/chromium");
    context = await chromium.launchPersistentContext(profile, {
      executablePath, headless: true,
      args: [`--disable-extensions-except=${extensionPath}`, `--load-extension=${extensionPath}`],
    });
    const worker = context.serviceWorkers()[0] ?? await context.waitForEvent("serviceworker", { timeout: 15000 });
    expect(worker.url()).toMatch(/^chrome-extension:\/\/[^/]+\/background\.js$/u);
    const page = await context.newPage();
    await page.goto(`http://127.0.0.1:${address.port}/reading`);
    for (const [index, example] of examples.entries()) {
      const responsePending = context.waitForEvent("response", {
        predicate: response => response.url() === apiUrl && response.request().postDataJSON()?.text === example.text,
        timeout: 15000,
      });
      await page.evaluate(id => {
        const range = document.createRange();
        range.selectNodeContents(document.getElementById(id)!);
        window.getSelection()?.removeAllRanges(); window.getSelection()?.addRange(range);
        document.dispatchEvent(new Event("selectionchange"));
      }, `word-${index}`);
      const response = await responsePending;
      expect(response.status()).toBe(200);
      const data = await response.json() as AnalysisResponse;
      expect(data.meta.contract_version).toBe("3");
      const entry = data.data.dictionary_matches.find(item => item.canonicalId === example.canonical);
      expect(entry).toBeDefined();
      expect(entry?.senses.flatMap(sense => sense.glosses).some(gloss =>
        gloss.languageTag === "en" && gloss.glossText === example.gloss)).toBe(true);
      const popup = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
      await expect(popup).toBeVisible();
      await expect(popup.locator(`[data-canonical-id="${example.canonical}"]`).getByText(`EN ${example.gloss}`, { exact: true })).toBeVisible();
      if (example.base !== null) expect(data.data.conjugations.some(item => item.base_form === example.base)).toBe(true);
      if (example.text === "食べる") {
        expect(data.data.kanji.some(kanji => kanji.character === "食" && kanji.meaningsEn.includes("eat"))).toBe(true);
        await popup.getByRole("tab", { name: "Kanji" }).click();
        await expect(popup.locator('[data-canonical-id="kanji:98DF"]')).toBeVisible();
      }
      await popup.getByRole("button", { name: "Đóng tra cứu" }).click();
      await expect(popup).toHaveCount(0);
    }
  } finally {
    await context?.close();
    await new Promise<void>(done => server.close(() => done()));
    if (!resolve(profile).startsWith(resolve(tmpdir()) + sep) || !profile.includes("jp-reading-real-smoke-")) throw new Error("Unexpected profile cleanup path");
    await rm(profile, { recursive: true, force: true });
  }
});
