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
