import { startSelectionPopup } from "../../src/browser/popup/startSelectionPopup.ts";
import { AnalysisFailure, type AnalysisResponse } from "../../src/api/analysisContract.ts";
import { analysisFixture, conjugationFixture, v3ConjugationFixture } from "../unit/analysisFixtures.ts";

interface Attempt {
  text: string; interactionId: string; signal: AbortSignal;
  resolve: (response: AnalysisResponse) => void; reject: (reason: unknown) => void;
}
interface PopupHarness {
  select(selector: string): void;
  clear(): void;
  attempts(): { text: string; interactionId: string; aborted: boolean }[];
  succeed(index: number, variant?: "rich" | "empty" | "unavailable" | "conjugation" | "conjugation-v3" | "kana-only"): void;
  fail(index: number): void;
  dispose(): void;
}
declare global { interface Window { popupHarness: PopupHarness } }
const attempts: Attempt[] = [];
const dispose = startSelectionPopup({ document, analyze: (text, interactionId, signal) =>
  new Promise<AnalysisResponse>((resolve, reject) => attempts.push({ text, interactionId, signal, resolve, reject })),
});
window.popupHarness = {
  select(selector) {
    const node = document.querySelector(selector);
    if (node === null) throw new Error("Missing fixture node");
    const range = document.createRange(); range.selectNodeContents(node);
    const selection = window.getSelection(); selection?.removeAllRanges(); selection?.addRange(range);
    // Dispatch immediately to make selection timing deterministic; native event also exercises de-duplication.
    document.dispatchEvent(new Event("selectionchange"));
  },
  clear() { window.getSelection()?.removeAllRanges(); document.dispatchEvent(new Event("selectionchange")); },
  attempts: () => attempts.map(({ text, interactionId, signal }) => ({ text, interactionId, aborted: signal.aborted })),
  succeed(index, variant = "rich") {
    const attempt = attempts[index]; if (!attempt) throw new Error("Missing attempt");
    if (variant === "conjugation") { attempt.resolve(conjugationFixture(attempt.interactionId)); return; }
    if (variant === "conjugation-v3") { attempt.resolve(v3ConjugationFixture(attempt.interactionId, attempt.text)); return; }
    const response = analysisFixture(attempt.interactionId);
    response.data.normalized_text = attempt.text;
    if (variant === "kana-only") {
      const match = response.data.dictionary_matches[0]!;
      match.matchedWrittenForm = null; match.matchedReading = "かな"; match.writtenForms = [];
      match.readings[0]!.readingText = "かな"; match.readings[0]!.info = ["reading:no-kanji"];
      match.readings[0]!.restrictedToForms = null; match.senses[0]!.restrictions = [];
    } else if (variant === "rich") {
      response.data.dictionary_matches[0]!.senses[0]!.glosses.push({ languageTag: "en", glossText: "meal" });
      response.data.kanji[0]!.jlptLevel = 5;
      response.data.kanji[0]!.jlptProvenance = "legacy-estimate";
      response.data.grammar_occurrences[0]!.span = { start: 0, end: 1 };
      response.data.grammar_occurrences.push({ ...response.data.grammar_occurrences[0]!, matched_text: "事", span: { start: 1, end: 2 } });
    } else {
      response.data.dictionary_matches = []; response.data.kanji = []; response.data.grammar_occurrences = [];
      if (variant === "unavailable") response.data.capabilities.grammar = "unavailable";
    }
    attempt.resolve(response);
  },
  fail(index) { attempts[index]?.reject(new AnalysisFailure("NETWORK_ERROR", true)); },
  dispose,
};
