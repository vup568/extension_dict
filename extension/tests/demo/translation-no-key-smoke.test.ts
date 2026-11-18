import { test, expect, chromium, type BrowserContext } from "@playwright/test";
import { createServer } from "node:http";
import { mkdtemp, rm } from "node:fs/promises";
import { existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve, sep } from "node:path";

// Synthetic text only. No provider call or credentials are needed for this test.
test("packaged extension requests translation only on click and keeps lookup when unconfigured", async ({ request }) => {
  expect((await request.get("http://127.0.0.1:5000/health/live")).status()).toBe(200);
  const executablePath = [
    process.env.EXTENSION_SMOKE_EXECUTABLE_PATH,
    process.env.BRAVE_EXECUTABLE_PATH,
    process.env.LOCALAPPDATA ? join(process.env.LOCALAPPDATA, "BraveSoftware/Brave-Browser/Application/brave.exe") : undefined,
    chromium.executablePath(),
  ].find((candidate): candidate is string => candidate !== undefined && existsSync(candidate));
  expect(executablePath, "An installed extension-capable browser is required").toBeDefined();
  if (!executablePath) throw new Error("No installed extension-capable browser");

  const server = createServer((_request, response) => {
    response.writeHead(200, { "Content-Type": "text/html; charset=utf-8" });
    response.end('<!doctype html><html><head><meta charset="UTF-8"></head><body><p id="reading">猫を食べました。</p></body></html>');
  });
  await new Promise<void>((ready, reject) => { server.once("error", reject); server.listen(0, "127.0.0.1", ready); });
  const address = server.address();
  if (address === null || typeof address === "string") throw new Error("Unexpected reading page address");
  const profile = await mkdtemp(join(tmpdir(), "jp-reading-translation-smoke-"));
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
    const translationRequests: string[] = [];
    context.on("request", outgoing => {
      if (outgoing.url() === "http://127.0.0.1:5000/api/translations") translationRequests.push(outgoing.method());
    });
    const analysisPending = context.waitForEvent("response", {
      predicate: response => response.url() === "http://127.0.0.1:5000/api/analysis",
      timeout: 20000,
    });
    await page.evaluate(() => {
      const range = document.createRange();
      range.selectNodeContents(document.getElementById("reading")!);
      window.getSelection()?.removeAllRanges();
      window.getSelection()?.addRange(range);
      document.dispatchEvent(new Event("selectionchange"));
    });
    expect((await analysisPending).status()).toBe(200);
    const popup = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
    const lookup = popup.locator('[data-canonical-id="dictionary:jmdict:1358280"]');
    await expect(lookup).toBeVisible();
    await popup.getByRole("heading", { name: "Dịch câu", exact: true }).click();
    await expect(popup.getByText(/văn bản đã chọn sẽ được gửi đến Google Gemini/)).toBeVisible();
    await page.waitForTimeout(400);
    expect(translationRequests).toHaveLength(0);

    const translationPending = context.waitForEvent("response", {
      predicate: response => response.url() === "http://127.0.0.1:5000/api/translations",
      timeout: 20000,
    });
    await popup.getByRole("button", { name: "Dịch sang tiếng Anh" }).click();
    const translation = await translationPending;
    expect(translation.status()).toBe(503);
    expect((await translation.json()).error_code).toBe("SERVICE_UNAVAILABLE");
    expect(translationRequests).toEqual(["POST"]);
    await expect(popup.getByText("Dịch vụ chưa khả dụng hoặc chưa được cấu hình. Hãy kiểm tra cấu hình backend.")).toBeVisible();
    await expect(lookup).toBeVisible();
  } finally {
    await context?.close();
    await new Promise<void>(done => server.close(() => done()));
    const resolvedProfile = resolve(profile);
    if (!resolvedProfile.startsWith(resolve(tmpdir()) + sep) || !resolvedProfile.includes("jp-reading-translation-smoke-"))
      throw new Error("Unexpected profile cleanup path");
    await rm(resolvedProfile, { recursive: true, force: true });
  }
});
