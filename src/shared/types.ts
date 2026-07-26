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

/** One token of a tokenized selection, for the clickable token list. */
export interface TokenInfo {
  /** The text exactly as it appears in the selection. */
  readonly surface: string
  /** Dictionary form to look up when clicked (deinflected when possible). */
  readonly lookupTerm: string
}

export interface DictionaryEntry {
  readonly id: string
  /** Headword: the kanji form when one exists, otherwise the kana form. */
  readonly expression: string
  /** Kana reading of the expression. */
  readonly reading: string
  readonly senses: readonly WordSense[]
}
