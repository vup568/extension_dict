/**
 * Japanese character detection. Lives under core/language/japanese so no
 * Japanese assumption leaks into the content script or UI layers — they only
 * ever receive this as an opaque `(text) => boolean` predicate.
 *
 * Covered ranges:
 *   U+3005–3006  iteration marks 々 / 〆 (CJK Symbols block)
 *   U+3040–309F  hiragana
 *   U+30A0–30FF  katakana (incl. prolonged-sound mark ー and ヶ)
 *   U+3400–4DBF  CJK Unified Ideographs Extension A
 *   U+4E00–9FFF  CJK Unified Ideographs
 *   U+F900–FAFF  CJK Compatibility Ideographs
 *   U+FF66–FF9F  halfwidth katakana
 */
const JAPANESE_CHAR = /[々〆぀-ヿ㐀-䶿一-鿿豈-﫿ｦ-ﾟ]/

export function containsJapanese(text: string): boolean {
  return JAPANESE_CHAR.test(text)
}
