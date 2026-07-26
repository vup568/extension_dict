/**
 * Content script entry: will detect Japanese text selections and mount the
 * popup UI. Phase 1: a single one-shot ping to verify the service worker is
 * reachable (console.debug only, invisible unless verbose logging is on).
 */
import type { BackgroundRequest, BackgroundResponse } from '../shared/messages'

async function verifyServiceWorker(): Promise<void> {
  try {
    const request: BackgroundRequest = { type: 'ping' }
    const response = await chrome.runtime.sendMessage<BackgroundRequest, BackgroundResponse>(request)
    console.debug('[jpdict] service worker reachable:', response.type === 'pong')
  } catch (error) {
    console.debug('[jpdict] service worker not reachable:', error)
  }
}

void verifyServiceWorker()
