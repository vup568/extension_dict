import { chromium, expect, test, type Frame, type Locator } from "@playwright/test";
import type {} from "./reading-interaction-harness.ts";

const harness = "/tests/browser/reading-interaction-harness.html";
const hostSelector = "[data-jp-reading-popup]";
const dialogName = "Tra cứu tiếng Nhật";

async function expectInsideViewport(popup: Locator) {
  await expect(popup).toBeVisible();
  await expect.poll(() => popup.evaluate(element => {
    const view = element.ownerDocument.defaultView!;
    const viewport = view.visualViewport!;
    const rect = element.getBoundingClientRect();
    // Subpixel box rounding can differ across engines and display scales.
    return rect.left >= viewport.offsetLeft + 11.5 && rect.top >= viewport.offsetTop + 11.5 &&
      rect.right <= viewport.offsetLeft + viewport.width - 11.5 && rect.bottom <= viewport.offsetTop + viewport.height - 11.5;
  })).toBe(true);
}

test("YPO-AC02: the popup follows the actual visual viewport at 200% zoom", async ({ page, context, browserName }) => {
  test.skip(browserName !== "chromium", "Actual page scale is set through Chromium DevTools Protocol");
  await page.setViewportSize({ width: 640, height: 760 });
  await page.goto(harness);
  await page.waitForFunction(() => window.readingHarness !== undefined);
  await page.locator("#focus").focus();
  await page.evaluate(() => window.readingHarness.select("#selection"));
  const popup = page.locator(hostSelector);
  await expectInsideViewport(popup);
  await expect(page.locator(`body:last-child > ${hostSelector}:popover-open`)).toBeVisible();
  await expect(page.locator("#focus")).toBeFocused();

  const session = await context.newCDPSession(page);
  await session.send("Emulation.setPageScaleFactor", { pageScaleFactor: 2 });
  await expect.poll(() => page.evaluate(() => ({ scale: visualViewport?.scale, width: visualViewport?.width })))
    .toEqual({ scale: 2, width: 320 });
  await expectInsideViewport(popup);
  await expect.poll(() => popup.evaluate(element => element.getBoundingClientRect().width)).toBe(296);
  const close = page.getByRole("dialog", { name: dialogName }).getByRole("button", { name: "Đóng tra cứu" });
  await close.focus();
  await page.keyboard.press("Escape");
  await expect(popup).toHaveCount(0);
  await expect(page.locator("#focus")).toBeFocused();
});

test("YPO-AC03: pointercancel ends active capture without replacing selection or pending speech", async ({ page }) => {
  await page.goto(harness);
  await page.waitForFunction(() => window.readingHarness !== undefined);
  await page.evaluate(() => window.readingHarness.select("#selection"));
  const dialog = page.getByRole("dialog", { name: dialogName });
  await dialog.getByRole("tab", { name: "Câu" }).click();
  await dialog.getByRole("button", { name: "Nghe vùng bôi đen" }).click();
  const selection = await page.evaluate(() => getSelection()?.toString());
  const handle = dialog.getByRole("button", { name: "Kéo đổi kích thước popup" });
  await handle.evaluate(element => {
    element.addEventListener("pointerdown", event => {
      element.setAttribute("data-test-pointer", String((event as PointerEvent).pointerId));
    }, { once: true });
  });
  const handleRect = (await handle.boundingBox())!;
  const x = handleRect.x + handleRect.width / 2;
  const y = handleRect.y + handleRect.height / 2;
  const popup = page.locator(hostSelector);
  const initial = (await popup.boundingBox())!;
  await page.mouse.move(x, y);
  await page.mouse.down();
  await page.mouse.move(x + 32, y - 20);
  const resized = (await popup.boundingBox())!;
  expect(resized.width).toBe(initial.width + 32);
  expect(resized.height).toBe(initial.height - 20);
  const pointerId = Number(await handle.getAttribute("data-test-pointer"));
  expect(await handle.evaluate((element, id) => element.hasPointerCapture(id), pointerId)).toBe(true);
  await handle.dispatchEvent("pointercancel", { pointerId, pointerType: "mouse", bubbles: true });
  expect(await handle.evaluate((element, id) => element.hasPointerCapture(id), pointerId)).toBe(false);
  await page.mouse.move(x + 58, y - 38);
  await page.mouse.up();
  expect(await popup.boundingBox()).toEqual(resized);
  expect(await page.evaluate(() => getSelection()?.toString())).toBe(selection);
  await expect(dialog.locator(".jp-selected")).toHaveText("食事");
  expect(await page.evaluate(() => window.readingHarness.speech())).toEqual([
    { source: { kind: "selection", text: "食事" }, aborted: false },
  ]);
  expect(await page.evaluate(() => window.readingHarness.attempts())).toEqual([]);
});

