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
import type { TokenInfo } from '../shared/types'
import type { PopupModel } from '../ui'

const controller = new PopupController()

/**
 * Monotonic sequence number guarding against stale async responses: if the
 * selection changed (or cleared) while a lookup was in flight, the late
 * response must not resurrect an outdated popup.
 */
let requestSeq = 0
/** Anchor of the current selection — token clicks re-show at this spot. */
let lastRange: Range | null = null
/** Token list of the current selection, kept across token-click lookups. */
let lastTokens: readonly TokenInfo[] | null = null

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

/** Model for the non-ready lookup states; null when status is 'ready'. */
function transientModel(response: LookupResponse): PopupModel | null {
  switch (response.status) {
    case 'ready':
      return null
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

function showModel(model: PopupModel, range: Range): void {
  controller.show(model, range, handleTokenClick)
}

/** A token chip was clicked: look up its dictionary form, keep the strip. */
function handleTokenClick(lookupTerm: string): void {
  const range = lastRange
  if (range === null) return
  const seq = ++requestSeq
  void requestLookup(lookupTerm).then((response) => {
    if (seq !== requestSeq || response === null) return
    const transient = transientModel(response)
    if (transient !== null) {
      showModel(transient, range)
      return
    }
    if (response.status !== 'ready') return
    const model: PopupModel =
      response.matches.length > 0
        ? { kind: 'entries', entries: response.matches, tokens: lastTokens }
        : { kind: 'no-match', tokens: lastTokens, deinflectionAvailable: response.deinflectionAvailable }
    showModel(model, range)
  })
}

watchSelection(containsJapanese, {
  onSelect: (text, range) => {
    lastRange = range.cloneRange()
    const seq = ++requestSeq
    void requestLookup(text).then((response) => {
      if (seq !== requestSeq || response === null) return
      const transient = transientModel(response)
      if (transient !== null) {
        showModel(transient, range)
        return
      }
      if (response.status !== 'ready') return
      lastTokens = response.tokens
      const model: PopupModel =
        response.matches.length > 0
          ? { kind: 'entries', entries: response.matches, tokens: response.tokens }
          : { kind: 'no-match', tokens: response.tokens, deinflectionAvailable: response.deinflectionAvailable }
      showModel(model, range)
    })
  },
  onClear: () => {
    // Interactions inside the popup (token clicks, "show more") collapse
    // the page selection; don't let that dismiss the popup.
    if (controller.hasRecentInnerInteraction()) return
    requestSeq += 1
    lastRange = null
    lastTokens = null
    controller.hide()
  },
})
