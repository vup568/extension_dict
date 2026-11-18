import { describe, expect, it } from "vitest";
import type { EntryMatch } from "../../src/api/analysisContract.ts";
import { dictionarySpeechOptions } from "../../src/ui/dictionarySpeechOptions.ts";

function entry(): EntryMatch {
  return {
    canonicalId: "dictionary:jmdict:1",
    matchedWrittenForm: null,
    matchedReading: "はし",
    writtenForms: ["橋", "箸"].map((form, index) => ({ id: index + 1, form, priority: null, isCommon: true, info: null })),
    readings: [
      { id: 11, readingText: "はし", priority: null, isCommon: true, info: null, restrictedToForms: ["橋", "箸"] },
      { id: 12, readingText: "ばし", priority: null, isCommon: false, info: null, restrictedToForms: ["橋"] },
    ],
    senses: [{ senseKey: "1", partOfSpeech: ["n"], position: 1, glosses: [{ languageTag: "en", glossText: "bridge" }],
      restrictions: [{ writtenFormId: 1, readingId: 11 }, { writtenFormId: 2, readingId: 12 }] }],
  };
}

describe("dictionary speech source options", () => {
  it("keeps joint form and reading applicability instead of recombining independent IDs", () => {
    expect(dictionarySpeechOptions(entry())).toEqual([{
      label: "橋 · はし",
      source: { kind: "dictionary", canonical_id: "dictionary:jmdict:1", reading_id: 11, written_form_id: 1 },
    }]);
  });

  it("offers each applicable reading for an exact written form", () => {
    const match = entry();
    match.matchedReading = null;
    match.matchedWrittenForm = "橋";
    match.senses[0]!.restrictions = [];
    expect(dictionarySpeechOptions(match).map(option => [option.source.reading_id, option.source.written_form_id])).toEqual([
      [11, 1], [12, 1],
    ]);
  });

  it("omits the written form for a kana-only reading", () => {
    const match = entry();
    match.readings[0]!.info = ["reading:no-kanji"];
    match.senses[0]!.restrictions = [];
    expect(dictionarySpeechOptions(match)).toEqual([{
      label: "はし", source: { kind: "dictionary", canonical_id: "dictionary:jmdict:1", reading_id: 11 },
    }]);
  });
});
