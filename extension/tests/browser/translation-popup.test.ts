import { test, expect, type Page } from "@playwright/test";
import type {} from "./translation-popup-harness.ts";

const dialog = (page: Page) => page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
const select = async (page: Page, selector: string) => page.evaluate(value => window.translationHarness.select(value), selector);
const translationHeading = (page: Page) => dialog(page).getByRole("heading", { name: "Dịch câu", exact: true });
const openTranslation = (page: Page) => translationHeading(page).click();
test.beforeEach(async ({ page }) => { await page.goto("/tests/browser/translation-popup-harness.html"); await page.waitForFunction(() => !!window.translationHarness); });

test("shows disclosure and sends only after an explicit click", async ({ page }) => {
  await select(page, "#a");
  await expect(dialog(page).getByText("bữa ăn", { exact: true })).toBeVisible();
  await expect(dialog(page).getByRole("button", { name: "Dịch sang tiếng Anh" })).toBeHidden();
  await openTranslation(page);
  await expect(dialog(page).getByText(/văn bản đã chọn sẽ được gửi đến Google Gemini/)).toBeVisible();
  await openTranslation(page);
  expect(await page.evaluate(() => window.translationHarness.attempts())).toEqual([]);
  await openTranslation(page);
  await dialog(page).getByRole("button", { name: "Dịch sang tiếng Anh" }).evaluate(button => {
    if (button instanceof HTMLButtonElement) button.click();
  });
  expect(await page.evaluate(() => window.translationHarness.attempts())).toEqual([]);
  await dialog(page).getByRole("button", { name: "Dịch sang tiếng Anh" }).click();
  expect(await page.evaluate(() => window.translationHarness.attempts())).toEqual([{ text: "猫です。", aborted: false }]);
  await expect(dialog(page).getByText("Đang dịch…")).toBeVisible();
  await page.evaluate(() => window.translationHarness.succeed(0, "It is a cat."));
  await expect(dialog(page).getByText("It is a cat.")).toBeVisible();
  await expect(dialog(page).getByText("bữa ăn", { exact: true })).toBeVisible();
});

test("translation errors preserve lookup and allow explicit retry", async ({ page }) => {
  await select(page, "#a"); await openTranslation(page); await dialog(page).getByRole("button", { name: "Dịch sang tiếng Anh" }).click();
  await page.evaluate(() => window.translationHarness.fail(0));
  await expect(dialog(page).getByRole("alert")).toContainText("chưa khả dụng hoặc chưa được cấu hình");
  await expect(dialog(page).getByText("bữa ăn", { exact: true })).toBeVisible();
  await dialog(page).getByRole("button", { name: "Thử dịch lại" }).click();
  expect(await page.evaluate(() => window.translationHarness.attempts().length)).toBe(2);
  await page.evaluate(() => window.translationHarness.succeed(1, "It is a cat."));
  await expect(dialog(page).getByText("It is a cat.")).toBeVisible();
});

test("translation remains available when dictionary analysis fails", async ({ page }) => {
  await select(page, "#c");
  await expect(dialog(page).getByRole("alert")).toContainText("chưa khả dụng");
  expect(await page.evaluate(() => window.translationHarness.attempts())).toEqual([]);
  await openTranslation(page);
  await dialog(page).getByRole("button", { name: "Dịch sang tiếng Anh" }).click();
  expect(await page.evaluate(() => window.translationHarness.attempts())).toEqual([{ text: "分析エラー", aborted: false }]);
  await page.evaluate(() => window.translationHarness.succeed(0, "Analysis error."));
  await expect(dialog(page).getByText("Analysis error.")).toBeVisible();
  await expect(dialog(page).getByRole("alert")).toContainText("chưa khả dụng");
});

test("changing selection aborts translation and prevents a late result from replacing the new selection", async ({ page }) => {
  await select(page, "#a"); await openTranslation(page); await dialog(page).getByRole("button", { name: "Dịch sang tiếng Anh" }).click();
  await select(page, "#b");
  expect(await page.evaluate(() => window.translationHarness.attempts()[0]?.aborted)).toBe(true);
  await page.evaluate(() => window.translationHarness.succeed(0, "OLD RESULT"));
  await expect(dialog(page).locator(".jp-selected")).toHaveText("食事");
  await expect(dialog(page).getByText("OLD RESULT")).toHaveCount(0);
  await expect(translationHeading(page)).toBeVisible();
  await expect(dialog(page).getByRole("button", { name: "Dịch sang tiếng Anh" })).toBeHidden();
});

