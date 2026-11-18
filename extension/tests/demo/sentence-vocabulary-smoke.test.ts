import { test, expect, chromium, type BrowserContext } from "@playwright/test";
import { createServer } from "node:http";
import { mkdtemp, rm } from "node:fs/promises";
import { existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { resolve, join, sep } from "node:path";
import type { AnalysisResponse } from "../../src/api/analysisContract.ts";
import { parseScanResponse } from "../../src/api/readingContracts.ts";

// Expected identities and glosses were checked against the pinned JMdict source,
// not inferred from the backend response. All examples are synthetic text.
const examples = [
  {
    text: "私は日本語を勉強しています。",
    expected: [
      { surface: "私", start: 0, end: 1 },
      { surface: "日本語", start: 2, end: 5, canonical: "dictionary:jmdict:1464530", gloss: "Japanese (language)" },
      { surface: "勉強しています", start: 6, end: 13, canonical: "dictionary:jmdict:1512670", gloss: "study" },
    ],
  },
  {
    text: "猫を食べました。",
    expected: [
      { surface: "猫", start: 0, end: 1 },
      { surface: "食べました", start: 2, end: 7, canonical: "dictionary:jmdict:1358280", gloss: "to eat", base: "食べる" },
    ],
  },
  {
    text: "🐈猫、猫を食べました。",
    expected: [
      { surface: "猫", start: 2, end: 3 },
      { surface: "猫", start: 4, end: 5 },
      { surface: "食べました", start: 6, end: 11, canonical: "dictionary:jmdict:1358280", gloss: "to eat", base: "食べる" },
    ],
  },
  {
    text: "お茶を飲みます。",
    expected: [
      { surface: "お茶", start: 0, end: 2 },
      { surface: "飲みます", start: 3, end: 7, base: "飲む" },
    ],
  },
] as const;

const apiUrl = "http://127.0.0.1:5000/api/analysis";
const scanUrl = "http://127.0.0.1:5000/api/scan";
const scanExamples = [
  { selector: "#scan-language", text: "🐈日本語を勉強しています。", offset: 4, span: [2, 5], surface: "日本語", canonical: "dictionary:jmdict:1464530", gloss: "Japanese (language)" },
  { selector: "#scan-verb", text: "猫を食べました。", offset: 3, span: [2, 7], surface: "食べました", canonical: "dictionary:jmdict:1358280", gloss: "to eat", base: "食べる" },
  { selector: "#scan-particle", text: "猫を食べました。", offset: 1, span: null },
] as const;

test("packaged extension uses the real dictionary for selection and Shift scan", async ({ request }) => {
  expect((await request.get("http://127.0.0.1:5000/health/live")).status()).toBe(200);
  const executablePath = [
    process.env.EXTENSION_SMOKE_EXECUTABLE_PATH,
    process.env.BRAVE_EXECUTABLE_PATH,
    process.env.LOCALAPPDATA ? join(process.env.LOCALAPPDATA, "BraveSoftware/Brave-Browser/Application/brave.exe") : undefined,
    chromium.executablePath(),
  ].find((candidate): candidate is string => candidate !== undefined && existsSync(candidate));
  expect(executablePath, "An installed extension-capable browser is required; this test never downloads one").toBeDefined();
  if (executablePath === undefined) throw new Error("No installed extension-capable browser");

  const server = createServer((_request, response) => {
    response.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
    response.end(`<!doctype html><html lang="ja"><head><meta charset="UTF-8"></head><body style="font: 24px sans-serif">
      ${examples.map((x, i) => `<p id="sentence-${i}">${x.text}</p>`).join("")}
      <p>🐈<ruby>日本<rt>にほん</rt></ruby><span id="scan-language">語</span>を勉強しています。</p>
      <p>猫<span id="scan-particle">を</span>食<span id="scan-verb">べ</span>ました。</p>
      <p>ここは別の段落です。</p>
    </body></html>`);
  });
  await new Promise<void>((ready, reject) => { server.once("error", reject); server.listen(0, "127.0.0.1", ready); });
  const address = server.address();
  if (address === null || typeof address === "string") throw new Error("Unexpected reading page address");
  const profile = await mkdtemp(join(tmpdir(), "jp-reading-sentence-smoke-"));
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
    const popup = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
    let scanRequests = 0;
    let providerRequests = 0;
    context.on("request", event => {
      if (event.url() === scanUrl) scanRequests++;
      if (/^http:\/\/127\.0\.0\.1:5000\/api\/(translations|speech)$/u.test(event.url())) providerRequests++;
    });
    await page.goto(`http://127.0.0.1:${address.port}/reading`);
    await page.locator(scanExamples[0].selector).hover({ modifiers: ["Shift"] });
    await page.waitForTimeout(350);
    expect(scanRequests).toBe(0);
    for (const [index, example] of examples.entries()) {
      const responsePending = context.waitForEvent("response", {
        predicate: response => response.url() === apiUrl && response.request().postDataJSON()?.text === example.text,
        timeout: 20000,
      });
      await page.evaluate(id => {
        const range = document.createRange();
        range.selectNodeContents(document.getElementById(id)!);
        window.getSelection()?.removeAllRanges();
        window.getSelection()?.addRange(range);
        document.dispatchEvent(new Event("selectionchange"));
      }, `sentence-${index}`);
      const response = await responsePending;
      expect(response.status()).toBe(200);
      const result = await response.json() as AnalysisResponse;
      expect(result.meta.contract_version).toBe("3");
      expect(result.data.normalized_text).toBe(example.text);
      expect(result.data.capabilities.vocabulary).toBe("completed");
      const occurrences = result.data.vocabulary_occurrences;
      expect(occurrences).toBeDefined();
      if (!occurrences) throw new Error("Sentence vocabulary occurrences are missing");
      for (const item of occurrences) {
        expect(item.span.unit).toBe("utf16");
        expect(example.text.slice(item.span.start, item.span.end)).toBe(item.surface);
      }
      await expect(popup).toBeVisible();
      for (const expected of example.expected) {
        const occurrence = occurrences.find(item => item.span.start === expected.start && item.span.end === expected.end);
        expect(occurrence, `Missing occurrence at ${expected.start}..${expected.end}`).toBeDefined();
        if (!occurrence) throw new Error("Expected occurrence missing");
        expect(occurrence.surface).toBe(expected.surface);
        expect(occurrence.matches.length).toBeGreaterThan(0);
        await popup.locator(`[data-occurrence-choice="${expected.start}:${expected.end}"]`).click();
        const group = popup.locator(`[data-occurrence-start="${expected.start}"][data-occurrence-end="${expected.end}"]`);
        await expect(group).toBeVisible();
        if ("canonical" in expected) {
          const entry = occurrence.matches.find(match => match.canonicalId === expected.canonical);
          expect(entry).toBeDefined();
          expect(entry?.senses.flatMap(sense => sense.glosses).some(gloss => gloss.languageTag === "en" && gloss.glossText === expected.gloss)).toBe(true);
          await expect(group.locator(`[data-canonical-id="${expected.canonical}"]`).getByText(`EN ${expected.gloss}`, { exact: true })).toBeVisible();
        }
        if ("base" in expected) {
          expect(occurrence.conjugations.some(item => item.base_form === expected.base && item.surface === expected.surface)).toBe(true);
          await expect(group.getByText(`${expected.surface} → ${expected.base}`, { exact: true })).toBeVisible();
        }
      }
      if (index === examples.length - 1) {
        await popup.getByLabel("Cài đặt popup").click();
        await popup.getByRole("checkbox", { name: /Tra từ bằng con trỏ/ }).check();
      }
      await popup.getByRole("button", { name: "Đóng tra cứu" }).click();
      await expect(popup).toHaveCount(0);
    }

    await page.locator(scanExamples[0].selector).hover();
    await page.waitForTimeout(350);
    expect(scanRequests).toBe(0);
    for (const example of scanExamples) {
      const responsePending = context.waitForEvent("response", { predicate: response => response.url() === scanUrl, timeout: 15000 });
      await page.keyboard.down("Shift");
      await page.locator(example.selector).hover({ modifiers: ["Shift"] });
      const response = await responsePending;
      expect(response.status()).toBe(200);
      const request = response.request();
      const payload = request.postDataJSON() as { text: string; offset: number; interaction_id: string };
      expect(Object.keys(payload).sort()).toEqual(["interaction_id", "offset", "text"]);
      expect(payload).toMatchObject({ text: example.text, offset: example.offset });
      expect(await request.headerValue("x-contract-version")).toBe("1");
      expect(await request.headerValue("cookie")).toBeNull();
      const result = parseScanResponse(await response.json(), payload.interaction_id, { text: example.text, offset: example.offset });
      expect(result.analysis.data.capabilities.vocabulary).toBe("completed");
      if (example.span === null) {
        expect(result.targetSpan).toBeNull();
        await expect(popup.getByText("Không tìm thấy từ tại điểm quét.")).toBeVisible();
      } else {
        const [start, end] = example.span;
        expect(result.targetSpan).toEqual({ start, end, unit: "utf16" });
        await expect(popup.locator(".jp-selected")).toHaveText(example.surface);
        await expect(popup.locator(`[data-canonical-id="${example.canonical}"]`).getByText(`EN ${example.gloss}`, { exact: true })).toBeVisible();
        if ("base" in example) {
          await expect(popup.getByText(`${example.surface} → ${example.base}`, { exact: true })).toBeVisible();
        }
      }
      await page.keyboard.up("Shift");
      await expect(popup).toBeVisible();
      await expect(popup.getByText("Dịch câu", { exact: true })).toHaveCount(0);
      await popup.getByRole("button", { name: "Đóng tra cứu" }).click();
      await expect(popup).toHaveCount(0);
    }
    expect(scanRequests).toBe(scanExamples.length);
    expect(providerRequests).toBe(0);
  } finally {
    await context?.close();
    await new Promise<void>(done => server.close(() => done()));
    const resolvedProfile = resolve(profile);
    if (!resolvedProfile.startsWith(resolve(tmpdir()) + sep) || !resolvedProfile.includes("jp-reading-sentence-smoke-"))
      throw new Error("Unexpected profile cleanup path");
    await rm(resolvedProfile, { recursive: true, force: true });
  }
});
