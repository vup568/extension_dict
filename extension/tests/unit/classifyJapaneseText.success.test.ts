import { describe, expect, it } from "vitest";

import { classifyJapaneseText } from "../../src/boundary/classifyJapaneseText";

describe("classifyJapaneseText valid input contract", () => {
  it("returns an explicit successful true outcome", () => {
    expect(classifyJapaneseText("Hello 日本語")).toEqual({
      status: "ok",
      containsJapaneseText: true,
    });
  });

  it("returns an explicit successful false outcome", () => {
    expect(classifyJapaneseText("Hello")).toEqual({
      status: "ok",
      containsJapaneseText: false,
    });
  });

  it("does not modify the caller input", () => {
    const input = "か\u3099";

    classifyJapaneseText(input);

    expect(input).toBe("か\u3099");
  });
});
