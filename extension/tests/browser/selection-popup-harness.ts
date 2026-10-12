import { startSelectionPopup } from "../../src/browser/popup/startSelectionPopup.ts";
import { AnalysisFailure, type AnalysisResponse } from "../../src/api/analysisContract.ts";
import { analysisFixture } from "../unit/analysisFixtures.ts";

interface Attempt {
  text: string; interactionId: string; signal: AbortSignal;
  resolve: (response: AnalysisResponse) => void; reject: (reason: unknown) => void;
}
interface PopupHarness {
  select(selector: string): void;
  clear(): void;
  attempts(): { text: string; interactionId: string; aborted: boolean }[];
  succeed(index: number, variant?: "rich" | "empty" | "unavailable"): void;
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
    const response = analysisFixture(attempt.interactionId);
    response.data.normalized_text = attempt.text;
    if (variant === "rich") {
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
