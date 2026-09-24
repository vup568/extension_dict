import { describe, expect, it, vi } from "vitest";

import { createJapaneseTextClassifier } from "../../src/boundary/classifyJapaneseText";

describe("classifyJapaneseText error contract", () => {
  const classifier = createJapaneseTextClassifier();

  it.each([null, undefined, 1, true, [], {}])(
    "rejects non-string input without coercion",
    (input) => {
      expect(classifier(input)).toEqual({
        status: "error",
        code: "invalid-input",
      });
    },
  );

  it("separates unexpected execution failure from false", () => {
    const detector = vi.fn(() => {
      throw new Error("synthetic failure");
    });
    const failingClassifier = createJapaneseTextClassifier(detector);

    expect(failingClassifier("SYNTHETIC_PRIVATE_MARKER_日本語")).toEqual({
      status: "error",
      code: "unexpected-failure",
    });
  });

  it("never exposes raw input in an error result", () => {
    const marker = "SYNTHETIC_PRIVATE_MARKER_日本語";
    const result = createJapaneseTextClassifier(() => {
      throw new Error(marker);
    })(marker);

    expect(JSON.stringify(result)).not.toContain(marker);
  });
});
