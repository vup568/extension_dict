/**
 * Language-agnostic interface every language implementation must satisfy.
 * The content script and UI layers depend only on this interface and the
 * shared message types — never on Japanese specifics directly — so other
 * languages can be plugged in later without touching those layers.
 */
import type { DictionaryEntry, TokenInfo } from '../../shared/types'

export interface LookupResolution {
  readonly matches: readonly DictionaryEntry[]
  /** Present when the input splits into 2+ tokens; null otherwise. */
  readonly tokens: readonly TokenInfo[] | null
  /** False when the tokenizer failed to initialize (exact match only). */
  readonly deinflectionAvailable: boolean
}

export interface LanguagePack {
  /** Identifier such as "ja". */
  readonly id: string

  /** True if the text contains characters this language pack can handle. */
  containsRelevantText(text: string): boolean

  /**
   * Resolve a selection to dictionary entries:
   * exact match → deinflected base forms → longest-prefix matching.
   */
  resolve(text: string): Promise<LookupResolution>
}
