/**
 * Background service worker: owns the dictionary (IndexedDB) and the
 * kuromoji tokenizer, and routes typed messages from content scripts.
 * Content scripts never touch either directly, so both exist exactly once
 * instead of per-tab.
 */
import { getDb } from '../core/dictionary/db'
import { configureImporter, getDictionaryStatus, startImport } from '../core/dictionary/importer'
import { lookupExact } from '../core/dictionary/lookup'
import { configureTokenizer } from '../core/language/japanese/tokenizer'
import { japanesePack } from '../core/language/japanese/japanese-pack'
import { cloudTranslate } from '../core/translation/cloud-translator'
import type {
  BackgroundRequest,
  BackgroundResponse,
  DictProgress,
  DictStatusResponse,
  LookupResponse,
  PrefsResponse,
  TranslateCloudResponse,
} from '../shared/messages'
import { DEFAULT_TARGET_LANG } from '../shared/types'
import type { TargetLang } from '../shared/types'

configureImporter({
  fileUrl: (name) => chrome.runtime.getURL(`dict/${name}`),
  // Badge doubles as the first-install progress UI AND as a keep-alive:
  // every chrome.* API call resets the service worker's 30s idle timer, so
  // per-chunk badge updates keep the worker alive through the long import.
  onProgress: (progress) => {
    void updateBadge(progress)
  },
  onError: (reason) => {
    console.error('[jpdict] dictionary import failed:', reason)
    void chrome.action.setBadgeText({ text: '!' })
    void chrome.action.setTitle({ title: `Dictionary import failed: ${reason}` })
  },
})

// Root-relative on purpose — see the path gotcha note in tokenizer.ts.
configureTokenizer('/kuromoji')

void chrome.action.setBadgeBackgroundColor({ color: '#2f5fd0' })

chrome.runtime.onInstalled.addListener(() => {
  startImport()
})
// Also resume a possibly interrupted import on every browser launch.
chrome.runtime.onStartup.addListener(() => {
  startImport()
})

chrome.runtime.onMessage.addListener(
  (
    message: BackgroundRequest,
    _sender: chrome.runtime.MessageSender,
    sendResponse: (response: BackgroundResponse) => void,
  ): boolean => {
    switch (message.type) {
      case 'lookup':
        void handleLookup(message.text).then(sendResponse)
        return true // keep the channel open for the async response
      case 'dict-status':
        void handleDictStatus().then(sendResponse)
        return true
      case 'get-prefs':
        void handleGetPrefs().then(sendResponse)
        return true
      case 'set-prefs':
        void handleSetPrefs(message.targetLang).then(sendResponse)
        return true
      case 'translate-cloud':
        void handleCloudTranslate(message.text, message.target).then(sendResponse)
        return true
    }
  },
)

async function handleCloudTranslate(text: string, target: TargetLang): Promise<TranslateCloudResponse> {
  try {
    return { type: 'translate-cloud-result', ok: true, text: await cloudTranslate(text, target) }
  } catch (error) {
    return {
      type: 'translate-cloud-result',
      ok: false,
      reason: error instanceof Error ? error.message : String(error),
    }
  }
}

async function handleGetPrefs(): Promise<PrefsResponse> {
  try {
    const db = await getDb()
    const prefs = await db.get('meta', 'prefs')
    return {
      type: 'prefs',
      targetLang: prefs?.key === 'prefs' ? prefs.targetLang : DEFAULT_TARGET_LANG,
    }
  } catch {
    return { type: 'prefs', targetLang: DEFAULT_TARGET_LANG }
  }
}

async function handleSetPrefs(targetLang: TargetLang): Promise<PrefsResponse> {
  try {
    const db = await getDb()
    await db.put('meta', { key: 'prefs', targetLang })
  } catch (error) {
    console.warn('[jpdict] failed to persist prefs:', error)
  }
  return { type: 'prefs', targetLang }
}

async function handleLookup(text: string): Promise<LookupResponse> {
  try {
    const status = await getDictionaryStatus()
    switch (status.state) {
      case 'ready': {
        const resolution = await japanesePack.resolve(text)
        return {
          type: 'lookup-result',
          status: 'ready',
          matches: resolution.matches,
          tokens: resolution.tokens,
          grammar: resolution.grammar,
          deinflectionAvailable: resolution.deinflectionAvailable,
        }
      }
      case 'importing':
        startImport() // resume in case the worker restarted mid-import
        return { type: 'lookup-result', status: 'initializing', progress: status.progress }
      case 'unavailable':
        return { type: 'lookup-result', status: 'unavailable', reason: status.reason }
    }
  } catch (error) {
    return {
      type: 'lookup-result',
      status: 'unavailable',
      reason: error instanceof Error ? error.message : String(error),
    }
  }
}

async function handleDictStatus(): Promise<DictStatusResponse> {
  try {
    return { type: 'dict-status', status: await getDictionaryStatus() }
  } catch (error) {
    return {
      type: 'dict-status',
      status: { state: 'unavailable', reason: error instanceof Error ? error.message : String(error) },
    }
  }
}

async function updateBadge(progress: DictProgress): Promise<void> {
  const done = progress.chunkCount > 0 && progress.chunksDone >= progress.chunkCount
  const pct = progress.chunkCount === 0 ? 0 : Math.round((progress.chunksDone / progress.chunkCount) * 100)
  await chrome.action.setBadgeText({ text: done ? '' : `${pct}%` })
}

/** Average resolve latency after warm-up — for the <50ms performance check. */
async function bench(term: string, runs = 20): Promise<string> {
  await japanesePack.resolve(term)
  const start = performance.now()
  for (let i = 0; i < runs; i++) await japanesePack.resolve(term)
  const avg = (performance.now() - start) / runs
  return `${avg.toFixed(2)} ms average over ${runs} lookups`
}

/**
 * Manual verification hooks. Open the service-worker console from
 * chrome://extensions and run e.g.:
 *   await jpdictDebug.status()
 *   await jpdictDebug.lookup('食べました')  // full resolution w/ deinflection
 *   await jpdictDebug.exact('学生')         // raw exact match
 *   await jpdictDebug.bench('食べました')   // perf target: <50ms
 */
Object.assign(globalThis, {
  jpdictDebug: {
    lookup: (text: string) => japanesePack.resolve(text),
    exact: lookupExact,
    status: getDictionaryStatus,
    bench,
  },
})
