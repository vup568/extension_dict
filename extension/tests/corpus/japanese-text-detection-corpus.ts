export type CorpusInput =
  | { readonly kind: "literal"; readonly value: string }
  | { readonly kind: "code-points"; readonly values: readonly number[] }
  | { readonly kind: "code-units"; readonly values: readonly number[] };

export type CorpusExpected =
  | boolean
  | "invalid-input"
  | "unexpected-failure";

export interface JapaneseTextCorpusCase {
  readonly id: string;
  readonly acceptanceCriterion: string;
  readonly input: CorpusInput;
  readonly expected: CorpusExpected;
  readonly policyVersion: "17.0.0";
  readonly sourceNote: string;
  readonly story: "US1" | "US2";
}

export function materializeCorpusInput(input: CorpusInput): string {
  switch (input.kind) {
    case "literal":
      return input.value;
    case "code-points":
      return String.fromCodePoint(...input.values);
    case "code-units":
      return String.fromCharCode(...input.values);
  }
}

type Story = JapaneseTextCorpusCase["story"];

function literalCase(
  id: string,
  acceptanceCriterion: string,
  value: string,
  expected: boolean,
  story: Story,
  sourceNote: string,
): JapaneseTextCorpusCase {
  return {
    id,
    acceptanceCriterion,
    input: { kind: "literal", value },
    expected,
    policyVersion: "17.0.0",
    sourceNote,
    story,
  };
}

function codePointCase(
  id: string,
  acceptanceCriterion: string,
  values: readonly number[],
  expected: boolean,
  story: Story,
  sourceNote: string,
): JapaneseTextCorpusCase {
  return {
    id,
    acceptanceCriterion,
    input: { kind: "code-points", values },
    expected,
    policyVersion: "17.0.0",
    sourceNote,
    story,
  };
}

function codeUnitCase(
  id: string,
  acceptanceCriterion: string,
  values: readonly number[],
  expected: boolean,
  story: Story,
  sourceNote: string,
): JapaneseTextCorpusCase {
  return {
    id,
    acceptanceCriterion,
    input: { kind: "code-units", values },
    expected,
    policyVersion: "17.0.0",
    sourceNote,
    story,
  };
}

