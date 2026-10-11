import { describe, expect, expectTypeOf, it } from "vitest";

import {
  RUBY_SAFE_SELECTION_ERROR_CODES,
  type RubySafeSelectionError,
  type RubySafeSelectionResult,
  type RubySafeSelectionSuccess,
} from "../../src/boundary/rubySafeSelectionResult";

describe("RubySafeSelectionResult contract", () => {
  it("publishes exactly the six approved content-free error codes", () => {
    expect(RUBY_SAFE_SELECTION_ERROR_CODES).toEqual([
      "no-selection",
      "unsupported-multi-range",
      "no-base-text",
      "stale-selection",
      "inaccessible-context",
      "unexpected-failure",
    ]);
  });

  it("keeps success and error shapes discriminated", () => {
    const success: RubySafeSelectionSuccess = { status: "ok", text: "日本" };
    const error: RubySafeSelectionError = {
      status: "error",
      code: "no-selection",
    };

    expect(success).toEqual({ status: "ok", text: "日本" });
    expect(error).toEqual({ status: "error", code: "no-selection" });
    expectTypeOf(success).toMatchTypeOf<RubySafeSelectionResult>();
    expectTypeOf(error).toMatchTypeOf<RubySafeSelectionResult>();
  });

  it.each(RUBY_SAFE_SELECTION_ERROR_CODES)(
    "%s never carries raw or partial content",
    (code) => {
      const marker = "PRIVATE_SELECTION_日本語";
      const result: RubySafeSelectionResult = { status: "error", code };
      const serialized = JSON.stringify(result);

      expect(serialized).not.toContain(marker);
      expect(result).not.toHaveProperty("text");
      expect(result).not.toHaveProperty("html");
      expect(result).not.toHaveProperty("cause");
      expect(result).not.toHaveProperty("stack");
    },
  );
});
