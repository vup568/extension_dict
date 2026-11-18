import { describe, expect, it } from "vitest";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { AnalysisFailure, parseAnalysisResponse, type AnalysisResponse } from "../../src/api/analysisContract.ts";
import { PopupShell } from "../../src/ui/PopupShell.tsx";
import { conjugationFixture, interactionId } from "./analysisFixtures.ts";

function sentenceFixture(): AnalysisResponse {
  const response = conjugationFixture();
  const baseEntry = response.data.dictionary_matches[0]!;
  const explanation = response.data.conjugations[0]!;
  response.meta.contract_version = "3";
  response.data.normalized_text = "日本語と食べましたと食べました";
  response.data.dictionary_matches = [];
  response.data.conjugations = [];
  response.data.capabilities.conjugation = "unavailable";
  const japanese = structuredClone(baseEntry);
  japanese.canonicalId = "dictionary:jmdict:japanese";
  japanese.matchedWrittenForm = "日本語";
  japanese.writtenForms[0]!.form = "日本語";
  japanese.senses[0]!.glosses = [{ languageTag: "en", glossText: "Japanese language" }];
  response.data.vocabulary_occurrences = [
    { surface: "日本語", span: { start: 0, end: 3, unit: "utf16" }, matches: [japanese], conjugations: [] },
    { surface: "食べました", span: { start: 4, end: 9, unit: "utf16" }, matches: [baseEntry], conjugations: [explanation] },
    { surface: "食べました", span: { start: 10, end: 15, unit: "utf16" }, matches: [structuredClone(baseEntry)], conjugations: [structuredClone(explanation)] },
  ];
  return response;
}

describe("sentence vocabulary occurrences", () => {
  it("accepts ordered repeated words and validates each explanation against its own surface and entry", () => {
    const response = sentenceFixture();
    const parsed = parseAnalysisResponse(response, interactionId);
    expect(parsed.data.vocabulary_occurrences?.map(item => item.span.start)).toEqual([0, 4, 10]);
    expect(parsed.data.vocabulary_occurrences?.[1]?.conjugations[0]?.base_form).toBe("食べる");
  });

  it("lists repeated occurrences by span and initially shows the first entry with results", () => {
    const response = sentenceFixture();
    const html = renderToStaticMarkup(createElement(PopupShell, { state: { phase: "result", text: response.data.normalized_text, interactionId, response }, onClose: () => {}, onRetry: () => {} }));
    expect(html).toContain('data-occurrence-choice="4:9"');
    expect(html).toContain('data-occurrence-choice="10:15"');
    expect(html).toContain('data-occurrence-start="0"');
    expect(html).toContain("Japanese language");
    expect(html).not.toContain("食べました → 食べる");
    expect(html).not.toContain("Tách từ và biến đổi từ: chưa khả dụng");
  });

  it("renders the scan target and its own conjugation, or an explicit no-match when the target is null", () => {
    const response = sentenceFixture();
    const state = { phase: "result" as const, text: response.data.normalized_text, interactionId, response };
    const props = { state, onClose: () => {}, onRetry: () => {}, isScan: true };
    const target = response.data.vocabulary_occurrences![2]!.span;
    const match = renderToStaticMarkup(createElement(PopupShell, { ...props, targetSpan: target }));
    expect(match).toContain('data-occurrence-start="10"');
    expect(match).toContain("食べました → 食べる");
    expect(match).not.toContain("Japanese language");

    const empty = renderToStaticMarkup(createElement(PopupShell, { ...props, targetSpan: null, scanOffset: 3 }));
    expect(empty).toContain("Không tìm thấy từ tại điểm quét.");
    expect(empty).not.toContain("食べました → 食べる");
    expect(empty).not.toContain("Japanese language");
  });

  it("preserves legacy single-word display when occurrences are absent", () => {
    const response = conjugationFixture();
    const html = renderToStaticMarkup(createElement(PopupShell, { state: { phase: "result", text: response.data.normalized_text, interactionId, response }, onClose: () => {}, onRetry: () => {} }));
    expect(html).toContain("Biến đổi từ");
    expect(html).toContain("食べました → 食べる");
    expect(html).not.toContain("data-occurrence-start");
  });

  it("shows an explicit notice when a sentence result was truncated", () => {
    const response = sentenceFixture();
    response.data.vocabulary_truncated = true;
    parseAnalysisResponse(response, interactionId);
    const html = renderToStaticMarkup(createElement(PopupShell, { state: { phase: "result", text: response.data.normalized_text, interactionId, response }, onClose: () => {}, onRetry: () => {} }));
    expect(html).toContain("Chỉ hiển thị một phần từ vựng do giới hạn xử lý.");
  });

  const corruptions: Array<[string, (response: AnalysisResponse) => void]> = [
    ["wrong surface", response => { response.data.vocabulary_occurrences![0]!.surface = "不正"; }],
    ["negative offset", response => { response.data.vocabulary_occurrences![0]!.span.start = -1; }],
    ["out of bounds", response => { response.data.vocabulary_occurrences![2]!.span.end = 16; }],
    ["overlap", response => { response.data.vocabulary_occurrences![2]!.span.start = 8; }],
    ["duplicate position", response => { response.data.vocabulary_occurrences!.push(structuredClone(response.data.vocabulary_occurrences![2]!)); }],
    ["wrong span unit", response => { (response.data.vocabulary_occurrences![0]!.span as unknown as Record<string, unknown>).unit = "bytes"; }],
    ["missing occurrence matches", response => { response.data.vocabulary_occurrences![1]!.matches = []; }],
    ["unmatched base form", response => { response.data.vocabulary_occurrences![1]!.conjugations[0]!.base_form = "見る"; }],
    ["absolute rather than local conjugation offset", response => { response.data.vocabulary_occurrences![1]!.conjugations[0]!.span.start = 4; }],
    ["invalid explanation segment", response => { response.data.vocabulary_occurrences![1]!.conjugations[0]!.segments[0]!.span.end = 1; }],
    ["mixed conjugation datasets", response => { response.data.vocabulary_occurrences![2]!.conjugations[0]!.dataset_version = "other"; }],
    ["vocabulary unavailable with occurrences", response => { response.data.capabilities.vocabulary = "unavailable"; }],
    ["legacy contract with occurrence extension", response => { response.meta.contract_version = "2"; }],
    ["null occurrences", response => { (response.data as unknown as Record<string, unknown>).vocabulary_occurrences = null; }],
    ["invalid truncated flag", response => { (response.data as unknown as Record<string, unknown>).vocabulary_truncated = "yes"; }],
    ["truncated without occurrences", response => { response.data.vocabulary_truncated = true; delete response.data.vocabulary_occurrences; }],
  ];
  it.each(corruptions)("rejects %s", (_, mutate) => {
    const response = sentenceFixture(); mutate(response);
    expect(() => parseAnalysisResponse(response, interactionId)).toThrow(AnalysisFailure);
  });

  it("rejects a surface that splits a supplementary Unicode character", () => {
    const response = sentenceFixture();
    response.data.normalized_text = "𠮷";
    response.data.vocabulary_occurrences = [{ surface: "\ud840", span: { start: 0, end: 1, unit: "utf16" }, matches: [], conjugations: [] }];
    expect(() => parseAnalysisResponse(response, interactionId)).toThrow(AnalysisFailure);
  });
});
