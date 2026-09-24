import { describe, expect, it } from "vitest";

import { containsJapaneseText } from "../../src/core/japaneseTextDetector";
import {
  japaneseTextDetectionCorpus,
  materializeCorpusInput,
} from "../corpus/japanese-text-detection-corpus";

const negativeCases = japaneseTextDetectionCorpus.filter(
  (testCase) => testCase.story === "US2" && testCase.expected === false,
);

describe("containsJapaneseText negative and malformed Unicode corpus", () => {
  it.each(negativeCases)("$id", (testCase) => {
    expect(containsJapaneseText(materializeCorpusInput(testCase.input))).toBe(
      false,
    );
  });

  it("scans the complete input and finds a trigger at the end", () => {
    expect(containsJapaneseText(`${"a".repeat(100_000)}𠮟`)).toBe(true);
  });

  it("does not retain state between calls", () => {
    const sequence = ["日", "Hello", "日", "", "𠮟", "😀"] as const;
    const expected = [true, false, true, false, true, false] as const;

    for (let iteration = 0; iteration < 100; iteration += 1) {
      expect(sequence.map(containsJapaneseText)).toEqual(expected);
    }
  });

  it("continues after a lone high surrogate", () => {
    expect(containsJapaneseText(String.fromCharCode(0xd800, 0x65e5))).toBe(true);
  });

  it("continues after a valid supplementary pair followed by a lone surrogate", () => {
    const input = String.fromCharCode(0xd842, 0xdf9f, 0xdc00);

    expect(containsJapaneseText(input)).toBe(true);
  });
});
