import { expect, test } from "@playwright/test";

import type { RubySafeSelectionErrorCode } from "../../src/boundary/rubySafeSelectionResult";

const expectedErrors: readonly [string, RubySafeSelectionErrorCode][] = [
  ["none", "no-selection"],
  ["collapsed", "no-selection"],
  ["multi-range", "unsupported-multi-range"],
  ["annotation-only", "no-base-text"],
  ["stale", "stale-selection"],
  ["inaccessible", "inaccessible-context"],
  ["unexpected", "unexpected-failure"],
];

test("maps every invalid or hostile selection to a content-free error", async ({
  page,
}, testInfo) => {
  await page.goto("/tests/browser/ruby-safe-selection-harness.html");

  for (const [scenario, code] of expectedErrors) {
    const marker = `PRIVATE_${scenario}_日本語`;
    const result = await page.evaluate(
      ({ scenarioName, privateMarker }) =>
        window.f02RunErrorCase(scenarioName, privateMarker),
      { scenarioName: scenario, privateMarker: marker },
    );

    expect(result, `${scenario} on ${testInfo.project.name}`).toEqual({
      status: "error",
      code,
    });
    expect(JSON.stringify(result)).not.toContain(marker);
  }
});

test("extracts only the allowed event frame selection", async ({ page }, testInfo) => {
  await page.goto("/tests/browser/ruby-safe-selection-harness.html");

  const result = await page.evaluate(() => window.f02RunFrameCase());

  expect(result, `frame locality on ${testInfo.project.name}`).toEqual({
    status: "ok",
    text: "対象日本",
  });
  expect(JSON.stringify(result)).not.toContain("PARENT_PRIVATE_MARKER");
  expect(JSON.stringify(result)).not.toContain("SIBLING_PRIVATE_MARKER");
});