test("closing popup aborts translation and ignores a late response", async ({ page }) => {
  await select(page, "#a"); await openTranslation(page); await dialog(page).getByRole("button", { name: "Dịch sang tiếng Anh" }).click();
  await dialog(page).getByRole("button", { name: "Đóng tra cứu" }).click();
  expect(await page.evaluate(() => window.translationHarness.attempts()[0]?.aborted)).toBe(true);
  await page.evaluate(() => window.translationHarness.succeed(0, "OLD RESULT"));
  await expect(dialog(page)).toHaveCount(0);
});

test("YUI repeated words keep separate spans, accessible details and independent translation", async ({ page }) => {
  await select(page, "#repeated");
  const popup = dialog(page);
  const words = popup.getByRole("list", { name: "Từ trong vùng chọn" }).getByRole("button");
  await expect(words).toHaveCount(2);
  await expect(words.nth(0)).toHaveAttribute("aria-pressed", "true");
  await expect(popup.locator("[data-occurrence-start]")).toHaveAttribute("data-occurrence-start", "0");
  await words.nth(1).focus();
  await page.keyboard.press("Space");
  await expect(words.nth(1)).toHaveAttribute("aria-pressed", "true");
  await expect(popup.locator("[data-occurrence-start]")).toHaveAttribute("data-occurrence-start", "2");
  await expect(popup.locator("[data-occurrence-start]").getByText("EN cat")).toHaveAttribute("lang", "en");
  await expect(popup.getByText("Chỉ hiển thị một phần từ vựng do giới hạn xử lý.")).toBeVisible();
  await expect(popup.getByText("Mục này chưa khả dụng.")).toBeVisible();
  await popup.locator("summary").filter({ has: page.getByRole("heading", { name: "Kanji", exact: true }) }).press("Enter");
  await expect(popup.locator('[data-canonical-id="kanji:猫"]')).toBeVisible();
  await openTranslation(page);
  expect(await page.evaluate(() => window.translationHarness.attempts())).toHaveLength(0);
  await popup.getByRole("button", { name: "Dịch sang tiếng Anh" }).click();
  await page.evaluate(() => window.translationHarness.succeed(0, "Cat, cat."));
  await expect(popup.getByText("Cat, cat.")).toBeVisible();
  await expect(words.nth(1)).toHaveAttribute("aria-pressed", "true");
  expect(await page.evaluate(() => window.translationHarness.attempts())).toEqual([{ text: "猫、猫。", aborted: false }]);
});

test("YUI homophones preserve applicable pairs and render glossary markup as plain text", async ({ page }) => {
  await select(page, "#homophones");
  const popup = dialog(page);
  const bridge = popup.locator('[data-canonical-id="dictionary:jmdict:1"]');
  const chopsticks = popup.locator('[data-canonical-id="dictionary:jmdict:2"]');
  await expect(bridge.getByText("EN bridge")).toBeVisible();
  await expect(bridge).toContainText("Giới hạn nghĩa: 橋 / はし");
  await expect(chopsticks).toContainText("Giới hạn nghĩa: 箸 / はし");
  await expect(chopsticks.getByText('EN <img src=x onerror="alert(1)"> chopsticks')).toBeVisible();
  await expect(popup.locator("img")).toHaveCount(0);
  const summary = bridge.getByText("Dạng viết, cách đọc và nguồn");
  await summary.press("Enter");
  await expect(bridge.getByText("dictionary:jmdict:1", { exact: true })).toBeVisible();
});

test("YUI single words avoid repeating the surface and lemma-only results avoid inventing an explanation", async ({ page }) => {
  await select(page, "#electricity");
  const popup = dialog(page);
  await expect(popup.locator(".jp-selected")).toHaveText("電気代");
  await expect(popup.getByRole("heading", { name: "電気代", exact: true })).toHaveCount(0);
  await expect(popup.getByText("でんきだい", { exact: false }).first()).toBeVisible();
  await expect(popup.getByText("EN electricity bill")).toBeVisible();
  await select(page, "#lemma");
  await expect(popup.locator(".jp-selected")).toHaveText("食べました");
  await expect(popup.getByRole("heading", { name: "Dạng từ điển 食べる" })).toBeVisible();
  await expect(popup.getByText("EN to eat")).toBeVisible();
  await expect(popup.locator("[data-rule-id]")).toHaveCount(0);
});
