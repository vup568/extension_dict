import { describe, expect, it } from "vitest";

import boundarySource from "../../src/boundary/classifyJapaneseText.ts?raw";
import detectorSource from "../../src/core/japaneseTextDetector.ts?raw";

describe("F-01 runtime dependency isolation", () => {
  it("imports only the generated policy and local result contract", () => {
    const source = `${detectorSource}\n${boundarySource}`;
    const forbidden = [
      /\bfetch\s*\(/u,
      /XMLHttpRequest/u,
      /localStorage/u,
      /sessionStorage/u,
      /indexedDB/u,
      /\bdocument\b/u,
      /\bwindow\b/u,
      /chrome\./u,
      /browser\./u,
      /react/u,
      /console\./u,
    ];

    for (const pattern of forbidden) {
      expect(source).not.toMatch(pattern);
    }
  });
});
