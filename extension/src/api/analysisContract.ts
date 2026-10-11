export type CapabilityStatus = "completed" | "failed" | "unavailable";
export interface WrittenForm { id: number; form: string; priority: number | null; isCommon: boolean; info: string[] | null }
export interface Reading { id: number; readingText: string; priority: number | null; isCommon: boolean; info: string[] | null; restrictedToForms: string[] | null }
export interface Gloss { languageTag: string; glossText: string }
export interface SenseRestriction { writtenFormId: number | null; readingId: number | null }
export interface Sense { senseKey: string; partOfSpeech: string[]; position: number; glosses: Gloss[]; restrictions: SenseRestriction[] }
export interface EntryMatch { canonicalId: string; matchedWrittenForm: string | null; matchedReading: string | null; writtenForms: WrittenForm[]; readings: Reading[]; senses: Sense[] }
export interface KanjiDetail {
  canonicalId: string; character: string; strokeCount: number | null; grade: number | null; jlptLevel: number | null;
  jlptProvenance: string | null; frequency: number | null; unicodeCodepoint: string; radicalNumber: number | null; hanViet: string | null;
  onReadings: string[]; kunReadings: string[]; meaningsVi: string[]; meaningsEn: string[]; nanoriReadings: string[];
}
export interface GrammarOccurrence { grammar_id: string; pattern: string; jlpt_level: string | null; meaning_vi: string | null; matched_text: string; span: { start: number; end: number } }
export interface AnalysisResponse {
  data: { normalized_text: string; tokens: unknown[]; dictionary_matches: EntryMatch[]; kanji: KanjiDetail[]; conjugations: unknown[];
    grammar_occurrences: GrammarOccurrence[]; capabilities: Record<"vocabulary" | "kanji" | "grammar" | "morphology" | "conjugation", CapabilityStatus> };
  meta: { request_id: string; interaction_id: string; contract_version: "1" };
}

const failureMessages: Record<string, string> = {
  TIMEOUT: "Phân tích quá thời gian chờ.", NETWORK_ERROR: "Không thể kết nối dịch vụ phân tích.",
  TRANSPORT_UNAVAILABLE: "Không thể kết nối dịch vụ phân tích.", SERVICE_UNAVAILABLE: "Dịch vụ phân tích chưa khả dụng.",
  VALIDATION_FAILED: "Nội dung phân tích không hợp lệ.", INVALID_RESPONSE: "Dịch vụ trả về kết quả không hợp lệ.",
  HTTP_ERROR: "Không thể hoàn thành phân tích.", ABORTED: "Yêu cầu đã được hủy.",
  INTERNAL_ERROR: "Đã xảy ra lỗi nội bộ hệ thống.", RATE_LIMITED: "Dịch vụ đang nhận quá nhiều yêu cầu.",
};
export function safeFailureCode(code: string): string {
  return Object.hasOwn(failureMessages, code) ? code : "HTTP_ERROR";
}
export class AnalysisFailure extends Error {
  public readonly code: string;
  constructor(code: string, public readonly retryable: boolean) {
    const safeCode = safeFailureCode(code);
    super(failureMessages[safeCode]);
    this.code = safeCode;
    this.name = "AnalysisFailure";
  }
}
function invalid(): never { throw new AnalysisFailure("INVALID_RESPONSE", true); }
function record(value: unknown): Record<string, unknown> {
  if (value === null || typeof value !== "object" || Array.isArray(value)) return invalid();
  return value as Record<string, unknown>;
}
function string(value: unknown): asserts value is string { if (typeof value !== "string") invalid(); }
function nullableString(value: unknown): void { if (value !== null) string(value); }
function integer(value: unknown): asserts value is number { if (!Number.isSafeInteger(value)) invalid(); }
function nullableInteger(value: unknown): void { if (value !== null) integer(value); }
function array(value: unknown, check: (item: unknown) => void): asserts value is unknown[] {
  if (!Array.isArray(value)) invalid();
  value.forEach(check);
}
function strings(value: unknown): void { array(value, string); }
function nullableStrings(value: unknown): void { if (value !== null) strings(value); }
function written(value: unknown): void {
  const v = record(value); integer(v.id); string(v.form); nullableInteger(v.priority); nullableStrings(v.info);
  if (typeof v.isCommon !== "boolean") invalid();
}
function reading(value: unknown): void {
  const v = record(value); integer(v.id); string(v.readingText); nullableInteger(v.priority); nullableStrings(v.info); nullableStrings(v.restrictedToForms);
  if (typeof v.isCommon !== "boolean") invalid();
}
function sense(value: unknown): void {
  const v = record(value); string(v.senseKey); strings(v.partOfSpeech); integer(v.position);
  array(v.glosses, item => { const g = record(item); string(g.languageTag); string(g.glossText); });
  array(v.restrictions, item => { const r = record(item); nullableInteger(r.writtenFormId); nullableInteger(r.readingId); });
}
function entry(value: unknown): void {
  const v = record(value); string(v.canonicalId); nullableString(v.matchedWrittenForm); nullableString(v.matchedReading);
  array(v.writtenForms, written); array(v.readings, reading); array(v.senses, sense);
}
function kanji(value: unknown): void {
  const v = record(value);
  for (const key of ["canonicalId", "character", "unicodeCodepoint"]) string(v[key]);
  for (const key of ["jlptProvenance", "hanViet"]) nullableString(v[key]);
  for (const key of ["strokeCount", "grade", "jlptLevel", "frequency", "radicalNumber"]) nullableInteger(v[key]);
  for (const key of ["onReadings", "kunReadings", "meaningsVi", "meaningsEn", "nanoriReadings"]) strings(v[key]);
}
/** EARS[Ubiquitous]: Untrusted results SHALL preserve contract and current interaction identity. */
export function parseAnalysisResponse(value: unknown, expectedId: string): AnalysisResponse {
  const v = record(value), data = record(v.data), meta = record(v.meta), capabilities = record(data.capabilities);
  if (meta.contract_version !== "1" || meta.interaction_id !== expectedId) invalid();
  string(meta.request_id); if (meta.request_id.length === 0) invalid();
  string(data.normalized_text);
  const normalizedText = data.normalized_text;
  array(data.dictionary_matches, entry); array(data.kanji, kanji);
  array(data.grammar_occurrences, item => {
    const g = record(item); string(g.grammar_id); string(g.pattern); nullableString(g.jlpt_level); nullableString(g.meaning_vi); string(g.matched_text);
    const span = record(g.span); integer(span.start); integer(span.end);
    if (span.start < 0 || span.end <= span.start || span.end > normalizedText.length) invalid();
  });
  for (const key of ["vocabulary", "kanji", "grammar", "morphology", "conjugation"]) {
    if (!["completed", "failed", "unavailable"].includes(capabilities[key] as string)) invalid();
  }
  // Version 1 currently has no normalized morphology/conjugation schema; never render opaque future data.
  if (!Array.isArray(data.tokens) || data.tokens.length !== 0 || !Array.isArray(data.conjugations) || data.conjugations.length !== 0) invalid();
  if (capabilities.morphology !== "unavailable" || capabilities.conjugation !== "unavailable") invalid();
  for (const [capability, field] of [["vocabulary", "dictionary_matches"], ["kanji", "kanji"], ["grammar", "grammar_occurrences"]]) {
    if (capabilities[capability!] !== "completed" && (data[field!] as unknown[]).length !== 0) invalid();
  }
  return value as AnalysisResponse;
}
