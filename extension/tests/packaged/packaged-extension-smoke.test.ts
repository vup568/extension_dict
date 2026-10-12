import { test, expect, chromium } from "@playwright/test";
import { createServer } from "node:http";
import { readFile, mkdtemp, rm } from "node:fs/promises";
import { existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { resolve, join, sep } from "node:path";
import { analysisFixture } from "../unit/analysisFixtures.ts";

test("AC05/09/10 actual MV3 content -> background POST preserves minimal ruby-safe envelope", async () => {
  test.setTimeout(60000);
  const executablePath = [
    process.env.EXTENSION_SMOKE_EXECUTABLE_PATH,
    process.env.BRAVE_EXECUTABLE_PATH,
    process.env.LOCALAPPDATA ? join(process.env.LOCALAPPDATA, "BraveSoftware/Brave-Browser/Application/brave.exe") : undefined,
    chromium.executablePath(),
  ].find((candidate): candidate is string => candidate !== undefined && existsSync(candidate));
  test.skip(executablePath === undefined, "No installed Extension-capable browser. Set EXTENSION_SMOKE_EXECUTABLE_PATH or BRAVE_EXECUTABLE_PATH; no browser download is performed.");
  if (executablePath === undefined) return;
  // Requires default build backend http://127.0.0.1:5000 and that port to be free.
  const requests: { body: Record<string, unknown>; origin: string | undefined; cookie: string | undefined }[] = [];
  const server = createServer((req, res) => {
    let body = "";
    req.on("data", chunk => { body += String(chunk); });
    req.on("end", () => {
      if (req.url !== "/api/analysis" || req.method !== "POST") { res.writeHead(404); res.end(); return; }
      const payload = JSON.parse(body) as Record<string, unknown>;
      requests.push({ body: payload, origin: req.headers.origin, cookie: req.headers.cookie });
      const response = analysisFixture(String(payload.interaction_id));
      res.writeHead(200, { "Content-Type": "application/json" }); res.end(JSON.stringify(response));
    });
  });
  await new Promise<void>((ready, reject) => { server.once("error", reject); server.listen(5000, "127.0.0.1", ready); });
  const profile = await mkdtemp(join(tmpdir(), "jp-reading-smoke-"));
  const extensionPath = resolve("dist/chromium");
  let context: Awaited<ReturnType<typeof chromium.launchPersistentContext>> | undefined;
  try {
    const manifest = JSON.parse(await readFile(join(extensionPath, "manifest.json"), "utf8")) as { host_permissions: string[]; background: { service_worker: string } };
    expect(manifest.host_permissions).toEqual(["http://127.0.0.1:5000/*"]);
    expect(manifest.background.service_worker).toBe("background.js");
    context = await chromium.launchPersistentContext(profile, {
      executablePath, headless: true,
      args: [`--disable-extensions-except=${extensionPath}`, `--load-extension=${extensionPath}`],
    });
    let worker = context.serviceWorkers()[0];
    worker ??= await context.waitForEvent("serviceworker", { timeout: 15000 });
    expect(worker.url()).toMatch(/^chrome-extension:\/\/[^/]+\/background\.js$/u);
    // Ordinary non-secure HTTP page: no test harness or direct runtime injection.
    await context.route("http://jp-host.test/**", route => route.fulfill({ contentType: "text/html; charset=utf-8", body:
      '<!doctype html><html><head><meta charset="UTF-8"></head><body><p id="private-context">DO_NOT_SEND_PAGE_CONTEXT</p><p id="ruby"><ruby>食<rt>しょく</rt><rp>(</rp></ruby>事</p></body></html>' }));
    const page = await context.newPage(); const errors: string[] = [];
    page.on("pageerror", error => errors.push(error.message));
    await page.goto("http://jp-host.test/reading");
    await page.evaluate(() => {
      const range = document.createRange(); range.selectNodeContents(document.querySelector("#ruby")!);
      window.getSelection()?.removeAllRanges(); window.getSelection()?.addRange(range);
      document.dispatchEvent(new Event("selectionchange"));
    });
    const popup = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
    await expect(popup).toBeVisible(); await expect(popup.getByText("bữa ăn", { exact: true })).toBeVisible({ timeout: 10000 });
    expect(requests).toHaveLength(1);
    expect(Object.keys(requests[0]!.body).sort()).toEqual(["interaction_id", "text"]);
    expect(requests[0]!.body.text).toBe("食事");
    expect(requests[0]!.body.interaction_id).toMatch(/^[0-9a-f-]{36}$/u);
    expect(requests[0]!.cookie).toBeUndefined();
    expect(errors).toEqual([]);
    expect(await page.evaluate(() => [localStorage.length, sessionStorage.length])).toEqual([0, 0]);
    await popup.getByRole("button", { name: "Đóng tra cứu" }).click(); await expect(popup).toHaveCount(0);
  } finally {
    await context?.close();
    await new Promise<void>(done => server.close(() => done()));
    if (!resolve(profile).startsWith(resolve(tmpdir()) + sep) || !profile.includes("jp-reading-smoke-")) throw new Error("Unexpected profile cleanup path");
    await rm(profile, { recursive: true, force: true });
  }
});
