import { expect, test, type Page } from "@playwright/test";
import type {} from "./reading-interaction-harness.ts";

const dialog = (page: Page) => page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
const attempts = (page: Page) => page.evaluate(() => window.readingHarness.attempts());
const select = (page: Page) => page.evaluate(() => window.readingHarness.select("#selection"));

async function enableScan(page: Page) {
  await select(page);
  await dialog(page).getByLabel("Cài đặt popup").click();
  await dialog(page).getByRole("checkbox", { name: /Tra từ bằng con trỏ/ }).check();
  await page.keyboard.press("Escape");
  await expect(dialog(page)).toHaveCount(0);
}

test.beforeEach(async ({ page }) => {
  await page.goto("/tests/browser/reading-interaction-harness.html");
  await page.waitForFunction(() => window.readingHarness !== undefined);
});

test("trusted opt-in and Shift scan skip ruby annotations and preserve the committed result when Shift is released", async ({ page }) => {
  const glyph = (await page.locator("#glyph").boundingBox())!;
  const x = glyph.x + glyph.width / 2;
  const y = glyph.y + glyph.height / 2;
  await page.keyboard.down("Shift");
  await page.mouse.move(x, y, { steps: 100 });
  await page.waitForTimeout(330);
  await page.keyboard.up("Shift");
  expect(await attempts(page)).toEqual([]);

  await enableScan(page);
  await page.mouse.move(x, y, { steps: 100 });
  await page.evaluate(() => {
    const rect = document.querySelector("#glyph")!.getBoundingClientRect();
    document.querySelector("#glyph")!.dispatchEvent(new PointerEvent("pointermove", { bubbles: true, pointerType: "mouse",
      shiftKey: true, clientX: rect.x + 4, clientY: rect.y + 4 }));
  });
  await page.waitForTimeout(330);
  expect(await attempts(page)).toEqual([]);

  await page.keyboard.down("Shift");
  await expect.poll(() => attempts(page)).toEqual([{ text: "日本語", offset: 0, aborted: false }]);
  await expect(dialog(page).getByRole("status")).toHaveText("Đang phân tích…");
  await page.evaluate(() => window.readingHarness.succeed(0));
  await expect(dialog(page).locator(".jp-selected")).toHaveText("日本語");
  await page.keyboard.up("Shift");
  await expect(dialog(page)).toBeVisible();
  await expect(dialog(page).getByText("Dịch câu", { exact: true })).toHaveCount(0);
  await expect(dialog(page).getByRole("button", { name: "Nghe vùng bôi đen" })).toHaveCount(0);
  // A scan shows its bounded text with the verified target highlighted and offers no sentence tab.
  await expect(dialog(page).getByText("Tra bằng con trỏ", { exact: true })).toBeVisible();
  await expect(dialog(page).locator(".jp-selected .jp-target")).toHaveText("日本語");
  await expect(dialog(page).getByRole("tab")).toHaveText(["Từ vựng", "Kanji", "Ngữ pháp"].map(label => new RegExp(`^${label}`)));
  await page.keyboard.down("Shift");
  await page.waitForTimeout(330);
  await page.keyboard.up("Shift");
  expect(await attempts(page)).toHaveLength(1);
});

test("a replacement scan and a selection discard late results from earlier scans", async ({ page }) => {
  await enableScan(page);
  await page.locator("#glyph").hover();
  await page.keyboard.down("Shift");
  await expect.poll(async () => (await attempts(page)).length).toBe(1);
  const nextGlyph = await page.locator("#selection").evaluate(element => {
    const range = document.createRange();
    range.setStart(element.firstChild!, 0);
    range.setEnd(element.firstChild!, 1);
    const rect = range.getBoundingClientRect();
    return { x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 };
  });
  await page.mouse.move(nextGlyph.x, nextGlyph.y);
  await expect.poll(() => attempts(page)).toEqual([
    { text: "日本語", offset: 0, aborted: true },
    { text: "食事", offset: 0, aborted: false },
  ]);
  await page.evaluate(() => window.readingHarness.succeed(1));
  await expect(dialog(page).locator(".jp-selected")).toHaveText("食事");
  await page.evaluate(() => window.readingHarness.succeed(0));
  await expect(dialog(page).locator(".jp-selected")).toHaveText("食事");
  await page.keyboard.up("Shift");
  await dialog(page).getByRole("button", { name: "Đóng tra cứu" }).click();

  await page.locator("#glyph").hover();
  await page.keyboard.down("Shift");
  await expect.poll(async () => (await attempts(page)).length).toBe(3);
  await select(page);
  await expect(dialog(page).locator(".jp-selected")).toHaveText("食事");
  expect((await attempts(page))[2]?.aborted).toBe(true);
  await page.evaluate(() => window.readingHarness.succeed(2));
  await page.keyboard.up("Shift");
  await expect(dialog(page).locator(".jp-selected")).toHaveText("食事");
});

