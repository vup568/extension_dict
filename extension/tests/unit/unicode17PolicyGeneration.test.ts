import { describe, expect, it } from "vitest";

import {
  intersectRanges,
  mergeRanges,
  parseLetterOtherRanges,
  parsePropertyRanges,
} from "../../unicode/unicodePolicyRanges";

describe("Unicode 17 policy generation", () => {
  it("parses only requested property values", () => {
    const content = [
      "3041..3042 ; Hiragana # Lo",
      "30A1 ; Katakana # Lo",
      "0041..005A ; Latin # L&",
    ].join("\n");

    expect(
      parsePropertyRanges(content, new Set(["Hiragana", "Katakana"])),
    ).toEqual([
      [0x3041, 0x3042],
      [0x30a1, 0x30a1],
    ]);
  });

  it("expands UnicodeData First/Last records for Letter_Other", () => {
    const content = [
      "3400;<CJK Ideograph Extension A, First>;Lo;0;L;;;;;N;;;;;",
      "4DBF;<CJK Ideograph Extension A, Last>;Lo;0;L;;;;;N;;;;;",
      "0041;LATIN CAPITAL LETTER A;Lu;0;L;;;;;N;;;;0061;",
      "3007;IDEOGRAPHIC NUMBER ZERO;Nl;0;L;;;;;N;;;;;",
    ].join("\n");

    expect(parseLetterOtherRanges(content)).toEqual([[0x3400, 0x4dbf]]);
  });

  it("intersects property and category ranges", () => {
    expect(
      intersectRanges(
        [
          [0x3000, 0x30ff],
          [0x3100, 0x31ff],
        ],
        [[0x3041, 0x3096]],
      ),
    ).toEqual([[0x3041, 0x3096]]);
  });

  it("sorts and merges overlapping or adjacent ranges", () => {
    expect(
      mergeRanges([
        [0x30a0, 0x30ff],
        [0x3041, 0x3096],
        [0x3097, 0x309f],
        [0x30f0, 0x3100],
      ]),
    ).toEqual([[0x3041, 0x3100]]);
  });
});
