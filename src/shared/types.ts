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
  readonly strokes: number | null
  /** Japanese school grade (1–6; 8 = other jōyō; 9/10 = name kanji). */
  readonly grade: number | null
  /** JLPT level, modern N-scale (5,4,2,1 — converted from the old scale). */
  readonly jlpt: number | null
  /** Newspaper frequency rank 1–2501. */
  readonly freq: number | null
}