test("pending scan ignores stale completion; pin blocks hover while an explicit selection can replace it", async ({ page }) => {
  await enableScan(page);
  await page.locator("#glyph").hover();
  await page.keyboard.down("Shift");
  await expect.poll(async () => (await attempts(page)).length).toBe(1);
  await page.keyboard.up("Shift");
  await page.evaluate(() => window.readingHarness.succeed(0));
  await expect(dialog(page)).toHaveCount(0);
  expect((await attempts(page))[0]?.aborted).toBe(true);

  await page.locator("#glyph").hover();
  await page.keyboard.down("Shift");
  await expect.poll(async () => (await attempts(page)).length).toBe(2);
  await page.evaluate(() => window.readingHarness.succeed(1));
  await expect(dialog(page).locator(".jp-selected")).toHaveText("日本語");
  await page.keyboard.up("Shift");
  await dialog(page).getByRole("button", { name: "Ghim", exact: true }).click();
  await page.keyboard.down("Shift");
  // Move directly over a neighboring glyph even when the overlay covers part of its row.
  const next = await page.locator("#another").boundingBox();
  await page.mouse.move(next!.x + 3, next!.y + 3);
  await page.waitForTimeout(340);
  await page.keyboard.up("Shift");
  expect(await attempts(page)).toHaveLength(2);
  await select(page);
  await expect(dialog(page).locator(".jp-selected")).toHaveText("食事");
  await expect(dialog(page).getByRole("button", { name: "Ghim", exact: true })).toHaveAttribute("aria-pressed", "false");
});

test("editable, ruby annotation and closed shadow points dispatch no scan", async ({ page }) => {
  await enableScan(page);
  await page.evaluate(() => {
    const host = document.createElement("span");
    host.id = "closed-shadow";
    host.attachShadow({ mode: "closed" }).textContent = "非公開";
    document.querySelector("main")!.append(host);
  });
  await page.keyboard.down("Shift");
  for (const selector of ["#annotation", "#editable", "input[type=password]", "#closed-shadow"]) {
    await page.locator(selector).hover();
    await page.waitForTimeout(330);
  }
  await page.keyboard.up("Shift");
  expect(await attempts(page)).toEqual([]);
});

test("pinned result keeps current audio while the pointer moves over another word", async ({ page }) => {
  await enableScan(page);
  await page.evaluate(() => {
    const word = document.createElement("span");
    word.id = "outside-popup";
    word.textContent = "猫";
    word.style.cssText = "position:fixed;right:24px;top:24px";
    document.body.append(word);
  });
  await select(page);
  await dialog(page).getByRole("tab", { name: "Câu" }).click();
  await dialog(page).getByRole("button", { name: "Nghe vùng bôi đen" }).click();
  await dialog(page).getByRole("button", { name: "Ghim", exact: true }).click();
  await page.keyboard.down("Shift");
  await page.locator("#outside-popup").hover({ modifiers: ["Shift"] });
  await page.waitForTimeout(340);
  await page.keyboard.up("Shift");
  expect(await attempts(page)).toEqual([]);
  expect(await page.evaluate(() => window.readingHarness.speech())).toEqual([{ source: { kind: "selection", text: "食事" }, aborted: false }]);
  await expect(dialog(page).locator(".jp-selected")).toHaveText("食事");
});