test("YPO-AC09: a vertical Japanese selection stays anchored inside the viewport", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 640 });
  await page.goto(harness);
  await page.waitForFunction(() => window.readingHarness !== undefined);
  await page.evaluate(() => {
    const vertical = document.createElement("p");
    vertical.id = "vertical";
    vertical.style.cssText = "writing-mode:vertical-rl;position:absolute;top:52px;right:4px;font-size:28px";
    vertical.textContent = "縦書き日本語";
    document.body.append(vertical);
    window.readingHarness.select("#vertical");
  });
  const popup = page.locator(hostSelector);
  const dialog = page.getByRole("dialog", { name: dialogName });
  await expect(dialog.locator(".jp-selected")).toHaveText("縦書き日本語");
  expect(await page.locator("#vertical").evaluate(element => {
    const rect = element.getBoundingClientRect();
    return rect.height > rect.width && getComputedStyle(element).writingMode === "vertical-rl";
  })).toBe(true);
  await expectInsideViewport(popup);
  await dialog.getByRole("button", { name: "Đóng tra cứu" }).click();
  await expect(popup).toHaveCount(0);
});

const longSelection = "日本語".repeat(334);
const readerPage = `<!doctype html><html lang="ja"><meta charset="UTF-8"><style>#frame-selection{margin:0;font-size:22px}</style><p id="frame-selection">${longSelection}</p></html>`;
const framePage = `<!doctype html><html><meta charset="UTF-8"><style>iframe{width:320px;height:400px;border:0;display:block}</style>
  <iframe id="same" src="/reader"></iframe>
  <iframe id="cross" src="http://jp-child.test/reader"></iframe>
  <iframe id="denied" src="data:text/html;charset=UTF-8,${encodeURIComponent(readerPage)}"></iframe></html>`;

async function selectLongText(frame: Frame) {
  return frame.evaluate(() => {
    const range = document.createRange();
    range.selectNodeContents(document.querySelector("#frame-selection")!);
    const selection = window.getSelection()!;
    selection.removeAllRanges();
    selection.addRange(range);
    document.dispatchEvent(new Event("selectionchange"));
    return selection.toString();
  });
}

