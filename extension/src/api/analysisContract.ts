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
export interface ConjugationSpan { start: number; end: number; unit: "utf16" }
export interface ConjugationExplanation {
  surface: string; base_form: string; explanation: string; language_tag: "vi"; span: ConjugationSpan;
  rule_id: string; rule_version: number; dataset_version: string;
  segments: { surface: string; explanation: string; span: ConjugationSpan }[];
}
export interface AnalysisResponse {
  data: { normalized_text: string; tokens: unknown[]; dictionary_matches: EntryMatch[]; kanji: KanjiDetail[]; conjugations: ConjugationExplanation[];
    grammar_occurrences: GrammarOccurrence[]; capabilities: Record<"vocabulary" | "kanji" | "grammar" | "morphology" | "conjugation", CapabilityStatus> };
  meta: { request_id: string; interaction_id: string; contract_version: "1" | "2" | "3" };
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
function utf16Boundary(text: string, position: number): boolean {
  const previous = text.charCodeAt(position - 1), next = text.charCodeAt(position);
  return !(previous >= 0xd800 && previous <= 0xdbff && next >= 0xdc00 && next <= 0xdfff);
}
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
  if (!["1", "2", "3"].includes(meta.contract_version as string) || meta.interaction_id !== expectedId) invalid();
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
  if (!Array.isArray(data.tokens) || data.tokens.length !== 0 || capabilities.morphology !== "unavailable") invalid();
  const identities = new Set<string>();
  let datasetVersion: string | undefined;
  array(data.conjugations, item => {
    const c = record(item);
    for (const key of ["surface", "base_form", "explanation", "rule_id", "dataset_version"]) {
      string(c[key]); if ((c[key] as string).trim().length === 0) invalid();
    }
    integer(c.rule_version); if (c.rule_version <= 0 || c.language_tag !== "vi") invalid();
    const identity = JSON.stringify([c.rule_id, c.rule_version, c.base_form]);
    if (identities.has(identity) || (datasetVersion !== undefined && datasetVersion !== c.dataset_version)) invalid();
    identities.add(identity); datasetVersion = c.dataset_version as string;
    const span = record(c.span); integer(span.start); integer(span.end);
    const occurrenceEnd = span.end;
    if (span.unit !== "utf16" || span.start !== 0 || span.end !== normalizedText.length || c.surface !== normalizedText) invalid();
    let position = span.start;
    array(c.segments, item => {
      const segment = record(item); string(segment.surface); string(segment.explanation);
      const bounds = record(segment.span); integer(bounds.start); integer(bounds.end);
      if (bounds.unit !== "utf16" || bounds.start !== position || bounds.end <= bounds.start || bounds.end > occurrenceEnd ||
        !utf16Boundary(normalizedText, bounds.start) || !utf16Boundary(normalizedText, bounds.end) ||
        normalizedText.slice(bounds.start, bounds.end) !== segment.surface || segment.explanation.trim().length === 0) invalid();
      position = bounds.end;
    });
    if (position !== span.end || capabilities.vocabulary !== "completed" ||
      !(data.dictionary_matches as EntryMatch[]).some(entry => entry.matchedWrittenForm === c.base_form || entry.matchedReading === c.base_form)) invalid();
    if (meta.contract_version === "3" && !(data.dictionary_matches as EntryMatch[]).some(entry =>
      entry.canonicalId.trim().length > 0 &&
      ((entry.matchedWrittenForm === c.base_form && entry.writtenForms.some(w => w.form === c.base_form)) ||
       (entry.matchedReading === c.base_form && entry.readings.some(r => r.readingText === c.base_form))))) invalid();
  });
  if (meta.contract_version !== "3" && (data.conjugations as unknown[]).length > 1) invalid();
  if (meta.contract_version === "1" && ((data.conjugations as unknown[]).length !== 0 || capabilities.conjugation !== "unavailable")) invalid();
  for (const [capability, field] of [["vocabulary", "dictionary_matches"], ["kanji", "kanji"], ["grammar", "grammar_occurrences"], ["conjugation", "conjugations"]]) {
    if (capabilities[capability!] !== "completed" && (data[field!] as unknown[]).length !== 0) invalid();
  }
  return value as AnalysisResponse;
}
