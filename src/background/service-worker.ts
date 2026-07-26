/**
 * Background service worker: owns the dictionary (IndexedDB) and — from
 * Phase 5 — the tokenizer, and routes typed messages from content scripts.
 * Content scripts never touch the dictionary directly, so the ~200k-entry
 * database exists exactly once instead of per-tab.
 */
import { configureImporter, getDictionaryStatus, startImport } from '../core/dictionary/importer'
import { lookupExact } from '../core/dictionary/lookup'
import type {
  BackgroundRequest,
  BackgroundResponse,
  DictProgress,
  DictStatusResponse,
  LookupResponse,
} from '../shared/messages'

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
      case 'ping':
        sendResponse({ type: 'pong' })
        return false
      case 'lookup':
        void handleLookup(message.text).then(sendResponse)
        return true // keep the channel open for the async response
      case 'dict-status':
        void handleDictStatus().then(sendResponse)
        return true
    }
  },
)

async function handleLookup(text: string): Promise<LookupResponse> {
  try {
    const status = await getDictionaryStatus()
    switch (status.state) {
      case 'ready':
        return { type: 'lookup-result', status: 'ready', matches: await lookupExact(text) }
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

/**
 * Manual verification hook. Open the service-worker console from
 * chrome://extensions and run:
 *   await jpdictDebug.status()
 *   await jpdictDebug.lookup('学生')
 */
Object.assign(globalThis, {
  jpdictDebug: { lookup: lookupExact, status: getDictionaryStatus },
})
