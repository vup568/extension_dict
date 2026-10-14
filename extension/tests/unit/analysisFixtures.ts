import type { AnalysisResponse } from "../../src/api/analysisContract.ts";
import { approvedConjugationExamples } from "./approvedConjugationExamples.ts";
export const interactionId = "11111111-1111-4111-8111-111111111111";
export function v3ConjugationFixture(id = interactionId, text = "食べられる"): AnalysisResponse {
  const sample = structuredClone(approvedConjugationExamples[text]);
  if (!sample) throw new Error("Unknown approved fixture");
  const value = analysisFixture(id);
  value.meta.contract_version = "3";
  value.data.normalized_text = text;
  value.data.grammar_occurrences = [];
  value.data.dictionary_matches = [{ canonicalId: sample.canonicalId, matchedWrittenForm: sample.baseForm,
    matchedReading: null, writtenForms: [{ id: 1, form: sample.baseForm, priority: null, isCommon: true, info: null }],
    readings: sample.readings.map((reading, i) => ({ id: 100 + i, readingText: reading, priority: null, isCommon: true, info: null, restrictedToForms: null })),
    senses: [{ senseKey: "1", position: 1, partOfSpeech: [], glosses: [{ languageTag: "vi", glossText: sample.glossVi }], restrictions: [] }] }];
  value.data.conjugations = sample.conjugations;
  value.data.capabilities.conjugation = "completed";
  return value;
}
export function conjugationFixture(id = interactionId): AnalysisResponse {
  const value = analysisFixture(id);
  value.meta.contract_version = "2";
  value.data.normalized_text = "食べました";
  value.data.dictionary_matches[0]!.matchedWrittenForm = "食べる";
  value.data.dictionary_matches[0]!.writtenForms[0]!.form = "食べる";
  value.data.grammar_occurrences = [];
  value.data.capabilities.conjugation = "completed";
  value.data.conjugations = [{ surface: "食べました", base_form: "食べる", explanation: "Dạng lịch sự, quá khứ của 食べる.",
    language_tag: "vi", rule_id: "conjugation:verb-polite-past", rule_version: 1, dataset_version: "development-20261013.1",
    span: { start: 0, end: 5, unit: "utf16" }, segments: [
      { surface: "食べ", explanation: "Phần động từ.", span: { start: 0, end: 2, unit: "utf16" } },
      { surface: "まし", explanation: "Trợ động từ lịch sự.", span: { start: 2, end: 4, unit: "utf16" } },
      { surface: "た", explanation: "Trợ động từ quá khứ.", span: { start: 4, end: 5, unit: "utf16" } },
    ] }];
  return value;
}
export function analysisFixture(id = interactionId): AnalysisResponse {
  return {
    data: {
      normalized_text: "食事", tokens: [], conjugations: [],
      dictionary_matches: [{ canonicalId: "jmdict:1", matchedWrittenForm: "食事", matchedReading: null,
        writtenForms: [{ id: 1, form: "食事", priority: null, isCommon: true, info: null }],
        readings: [{ id: 2, readingText: "しょくじ", priority: 1, isCommon: true, info: [], restrictedToForms: ["食事"] }],
        senses: [{ senseKey: "sense:1", position: 1, partOfSpeech: ["n"], glosses: [{ languageTag: "vi", glossText: "bữa ăn" }],
          restrictions: [{ writtenFormId: 1, readingId: 2 }] }] }],
      kanji: [{ canonicalId: "kanji:食", character: "食", strokeCount: 9, grade: 2, jlptLevel: null, jlptProvenance: null,
        frequency: 328, unicodeCodepoint: "98DF", radicalNumber: 184, hanViet: "thực", onReadings: ["ショク"], kunReadings: ["た.べる"],
        meaningsVi: ["ăn"], meaningsEn: ["eat"], nanoriReadings: [] }],
      grammar_occurrences: [{ grammar_id: "grammar:1", pattern: "食事", jlpt_level: "N5", meaning_vi: null, matched_text: "食事", span: { start: 0, end: 2 } }],
      capabilities: { vocabulary: "completed", kanji: "completed", grammar: "completed", morphology: "unavailable", conjugation: "unavailable" },
    }, meta: { request_id: "request-1", interaction_id: id, contract_version: "1" },
  };
}
