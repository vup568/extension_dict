import { describe, expect, it } from "vitest";
import { parseAnalysisResponse, type AnalysisResponse } from "../../src/api/analysisContract.ts";
import { conjugationFixture, v3ConjugationFixture, interactionId } from "./analysisFixtures.ts";

describe("F-09 opt-in v2 contract", () => {
  it("accepts complete Vietnamese occurrence linked to the found base entry", () => {
    const value = conjugationFixture(); expect(parseAnalysisResponse(value, interactionId)).toBe(value);
  });
  const corruptions: Array<[string, (v: AnalysisResponse) => void]> = [
    ["old client", v => { v.meta.contract_version = "1"; }],
    ["missing base entry", v => { v.data.dictionary_matches = []; }],
    ["false status", v => { v.data.capabilities.conjugation = "unavailable"; }],
    ["wrong observed surface", v => { v.data.conjugations[0]!.surface = "wrong"; }],
    ["wrong base", v => { v.data.conjugations[0]!.base_form = "other"; }],
    ["overlap", v => { v.data.conjugations[0]!.segments[1]!.span.start = 1; }],
    ["uncovered suffix", v => { v.data.conjugations[0]!.segments.pop(); }],
    ["out of bounds", v => { v.data.conjugations[0]!.segments[2]!.span.end = 6; }],
    ["non-Vietnamese", v => { (v.data.conjugations[0]! as unknown as Record<string, unknown>).language_tag = "en"; }],
    ["blank content", v => { v.data.conjugations[0]!.segments[0]!.explanation = " "; }],
    ["non-versioned rule", v => { v.data.conjugations[0]!.rule_version = 0; }],
    ["non-versioned dataset", v => { v.data.conjugations[0]!.dataset_version = ""; }],
  ];
  it.each(corruptions)("rejects %s", (_, mutate) => {
    const value = conjugationFixture(); mutate(value); expect(() => parseAnalysisResponse(value, interactionId)).toThrow();
  });
});

describe("F-09 v3 approved alternatives", () => {
  it.each([["食べられる", 3], ["食べさせられる", 2], ["学生です", 1], ["静かです", 1], ["𠮷です", 1]] as const)("accepts %s with %i distinct meanings", (text, count) => {
    const value = v3ConjugationFixture(interactionId, text);
    expect(parseAnalysisResponse(value, interactionId).data.conjugations).toHaveLength(count);
  });
  const corruptions: Array<[string, (v: AnalysisResponse) => void]> = [
    ["v2 cannot carry alternatives", v => { v.meta.contract_version = "2"; }],
    ["duplicate identity", v => { v.data.conjugations.push(structuredClone(v.data.conjugations[0]!)); }],
    ["mixed pinned datasets", v => { v.data.conjugations[1]!.dataset_version = "other"; }],
    ["unverified root", v => { v.data.conjugations[1]!.base_form = "見える"; }],
    ["blank canonical identity", v => { v.data.dictionary_matches[0]!.canonicalId = " "; }],
    ["fabricated matched form", v => { v.data.dictionary_matches[0]!.writtenForms = []; }],
    ["blank Vietnamese", v => { v.data.conjugations[1]!.explanation = " "; }],
    ["wrong span", v => { v.data.conjugations[1]!.span.start = 1; }],
    ["failed capability", v => { v.data.capabilities.conjugation = "failed"; }],
  ];
  it.each(corruptions)("rejects %s", (_, change) => {
    const value = v3ConjugationFixture(); change(value); expect(() => parseAnalysisResponse(value, interactionId)).toThrow();
  });
  it("rejects segmentation inside a supplementary character", () => {
    const value = v3ConjugationFixture(interactionId, "𠮷です");
    const c = value.data.conjugations[0]!;
    c.segments = [{ surface: c.surface.slice(0, 1), explanation: "Phần đầu.", span: { start: 0, end: 1, unit: "utf16" } },
      { surface: c.surface.slice(1), explanation: "Phần sau.", span: { start: 1, end: 4, unit: "utf16" } }];
    expect(() => parseAnalysisResponse(value, interactionId)).toThrow();
  });
});
