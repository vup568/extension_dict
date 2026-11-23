import { test, expect, chromium, type BrowserContext } from "@playwright/test";
import { createServer } from "node:http";
import { readFile, mkdtemp, rm } from "node:fs/promises";
import { existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { resolve, join, sep } from "node:path";
import type { AnalysisResponse } from "../../src/api/analysisContract.ts";

const apiUrl = "http://127.0.0.1:5000/api/analysis";
const examples = [
  { text: "食べる", canonical: "fixture:word:001", gloss: "Ăn", base: null, alternatives: 0 },
  { text: "食べました", canonical: "fixture:word:001", gloss: "Ăn", base: "食べる", alternatives: 1 },
  { text: "食べられる", canonical: "fixture:word:001", gloss: "Ăn", base: "食べる", alternatives: 3 },
  { text: "静かでした", canonical: "fixture:word:010", gloss: "Yên tĩnh", base: "静か", alternatives: 1 },
  { text: "学生です", canonical: "fixture:word:013", gloss: "Học sinh/sinh viên", base: "学生", alternatives: 1 },
  { text: "架空辞書項目未登録", canonical: null, gloss: null, base: null, alternatives: 0 },
] as const;

test("actual packaged MV3 popup reads isolated demo dictionary and live F09 API", async ({ request }, testInfo) => {
  const health = await request.get("http://127.0.0.1:5000/health/live");
  expect(health.status(), "Start scripts/start-demo.ps1 before running this explicit suite").toBe(200);
  const executablePath = [
    process.env.EXTENSION_SMOKE_EXECUTABLE_PATH,
    // Current branded Chrome ignores automation's --load-extension. Use an installed
    // extension-capable Chromium sibling; manual Chrome Load unpacked remains separate.
    process.env.BRAVE_EXECUTABLE_PATH,
    process.env.LOCALAPPDATA ? join(process.env.LOCALAPPDATA, "BraveSoftware/Brave-Browser/Application/brave.exe") : undefined,
    chromium.executablePath(),
  ].find((candidate): candidate is string => candidate !== undefined && existsSync(candidate));
  expect(executablePath, "An installed extension-capable browser is required; this test never downloads one").toBeDefined();
  if (executablePath === undefined) throw new Error("No installed extension-capable browser");

  // Only serves a synthetic reading page; /api/analysis is never intercepted or mocked.
  const server = createServer((_req, res) => {
    res.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
    res.end(`<!doctype html><html><head><meta charset="UTF-8"></head><body>${examples.map((example, index) => `<p id="word-${index}">${example.text}</p>`).join("")}</body></html>`);
  });
  await new Promise<void>((ready, reject) => { server.once("error", reject); server.listen(0, "127.0.0.1", ready); });
  const address = server.address();
  if (address === null || typeof address === "string") throw new Error("Unexpected local reading page address");
  const profile = await mkdtemp(join(tmpdir(), "jp-reading-demo-"));
  let context: BrowserContext | undefined;
  try {
    const extensionPath = resolve("dist/chromium");
    const manifest = JSON.parse(await readFile(join(extensionPath, "manifest.json"), "utf8")) as { host_permissions: string[] };
    expect(manifest.host_permissions).toEqual(["http://127.0.0.1:5000/*"]);
    context = await chromium.launchPersistentContext(profile, {
      executablePath, headless: true,
      args: [`--disable-extensions-except=${extensionPath}`, `--load-extension=${extensionPath}`],
    });
    let worker = context.serviceWorkers()[0];
    worker ??= await context.waitForEvent("serviceworker", { timeout: 15000 });
    expect(worker.url()).toMatch(/^chrome-extension:\/\/[^/]+\/background\.js$/u);
    const page = await context.newPage();
    const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    const requests: { text: string; header: string | undefined; keys: string[] }[] = [];
    context.on("request", outgoing => {
      if (outgoing.url() !== apiUrl || outgoing.method() !== "POST") return;
      const body = outgoing.postDataJSON() as { text: string };
      requests.push({ text: body.text, header: outgoing.headers()["x-contract-version"], keys: Object.keys(body).sort() });
    });
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
      expect(data.data.capabilities.vocabulary).toBe("completed");
      expect(data.data.capabilities.conjugation, `Conjugation capability for synthetic example #${index + 1}`).toBe(example.alternatives > 0 ? "completed" : "unavailable");
      const popup = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
      await expect(popup).toBeVisible();
      await expect(popup.getByRole("alert")).toHaveCount(0);
      const vocabulary = popup.getByRole("tabpanel", { name: "Từ vựng" });
      if (example.canonical === null) {
        expect(data.data.dictionary_matches).toHaveLength(0);
        await expect(vocabulary.getByText("Không tìm thấy kết quả.", { exact: true })).toBeVisible();
      } else {
        expect(data.data.dictionary_matches.map(entry => entry.canonicalId)).toContain(example.canonical);
        await expect(vocabulary.locator(`[data-canonical-id="${example.canonical}"]`).getByText(example.gloss!, { exact: true })).toBeVisible();
      }
      expect(data.data.conjugations).toHaveLength(example.alternatives);
      await expect(popup.locator("[data-rule-id]")).toHaveCount(example.alternatives);
      for (const conjugation of data.data.conjugations) {
        expect(conjugation.base_form).toBe(example.base);
        expect(conjugation.language_tag).toBe("vi");
        expect(conjugation.dataset_version).toBe("candidate-20261110.1");
        await expect(popup.locator(`[data-rule-id="${conjugation.rule_id}"]`).getByText(conjugation.explanation, { exact: true })).toBeVisible();
      }
      if (example.text === "食べられる") {
        await expect(popup.getByText("Các cách hiểu có thể", { exact: true })).toBeVisible();
        const explanations = data.data.conjugations.map(item => item.explanation).join(" ");
        expect(explanations).toMatch(/khả năng/iu);
        expect(explanations).toMatch(/bị động/iu);
        expect(explanations).toMatch(/kính ngữ/iu);
      }
      if (example.text === "食べました") await page.screenshot({ path: testInfo.outputPath("tabemashita-popup.png"), fullPage: true });
      await popup.getByRole("button", { name: "Đóng tra cứu" }).click();
      await expect(popup).toHaveCount(0);
    }
    expect(requests.map(item => item.text)).toEqual(examples.map(item => item.text));
    for (const outgoing of requests) {
      expect(outgoing.header).toBe("3");
      expect(outgoing.keys).toEqual(["interaction_id", "text"]);
    }
    expect(errors).toEqual([]);
    expect(await page.evaluate(() => [localStorage.length, sessionStorage.length])).toEqual([0, 0]);
  } finally {
    await context?.close();
    await new Promise<void>(done => server.close(() => done()));
    if (!resolve(profile).startsWith(resolve(tmpdir()) + sep) || !profile.includes("jp-reading-demo-")) throw new Error("Unexpected profile cleanup path");
    await rm(profile, { recursive: true, force: true });
  }
});
