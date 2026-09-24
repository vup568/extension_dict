import { expect, test } from "@playwright/test";

import {
  rubySafeSelectionCorpus,
  type RubySafeSelectionCorpusCase,
} from "../corpus/ruby-safe-selection-corpus";

const editableCases = rubySafeSelectionCorpus.filter(({ id }) =>
  id.startsWith("F02-contenteditable-"),
);

test("preserves editable content and exact Unicode without mutation", async ({
  page,
}, testInfo) => {
  await page.goto("/tests/browser/ruby-safe-selection-harness.html");

  for (const corpusCase of editableCases) {
    const observation = await page.evaluate(
      (fixture: RubySafeSelectionCorpusCase) => {
        window.f02SetCase(fixture);
        const target = document.querySelector<HTMLElement>("#target");
        if (target === null) {
          throw new Error("editable fixture missing");
        }

        const before = {
          html: target.innerHTML,
          text: target.textContent,
          selection: window.getSelection()?.toString(),
        };
        const result = window.f02Extract();
        const after = {
          html: target.innerHTML,
          text: target.textContent,
          selection: window.getSelection()?.toString(),
        };

        return { before, after, result };
      },
      corpusCase,
    );

    expect(observation.result, `${corpusCase.id} on ${testInfo.project.name}`).toEqual(
      corpusCase.expected,
    );
    expect(observation.after).toEqual(observation.before);
  }
});
