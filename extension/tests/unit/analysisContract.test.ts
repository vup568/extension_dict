import { describe, expect, it } from "vitest";
import { AnalysisFailure, parseAnalysisResponse, type AnalysisResponse } from "../../src/api/analysisContract.ts";
import { analysisFixture, interactionId } from "./analysisFixtures.ts";

describe("F-04 actual unified analysis contract", () => {
  it("accepts actual mixed-case dictionary/kanji and snake_case envelope/grammar", () => {
    const fixture = analysisFixture(); expect(parseAnalysisResponse(fixture, interactionId)).toBe(fixture);
  });
  it("preserves language, sense restrictions, and separate grammar occurrence identity", () => {
    const fixture = analysisFixture(); fixture.data.grammar_occurrences.push({ ...fixture.data.grammar_occurrences[0]!, span: { start: 1, end: 2 } });
    const result = parseAnalysisResponse(fixture, interactionId);
    expect(result.data.dictionary_matches[0]?.senses[0]?.restrictions).toEqual([{ writtenFormId: 1, readingId: 2 }]);
    expect(result.data.grammar_occurrences).toHaveLength(2);
  });
  it("accepts empty completed capabilities and explicit partial statuses", () => {
    const fixture = analysisFixture(); fixture.data.dictionary_matches = []; fixture.data.grammar_occurrences = [];
    fixture.data.capabilities.grammar = "failed";
    expect(parseAnalysisResponse(fixture, interactionId).data.capabilities.grammar).toBe("failed");
  });
  it.each([null, [], "raw_selected_text", {}, { data: {}, meta: {} }])("rejects malformed envelope %j without content in errors", value => {
    expect(() => parseAnalysisResponse(value, interactionId)).toThrow(AnalysisFailure);
    try { parseAnalysisResponse(value, interactionId); } catch (error) { expect(String(error)).not.toContain("raw_selected_text"); }
  });
  const invalidChanges: Array<[string, (value: AnalysisResponse) => void]> = [
    ["wrong interaction", value => { value.meta.interaction_id = "old"; }],
    ["unsupported contract", value => { (value.meta as { contract_version: string }).contract_version = "2"; }],
    ["missing request correlation", value => { value.meta.request_id = ""; }],
    ["bad language tag", value => { (value.data.dictionary_matches[0]!.senses[0]!.glosses[0]! as unknown as Record<string, unknown>).languageTag = 1; }],
    ["bad restriction identity", value => { value.data.dictionary_matches[0]!.senses[0]!.restrictions[0]!.readingId = 1.5; }],
    ["bad reading restriction", value => { (value.data.dictionary_matches[0]!.readings[0]! as unknown as Record<string, unknown>).restrictedToForms = {}; }],
    ["bad boolean", value => { (value.data.dictionary_matches[0]!.writtenForms[0]! as unknown as Record<string, unknown>).isCommon = "yes"; }],
    ["bad kanji meaning", value => { (value.data.kanji[0]! as unknown as Record<string, unknown>).meaningsVi = [1]; }],
    ["bad stroke count", value => { value.data.kanji[0]!.strokeCount = Number.NaN; }],
    ["inverted span", value => { value.data.grammar_occurrences[0]!.span = { start: 2, end: 1 }; }],
    ["out of bounds span", value => { value.data.grammar_occurrences[0]!.span.end = 9; }],
    ["negative span", value => { value.data.grammar_occurrences[0]!.span.start = -1; }],
    ["unknown capability", value => { (value.data.capabilities as Record<string, unknown>).grammar = "success"; }],
    ["opaque tokens", value => { value.data.tokens = [{ surface: "x" }]; }],
    ["opaque conjugations", value => { value.data.conjugations = ["x"]; }],
    ["failed capability with data", value => { value.data.capabilities.vocabulary = "failed"; }],
    ["unsupported morphology", value => { value.data.capabilities.morphology = "completed"; }],
  ];
  it.each(invalidChanges)("rejects %s at runtime", (_, mutate) => {
    const value = analysisFixture(); mutate(value); expect(() => parseAnalysisResponse(value, interactionId)).toThrow(AnalysisFailure);
  });
  it("never exposes backend error content as a client message", () => {
    const failure = new AnalysisFailure("PRIVATE_TEXT_日本語", true);
    expect(failure.message).toBe("Không thể hoàn thành phân tích.");
    expect(failure.code).toBe("HTTP_ERROR");
  });
});
