import { expect, test } from "@playwright/test";

import {
  rubySafeSelectionCorpus,
  type RubySafeSelectionCorpusCase,
} from "../corpus/ruby-safe-selection-corpus";

const structureCaseIds = new Set([
  "F02-explicit-br",
  "F02-adjacent-blocks",
  "F02-consecutive-br",
  "F02-existing-lf-before-block",
  "F02-whitespace-preserved",
  "F02-forward-direction",
  "F02-backward-direction",
  "F02-long-complete-selection",
  "F02-css-generated-and-neighbor-excluded",
]);

const structureCases = rubySafeSelectionCorpus.filter(({ id }) =>
  structureCaseIds.has(id),
);

test("preserves exact structure, direction, boundaries, and long content", async ({
  page,
}, testInfo) => {
  await page.goto("/tests/browser/ruby-safe-selection-harness.html");

  for (const corpusCase of structureCases) {
    const result = await page.evaluate(
      (fixture: RubySafeSelectionCorpusCase) => window.f02RunCase(fixture),
      corpusCase,
    );

    expect(result, `${corpusCase.id} on ${testInfo.project.name}`).toEqual(
      corpusCase.expected,
    );
  }
});
