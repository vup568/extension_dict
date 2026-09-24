import { expect, test } from "@playwright/test";

test("is invariant across connectivity and simulated page/user state", async ({
  context,
  page,
}) => {
  await page.goto("/tests/browser/harness.html");
  await page.evaluate(() => {
    document.documentElement.lang = "vi";
    document.body.style.fontFamily = "serif";
    document.cookie = "synthetic-auth-state=anonymous";
  });

  const online = await page.evaluate(() => window.f01Classify("Hello 日本語"));
  await context.setOffline(true);
  const offline = await page.evaluate(() => window.f01Classify("Hello 日本語"));
  await context.setOffline(false);
  await page.evaluate(() => {
    document.documentElement.lang = "en";
    document.body.style.fontFamily = "sans-serif";
    document.cookie = "synthetic-auth-state=authenticated";
  });
  const changedContext = await page.evaluate(() =>
    window.f01Classify("Hello 日本語"),
  );

  expect(online).toEqual({ status: "ok", containsJapaneseText: true });
  expect(offline).toEqual(online);
  expect(changedContext).toEqual(online);
});
