/**
 * Content script composition root: wires selection detection to real
 * dictionary lookups over the extension messaging layer. This is the ONLY
 * place Japanese-specific knowledge enters the content layer (the
 * containsJapanese predicate); everything else here is language-agnostic.
 *
 * Sentence translation is orchestrated from here too (see
 * translation-flow.ts): multi-token selections translate automatically —
 * on-device when Chrome's pack is installed, online otherwise.
 */
import { containsJapanese } from '../core/language/japanese/detect'
import { PopupController } from './popup-controller'
import type { PopupHandlers } from './popup-controller'
import { LOOKUP_MAX_LENGTH, watchSelection } from './selection'
import {
  downloadOfflinePack,
  offlinePackReady,
  probeOfflinePacks,
  translateAuto,
  translatorApiPresent,
} from './translation-flow'
import type {
  GetPrefsRequest,
  LookupRequest,
  LookupResponse,
  PrefsResponse,
  SetPrefsRequest,
} from '../shared/messages'
import { DEFAULT_TARGET_LANG } from '../shared/types'
import type { GrammarPart, TargetLang, TokenInfo, TranslationState } from '../shared/types'
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
/** Raw text of the current selection — what gets translated. */
let lastText: string | null = null
/** The model currently rendered, so translation states can patch onto it. */
let currentModel: PopupModel | null = null

let targetLang: TargetLang = DEFAULT_TARGET_LANG

void probeOfflinePacks()
void loadPrefs()

async function loadPrefs(): Promise<void> {
  try {
    const prefs = await chrome.runtime.sendMessage<GetPrefsRequest, PrefsResponse>({ type: 'get-prefs' })
    targetLang = prefs.targetLang
  } catch {
    // Service worker unreachable — the default stands.
  }
}

// ---------------------------------------------------------------------------
// Model plumbing

type NonStatusModel = Exclude<PopupModel, { kind: 'status' }>

function withTranslation(model: NonStatusModel, translation: TranslationState): PopupModel {
  // The branches look identical, but the kind-narrowing is required:
  // TypeScript cannot spread a union type directly into an object literal.
  switch (model.kind) {
    case 'entries':
      return { ...model, translation }
    case 'no-match':
      return { ...model, translation }
    case 'translation':
      return { kind: 'translation', translation }
  }
}

/** Grammar shown for a token click: the chip's own breakdown. */
function grammarForToken(token: TokenInfo): readonly GrammarPart[] | null {
  return token.grammar
}

/** Translation applies to sentence-ish selections only (2+ tokens). */
function shouldTranslate(tokens: readonly TokenInfo[] | null): boolean {
  return tokens !== null && tokens.length >= 2
}

function makeHandlers(): PopupHandlers {
  return {
    onTokenClick: handleTokenClick,
    translation: {
      targetLang,
      onTargetLangChange: handleTargetLangChange,
      onRetry: () => startTranslation(),
      offlinePackAvailable: translatorApiPresent() && !offlinePackReady(targetLang),
      onDownloadPack: handleDownloadPack,
    },
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

/** The translation slot of the currently shown model (kept across updates). */
function currentTranslation(): TranslationState | null {
  return currentModel !== null && currentModel.kind !== 'status' ? currentModel.translation : null
}

/** A token chip was clicked: look up its dictionary form, keep the strip. */
function handleTokenClick(token: TokenInfo): void {
  const range = lastRange
  if (range === null) return
  const seq = ++requestSeq
  void requestLookup(token.lookupTerm).then((response) => {
    if (seq !== requestSeq || response === null) return
    const transient = transientModel(response)
    if (transient !== null) {
      showCurrent(transient, range)
      return
    }
    if (response.status !== 'ready') return
    // Keep the whole-selection translation visible while exploring tokens;
    // the grammar section shows the clicked chip's own breakdown.
    const translation = currentTranslation()
    const grammar = grammarForToken(token)
    const model: PopupModel =
      response.matches.length > 0
        ? { kind: 'entries', entries: response.matches, tokens: lastTokens, grammar, translation }
        : {
            kind: 'no-match',
            tokens: lastTokens,
            grammar,
            deinflectionAvailable: response.deinflectionAvailable,
            translation,
          }
    showCurrent(model, range)
  })
}

// ---------------------------------------------------------------------------
// Sentence-translation flow (auto)

/** Applies translation states onto the live model, dropping stale ones. */
function makeApplier(): (state: TranslationState) => void {
  const seqAtStart = requestSeq
  return (state) => {
    if (requestSeq !== seqAtStart) return // selection changed meanwhile
    const model = currentModel
    if (model === null || model.kind === 'status') return
    updateCurrent(withTranslation(model, state))
  }
}

function startTranslation(): void {
  const text = lastText
  if (text === null) return
  void translateAuto(text, targetLang, makeApplier())
}

function handleDownloadPack(): void {
  const text = lastText
  if (text === null) return
  void downloadOfflinePack(text, targetLang, makeApplier())
}

function handleTargetLangChange(target: TargetLang): void {
  if (target === targetLang) return
  targetLang = target
  chrome.runtime
    .sendMessage<SetPrefsRequest, PrefsResponse>({ type: 'set-prefs', targetLang: target })
    .catch(() => {
      // Preference just won't persist; the in-page value still applies.
    })
  if (currentTranslation() !== null) startTranslation() // re-translate (cached = instant)
}

// ---------------------------------------------------------------------------
// Selection wiring

watchSelection(containsJapanese, {
  onSelect: (text, range) => {
    lastRange = range.cloneRange()
    lastText = text
    requestSeq += 1
    if (text.length > LOOKUP_MAX_LENGTH) {
      // Tier 2: paragraph-sized selection — translation only. Tokenizing
      // hundreds of chips and dictionary work would be slow and useless.
      lastTokens = null
      showCurrent({ kind: 'translation', translation: { status: 'translating' } }, range)
      startTranslation()
      return
    }
    const seq = requestSeq
    void requestLookup(text).then((response) => {
      if (seq !== requestSeq || response === null) return
      const transient = transientModel(response)
      if (transient !== null) {
        showCurrent(transient, range)
        return
      }
      if (response.status !== 'ready') return
      lastTokens = response.tokens
      const translate = shouldTranslate(response.tokens)
      const translation: TranslationState | null = translate ? { status: 'translating' } : null
      const model: PopupModel =
        response.matches.length > 0
          ? {
              kind: 'entries',
              entries: response.matches,
              tokens: response.tokens,
              grammar: response.grammar,
              translation,
            }
          : {
              kind: 'no-match',
              tokens: response.tokens,
              grammar: response.grammar,
              deinflectionAvailable: response.deinflectionAvailable,
              translation,
            }
      showCurrent(model, range)
      if (translate) startTranslation()
    })
  },
  onClear: () => {
    // Interactions inside the popup (token clicks, toggles, "show more")
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
