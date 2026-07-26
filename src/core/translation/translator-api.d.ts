/**
 * Minimal ambient types for Chrome's built-in Translator API (stable since
 * Chrome 138) — not yet part of TypeScript's lib.dom as of TS 5.9.
 * https://developer.chrome.com/docs/ai/translator-api
 *
 * This file is a global script declaration (no imports/exports) so the
 * `Translator` global is visible project-wide; absence at runtime is
 * detected with `typeof Translator === 'undefined'`.
 */

type TranslatorAvailabilityState = 'unavailable' | 'downloadable' | 'downloading' | 'available'

interface TranslatorLanguageOptions {
  sourceLanguage: string
  targetLanguage: string
}

interface TranslatorCreateOptions extends TranslatorLanguageOptions {
  /** Receives an EventTarget emitting 'downloadprogress' events. */
  monitor?(monitor: EventTarget): void
  signal?: AbortSignal
}

interface TranslatorDownloadProgressEvent extends Event {
  /** 0–1 fraction of the language-pack download. */
  readonly loaded: number
}

interface TranslatorInstance {
  translate(input: string): Promise<string>
  destroy(): void
  readonly sourceLanguage: string
  readonly targetLanguage: string
}

declare const Translator: {
  availability(options: TranslatorLanguageOptions): Promise<TranslatorAvailabilityState>
  create(options: TranslatorCreateOptions): Promise<TranslatorInstance>
}
