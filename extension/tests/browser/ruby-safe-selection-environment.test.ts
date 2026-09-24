import { expect, test } from "@playwright/test";

import type { RubySafeSelectionCorpusCase } from "../corpus/ruby-safe-selection-corpus";

test("is deterministic across environment state and repeated calls", async ({
  context,
  page,
}, testInfo) => {
  await page.goto("/tests/browser/ruby-safe-selection-harness.html");
  const fixture: RubySafeSelectionCorpusCase = {
    id: "F02-environment-invariance",
    acceptanceCriteria: ["F02-AC-023"],
    html: "<div id='target'><ruby>日本<rt>にほん</rt></ruby><br>語</div>",
    range: { kind: "contents", selector: "#target" },
    expected: { status: "ok", text: "日本\n語" },
  };

  const online = await page.evaluate((value) => {
    document.documentElement.lang = "vi";
    document.body.style.fontFamily = "serif";
    document.body.dataset.loginState = "anonymous";
    window.f02SetCase(value);
    return [window.f02Extract(), window.f02Extract()];
  }, fixture);

  await context.setOffline(true);
  const offline = await page.evaluate((value) => {
    document.documentElement.lang = "ja";
    document.body.style.fontFamily = "sans-serif";
    document.body.dataset.loginState = "authenticated";
    window.f02SetCase(value);
    return [window.f02Extract(), window.f02Extract()];
  }, fixture);
  await context.setOffline(false);

  expect(online, `online on ${testInfo.project.name}`).toEqual([
    fixture.expected,
    fixture.expected,
  ]);
  expect(offline, `offline on ${testInfo.project.name}`).toEqual([
    fixture.expected,
    fixture.expected,
  ]);
});
