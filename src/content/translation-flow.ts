/**
 * Hybrid sentence-translation orchestrator for the content script.
 *
 * Engine choice per call:
 *  1. Chrome's on-device translator, when its language pack is already on
 *     disk (private, offline, no network) — see sentence-translator.ts for
 *     why this must run in the content script.
 *  2. Otherwise the online endpoint via the service worker (instant, zero
 *     setup, but needs internet and sends the text to Google).
 * An explicit user click can download the on-device pack; from then on
 * engine 1 wins everywhere.
 */
import { builtinSentenceTranslator } from '../core/translation/sentence-translator'
import type { TranslateCloudRequest, TranslateCloudResponse } from '../shared/messages'
import type { TargetLang, TranslationEngine, TranslationState } from '../shared/types'

const CACHE_CAP = 50

interface CachedTranslation {
  readonly text: string
  readonly engine: TranslationEngine
}

/** Re-selecting the same text must not re-hit any engine. FIFO-capped. */
const cache = new Map<string, CachedTranslation>()

/** Per-target on-device pack state, probed once at startup. */
const packReady: Record<TargetLang, boolean> = { en: false, vi: false }

/** Set when on-device create() refuses to run without a fresh gesture. */
let deviceAutoBlocked = false

export type ApplyState = (state: TranslationState) => void

export function translatorApiPresent(): boolean {
  return builtinSentenceTranslator.isSupported()
}

export function offlinePackReady(target: TargetLang): boolean {
  return packReady[target]
}

export async function probeOfflinePacks(): Promise<void> {
  if (!builtinSentenceTranslator.isSupported()) return
  for (const target of ['vi', 'en'] as const) {
    try {
      packReady[target] = (await builtinSentenceTranslator.availability(target)) === 'available'
    } catch {
      packReady[target] = false
    }
  }
}

export async function translateAuto(text: string, target: TargetLang, apply: ApplyState): Promise<void> {
  const key = `${target}:${text}`
  const hit = cache.get(key)
  if (hit !== undefined) {
    apply({ status: 'done', text: hit.text, target, engine: hit.engine })
    return
  }
  apply({ status: 'translating' })

  if (packReady[target] && !deviceAutoBlocked) {
    try {
      const translated = await builtinSentenceTranslator.translate(text, target)
      remember(key, { text: translated, engine: 'device' })
      apply({ status: 'done', text: translated, target, engine: 'device' })
      return
    } catch (error) {
      // Some setups demand a fresh user gesture even with the pack on disk;
      // remember that and use the online path from here on.
      deviceAutoBlocked = true
      console.debug('[jpdict] on-device translate failed, falling back to online:', error)
    }
  }

  try {
    const response = await chrome.runtime.sendMessage<TranslateCloudRequest, TranslateCloudResponse>({
      type: 'translate-cloud',
      text,
      target,
    })
    if (response.ok) {
      remember(key, { text: response.text, engine: 'cloud' })
      apply({ status: 'done', text: response.text, target, engine: 'cloud' })
    } else {
      apply({ status: 'error', message: response.reason })
    }
  } catch (error) {
    apply({ status: 'error', message: error instanceof Error ? error.message : String(error) })
  }
}

/**
 * Explicit user-gesture path: downloads the Chrome language pack (showing
 * progress), translates with it, and makes on-device the engine of choice.
 */
export async function downloadOfflinePack(text: string, target: TargetLang, apply: ApplyState): Promise<void> {
  apply({ status: 'downloading', pct: 0 })
  try {
    const translated = await builtinSentenceTranslator.translate(text, target, (pct) =>
      apply({ status: 'downloading', pct }),
    )
    packReady[target] = true
    deviceAutoBlocked = false
    remember(`${target}:${text}`, { text: translated, engine: 'device' })
    apply({ status: 'done', text: translated, target, engine: 'device' })
  } catch (error) {
    apply({ status: 'error', message: error instanceof Error ? error.message : String(error) })
  }
}

function remember(key: string, value: CachedTranslation): void {
  if (cache.size >= CACHE_CAP) {
    const oldest = cache.keys().next().value
    if (oldest !== undefined) cache.delete(oldest)
  }
  cache.set(key, value)
}
