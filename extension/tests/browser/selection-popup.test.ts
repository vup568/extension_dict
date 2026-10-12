import { test, expect, type Page } from "@playwright/test";
import type {} from "./selection-popup-harness.ts";

async function select(page: Page, selector: string) { await page.evaluate(value => window.popupHarness.select(value), selector); }
async function count(page: Page, expected: number) { await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(expected); }
const dialog = (page: Page) => page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
test.beforeEach(async ({ page }) => { await page.goto("/tests/browser/selection-popup-harness.html"); await page.waitForFunction(() => window.popupHarness !== undefined); });

test("AC01/02 ruby-safe selection opens loading before one minimal dispatch", async ({ page }) => {
  await select(page, "#ruby");
  await expect(dialog(page)).toBeVisible();
  await expect(dialog(page).getByRole("status")).toHaveText("Đang phân tích…");
  expect(await page.evaluate(() => window.popupHarness.attempts())).toEqual([]);
  await count(page, 1);
  expect(await page.evaluate(() => window.popupHarness.attempts()[0]?.text)).toBe("食事");
  await page.evaluate(() => window.popupHarness.succeed(0));
  await expect(dialog(page).getByText("bữa ăn", { exact: true })).toBeVisible();
  await expect(page.locator("[data-jp-reading-popup]")).toHaveCount(1);
});

test("AC01 non-Japanese, collapsed and annotation-only selections dispatch nothing", async ({ page }) => {
  for (const selector of ["#latin", "#ruby rt"]) { await select(page, selector); await expect(dialog(page)).toHaveCount(0); }
  await page.evaluate(() => { const selection = window.getSelection(); selection?.collapse(document.querySelector("#a")!.firstChild, 0); document.dispatchEvent(new Event("selectionchange")); });
  await page.waitForTimeout(350);
  expect(await page.evaluate(() => window.popupHarness.attempts())).toEqual([]);
});

test("AC02 rapid changes debounce only final selection", async ({ page }) => {
  await select(page, "#a"); await page.waitForTimeout(100); await select(page, "#b");
  await count(page, 1);
  expect(await page.evaluate(() => window.popupHarness.attempts().map(a => a.text))).toEqual(["食事"]);
});

for (const outcome of ["success", "error"] as const) {
  test(`AC03/04 late ${outcome} ignored while replacement is debouncing`, async ({ page }) => {
    await select(page, "#a"); await count(page, 1); await select(page, "#b");
    expect(await page.evaluate(() => window.popupHarness.attempts()[0]?.aborted)).toBe(true);
    await page.evaluate(value => value === "success" ? window.popupHarness.succeed(0) : window.popupHarness.fail(0), outcome);
    await expect(dialog(page).getByRole("status")).toHaveText("Đang phân tích…");
    await expect(dialog(page).locator(".jp-selected")).toHaveText("食事");
    await count(page, 2); await page.evaluate(() => window.popupHarness.succeed(1));
    await expect(dialog(page).getByText("bữa ăn", { exact: true })).toBeVisible();
  });
  test(`AC04 late ${outcome} never replaces completed current result`, async ({ page }) => {
    await select(page, "#a"); await count(page, 1); await select(page, "#b"); await count(page, 2);
    await page.evaluate(() => window.popupHarness.succeed(1));
    await page.evaluate(value => value === "success" ? window.popupHarness.succeed(0) : window.popupHarness.fail(0), outcome);
    await expect(dialog(page).locator(".jp-selected")).toHaveText("食事");
    await expect(dialog(page).getByText("bữa ăn", { exact: true })).toBeVisible();
    await expect(dialog(page).getByRole("alert")).toHaveCount(0);
  });
}

test("AC04 close cancels active request and prevents late result reopening", async ({ page }) => {
  await select(page, "#a"); await count(page, 1); await dialog(page).getByRole("button", { name: "Đóng tra cứu" }).click();
  await expect(dialog(page)).toHaveCount(0);
  expect(await page.evaluate(() => window.popupHarness.attempts()[0]?.aborted)).toBe(true);
  await page.evaluate(() => window.popupHarness.succeed(0)); await expect(dialog(page)).toHaveCount(0);
});

test("AC04/08 BFCache suspension clears active text and resumes new selection", async ({ page }) => {
  await select(page, "#a"); await count(page, 1);
  await page.evaluate(() => window.dispatchEvent(new PageTransitionEvent("pagehide", { persisted: true })));
  await expect(dialog(page)).toHaveCount(0);
  await page.evaluate(() => { window.popupHarness.succeed(0); window.dispatchEvent(new PageTransitionEvent("pageshow", { persisted: true })); });
  await expect(dialog(page)).toHaveCount(0);
  await select(page, "#b"); await count(page, 2); await page.evaluate(() => window.popupHarness.succeed(1));
  await expect(dialog(page).getByText("bữa ăn", { exact: true })).toBeVisible();
});

