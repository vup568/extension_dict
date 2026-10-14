import { test, expect } from "@playwright/test";
import type {} from "./selection-popup-harness.ts";

for (const [text, count] of [["食べられる", 3], ["食べさせられる", 2]] as const) {
  test(`F09 v3 explains all ${count} readings of ${text}`, async ({ page }) => {
    await page.goto("/tests/browser/selection-popup-harness.html");
    await page.waitForFunction(() => window.popupHarness !== undefined);
    await page.evaluate(text => { document.querySelector("#b")!.textContent = text; window.popupHarness.select("#b"); }, text);
    await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(1);
    await page.evaluate(() => window.popupHarness.succeed(0, "conjugation-v3"));
    const dialog = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
    await expect(dialog.getByText("Các cách hiểu có thể", { exact: true })).toBeVisible();
    await expect(dialog.locator("[data-rule-id]")).toHaveCount(count);
    await expect(dialog.getByText("Cách hiểu có thể", { exact: true })).toHaveCount(count);
    await dialog.getByRole("button", { name: "Đóng tra cứu" }).click();
    await expect(dialog).toHaveCount(0);
  });
}

test("F09 v3 retry uses a fresh request identity", async ({ page }) => {
  await page.goto("/tests/browser/selection-popup-harness.html");
  await page.waitForFunction(() => window.popupHarness !== undefined);
  await page.evaluate(() => { document.querySelector("#a")!.textContent = "食べられる"; window.popupHarness.select("#a"); });
  await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(1);
  await page.evaluate(() => window.popupHarness.fail(0));
  const dialog = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
  await dialog.getByRole("button", { name: "Thử lại" }).click();
  await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(2);
  const attempts = await page.evaluate(() => window.popupHarness.attempts());
  expect(attempts[0]!.interactionId).not.toBe(attempts[1]!.interactionId);
  await page.evaluate(() => window.popupHarness.succeed(1, "conjugation-v3"));
  await expect(dialog.locator("[data-rule-id]")).toHaveCount(3);
  await page.evaluate(() => window.popupHarness.select("#b"));
  await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(3);
  await page.evaluate(() => { window.popupHarness.succeed(2); window.popupHarness.succeed(0, "conjugation-v3"); });
  await expect(dialog.locator(".jp-selected")).toHaveText("食事");
  await expect(dialog.locator("[data-rule-id]")).toHaveCount(0);
});

for (const close of [false, true]) {
  test(`F09 v3 pending late response is ignored after ${close ? "close" : "new selection"}`, async ({ page }) => {
    await page.goto("/tests/browser/selection-popup-harness.html");
    await page.waitForFunction(() => window.popupHarness !== undefined);
    await page.evaluate(() => { document.querySelector("#a")!.textContent = "食べられる"; window.popupHarness.select("#a"); });
    await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(1);
    const dialog = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
    if (close) await dialog.getByRole("button", { name: "Đóng tra cứu" }).click();
    else {
      await page.evaluate(() => window.popupHarness.select("#b"));
      await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(2);
      await page.evaluate(() => window.popupHarness.succeed(1));
      await expect(dialog.locator(".jp-selected")).toHaveText("食事");
    }
    expect(await page.evaluate(() => window.popupHarness.attempts()[0]!.aborted)).toBe(true);
    await page.evaluate(() => window.popupHarness.succeed(0, "conjugation-v3"));
    if (close) await expect(dialog).toHaveCount(0);
    else { await expect(dialog.locator("[data-rule-id]")).toHaveCount(0); await expect(dialog.locator(".jp-selected")).toHaveText("食事"); }
  });
}

test("F-09 v2 popup renders observed chain, base form and Vietnamese segments", async ({ page }) => {
  await page.goto("/tests/browser/selection-popup-harness.html");
  await page.waitForFunction(() => window.popupHarness !== undefined);
  await page.evaluate(() => {
    document.querySelector("#b")!.textContent = "食べました";
    window.popupHarness.select("#b");
  });
  await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(1);
  await page.evaluate(() => window.popupHarness.succeed(0, "conjugation"));
  const dialog = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
  await expect(dialog.getByRole("heading", { name: "Biến đổi từ", exact: true })).toBeVisible();
  await expect(dialog.getByText("食べました → 食べる", { exact: true })).toBeVisible();
  await expect(dialog.getByText("Dạng lịch sự, quá khứ của 食べる.", { exact: true })).toBeVisible();
  await expect(dialog.locator("[data-rule-id] li")).toHaveCount(3);
  await expect(dialog.getByText("Dữ liệu: development-20261013.1", { exact: true })).toBeVisible();
  await expect(dialog.getByText("Tách từ và biến đổi từ: chưa khả dụng.", { exact: true })).toHaveCount(0);
  await dialog.getByRole("button", { name: "Đóng tra cứu" }).click();
  await expect(dialog).toHaveCount(0);
});

test("F-09 retry uses a fresh identity and renders only the current v2 result", async ({ page }) => {
  await page.goto("/tests/browser/selection-popup-harness.html");
  await page.waitForFunction(() => window.popupHarness !== undefined);
  await page.evaluate(() => {
    document.querySelector("#b")!.textContent = "食べました";
    window.popupHarness.select("#b");
  });
  await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(1);
  await page.evaluate(() => window.popupHarness.fail(0));
  const dialog = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
  await expect(dialog.getByRole("alert")).toContainText("Không thể kết nối");
  await dialog.getByRole("button", { name: "Thử lại" }).click();
  await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(2);
  const attempts = await page.evaluate(() => window.popupHarness.attempts());
  expect(attempts[1]!.interactionId).not.toBe(attempts[0]!.interactionId);
  await page.evaluate(() => window.popupHarness.succeed(1, "conjugation"));
  await expect(dialog.getByText("食べました → 食べる", { exact: true })).toBeVisible();
  await expect(dialog.getByRole("alert")).toHaveCount(0);
});

test("F-09 late v2 result cannot overwrite a newer selection result", async ({ page }) => {
  await page.goto("/tests/browser/selection-popup-harness.html");
  await page.waitForFunction(() => window.popupHarness !== undefined);
  await page.evaluate(() => {
    document.querySelector("#a")!.textContent = "食べました";
    window.popupHarness.select("#a");
  });
  await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(1);
  await page.evaluate(() => window.popupHarness.select("#b"));
  await expect.poll(() => page.evaluate(() => window.popupHarness.attempts().length)).toBe(2);
  expect(await page.evaluate(() => window.popupHarness.attempts()[0]!.aborted)).toBe(true);
  await page.evaluate(() => window.popupHarness.succeed(1));
  await page.evaluate(() => window.popupHarness.succeed(0, "conjugation"));
  const dialog = page.getByRole("dialog", { name: "Tra cứu tiếng Nhật" });
  await expect(dialog.locator(".jp-selected")).toHaveText("食事");
  await expect(dialog.getByText("bữa ăn", { exact: true })).toBeVisible();
  await expect(dialog.locator("[data-rule-id]")).toHaveCount(0);
});
