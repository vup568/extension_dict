import { startSelectionPopup } from "../../src/browser/popup/startSelectionPopup.ts";
import { AnalysisFailure, parseAnalysisResponse, type EntryMatch } from "../../src/api/analysisContract.ts";
import type { TranslationResponse } from "../../src/api/translationContract.ts";
import { analysisFixture } from "../unit/analysisFixtures.ts";

interface Attempt { text: string; interactionId: string; signal: AbortSignal; resolve(value: TranslationResponse): void; reject(error: unknown): void }
interface TranslationHarness {
  select(selector: string): void;
  attempts(): { text: string; aborted: boolean }[];
  succeed(index: number, result: string): void;
  fail(index: number): void;
}
declare global { interface Window { translationHarness: TranslationHarness } }
const attempts: Attempt[] = [];

function entry(form: string, reading: string, gloss: string, id: number, matchReading = false): EntryMatch {
  return {
    canonicalId: `dictionary:jmdict:${id}`, matchedWrittenForm: matchReading ? null : form,
    matchedReading: matchReading ? reading : null,
    writtenForms: [{ id, form, priority: null, isCommon: true, info: null }],
    readings: [{ id, readingText: reading, priority: null, isCommon: true, info: null, restrictedToForms: [form] }],
    senses: [{ senseKey: "1", partOfSpeech: ["n"], position: 1, glosses: [{ languageTag: "en", glossText: gloss }],
      restrictions: [{ writtenFormId: id, readingId: id }] }],
  };
}

startSelectionPopup({ document,
  analyze: async (text, interactionId) => {
    if (text === "分析エラー") throw new AnalysisFailure("SERVICE_UNAVAILABLE", true);
    const value = analysisFixture(interactionId);
    value.data.normalized_text = text;
    if (text === "猫、猫。") {
      value.meta.contract_version = "3";
      value.data.dictionary_matches = [];
      value.data.grammar_occurrences = [];
      value.data.capabilities.grammar = "unavailable";
      value.data.kanji[0] = { ...value.data.kanji[0]!, canonicalId: "kanji:猫", character: "猫" };
      value.data.vocabulary_occurrences = [0, 2].map(start => ({
        surface: "猫", span: { start, end: start + 1, unit: "utf16" },
        matches: [entry("猫", "ねこ", "cat", 1)], conjugations: [],
      }));
      value.data.vocabulary_truncated = true;
      return parseAnalysisResponse(value, interactionId);
    }
    if (text === "はし" || text === "電気代" || text === "食べました") {
      value.meta.contract_version = "3";
      value.data.grammar_occurrences = [];
      value.data.dictionary_matches = text === "はし"
        ? [entry("橋", "はし", "bridge", 1, true), entry("箸", "はし", '<img src=x onerror="alert(1)"> chopsticks', 2, true)]
        : text === "電気代" ? [entry("電気代", "でんきだい", "electricity bill", 1)]
          : [entry("食べる", "たべる", "to eat", 1)];
      return parseAnalysisResponse(value, interactionId);
    }
    return value;
  },
  translate: (text, interactionId, signal) => new Promise((resolve, reject) => { attempts.push({ text, interactionId, signal, resolve, reject }); }),
});
window.translationHarness = {
  select(selector) {
    const node = document.querySelector(selector); if (!node) throw new Error("Missing fixture node");
    const range = document.createRange(); range.selectNodeContents(node);
    const selection = window.getSelection(); selection?.removeAllRanges(); selection?.addRange(range);
    document.dispatchEvent(new Event("selectionchange"));
  },
  attempts: () => attempts.map(({ text, signal }) => ({ text, aborted: signal.aborted })),
  succeed(index, result) { const item = attempts[index]!; item.resolve({ data: { source_language: "ja", target_language: "en", translated_text: result }, meta: { interaction_id: item.interactionId, request_id: "test" } }); },
  fail(index) { attempts[index]!.reject(new AnalysisFailure("SERVICE_UNAVAILABLE", true)); },
};
