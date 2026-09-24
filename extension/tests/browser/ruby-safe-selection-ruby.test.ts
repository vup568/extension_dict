import { expect, test } from "@playwright/test";

import {
  rubySafeSelectionCorpus,
  type RubySafeSelectionCorpusCase,
} from "../corpus/ruby-safe-selection-corpus";

test("extracts exact base text and excludes ruby annotation", async ({
  browser,
  page,
}, testInfo) => {
  await page.goto("/tests/browser/ruby-safe-selection-harness.html");

  for (const corpusCase of rubySafeSelectionCorpus) {
    const result = await page.evaluate(
      (fixture: RubySafeSelectionCorpusCase) =>
        (
          window as typeof window & {
            f02RunCase(value: RubySafeSelectionCorpusCase): unknown;
          }
        ).f02RunCase(fixture),
      corpusCase,
    );

    expect(
      result,
      `${corpusCase.id} on ${testInfo.project.name} ${browser.version()}`,
    ).toEqual(corpusCase.expected);
  }
});

test("hands only successful ruby-safe base text to F-01", async ({ page }) => {
  await page.goto("/tests/browser/ruby-safe-selection-harness.html");
  const corpusCase = rubySafeSelectionCorpus.find(
    ({ id }) => id === "F02-japanese-annotation-non-japanese-base",
  );
  if (corpusCase === undefined) {
    throw new Error("F-02/F-01 handoff fixture is missing");
  }

  const result = await page.evaluate((fixture) => {
    window.f02SetCase(fixture);
    return window.f02ExtractThenClassify();
  }, corpusCase);

  expect(result).toEqual({
    extraction: { status: "ok", text: "ABC" },
    classification: { status: "ok", containsJapaneseText: false },
  });
});
