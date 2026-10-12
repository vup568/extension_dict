import type { AnalysisResponse } from "../../src/api/analysisContract.ts";
export const interactionId = "11111111-1111-4111-8111-111111111111";
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
