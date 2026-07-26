/**
 * Content script composition root: wires selection detection to real
 * dictionary lookups over the extension messaging layer. This is the ONLY
 * place Japanese-specific knowledge enters the content layer (the
 * containsJapanese predicate); everything else here is language-agnostic.
 */
import { containsJapanese } from '../core/language/japanese/detect'
import { PopupController } from './popup-controller'
import { watchSelection } from './selection'
import type { LookupRequest, LookupResponse } from '../shared/messages'
import type { PopupModel } from '../ui'

const controller = new PopupController()

/**
 * Monotonic sequence number guarding against stale async responses: if the
 * selection changed (or cleared) while a lookup was in flight, the late
 * response must not resurrect an outdated popup.
 */
let requestSeq = 0

async function requestLookup(text: string): Promise<LookupResponse | null> {
  try {
    return await chrome.runtime.sendMessage<LookupRequest, LookupResponse>({ type: 'lookup', text })
  } catch (error) {
    // Typical cause: the extension was reloaded/updated while this page's
    // old content script kept running. Never break the host page over it.
    console.debug('[jpdict] lookup failed:', error)
    return null
  }
}

function toModel(response: LookupResponse): PopupModel | null {
  switch (response.status) {
    case 'ready':
      // No matches → no popup for now; Phase 5 adds the dedicated
      // "No match found" state together with deinflection.
      return response.matches.length > 0 ? { kind: 'entries', entries: response.matches } : null
    case 'initializing': {
      const { progress } = response
      const pct =
        progress === null || progress.chunkCount === 0
          ? null
          : Math.round((progress.chunksDone / progress.chunkCount) * 100)
      return { kind: 'status', text: pct === null ? 'Preparing dictionary…' : `Preparing dictionary… ${pct}%` }
    }
    case 'unavailable':
      return { kind: 'status', text: `Dictionary unavailable: ${response.reason}` }
  }
}

watchSelection(containsJapanese, {
  onSelect: (text, range) => {
    const seq = ++requestSeq
    void requestLookup(text).then((response) => {
      if (seq !== requestSeq || response === null) return
      const model = toModel(response)
      if (model === null) controller.hide()
      else controller.show(model, range)
    })
  },
  onClear: () => {
    requestSeq += 1
    controller.handleSelectionCleared()
  },
})
