export interface TextRange { readonly start: number; readonly end: number }
export interface Highlight extends TextRange { readonly className: string }
export interface SentencePart { readonly text: string; readonly className: string }
export interface SentenceSegment extends TextRange {
  readonly kind: "word" | "plain";
  /** Index into the word list for word segments. */
  readonly word: number;
  readonly parts: readonly SentencePart[];
}

function validRange(text: string, range: TextRange): boolean {
  return Number.isInteger(range.start) && Number.isInteger(range.end) && range.start >= 0 && range.start < range.end && range.end <= text.length;
}

function partsOf(text: string, range: TextRange, highlights: readonly Highlight[]): SentencePart[] {
  const cuts = new Set([range.start, range.end]);
  for (const highlight of highlights) {
    if (highlight.start > range.start && highlight.start < range.end) cuts.add(highlight.start);
    if (highlight.end > range.start && highlight.end < range.end) cuts.add(highlight.end);
  }
  const points = [...cuts].sort((a, b) => a - b);
  const parts: SentencePart[] = [];
  for (let i = 0; i < points.length - 1; i++) {
    const start = points[i]!;
    const end = points[i + 1]!;
    const className = highlights.filter(item => item.start <= start && item.end >= end).map(item => item.className).join(" ");
    parts.push({ text: text.slice(start, end), className });
  }
  return parts;
}

/**
 * Splits displayed text into word and plain runs, then splits each run at highlight edges.
 * Words must be ordered and disjoint (the analysis parser enforces this); anything else makes the
 * caller fall back to plain text instead of drawing a misleading sentence.
 */
export function sentenceSegments(text: string, words: readonly TextRange[], highlights: readonly Highlight[]): SentenceSegment[] | null {
  const usable = highlights.filter(item => validRange(text, item));
  const segments: SentenceSegment[] = [];
  let position = 0;
  for (const [index, word] of words.entries()) {
    if (!validRange(text, word) || word.start < position) return null;
    if (word.start > position) segments.push({ kind: "plain", word: -1, start: position, end: word.start, parts: partsOf(text, { start: position, end: word.start }, usable) });
    segments.push({ kind: "word", word: index, start: word.start, end: word.end, parts: partsOf(text, word, usable) });
    position = word.end;
  }
  if (position < text.length) segments.push({ kind: "plain", word: -1, start: position, end: text.length, parts: partsOf(text, { start: position, end: text.length }, usable) });
  return segments;
}
