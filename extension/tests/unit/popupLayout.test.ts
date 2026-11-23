import { describe, expect, it } from "vitest";
import { createElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";
import type { AnalysisResponse } from "../../src/api/analysisContract.ts";
import { PopupShell } from "../../src/ui/PopupShell.tsx";
import { partOfSpeechLabel } from "../../src/ui/partOfSpeechLabels.ts";
import { sentenceSegments } from "../../src/ui/sentenceSegments.ts";
import { analysisFixture, conjugationFixture, interactionId } from "./analysisFixtures.ts";

const render = (response: AnalysisResponse, extra: Record<string, unknown> = {}) => renderToStaticMarkup(createElement(PopupShell, {
  state: { phase: "result", text: response.data.normalized_text, interactionId, response }, onClose: () => {}, onRetry: () => {}, ...extra,
}));

function sentence(): AnalysisResponse {
  const response = conjugationFixture();
  const verb = response.data.dictionary_matches[0]!;
  const explanation = response.data.conjugations[0]!;
  response.meta.contract_version = "3";
  response.data.normalized_text = "猫を食べました";
  response.data.dictionary_matches = [];
  response.data.conjugations = [];
  response.data.capabilities.conjugation = "unavailable";
  const cat = structuredClone(verb);
  cat.canonicalId = "dictionary:jmdict:cat";
  cat.matchedWrittenForm = "猫";
  cat.writtenForms[0]!.form = "猫";
  response.data.kanji = [
    { ...response.data.kanji[0]!, canonicalId: "kanji:猫", character: "猫" },
    { ...response.data.kanji[0]!, canonicalId: "kanji:食", character: "食" },
  ];
  response.data.vocabulary_occurrences = [
    { surface: "猫", span: { start: 0, end: 1, unit: "utf16" }, matches: [cat], conjugations: [] },
    { surface: "食べました", span: { start: 2, end: 7, unit: "utf16" }, matches: [verb], conjugations: [explanation] },
  ];
  return response;
}

describe("sentence segments", () => {
  it("keeps plain text between words and splits runs at highlight edges", () => {
    const segments = sentenceSegments("猫を食べました", [{ start: 0, end: 1 }, { start: 2, end: 7 }], [{ start: 4, end: 7, className: "hl" }]);
    expect(segments?.map(segment => [segment.kind, segment.parts.map(part => `${part.text}|${part.className}`)])).toEqual([
      ["word", ["猫|"]], ["plain", ["を|"]], ["word", ["食べ|", "ました|hl"]],
    ]);
  });

  it("refuses overlapping or out-of-range words instead of drawing a wrong sentence", () => {
    expect(sentenceSegments("猫を", [{ start: 0, end: 2 }, { start: 1, end: 2 }], [])).toBeNull();
    expect(sentenceSegments("猫", [{ start: 0, end: 3 }], [])).toBeNull();
    expect(sentenceSegments("猫", [], [{ start: 0, end: 9, className: "hl" }])?.[0]?.parts).toEqual([{ text: "猫", className: "" }]);
  });
});

describe("part of speech labels", () => {
  it("translates both JMdict codes and expanded descriptions and keeps unknown values", () => {
    expect(partOfSpeechLabel("n")).toBe("Danh từ");
    expect(partOfSpeechLabel("noun (common) (futsuumeishi)")).toBe("Danh từ");
    expect(partOfSpeechLabel("Ichidan verb")).toBe("Động từ nhóm 2");
    expect(partOfSpeechLabel("Godan verb with 'mu' ending")).toBe("Động từ nhóm 1");
    expect(partOfSpeechLabel("v5r")).toBe("Động từ nhóm 1");
    expect(partOfSpeechLabel("dated term")).toBe("dated term");
  });
});

describe("tabbed popup layout", () => {
  it("renders the sentence as word buttons and opens on the vocabulary tab", () => {
    const html = render(sentence(), { translate: async () => { throw new Error("unused"); } });
    expect(html).toContain('role="group" aria-label="Từ trong vùng chọn"');
    expect(html).toContain('data-occurrence-choice="0:1"');
    expect(html).toContain('data-occurrence-choice="2:7"');
    expect(html).toMatch(/role="tab"[^>]*aria-selected="true"[^>]*><span>Từ vựng<\/span>/u);
    expect(html.match(/role="tab"/gu)).toHaveLength(4);
    expect(html).toContain("<span>Câu</span>");
    // Hidden panels stay in the document so their state survives a tab switch.
    expect(html.match(/role="tabpanel"/gu)).toHaveLength(4);
    expect(html.match(/role="tabpanel"[^>]*hidden=""/gu)).toHaveLength(3);
  });

  it("limits the Kanji tab to the word being read", () => {
    const html = render(sentence());
    expect(html).toContain('data-canonical-id="kanji:猫"');
    expect(html).not.toContain('data-canonical-id="kanji:食"');
    expect(html).toContain("Kanji trong <span lang=\"ja\">「猫」</span>");
  });

  it("omits the sentence tab for a scan and when no sentence action is available", () => {
    const response = sentence();
    expect(render(response).match(/role="tab"/gu)).toHaveLength(3);
    const scan = render(response, { isScan: true, targetSpan: response.data.vocabulary_occurrences![1]!.span, translate: async () => { throw new Error("unused"); } });
    expect(scan.match(/role="tab"/gu)).toHaveLength(3);
    expect(scan).toContain("Tra bằng con trỏ");
    expect(scan).toContain('<span class="jp-target">食べました</span>');
    expect(scan).toContain('data-canonical-id="kanji:食"');
    expect(scan).not.toContain('data-canonical-id="kanji:猫"');
  });

  it("keeps word speech closed behind a labelled toggle until the reader opens it", () => {
    const response = analysisFixture();
    response.data.dictionary_matches[0]!.canonicalId = "dictionary:jmdict:1";
    const sources: unknown[] = [];
    const html = render(response, { renderSpeech: (source: unknown) => { sources.push(source); return null; } });
    expect(html).toContain('aria-expanded="false" aria-label="Nghe cách đọc bằng giọng AI"');
    // Only the selection-speech section of the sentence tab asks for a panel; no word reading is prepared yet.
    expect(sources).toEqual([{ kind: "selection", text: "食事" }]);
  });

  it("labels parts of speech in Vietnamese and keeps the JMdict value as a tooltip", () => {
    const html = render(analysisFixture());
    expect(html).toContain('<span class="jp-tag" title="JMdict: n">Danh từ</span>');
  });

  it("keeps a failed analysis free of tabs and offers translation under the error", () => {
    const html = renderToStaticMarkup(createElement(PopupShell, {
      state: { phase: "error", text: "猫", interactionId, code: "NETWORK_ERROR", retryable: true },
      onClose: () => {}, onRetry: () => {}, translate: async () => { throw new Error("unused"); },
    }));
    expect(html).not.toContain('role="tab"');
    expect(html).toContain("Không thể kết nối dịch vụ phân tích.");
    expect(html).toContain("Dịch sang tiếng Anh");
    const scan = renderToStaticMarkup(createElement(PopupShell, {
      state: { phase: "error", text: "猫", interactionId, code: "NETWORK_ERROR", retryable: true },
      onClose: () => {}, onRetry: () => {}, isScan: true, targetSpan: null, translate: async () => { throw new Error("unused"); },
    }));
    expect(scan).not.toContain("Dịch sang tiếng Anh");
  });
});