test("AC06 hung transport times out at five seconds; retry uses fresh identity", async ({ page }) => {
  const start = new Date("2026-10-11T00:00:00Z");
  await page.clock.install({ time: start }); await page.clock.pauseAt(start);
  await select(page, "#b"); await page.clock.runFor(300); await count(page, 1);
  await page.clock.runFor(4999); await expect(dialog(page).getByRole("status")).toHaveText("Đang phân tích…");
  await page.clock.runFor(1); await expect(dialog(page).getByRole("alert")).toContainText("quá thời gian");
  await dialog(page).getByRole("button", { name: "Thử lại" }).click(); await page.clock.runFor(1); await count(page, 2);
  const attempts = await page.evaluate(() => window.popupHarness.attempts());
  expect(attempts[0]?.aborted).toBe(true); expect(attempts[1]?.interactionId).not.toBe(attempts[0]?.interactionId);
  await page.evaluate(() => window.popupHarness.succeed(0)); await expect(dialog(page).getByRole("status")).toHaveText("Đang phân tích…");
  await page.evaluate(() => window.popupHarness.succeed(1)); await expect(dialog(page).getByText("bữa ăn", { exact: true })).toBeVisible();
});

test("AC06 network failure is safe, explicit retry succeeds", async ({ page }) => {
  await select(page, "#b"); await count(page, 1); await page.evaluate(() => window.popupHarness.fail(0));
  await expect(dialog(page).getByRole("alert")).toContainText("Không thể kết nối");
  await dialog(page).getByRole("button", { name: "Thử lại" }).click(); await count(page, 2);
  await page.evaluate(() => window.popupHarness.succeed(1)); await expect(dialog(page).getByText("bữa ăn", { exact: true })).toBeVisible();
  expect(await page.evaluate(() => window.popupHarness.attempts().length)).toBe(2);
});

test("AC07 results preserve IDs, language, restrictions, provenance and repeated occurrences", async ({ page }) => {
  await select(page, "#b"); await count(page, 1); await page.evaluate(() => window.popupHarness.succeed(0));
  await expect(dialog(page).getByRole("status")).toHaveText("Có kết quả một phần");
  const heading = await dialog(page).getByRole("heading", { name: "Tra cứu tiếng Nhật", exact: true }).boundingBox();
  const closeButton = await dialog(page).getByRole("button", { name: "Đóng tra cứu" }).boundingBox();
  expect(heading).not.toBeNull(); expect(closeButton).not.toBeNull();
  expect(closeButton!.x).toBeGreaterThan(heading!.x + heading!.width);
  expect(heading!.y).toBeLessThan(closeButton!.y + closeButton!.height);
  expect(closeButton!.y).toBeLessThan(heading!.y + heading!.height);
  await expect(dialog(page).locator('[data-canonical-id="jmdict:1"]')).toContainText("Giới hạn nghĩa: 食事 / しょくじ");
  await expect(dialog(page).locator('[data-canonical-id="jmdict:1"]')).toContainText("chỉ dùng với 食事");
  await expect(dialog(page).locator('p[lang="en"]')).toContainText("EN meal");
  await expect(dialog(page).locator('[data-canonical-id="kanji:食"]')).toContainText("JLPT N5 (legacy-estimate)");
  await expect(dialog(page).locator('[data-canonical-id="grammar:1"]')).toHaveCount(2);
  await expect(dialog(page)).toContainText("Vị trí 0–1"); await expect(dialog(page)).toContainText("Vị trí 1–2");
  await expect(dialog(page)).toContainText("Tách từ và biến đổi từ: chưa khả dụng.");
});

test("AC07 empty completed and unavailable results are distinct", async ({ page }) => {
  await select(page, "#b"); await count(page, 1); await page.evaluate(() => window.popupHarness.succeed(0, "unavailable"));
  await expect(dialog(page).getByText("Không tìm thấy kết quả.", { exact: true })).toHaveCount(2);
  await expect(dialog(page).getByText("Mục này chưa khả dụng.", { exact: true })).toHaveCount(1);
});

test("AC08 automatic opening retains focus, Escape restores focus after popup focus", async ({ page }) => {
  await page.locator("#host-focus").focus(); await select(page, "#b"); await expect(page.locator("#host-focus")).toBeFocused();
  await dialog(page).getByRole("button", { name: "Đóng tra cứu" }).focus(); await page.keyboard.press("Escape");
  await expect(dialog(page)).toHaveCount(0); await expect(page.locator("#host-focus")).toBeFocused();
});

test("AC08 restores focus within existing host Shadow DOM", async ({ page }) => {
  await page.evaluate(() => {
    const host = document.createElement("host-component"); document.body.append(host);
    const root = host.attachShadow({ mode: "open" }); const input = document.createElement("input"); input.id = "deep-focus"; root.append(input); input.focus();
  });
  await select(page, "#b"); await expect(page.locator("#deep-focus")).toBeFocused();
  await dialog(page).getByRole("button", { name: "Đóng tra cứu" }).focus(); await page.keyboard.press("Escape");
  await expect(page.locator("#deep-focus")).toBeFocused();
});

