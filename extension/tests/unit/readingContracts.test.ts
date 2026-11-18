import { describe, expect, it, vi } from "vitest";
import { decodeSpeechWav, isScanInput, isSpeechSource, parseScanResponse, parseSpeechResponse } from "../../src/api/readingContracts.ts";
import { createReadingBackgroundHandler, createRuntimeScan } from "../../src/platform/readingTransport.ts";
import { analysisFixture } from "./analysisFixtures.ts";

const id = "a1d3b846-c077-4b5a-8b97-1d4d583ab0ad";
const meta = { request_id: id, interaction_id: id, contract_version: "1" };
const span = { start: 2, end: 4, unit: "utf16" } as const;
const input = { text: "🐈日本猫", offset: 3 };

function scanFixture() {
  const { data: analysis } = analysisFixture(id);
  analysis.normalized_text = input.text;
  analysis.grammar_occurrences = [];
  analysis.dictionary_matches = [];
  analysis.vocabulary_occurrences = [{ surface: "日本", span, matches: analysisFixture(id).data.dictionary_matches, conjugations: [] }];
  return { data: { analysis, target_span: span }, meta };
}

function wav(channels = 1) {
  const bytes = new Uint8Array(46);
  const view = new DataView(bytes.buffer);
  const encoder = new TextEncoder();
  bytes.set(encoder.encode("RIFF"));
  view.setUint32(4, 38, true);
  bytes.set(encoder.encode("WAVEfmt "), 8);
  view.setUint32(16, 16, true);
  view.setUint16(20, 1, true);
  view.setUint16(22, channels, true);
  view.setUint32(24, 24000, true);
  view.setUint32(28, 48000, true);
  view.setUint16(32, 2, true);
  view.setUint16(34, 16, true);
  bytes.set(encoder.encode("data"), 36);
  view.setUint32(40, 2, true);
  return btoa(String.fromCharCode(...bytes));
}

describe("reading contract v1", () => {
  it("rejects surrogate halves, non-Japanese hit points and untrimmed windows", () => {
    expect(isScanInput(input)).toBe(true);
    expect(isScanInput({ text: "𠮷野", offset: 0 })).toBe(true);
    expect(isScanInput({ text: "𠮷野", offset: 1 })).toBe(false);
    for (const text of ["\uD800野", " 野", "野 ", "a野", "野".repeat(65)]) {
      expect(isScanInput({ text, offset: 0 })).toBe(false);
    }
  });

  it("verifies correlation, substring and a target containing the actual pointer", () => {
    const response = scanFixture();
    expect(parseScanResponse(response, id, input).targetSpan).toEqual(span);
    expect(() => parseScanResponse(response, id, { ...input, offset: 4 })).toThrow();
    expect(() => parseScanResponse(response, "other", input)).toThrow();
    const other = { ...response, data: { ...response.data, target_span: { ...span, end: 5 } } };
    expect(() => parseScanResponse(other, id, input)).toThrow();
    response.data.analysis.vocabulary_occurrences![0]!.surface = "日本語";
    expect(() => parseScanResponse(response, id, input)).toThrow();
  });

  it("accepts only the selected source and validated PCM16 mono WAV", () => {
    expect(isSpeechSource({ kind: "dictionary", canonical_id: "dictionary:jmdict:1", reading_id: 2 })).toBe(true);
    expect(isSpeechSource({ kind: "selection", text: "猫", reading_id: 2 })).toBe(false);
    expect(isSpeechSource({ kind: "selection", text: "\ud800猫" })).toBe(false);
    const data = { audio_base64: wav(), mime_type: "audio/wav", language_tag: "ja", source: "ai" };
    expect(decodeSpeechWav(wav()).byteLength).toBe(46);
    expect(parseSpeechResponse({ data, meta }, id)).toEqual(data);
    for (const audio of [wav(2), "====", btoa("raw PCM")]) {
      expect(() => parseSpeechResponse({ data: { ...data, audio_base64: audio }, meta }, id)).toThrow();
    }
    expect(() => parseSpeechResponse({ data: { ...data, language_tag: "en" }, meta }, id)).toThrow();
  });
});

describe("reading transport", () => {
  it("rejects untrusted senders and does not let other frames or namespaces cancel scan", async () => {
    let fetchSignal: AbortSignal | null | undefined;
    const fetcher = vi.fn<typeof fetch>((_, options) => {
      fetchSignal = options?.signal;
      return new Promise(() => {});
    });
    const handle = createReadingBackgroundHandler("owned", "http://127.0.0.1:5000", fetcher);
    const sender = { id: "owned", tab: { id: 5 }, frameId: 2 };
    const reply = vi.fn();
    const payload = { ...input, interaction_id: id, type: "jp-reading-scan-v1" };
    handle(payload, { ...sender, id: "untrusted" }, reply);
    expect(fetcher).not.toHaveBeenCalled();
    handle(payload, sender, reply);
    expect(fetcher).toHaveBeenCalledWith("http://127.0.0.1:5000/api/scan", expect.objectContaining({ credentials: "omit", redirect: "error" }));
    const cancel = { type: "jp-reading-scan-v1:cancel", interaction_id: id };
    handle(cancel, { ...sender, frameId: 3 }, reply);
    handle({ ...cancel, type: "jp-reading-speech-v1:cancel" }, sender, reply);
    expect(fetchSignal?.aborted).toBe(false);
    handle(cancel, sender, reply);
    expect(fetchSignal?.aborted).toBe(true);
  });

  it("settles when a runtime callback ignores the deadline", async () => {
    vi.useFakeTimers();
    try {
      const sendMessage = vi.fn();
      const result = createRuntimeScan({ sendMessage })(input, id, new AbortController().signal);
      const failed = expect(result).rejects.toMatchObject({ code: "TIMEOUT" });
      await vi.advanceTimersByTimeAsync(5000);
      await failed;
      expect(sendMessage).toHaveBeenLastCalledWith({ type: "jp-reading-scan-v1:cancel", interaction_id: id }, expect.any(Function));
    } finally { vi.useRealTimers(); }
  });
});