test("YPO-AC07/09: packaged MV3 obeys frame boundaries and reloads only popup preferences", async ({}, testInfo) => {
  const edge = testInfo.project.name === "edge";
  const bravePath = testInfo.project.name === "brave" ? testInfo.project.use.launchOptions?.executablePath : undefined;
  test.skip(!edge && !bravePath, "Unpacked MV3 command-line smoke uses installed Edge or Brave");
  test.setTimeout(60_000);
  const extensionPath = decodeURIComponent(new URL("../../dist/chromium", import.meta.url).pathname).replace(/^\/(?=[a-z]:)/iu, "");

  // An empty path lets Playwright create and clean up a dedicated temporary profile.
  const browserContext = await chromium.launchPersistentContext("", {
    ...(edge ? { channel: "msedge" } : { executablePath: bravePath! }), headless: true,
    viewport: { width: 960, height: 700 },
    args: [`--disable-extensions-except=${extensionPath}`, `--load-extension=${extensionPath}`],
  });
  try {
    const worker = browserContext.serviceWorkers()[0] ?? await browserContext.waitForEvent("serviceworker", { timeout: 15_000 });
    expect(worker.url()).toMatch(/^chrome-extension:\/\/[^/]+\/background\.js$/u);
    const backendRequests: string[] = [];
    browserContext.on("request", request => {
      if (request.url().startsWith("http://127.0.0.1:5000/api/")) backendRequests.push(request.url());
    });
    await browserContext.route("http://127.0.0.1:5000/api/**", route => route.abort());
    await browserContext.route("http://jp-host.test/**", route => route.fulfill({
      contentType: "text/html; charset=utf-8", body: new URL(route.request().url()).pathname === "/frames" ? framePage : readerPage,
    }));
    await browserContext.route("http://jp-child.test/**", route => route.fulfill({ contentType: "text/html; charset=utf-8", body: readerPage }));
    const page = await browserContext.newPage();
    await page.goto("http://jp-host.test/frames", { waitUntil: "networkidle" });
    const same = page.frame({ url: "http://jp-host.test/reader" })!;
    const cross = page.frame({ url: "http://jp-child.test/reader" })!;
    const denied = page.frame({ url: /^data:text\/html/u })!;
    expect(await same.evaluate(() => location.origin)).toBe("http://jp-host.test");
    expect(await cross.evaluate(() => location.origin)).toBe("http://jp-child.test");
    expect(await denied.evaluate(() => location.origin)).toBe("null");
    // Oversize text exercises the actual content script, but the local guard prevents every backend call.
    for (const frame of [same, cross]) {
      expect(await selectLongText(frame)).toBe(longSelection);
      const popup = frame.locator(hostSelector);
      await expect(frame.getByRole("alert")).toHaveText(/Vùng chọn quá dài/u);
      await expectInsideViewport(popup);
      await frame.getByRole("button", { name: "Đóng tra cứu" }).click();
      await expect(popup).toHaveCount(0);
    }
    expect(await selectLongText(denied)).toBe(longSelection);
    await expect(page.locator(hostSelector)).toHaveCount(0);
    await expect(denied.locator(hostSelector)).toHaveCount(0);

    await page.goto("http://jp-host.test/reader", { waitUntil: "networkidle" });
    await selectLongText(page.mainFrame());
    const popup = page.locator(hostSelector);
    const dialog = page.getByRole("dialog", { name: dialogName });
    await expect(dialog.getByRole("alert")).toHaveText(/Vùng chọn quá dài/u);
    await dialog.getByLabel("Cài đặt popup").click();
    await dialog.getByRole("button", { name: "Tăng chiều rộng" }).click();
    await dialog.getByRole("button", { name: "Giảm chiều cao" }).click();
    await dialog.getByRole("button", { name: "Ghim", exact: true }).click();
    await expect(dialog.getByRole("button", { name: "Bỏ ghim", exact: true })).toHaveAttribute("aria-pressed", "true");
    const readStorage = () => worker.evaluate(() => {
      const extension = globalThis as unknown as { chrome: { storage: { local: { get(keys: null): Promise<Record<string, unknown>> } } } };
      return extension.chrome.storage.local.get(null);
    });
    await expect.poll(readStorage).toEqual({ jpReadingPopupSizeV1: { width: 400, height: 460 } });

    await page.reload({ waitUntil: "networkidle" });
    await expect(popup).toHaveCount(0);
    await selectLongText(page.mainFrame());
    await expectInsideViewport(popup);
    await expect.poll(() => popup.evaluate(element => [element.getBoundingClientRect().width, element.getBoundingClientRect().height]))
      .toEqual([400, 460]);
    await expect(dialog.getByRole("button", { name: "Ghim", exact: true })).toHaveAttribute("aria-pressed", "false");
    expect(await readStorage()).toEqual({ jpReadingPopupSizeV1: { width: 400, height: 460 } });
    expect(backendRequests).toEqual([]);
  } finally {
    await browserContext.close();
  }
});

for (const failure of ["missing", "throws"] as const) {
  test(`YPO-AC01: the fixed fallback stays in the viewport when showPopover is ${failure}`, async ({ page }) => {
    await page.addInitScript(mode => {
      const showPopover = mode === "throws"
        ? () => { throw new DOMException("Popover unavailable", "InvalidStateError"); }
        : undefined;
      Object.defineProperty(HTMLElement.prototype, "showPopover", { configurable: true, value: showPopover });
    }, failure);
    await page.setViewportSize({ width: 320, height: 400 });
    await page.goto(harness);
    await page.waitForFunction(() => window.readingHarness !== undefined);
    await page.evaluate(() => {
      document.body.style.transform = "translate(100px, 80px)";
      document.body.style.overflow = "hidden";
      window.readingHarness.select("#selection");
    });
    const popup = page.locator(hostSelector);
    const dialog = page.getByRole("dialog", { name: dialogName });
    await expect(dialog.locator(".jp-selected")).toHaveText("食事");
    await expectInsideViewport(popup);
    expect(await popup.evaluate(element => element.shadowRoot !== null && !element.hasAttribute("popover"))).toBe(true);
    await dialog.getByRole("button", { name: "Đóng tra cứu" }).click();
    await expect(popup).toHaveCount(0);
  });
}