test("AC03 new host selection remains observable after popup pointer interaction", async ({ page }) => {
  await select(page, "#b"); await count(page, 1); await page.evaluate(() => window.popupHarness.succeed(0));
  await dialog(page).getByRole("heading", { name: "Từ vựng" }).click();
  await select(page, "#a"); await count(page, 2); await expect(dialog(page).locator(".jp-selected")).toHaveText("日本語");
});

test("AC08 transformed body and RTL inheritance keep fixed popup within viewport", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 400 });
  await page.addStyleTag({ content: "body { transform: translate(70px, 100px); direction: rtl; min-height: 2400px; }" });
  await select(page, "#b"); await expect(dialog(page)).toBeVisible();
  await expect(dialog(page)).toHaveAttribute("dir", "ltr");
  expect(await dialog(page).evaluate(node => getComputedStyle(node).direction)).toBe("ltr");
  const box = await dialog(page).boundingBox(); expect(box).not.toBeNull();
  expect(box!.x + box!.width).toBeLessThanOrEqual(320); expect(box!.y + box!.height).toBeLessThanOrEqual(400);
  await page.evaluate(() => window.scrollTo(0, 150)); await page.waitForTimeout(50);
  const after = await dialog(page).boundingBox(); expect(after!.y).toBeGreaterThanOrEqual(0);
});

test("AC08 popup text selection and pointer interaction never start host lookup", async ({ page }) => {
  await select(page, "#b"); await count(page, 1); await page.evaluate(() => window.popupHarness.succeed(0));
  await dialog(page).locator(".jp-selected").dispatchEvent("pointerdown", { bubbles: true, composed: true });
  await page.evaluate(() => {
    const node = document.querySelector("[data-jp-reading-popup]")!.shadowRoot!.querySelector(".jp-selected")!;
    const range = document.createRange(); range.selectNodeContents(node); window.getSelection()?.removeAllRanges(); window.getSelection()?.addRange(range);
    document.dispatchEvent(new Event("selectionchange"));
  });
  await page.waitForTimeout(350); await expect(dialog(page)).toBeVisible();
  expect(await page.evaluate(() => window.popupHarness.attempts().length)).toBe(1);
});

test("AC08 Shadow DOM isolates hostile host CSS and keeps narrow viewport bounds on scroll/resize", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 480 });
  await page.addStyleTag({ content: "* { color: rgb(255, 0, 255) !important; font-size: 70px !important; } jp-reading-popup { display: none !important; opacity: 0 !important; } body { min-height: 2400px; }" });
  await select(page, "#b"); await expect(dialog(page)).toBeVisible();
  expect(await dialog(page).evaluate(node => getComputedStyle(node).color)).not.toBe("rgb(255, 0, 255)");
  const bounds = async () => {
    const box = await dialog(page).boundingBox(); expect(box).not.toBeNull();
    const viewport = page.viewportSize()!; expect(box!.x).toBeGreaterThanOrEqual(0); expect(box!.y).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width).toBeLessThanOrEqual(viewport.width); expect(box!.y + box!.height).toBeLessThanOrEqual(viewport.height);
  };
  await bounds(); await page.evaluate(() => window.scrollTo(0, 200)); await page.waitForTimeout(50); await bounds();
  await page.setViewportSize({ width: 280, height: 400 }); await page.waitForTimeout(50); await bounds();
});

test("AC01/09 oversized selection is explicit and never silently truncated or sent", async ({ page }) => {
  await page.evaluate(() => { document.querySelector("#a")!.textContent = "日".repeat(1001); });
  await select(page, "#a"); await expect(dialog(page).getByRole("alert")).toContainText("1.000 ký tự");
  await expect(dialog(page).getByRole("button", { name: "Thử lại" })).toHaveCount(0); await page.waitForTimeout(350);
  expect(await page.evaluate(() => window.popupHarness.attempts())).toEqual([]);
});

test("AC09 no selection storage, logging, navigation or content-origin network", async ({ page }) => {
  const logs: string[] = [], requests: string[] = [];
  page.on("console", message => logs.push(message.text()));
  page.on("request", request => { if (["xhr", "fetch"].includes(request.resourceType())) requests.push(request.url()); });
  await page.evaluate(() => { localStorage.clear(); sessionStorage.clear(); });
  const before = await page.evaluate(() => ({ url: location.href, history: history.length }));
  await select(page, "#ruby"); await count(page, 1); await page.evaluate(() => window.popupHarness.succeed(0));
  await dialog(page).getByRole("button", { name: "Đóng tra cứu" }).click();
  expect(await page.evaluate(() => ({ url: location.href, history: history.length }))).toEqual(before);
  expect(await page.evaluate(() => [localStorage.length, sessionStorage.length])).toEqual([0, 0]);
  expect(logs.some(value => value.includes("食事") || value.includes("しょく"))).toBe(false); expect(requests).toEqual([]);
});
