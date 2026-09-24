import {
  classifyJapaneseText,
  createJapaneseTextClassifier,
} from "../../src/boundary/classifyJapaneseText";
import type { JapaneseTextDetectionResult } from "../../src/boundary/japaneseTextDetectionResult";

declare global {
  interface Window {
    f01Classify(input: unknown): JapaneseTextDetectionResult;
    f01ClassifyUnexpectedFailure(input: unknown): JapaneseTextDetectionResult;
  }
}

window.f01Classify = classifyJapaneseText;
window.f01ClassifyUnexpectedFailure = createJapaneseTextClassifier(() => {
  throw new Error("synthetic non-content failure");
});
