import { containsJapaneseText } from "../core/japaneseTextDetector.ts";
import type { JapaneseTextDetectionResult } from "./japaneseTextDetectionResult.ts";

export type JapaneseTextDetector = (text: string) => boolean;
export type JapaneseTextClassifier = (
  input: unknown,
) => JapaneseTextDetectionResult;

/**
 * EARS[Unwanted]: IF detector input is not a string or classification fails,
 * the boundary SHALL return a non-content error distinct from false.
 */
export function createJapaneseTextClassifier(
  detector: JapaneseTextDetector = containsJapaneseText,
): JapaneseTextClassifier {
  return (input: unknown): JapaneseTextDetectionResult => {
    if (typeof input !== "string") {
      return {
        status: "error",
        code: "invalid-input",
      };
    }

    try {
      return {
        status: "ok",
        containsJapaneseText: detector(input),
      };
    } catch {
      return {
        status: "error",
        code: "unexpected-failure",
      };
    }
  };
}

export const classifyJapaneseText = createJapaneseTextClassifier();
