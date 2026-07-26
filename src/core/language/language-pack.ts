/**
 * Language-agnostic interface every language implementation must satisfy.
 * The content script and UI layers depend only on this interface and the
 * shared message types — never on Japanese specifics directly — so other
 * languages can be plugged in later without touching those layers.
 *
 * Filled out in Phase 5 (tokenize / deinflect / lookup); kept minimal until
 * the dictionary types exist.
 */
export interface LanguagePack {
  /** Identifier such as "ja". */
  readonly id: string

  /** True if the text contains characters this language pack can handle. */
  containsRelevantText(text: string): boolean
}