export const japaneseTextDetectionCorpus: JapaneseTextCorpusCase[] = [
  literalCase("F01-AC-001-a", "F01-AC-001", "日", true, "US1", "Single Kanji"),
  literalCase("F01-AC-001-b", "F01-AC-001", "日本語", true, "US1", "Kanji word"),
  literalCase("F01-AC-001-c", "F01-AC-001", "一", true, "US1", "Single ideograph"),
  literalCase("F01-AC-002-a", "F01-AC-002", "あ", true, "US1", "Single Hiragana"),
  literalCase("F01-AC-002-b", "F01-AC-002", "ひらがな", true, "US1", "Hiragana word"),
  literalCase("F01-AC-002-c", "F01-AC-002", "ア", true, "US1", "Single Katakana"),
  literalCase("F01-AC-002-d", "F01-AC-002", "カタカナ", true, "US1", "Katakana word"),
  codePointCase("F01-AC-003-a", "F01-AC-003", [0x20b9f], true, "US1", "Standalone supplementary Han U+20B9F"),
  literalCase("F01-AC-003-b", "F01-AC-003", "𠮟る", true, "US1", "Supplementary Han followed by Kana"),
  codePointCase("F01-AC-003-c", "F01-AC-003", [0x20000], true, "US1", "First CJK Extension B code point"),
  literalCase("F01-AC-004-a", "F01-AC-004", "ｶ", true, "US1", "Half-width Katakana"),
  literalCase("F01-AC-004-b", "F01-AC-004", "ｶﾀｶﾅ", true, "US1", "Half-width Katakana word"),
  literalCase("F01-AC-004-c", "F01-AC-004", "ｶﾞ", true, "US1", "Half-width base plus voiced mark"),
  literalCase("F01-AC-004-d", "F01-AC-004", "ㇰ", true, "US1", "Katakana phonetic extension"),
  codePointCase("F01-AC-004-e", "F01-AC-004", [0x1b000], true, "US1", "Supplementary Kana"),
  literalCase("F01-AC-005-a", "F01-AC-005", "が", true, "US1", "Composed Hiragana"),
  codePointCase("F01-AC-005-b", "F01-AC-005", [0x304b, 0x3099], true, "US1", "Decomposed Hiragana with combining mark"),
  codePointCase("F01-AC-006-a", "F01-AC-006", [0xf900], true, "US1", "Compatibility ideograph start"),
  codePointCase("F01-AC-006-b", "F01-AC-006", [0xfa10], true, "US1", "Compatibility ideograph"),
  codePointCase("F01-AC-006-c", "F01-AC-006", [0x2f800], true, "US1", "Supplementary compatibility ideograph"),
  codePointCase("F01-AC-006-d", "F01-AC-006", [0x3007], true, "US1", "Explicit ideographic zero exception"),
  literalCase("F01-AC-007-a", "F01-AC-007", "Hello 日本語 123", true, "US1", "Mixed Latin and Japanese"),
  literalCase("F01-AC-007-b", "F01-AC-007", "日本語와 한국어", true, "US1", "Mixed Japanese and Korean"),
  literalCase("F01-AC-007-c", "F01-AC-007", "日😀", true, "US1", "Kanji before emoji"),
  literalCase("F01-AC-007-d", "F01-AC-007", "😀あ", true, "US1", "Kana after emoji"),
  literalCase("F01-AC-008-a", "F01-AC-008", "中文", true, "US1", "Han presence policy is not language identification"),
  literalCase("F01-AC-008-b", "F01-AC-008", "漢字", true, "US1", "Han word"),
  literalCase("F01-AC-008-c", "F01-AC-008", "スーパー", true, "US1", "Katakana with prolonged sound mark"),
  literalCase("F01-AC-008-d", "F01-AC-008", "時々", true, "US1", "Kanji with iteration mark"),
  codePointCase("F01-AC-017-a", "F01-AC-017", [0x845b, 0xe0100], true, "US1", "Kanji followed by variation selector"),
  codePointCase("F01-AC-017-b", "F01-AC-017", [0x200d, 0x65e5, 0xfe0f], true, "US1", "Format and selector do not hide Kanji"),

  literalCase("F01-AC-009-a", "F01-AC-009", "", false, "US2", "Empty string"),
  literalCase("F01-AC-009-b", "F01-AC-009", " ", false, "US2", "ASCII space"),
  literalCase("F01-AC-009-c", "F01-AC-009", "\t\n", false, "US2", "ASCII whitespace"),
  codePointCase("F01-AC-009-d", "F01-AC-009", [0x3000], false, "US2", "Ideographic space"),
  codePointCase("F01-AC-009-e", "F01-AC-009", [0x00a0], false, "US2", "No-break space"),
  literalCase("F01-AC-010-a", "F01-AC-010", "Hello", false, "US2", "Latin"),
  literalCase("F01-AC-010-b", "F01-AC-010", "Tiếng Việt", false, "US2", "Vietnamese"),
  literalCase("F01-AC-010-c", "F01-AC-010", "123", false, "US2", "ASCII digits"),
  literalCase("F01-AC-010-d", "F01-AC-010", "ＡＢＣ１２３", false, "US2", "Full-width Latin and digits"),
  literalCase("F01-AC-010-e", "F01-AC-010", "!?。、「」・", false, "US2", "Punctuation"),
  literalCase("F01-AC-011-a", "F01-AC-011", "안녕하세요", false, "US2", "Hangul text"),
  literalCase("F01-AC-011-b", "F01-AC-011", "한", false, "US2", "Single Hangul syllable"),
  codePointCase("F01-AC-011-c", "F01-AC-011", [0x1100], false, "US2", "Hangul Jamo"),
  codePointCase("F01-AC-011-d", "F01-AC-011", [0x3131], false, "US2", "Compatibility Jamo"),
  codePointCase("F01-AC-011-e", "F01-AC-011", [0xffa1], false, "US2", "Half-width Jamo"),
  literalCase("F01-AC-012-a", "F01-AC-012", "Привет", false, "US2", "Cyrillic"),
  literalCase("F01-AC-012-b", "F01-AC-012", "مرحبا", false, "US2", "Arabic"),
  literalCase("F01-AC-012-c", "F01-AC-012", "αβ", false, "US2", "Greek"),
  codePointCase("F01-AC-012-d", "F01-AC-012", [0x3105], false, "US2", "Bopomofo"),
  codePointCase("F01-AC-012-e", "F01-AC-012", [0xa000], false, "US2", "Yi"),
  codePointCase("F01-AC-012-f", "F01-AC-012", [0x17000], false, "US2", "Tangut"),
  literalCase("F01-AC-013-a", "F01-AC-013", "😀", false, "US2", "Emoji"),
  literalCase("F01-AC-013-b", "F01-AC-013", "🈂️", false, "US2", "Squared Katakana symbol emoji"),
  literalCase("F01-AC-013-c", "F01-AC-013", "㊗️", false, "US2", "Circled ideograph symbol emoji"),
  codePointCase("F01-AC-013-d", "F01-AC-013", [0x1d11e], false, "US2", "Music symbol"),
  codePointCase("F01-AC-013-e", "F01-AC-013", [0x1d400], false, "US2", "Math symbol"),
  codePointCase("F01-AC-013-f", "F01-AC-013", [0x1f200], false, "US2", "Square Hiragana symbol"),
  codePointCase("F01-AC-014-a", "F01-AC-014", [0x2f00], false, "US2", "Kangxi radical"),
  codePointCase("F01-AC-014-b", "F01-AC-014", [0x2e80], false, "US2", "CJK radical supplement"),
  codePointCase("F01-AC-014-c", "F01-AC-014", [0x2ff0], false, "US2", "Ideographic description character"),
  literalCase("F01-AC-014-d", "F01-AC-014", "㋐", false, "US2", "Circled Katakana"),
  literalCase("F01-AC-014-e", "F01-AC-014", "㍿", false, "US2", "Compatibility square symbol"),
  codePointCase("F01-AC-014-f", "F01-AC-014", [0x3006], false, "US2", "Ideographic closing mark"),
  literalCase("F01-AC-015-a", "F01-AC-015", "ー", false, "US2", "Prolonged sound mark alone"),
  literalCase("F01-AC-015-b", "F01-AC-015", "ｰ", false, "US2", "Half-width prolonged mark alone"),
  literalCase("F01-AC-015-c", "F01-AC-015", "々", false, "US2", "Iteration mark alone"),
  literalCase("F01-AC-015-d", "F01-AC-015", "ゝ", false, "US2", "Hiragana iteration mark alone"),
  literalCase("F01-AC-015-e", "F01-AC-015", "ヽ", false, "US2", "Katakana iteration mark alone"),
  codePointCase("F01-AC-015-f", "F01-AC-015", [0x3099], false, "US2", "Combining voiced mark"),
  codePointCase("F01-AC-015-g", "F01-AC-015", [0x309a], false, "US2", "Combining semi-voiced mark"),
  codePointCase("F01-AC-015-h", "F01-AC-015", [0x309b], false, "US2", "Voiced mark"),
  codePointCase("F01-AC-015-i", "F01-AC-015", [0x309c], false, "US2", "Semi-voiced mark"),
  codePointCase("F01-AC-015-j", "F01-AC-015", [0xff9e], false, "US2", "Half-width voiced mark"),
  codePointCase("F01-AC-015-k", "F01-AC-015", [0xff9f], false, "US2", "Half-width semi-voiced mark"),
  codePointCase("F01-AC-016-a", "F01-AC-016", [0xfe0f], false, "US2", "Variation selector"),
  codePointCase("F01-AC-016-b", "F01-AC-016", [0xe0100], false, "US2", "Supplementary variation selector"),
  codePointCase("F01-AC-016-c", "F01-AC-016", [0x200d], false, "US2", "Zero-width joiner"),
  codePointCase("F01-AC-016-d", "F01-AC-016", [0x0000], false, "US2", "Null control"),
  codePointCase("F01-AC-016-e", "F01-AC-016", [0xfffd], false, "US2", "Replacement character"),
  codePointCase("F01-AC-016-f", "F01-AC-016", [0xe000], false, "US2", "BMP private use"),
  codePointCase("F01-AC-016-g", "F01-AC-016", [0xf0000], false, "US2", "Supplementary private use"),
  codePointCase("F01-AC-016-h", "F01-AC-016", [0x10ffff], false, "US2", "Noncharacter"),
  codePointCase("F01-AC-018-a", "F01-AC-018", [0x3097], false, "US2", "Unassigned point adjacent to Hiragana"),
  codePointCase("F01-AC-018-b", "F01-AC-018", [0xfa6e], false, "US2", "Unassigned compatibility point"),
  codePointCase("F01-AC-018-c", "F01-AC-018", [0x2a6e0], false, "US2", "Unassigned point after Extension B"),
  codePointCase("F01-AC-019-a", "F01-AC-019", [0x00f0, 0x00a0, 0x00ae, 0x0178], false, "US2", "Literal migration mojibake; do not repair"),
  literalCase("F01-AC-020-a", "F01-AC-020", "&#x65E5;", false, "US2", "Literal HTML entity"),
  literalCase("F01-AC-020-b", "F01-AC-020", "\\u65E5", false, "US2", "Literal JavaScript escape text"),
  literalCase("F01-AC-021-a", "F01-AC-021", "a".repeat(100_000), false, "US2", "Long negative input"),
  literalCase("F01-AC-021-b", "F01-AC-021", `${"a".repeat(100_000)}𠮟`, true, "US2", "Trigger at end of long input"),
  literalCase("F01-AC-021-c", "F01-AC-021", `𠮟${"a".repeat(100_000)}`, true, "US2", "Trigger at start of long input"),
  literalCase("F01-AC-022-a", "F01-AC-022", "日", true, "US2", "Repeated-call sequence item 1"),
  literalCase("F01-AC-022-b", "F01-AC-022", "Hello", false, "US2", "Repeated-call sequence item 2"),
  literalCase("F01-AC-022-c", "F01-AC-022", "日", true, "US2", "Repeated-call sequence item 3"),
  literalCase("F01-AC-022-d", "F01-AC-022", "", false, "US2", "Repeated-call sequence item 4"),
  literalCase("F01-AC-022-e", "F01-AC-022", "𠮟", true, "US2", "Repeated-call sequence item 5"),
  literalCase("F01-AC-022-f", "F01-AC-022", "😀", false, "US2", "Repeated-call sequence item 6"),
  codeUnitCase("F01-AC-024-a", "F01-AC-024", [0xd800], false, "US2", "Lone high surrogate"),
  codeUnitCase("F01-AC-024-b", "F01-AC-024", [0xdc00], false, "US2", "Lone low surrogate"),
  codeUnitCase("F01-AC-024-c", "F01-AC-024", [0xd800, 0x0061, 0xdc00], false, "US2", "Separated surrogate units"),
  codeUnitCase("F01-AC-025-a", "F01-AC-025", [0xd800, 0x65e5], true, "US2", "High surrogate before Kanji"),
  codeUnitCase("F01-AC-025-b", "F01-AC-025", [0xd842, 0xdf9f, 0xdc00], true, "US2", "Supplementary Han before lone low surrogate"),
];
