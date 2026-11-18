import { expect, test } from "@playwright/test";
import type {} from "./selection-popup-harness.ts";

test("kana-only reading uses Vietnamese label without inventing a written association", async ({ page }) => {
  await page.goto("/tests/browser/selection-popup-harness.html");
  await page.waitForFunction(() => window.popupHarness !== undefined);
  await page.evaluate(() => window.popupHarness.select("#b"));
  await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(1);
  await page.evaluate(() => window.popupHarness.succeed(0, "kana-only"));
  const dialog = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
  await expect(dialog.getByRole("heading", { name: "かな", exact: true })).toBeVisible();
  await expect(dialog.locator(".jp-reading")).toHaveText("かな — chỉ dùng kana");
  await expect(dialog.getByText("Khớp dạng viết:")).toHaveCount(0);
  await expect(dialog.getByText("reading:no-kanji")).toHaveCount(0);
});
