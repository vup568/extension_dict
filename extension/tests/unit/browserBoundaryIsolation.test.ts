import { describe, expect, it } from "vitest";

import classifyJapaneseTextSource from "../../src/boundary/classifyJapaneseText.ts?raw";
import semanticBlockElementsSource from "../../src/browser/selection/semanticBlockElements.ts?raw";
import selectedTextAccumulatorSource from "../../src/core/selectedTextAccumulator.ts?raw";

describe("browser boundary isolation", () => {
  it("keeps the text accumulator independent of DOM and browser globals", () => {
    expect(selectedTextAccumulatorSource).not.toMatch(
      /\b(?:window|document|Selection|Range|Node|Element|HTMLElement)\b/,
    );
    expect(selectedTextAccumulatorSource).not.toContain("../browser/");
  });

  it("does not leak the F-02 browser adapter into the F-01 boundary", () => {
    expect(classifyJapaneseTextSource).not.toContain("browser/selection");
    expect(classifyJapaneseTextSource).not.toContain(
      "extractRubySafeSelection",
    );
  });

  it("keeps the reviewed semantic block policy fixed and CSS-independent", () => {
    const reviewedNames = [
      "address",
      "article",
      "aside",
      "blockquote",
      "dd",
      "div",
      "dl",
      "dt",
      "fieldset",
      "figcaption",
      "figure",
      "footer",
      "form",
      "h1",
      "h2",
      "h3",
      "h4",
      "h5",
      "h6",
      "header",
      "hgroup",
      "hr",
      "li",
      "main",
      "nav",
      "ol",
      "p",
      "pre",
      "search",
      "section",
      "table",
      "tbody",
      "td",
      "tfoot",
      "th",
      "thead",
      "tr",
      "ul",
    ];

    for (const localName of reviewedNames) {
      expect(semanticBlockElementsSource).toContain(`"${localName}"`);
    }
    expect(semanticBlockElementsSource).not.toContain("getComputedStyle");
  });
});