test("pointer window clamps both directions without splitting supplementary characters or reading a nearby block", async ({ page }) => {
  await enableScan(page);
  await page.evaluate(() => {
    const block = document.createElement("p");
    const target = document.createElement("span");
    target.id = "bounded-hit";
    target.textContent = "猫";
    block.append("🧪".repeat(8) + "前", target, "𠮷".repeat(24), "後");
    document.querySelector("main")!.prepend(block);
  });
  await page.locator("#bounded-hit").hover();
  await page.keyboard.down("Shift");
  await expect.poll(() => attempts(page)).toEqual([{ text: "🧪".repeat(7) + "前猫" + "𠮷".repeat(23), offset: 15, aborted: false }]);
  await page.keyboard.up("Shift");
});

test("keyboard resize stores only size and keeps the close control inside a narrow viewport", async ({ page }) => {
  await page.setViewportSize({ width: 320, height: 380 });
  await page.locator("#focus").focus();
  await select(page);
  const popup = page.locator("[data-jp-reading-popup]");
  const initial = await popup.boundingBox();
  expect(initial?.width).toBeLessThanOrEqual(296);
  await dialog(page).getByLabel("Cài đặt popup").click();
  await dialog(page).getByRole("button", { name: "Giảm chiều cao" }).focus();
  await page.keyboard.press("Enter");
  const changed = await popup.boundingBox();
  expect(changed?.height).toBe((initial?.height ?? 0) - 20);
  expect(await page.evaluate(() => window.readingHarness.preferences())).toEqual({ sizes: [{ width: 296, height: 336 }], settings: [] });
  await page.keyboard.press("Escape");
  await expect(page.locator("#focus")).toBeFocused();
});

test("pointer resize releases capture on blur without canceling current speech", async ({ page }) => {
  await select(page);
  await dialog(page).getByRole("tab", { name: "Câu" }).click();
  await dialog(page).getByRole("button", { name: "Nghe vùng bôi đen" }).click();
  const popup = page.locator("[data-jp-reading-popup]");
  const handle = dialog(page).getByLabel("Kéo đổi kích thước popup");
  const box = (await handle.boundingBox())!;
  const initial = (await popup.boundingBox())!;
  const x = box.x + box.width / 2;
  const y = box.y + box.height / 2;
  await page.mouse.move(x, y);
  await page.mouse.down();
  await page.mouse.move(x + 40, y - 40);
  const resized = (await popup.boundingBox())!;
  expect(resized.width).toBe(initial.width + 40);
  expect(resized.height).toBe(initial.height - 40);
  await page.evaluate(() => window.dispatchEvent(new Event("blur")));
  await page.mouse.move(x + 80, y - 70);
  await page.mouse.up();
  expect((await popup.boundingBox())!.width).toBe(resized.width);
  expect(await page.evaluate(() => window.readingHarness.speech())).toEqual([{ source: { kind: "selection", text: "食事" }, aborted: false }]);
});

test("only a trusted listen click sends selected Japanese; close ignores late audio", async ({ page }) => {
  await page.evaluate(() => window.readingHarness.select("#padded"));
  await dialog(page).getByRole("tab", { name: "Câu" }).click();
  const button = dialog(page).getByRole("button", { name: "Nghe vùng bôi đen" });
  await expect(button).toBeVisible();
  await button.evaluate(element => (element as HTMLButtonElement).click());
  expect(await page.evaluate(() => window.readingHarness.speech())).toEqual([]);
  await button.click();
  expect(await page.evaluate(() => window.readingHarness.speech())).toEqual([{ source: { kind: "selection", text: "食事" }, aborted: false }]);
  await page.keyboard.press("Escape");
  await page.evaluate(() => window.readingHarness.finishSpeech(0));
  await expect(dialog(page)).toHaveCount(0);
  expect((await page.evaluate(() => window.readingHarness.speech()))[0]?.aborted).toBe(true);
});

test("decoded WAV can replay without a second request on pages that block media URLs", async ({ page }) => {
  await select(page);
  await dialog(page).getByRole("tab", { name: "Câu" }).click();
  await dialog(page).getByRole("button", { name: "Nghe vùng bôi đen" }).click();
  await expect.poll(() => page.evaluate(() => window.readingHarness.speech().length)).toBe(1);
  await page.evaluate(() => window.readingHarness.finishSpeech(0));
  const replay = dialog(page).getByRole("button", { name: "Nghe lại", exact: true });
  await expect(replay).toBeVisible();
  await replay.click();
  await expect(replay).toBeVisible();
  expect(await page.evaluate(() => window.readingHarness.speech())).toHaveLength(1);
});
