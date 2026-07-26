/**
 * Sentence-level machine translation behind an interface, so another engine
 * could replace Chrome's built-in Translator API later.
 *
 * DELIBERATE ARCHITECTURE EXCEPTION: unlike dictionary lookups (owned by the
 * service worker), translation must run in a WINDOW context — Chrome's
 * Translator API is not exposed in workers, so the content script calls this
 * module directly. The first create() for a language pair also requires
 * transient user activation (a real click), which only exists in the page.
 */
import type { TargetLang } from '../../shared/types'

export type TranslateAvailability = 'unavailable' | 'downloadable' | 'downloading' | 'available'

export interface SentenceTranslator {
  /** True when the runtime exposes a translation engine at all. */
  isSupported(): boolean
  availability(target: TargetLang): Promise<TranslateAvailability>
  /**
   * Translate Japanese text. onDownloadProgress fires with 0–100 while the
   * language pack downloads (first use per pair on this device only).
   */
  translate(text: string, target: TargetLang, onDownloadProgress?: (pct: number) => void): Promise<string>
}

const SOURCE_LANGUAGE = 'ja'

/** One live translator per target language, reused for the page's lifetime. */
const instances = new Map<TargetLang, TranslatorInstance>()

function translatorStatic(): typeof Translator | null {
  return typeof Translator === 'undefined' ? null : Translator
}

export const builtinSentenceTranslator: SentenceTranslator = {
  isSupported: () => translatorStatic() !== null,

  async availability(target) {
    const api = translatorStatic()
    if (api === null) return 'unavailable'
    try {
      return await api.availability({ sourceLanguage: SOURCE_LANGUAGE, targetLanguage: target })
    } catch {
      return 'unavailable'
    }
  },

  async translate(text, target, onDownloadProgress) {
    const api = translatorStatic()
    if (api === null) throw new Error('Translator API is not available in this browser')
    let instance = instances.get(target)
    if (instance === undefined) {
      try {
        instance = await api.create({
          sourceLanguage: SOURCE_LANGUAGE,
          targetLanguage: target,
          monitor(monitor) {
            monitor.addEventListener('downloadprogress', (event) => {
              const { loaded } = event as TranslatorDownloadProgressEvent
              onDownloadProgress?.(Math.round(loaded * 100))
            })
          },
        })
      } catch (error) {
        throw new Error(friendlyCreateError(error))
      }
      instances.set(target, instance)
    }
    return instance.translate(text)
  },
}

function friendlyCreateError(error: unknown): string {
  if (error instanceof DOMException && error.name === 'NotAllowedError') {
    // Either the host page blocks the API via Permissions-Policy, or the
    // language-pack download was attempted without a fresh user gesture.
    return 'Translation is blocked on this page — click Translate again, or try another site.'
  }
  return error instanceof Error ? error.message : String(error)
}
