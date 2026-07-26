/**
 * Shared domain types used by both the popup UI and (from Phase 3 on) the
 * dictionary lookup results coming back from the service worker.
 */

export interface WordSense {
  /** Part-of-speech tags for this sense, e.g. ["n"] or ["v1", "vt"]. */
  readonly partsOfSpeech: readonly string[]
  /** English glosses for this sense. */
  readonly glosses: readonly string[]
}

/** Sentence-translation target languages offered in the UI. */
export type TargetLang = 'en' | 'vi'

export const DEFAULT_TARGET_LANG: TargetLang = 'vi'

/** Which engine produced a translation. */
export type TranslationEngine = 'cloud' | 'device'

/** Lifecycle of one sentence-translation attempt in the popup. */
export type TranslationState =
  /** 'downloading' is only used by the explicit offline-pack download flow. */
  | { readonly status: 'downloading'; readonly pct: number }
  | { readonly status: 'translating' }
  | {
      readonly status: 'done'
      readonly text: string
      readonly target: TargetLang
      readonly engine: TranslationEngine
    }
  | { readonly status: 'error'; readonly message: string }

/** One piece of a conjugated grammar unit, with a short explanation. */
export interface GrammarPart {
  readonly surface: string
  readonly description: string
}

/** One grammar pattern detected in a sentence (Ngữ pháp tab). */
export interface GrammarPatternMatch {
  /** Canonical form shown to the user, e.g. 〜くなる. */
  readonly display: string
  /** JLPT level of the pattern: 'N5' | 'N4' | … */
  readonly level: string
  /** Vietnamese explanation. */
  readonly description: string
  /** The text in the sentence that matched, e.g. 暑くなり. */
  readonly surface: string
}

/** A conjugated unit in the analyzed sentence with its part breakdown. */
export interface GrammarUnitBreakdown {
  readonly surface: string
  readonly parts: readonly GrammarPart[]
}

/** Result of analyzing one sentence for the Ngữ pháp tab. */
export interface GrammarAnalysis {
  readonly patterns: readonly GrammarPatternMatch[]
  readonly units: readonly GrammarUnitBreakdown[]
}

/** The sentence containing the selection, with the selection's bounds. */
export interface SentenceInfo {
  readonly sentence: string
  readonly selStart: number
  readonly selEnd: number
}

/** One token of a tokenized selection, for the clickable token list. */
export interface TokenInfo {
  /** The text exactly as it appears in the selection. */
  readonly surface: string
  /** Dictionary form to look up when clicked (deinflected when possible). */
  readonly lookupTerm: string
  /**
   * Present when this token is a grammar unit (verb/adjective stem plus
   * auxiliaries, e.g. なりました): the per-part breakdown to display.
   */
  readonly grammar: readonly GrammarPart[] | null
}

export interface DictionaryEntry {
  readonly id: string
  /** Headword: the kanji form when one exists, otherwise the kana form. */
  readonly expression: string
  /** Kana reading of the expression. */
  readonly reading: string
  readonly senses: readonly WordSense[]
}

/** A kanji's Kangxi radical (bộ thủ), resolved for display. */
export interface KanjiRadicalInfo {
  readonly glyph: string
  /** Hán Việt name of the radical, e.g. "Thủy". */
  readonly hanViet: string
  /** In-kanji variant form when it differs (氵, ⻌, 亻 …). */
  readonly variant: string | null
}

/** One character's KANJIDIC2 data, shown as a card in the Hán tự tab. */
export interface KanjiInfo {
  readonly literal: string
  /** Hán Việt (Sino-Vietnamese) readings, e.g. ["Học"]. */
  readonly hanViet: readonly string[]
  /** On readings (katakana). */
  readonly on: readonly string[]
  /** Kun readings (hiragana, may contain okurigana dots: まな.ぶ). */
  readonly kun: readonly string[]
  /** English meanings. */
  readonly meanings: readonly string[]
  /** Vietnamese meaning from the ja-vi dictionary, when it has one. */
  readonly viMeaning: string | null
  readonly strokes: number | null
  /** JLPT level, modern N-scale (5,4,2,1 — converted from the old scale). */
  readonly jlpt: number | null
  readonly radical: KanjiRadicalInfo | null
}
