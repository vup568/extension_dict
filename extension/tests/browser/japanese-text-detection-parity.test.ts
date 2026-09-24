import { expect, test } from "@playwright/test";

import {
  japaneseTextDetectionCorpus,
  materializeCorpusInput,
} from "../corpus/japanese-text-detection-corpus";

test("runs the complete classification corpus with Unicode 17 semantics", async ({
  browser,
  page,
}, testInfo) => {
  await page.goto("/tests/browser/harness.html");

  const browserVersion = browser.version();
  testInfo.annotations.push({
    type: "browser-version",
    description: browserVersion,
  });
  testInfo.annotations.push({
    type: "unicode-policy",
    description: "17.0.0",
  });

  for (const corpusCase of japaneseTextDetectionCorpus) {
    const input = materializeCorpusInput(corpusCase.input);
    const result = await page.evaluate(
      (value) => window.f01Classify(value),
      input,
    );

    expect(
      result,
      `${corpusCase.id} on ${testInfo.project.name} ${browserVersion}`,
    ).toEqual({
      status: "ok",
      containsJapaneseText: corpusCase.expected,
    });
  }
});
