import { describe, expect, it } from "vitest";

import { containsJapaneseText } from "../../src/core/japaneseTextDetector";
import {
  japaneseTextDetectionCorpus,
  materializeCorpusInput,
} from "../corpus/japanese-text-detection-corpus";

const positiveCases = japaneseTextDetectionCorpus.filter(
  (testCase) => testCase.story === "US1" && testCase.expected === true,
);

describe("containsJapaneseText positive Unicode 17 corpus", () => {
  it.each(positiveCases)("$id", (testCase) => {
    const input = materializeCorpusInput(testCase.input);

    expect(containsJapaneseText(input)).toBe(true);
  });
});
