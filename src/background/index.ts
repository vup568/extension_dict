/**
 * Background service worker: will own the dictionary (IndexedDB) and the
 * tokenizer, and route typed messages from content scripts.
 * Phase 1: only answers pings to prove the messaging pipeline.
 */
import type { BackgroundRequest, BackgroundResponse } from '../shared/messages'

chrome.runtime.onMessage.addListener(
  (
    message: BackgroundRequest,
    _sender: chrome.runtime.MessageSender,
    sendResponse: (response: BackgroundResponse) => void,
  ): boolean => {
    switch (message.type) {
      case 'ping':
        sendResponse({ type: 'pong' })
        return false // response sent synchronously
    }
  },
)
