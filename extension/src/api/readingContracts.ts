import { classifyJapaneseText } from "../boundary/classifyJapaneseText.ts";
import { AnalysisFailure, parseAnalysisResponse, type AnalysisResponse, type ConjugationSpan } from "./analysisContract.ts";

export const readingUuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/iu;
export const speechWireLimit = 4 * 1024 * 1024 + 4096;
const wavLimit = 3 * 1024 * 1024;
const loneSurrogate = /[\uD800-\uDFFF]/u;

export interface ScanInput { text: string; offset: number }
export interface ScanResponse { analysis: AnalysisResponse; targetSpan: ConjugationSpan | null }
export type SpeechSource = { kind: "selection"; text: string }
  | { kind: "dictionary"; canonical_id: string; reading_id: number; written_form_id?: number };
export interface SpeechResponse { audio_base64: string; mime_type: "audio/wav"; language_tag: "ja"; source: "ai" }

export function isScanInput(input: Record<string, unknown>): input is Record<string, unknown> & ScanInput {
  const { text, offset } = input;
  if (typeof text !== "string" || loneSurrogate.test(text) || text.length > 64 || text.trim() !== text ||
      typeof offset !== "number" || !Number.isInteger(offset) || offset < 0 || offset >= text.length) return false;
  const point = text.codePointAt(offset);
  const character = classifyJapaneseText(String.fromCodePoint(point!));
  return character.status === "ok" && character.containsJapaneseText;
}

function isId(value: unknown): value is number {
  return typeof value === "number" && Number.isSafeInteger(value) && value > 0;
}

export function isSpeechSource(source: unknown): source is SpeechSource {
  if (source === null || typeof source !== "object" || Array.isArray(source)) return false;
  const input = source as Record<string, unknown>;
  const keys = Object.keys(input);
  if (input.kind === "selection") {
    return keys.length === 2 && typeof input.text === "string" && !loneSurrogate.test(input.text) &&
      input.text.length > 0 && input.text.length <= 1000 && input.text.trim() === input.text;
  }
  return input.kind === "dictionary" && keys.every(key => ["kind", "canonical_id", "reading_id", "written_form_id"].includes(key)) &&
    typeof input.canonical_id === "string" && /^dictionary:[^\s]{1,244}$/u.test(input.canonical_id) && isId(input.reading_id) &&
    (input.written_form_id === undefined || isId(input.written_form_id));
}

function invalid(): never { throw new AnalysisFailure("INVALID_RESPONSE", true); }

function record(value: unknown): Record<string, unknown> {
  if (value === null || typeof value !== "object" || Array.isArray(value)) return invalid();
  return value as Record<string, unknown>;
}

function envelope(value: unknown, interactionId: string) {
  const { data, meta: rawMeta } = record(value);
  const meta = record(rawMeta);
  if (meta.contract_version !== "1" || meta.interaction_id !== interactionId || !readingUuid.test(interactionId) ||
      typeof meta.request_id !== "string" || !readingUuid.test(meta.request_id)) invalid();
  return { data: record(data), meta };
}

export function parseScanResponse(value: unknown, interactionId: string, input: ScanInput): ScanResponse {
  const { data, meta } = envelope(value, interactionId);
  const analysis = parseAnalysisResponse({ data: data.analysis, meta: { ...meta, contract_version: "3" } }, interactionId);
  if (analysis.data.normalized_text !== input.text) invalid();
  const occurrence = analysis.data.vocabulary_occurrences?.find(item => {
    if (data.target_span === null) return false;
    const span = record(data.target_span);
    return span.unit === "utf16" && span.start === item.span.start && span.end === item.span.end &&
      item.span.start <= input.offset && input.offset < item.span.end;
  });
  if (data.target_span !== null && occurrence === undefined) invalid();
  return { analysis, targetSpan: occurrence?.span ?? null };
}

export function decodeSpeechWav(base64: string): Uint8Array<ArrayBuffer> {
  if (base64.length > wavLimit / 3 * 4 || base64.length % 4 !== 0 || !/^[A-Za-z0-9+/]+={0,2}$/u.test(base64)) invalid();
  const binary = atob(base64);
  if (binary.length < 44 || binary.length > wavLimit) invalid();
  const bytes = Uint8Array.from(binary, character => character.charCodeAt(0));
  const view = new DataView(bytes.buffer);
  if (binary.slice(0, 4) !== "RIFF" || view.getUint32(4, true) + 8 !== bytes.length || binary.slice(8, 12) !== "WAVE") invalid();
  let sampleRate = 0;
  let samples = 0;
  for (let offset = 12; offset < bytes.length;) {
    if (offset + 8 > bytes.length) invalid();
    const name = binary.slice(offset, offset + 4);
    const size = view.getUint32(offset + 4, true);
    const start = offset + 8;
    offset = start + size + (size % 2);
    if (offset > bytes.length) invalid();
    if (name === "fmt ") {
      if (sampleRate !== 0 || size < 16 || view.getUint16(start, true) !== 1 || view.getUint16(start + 2, true) !== 1 ||
          view.getUint16(start + 12, true) !== 2 || view.getUint16(start + 14, true) !== 16) invalid();
      sampleRate = view.getUint32(start + 4, true);
      if (sampleRate < 8000 || sampleRate > 48000 || view.getUint32(start + 8, true) !== sampleRate * 2) invalid();
    } else if (name === "data") {
      if (sampleRate === 0 || samples !== 0 || size === 0 || size % 2 !== 0) invalid();
      samples = size / 2;
    }
  }
  if (samples === 0 || samples > sampleRate * 60) invalid();
  return bytes;
}

export function parseSpeechResponse(value: unknown, interactionId: string): SpeechResponse {
  const { data } = envelope(value, interactionId);
  if (typeof data.audio_base64 !== "string" || data.mime_type !== "audio/wav" || data.language_tag !== "ja" || data.source !== "ai") invalid();
  decodeSpeechWav(data.audio_base64);
  return { audio_base64: data.audio_base64, mime_type: "audio/wav", language_tag: "ja", source: "ai" };
}
