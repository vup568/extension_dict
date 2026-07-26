/**
 * Content script composition root: wires selection detection to real
 * dictionary lookups over the extension messaging layer. This is the ONLY
 * place Japanese-specific knowledge enters the content layer (the
 * containsJapanese predicate); everything else here is language-agnostic.
 *
 * Sentence translation also lives here — a deliberate exception to the
 * "service worker owns everything" rule: Chrome's built-in Translator API is
 * only exposed to window contexts (not workers), and its first create() per
 * language pair requires a real user gesture, which the popup's Translate
 * button provides.
 */
import { containsJapanese } from '../core/language/japanese/detect'
import { builtinSentenceTranslator } from '../core/translation/sentence-translator'
import { PopupController } from './popup-controller'
import type { PopupHandlers } from './popup-controller'
import { watchSelection } from './selection'
import type {
  GetPrefsRequest,
  LookupRequest,
  LookupResponse,
  PrefsResponse,
  SetPrefsRequest,
} from '../shared/messages'
import { DEFAULT_TARGET_LANG } from '../shared/types'
import type { TargetLang, TokenInfo, TranslationState } from '../shared/types'
import type { PopupModel } from '../ui'

const controller = new PopupController()

/**
 * Monotonic sequence number guarding against stale async responses: if the
 * selection changed (or cleared) while a lookup or translation was in
 * flight, the late result must not resurrect an outdated popup.
 */
let requestSeq = 0
/** Anchor of the current selection — token clicks re-show at this spot. */
let lastRange: Range | null = null
/** Token list of the current selection, kept across token-click lookups. */
let lastTokens: readonly TokenInfo[] | null = null
/** Raw text of the current selection — what the Translate button sends. */
let lastText: string | null = null
/** The model currently rendered, so translation states can patch onto it. */
let currentModel: PopupModel | null = null

// ---------------------------------------------------------------------------
// Sentence-translation environment

const translatorSupported = builtinSentenceTranslator.isSupported()
/** Turns false if no target language is actually available on this device. */
let translationOffered = translatorSupported
let viAvailable = true
let targetLang: TargetLang = DEFAULT_TARGET_LANG

if (translatorSupported) {
  void initTranslationPrefs()
}

async function initTranslationPrefs(): Promise<void> {
  try {
    const prefs = await chrome.runtime.sendMessage<GetPrefsRequest, PrefsResponse>({ type: 'get-prefs' })
    targetLang = prefs.targetLang
  } catch {
    // Service worker unreachable — the default stands.
  }
  try {
    if ((await builtinSentenceTranslator.availability('vi')) === 'unavailable') {
      viAvailable = false
      if (targetLang === 'vi') targetLang = 'en'
      if ((await builtinSentenceTranslator.availability('en')) === 'unavailable') translationOffered = false
    }
  } catch {
    translationOffered = false
  }
}

// ---------------------------------------------------------------------------
// Model plumbing

type NonStatusModel = Exclude<PopupModel, { kind: 'status' }>

function withTranslation(model: NonStatusModel, translation: TranslationState): PopupModel {
  // Both branches look identical, but the kind-narrowing is required:
  // TypeScript cannot spread a union type directly into an object literal.
  return model.kind === 'entries' ? { ...model, translation } : { ...model, translation }
}

/** Offer translation only for sentence-ish selections (2+ tokens). */
function initialTranslation(tokens: readonly TokenInfo[] | null): TranslationState | null {
  return translationOffered && tokens !== null && tokens.length >= 2 ? { status: 'idle' } : null
}

function makeHandlers(): PopupHandlers {
  return {
    onTokenClick: handleTokenClick,
    translation: translationOffered
      ? { targetLang, viAvailable, onTranslate: handleTranslate, onTargetLangChange: handleTargetLangChange }
      : undefined,
  }
}

function showCurrent(model: PopupModel, range: Range): void {
  currentModel = model
  controller.show(model, range, makeHandlers())
}

function updateCurrent(model: PopupModel): void {
  currentModel = model
  controller.update(model, makeHandlers())
}

// ---------------------------------------------------------------------------
// Dictionary lookup flow

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

/** A token chip was clicked: look up its dictionary form, keep the strip. */
function handleTokenClick(lookupTerm: string): void {
  const range = lastRange
  if (range === null) return
  const seq = ++requestSeq
  void requestLookup(lookupTerm).then((response) => {
    if (seq !== requestSeq || response === null) return
    const transient = transientModel(response)
    if (transient !== null) {
      showCurrent(transient, range)
      return
    }
    if (response.status !== 'ready') return
    const model: PopupModel =
      response.matches.length > 0
        ? {
            kind: 'entries',
            entries: response.matches,
            tokens: lastTokens,
            translation: initialTranslation(lastTokens),
          }
        : {
            kind: 'no-match',
            tokens: lastTokens,
            deinflectionAvailable: response.deinflectionAvailable,
            translation: initialTranslation(lastTokens),
          }
    showCurrent(model, range)
  })
}

// ---------------------------------------------------------------------------
// Sentence-translation flow

function handleTranslate(target: TargetLang): void {
  const text = lastText
  if (text === null) return
  const seqAtStart = requestSeq
  const apply = (state: TranslationState): void => {
    if (requestSeq !== seqAtStart) return // selection changed meanwhile
    const model = currentModel
    if (model === null || model.kind === 'status') return
    updateCurrent(withTranslation(model, state))
  }
  apply({ status: 'translating' })
  builtinSentenceTranslator
    .translate(text, target, (pct) => apply({ status: 'downloading', pct }))
    .then((translated) => apply({ status: 'done', text: translated, target }))
    .catch((error: unknown) => {
      apply({ status: 'error', message: error instanceof Error ? error.message : String(error) })
    })
}

function handleTargetLangChange(target: TargetLang): void {
  if (target === targetLang) return
  targetLang = target
  chrome.runtime
    .sendMessage<SetPrefsRequest, PrefsResponse>({ type: 'set-prefs', targetLang: target })
    .catch(() => {
      // Preference just won't persist; the in-page value still applies.
    })
  const model = currentModel
  if (model === null || model.kind === 'status') return
  if (model.translation?.status === 'done') {
    handleTranslate(target) // re-translate into the newly chosen language
  } else {
    updateCurrent(model) // refresh the toggle highlight
  }
}

// ---------------------------------------------------------------------------
// Selection wiring

watchSelection(containsJapanese, {
  onSelect: (text, range) => {
    lastRange = range.cloneRange()
    lastText = text
    const seq = ++requestSeq
    void requestLookup(text).then((response) => {
      if (seq !== requestSeq || response === null) return
      const transient = transientModel(response)
      if (transient !== null) {
        showCurrent(transient, range)
        return
      }
      if (response.status !== 'ready') return
      lastTokens = response.tokens
      const model: PopupModel =
        response.matches.length > 0
          ? {
              kind: 'entries',
              entries: response.matches,
              tokens: response.tokens,
              translation: initialTranslation(response.tokens),
            }
          : {
              kind: 'no-match',
              tokens: response.tokens,
              deinflectionAvailable: response.deinflectionAvailable,
              translation: initialTranslation(response.tokens),
            }
      showCurrent(model, range)
    })
  },
  onClear: () => {
    // Interactions inside the popup (token clicks, Translate, "show more")
    // collapse the page selection; don't let that dismiss the popup.
    if (controller.hasRecentInnerInteraction()) return
    requestSeq += 1
    lastRange = null
    lastTokens = null
    lastText = null
    currentModel = null
    controller.hide()
  },
})
