/**
 * Content script composition root: wires selection detection to real
 * dictionary lookups over the extension messaging layer. This is the ONLY
 * place Japanese-specific knowledge enters the content layer (the
 * containsJapanese/uniqueKanji helpers); everything else here is
 * language-agnostic.
 *
 * The popup is tabbed (Từ vựng | Hán tự | Dịch); tab data loads lazily —
 * kanji info and single-word translations are only fetched when their tab
 * is first opened (see translation-flow.ts for the hybrid engine).
 */
import { containsJapanese, uniqueKanji } from '../core/language/japanese/detect'
import { PopupController } from './popup-controller'
import type { PopupHandlers } from './popup-controller'
import { LOOKUP_MAX_LENGTH, watchSelection } from './selection'
import { extractSentence } from './sentence'
import {
  downloadOfflinePack,
  offlinePackReady,
  probeOfflinePacks,
  translateAuto,
  translatorApiPresent,
} from './translation-flow'
import type {
  AnalyzeGrammarRequest,
  AnalyzeGrammarResponse,
  GetPrefsRequest,
  KanjiRequest,
  KanjiResponse,
  LookupRequest,
  LookupResponse,
  PrefsResponse,
  SetPrefsRequest,
} from '../shared/messages'
import { DEFAULT_TARGET_LANG } from '../shared/types'
import type { TargetLang, TokenInfo, TranslationState } from '../shared/types'
import type { PopupModel } from '../ui'

const controller = new PopupController()

/** Max kanji cards offered per selection. */
const KANJI_TAB_CAP = 30

/**
 * Monotonic sequence number guarding against stale async responses: if the
 * selection changed (or cleared) while a lookup, kanji fetch, or translation
 * was in flight, the late result must not resurrect an outdated popup.
 */
let requestSeq = 0
/** Orders chip-click lookups within one selection (last click wins). */
let tokenSeq = 0
/** Raw text of the current selection — what gets translated. */
let lastText: string | null = null
/** The model currently rendered, so async tab states can patch onto it. */
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

type ResultModel = Extract<PopupModel, { kind: 'result' }>

/** The live model when it is a tabbed result view, else null. */
function currentResult(): ResultModel | null {
  return currentModel !== null && currentModel.kind === 'result' ? currentModel : null
}

function withTranslation(
  model: Exclude<PopupModel, { kind: 'status' }>,
  translation: TranslationState,
): PopupModel {
  switch (model.kind) {
    case 'result':
      return { ...model, translation }
    case 'translation':
      return { kind: 'translation', translation }
  }
}

/** Translation starts automatically for sentence-ish selections (2+ tokens). */
function shouldTranslate(tokens: readonly TokenInfo[] | null): boolean {
  return tokens !== null && tokens.length >= 2
}

function makeHandlers(): PopupHandlers {
  return {
    onTokenClick: handleTokenClick,
    onRequestKanji: handleRequestKanji,
    onRequestGrammar: handleRequestGrammar,
    onRequestTranslation: handleRequestTranslation,
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
      return {
        kind: 'status',
        text: pct === null ? 'Đang chuẩn bị từ điển…' : `Đang chuẩn bị từ điển… ${pct}%`,
      }
    }
    case 'unavailable':
      return { kind: 'status', text: `Từ điển chưa dùng được: ${response.reason}` }
  }
}

/**
 * A token chip was clicked: look up its dictionary form and swap the Từ vựng
 * tab content in place — the strip, other tabs' data, pin position, and the
 * whole-selection translation all stay.
 */
function handleTokenClick(token: TokenInfo): void {
  const selectionSeq = requestSeq
  const mySeq = ++tokenSeq
  void requestLookup(token.lookupTerm).then((response) => {
    if (selectionSeq !== requestSeq || mySeq !== tokenSeq) return
    if (response === null || response.status !== 'ready') return
    const live = currentResult()
    if (live === null) return
    updateCurrent({
      ...live,
      entries: response.matches,
      grammar: token.grammar,
      deinflectionAvailable: response.deinflectionAvailable,
    })
  })
}

// ---------------------------------------------------------------------------
// Hán tự tab (lazy)

function handleRequestKanji(): void {
  const model = currentResult()
  if (model === null || model.kanjiTab !== 'idle' || model.kanjiChars.length === 0) return
  const seq = requestSeq
  updateCurrent({ ...model, kanjiTab: 'loading' })
  const applyKanji = (state: ResultModel['kanjiTab']): void => {
    if (seq !== requestSeq) return
    const live = currentResult()
    if (live !== null) updateCurrent({ ...live, kanjiTab: state })
  }
  chrome.runtime
    .sendMessage<KanjiRequest, KanjiResponse>({ type: 'kanji', chars: model.kanjiChars })
    .then((response) => applyKanji({ ready: response.ready, kanji: response.kanji }))
    .catch(() => applyKanji({ ready: false, kanji: [] }))
}

// ---------------------------------------------------------------------------
// Ngữ pháp tab (lazy)

function handleRequestGrammar(): void {
  const model = currentResult()
  if (model === null || model.grammarTab !== 'idle' || model.sentence === null) return
  const seq = requestSeq
  updateCurrent({ ...model, grammarTab: 'loading' })
  const applyGrammar = (state: ResultModel['grammarTab']): void => {
    if (seq !== requestSeq) return
    const live = currentResult()
    if (live !== null) updateCurrent({ ...live, grammarTab: state })
  }
  chrome.runtime
    .sendMessage<AnalyzeGrammarRequest, AnalyzeGrammarResponse>({
      type: 'analyze-grammar',
      text: model.sentence.sentence,
    })
    .then((response) => applyGrammar(response.ok ? { analysis: response.analysis } : 'unavailable'))
    .catch(() => applyGrammar('unavailable'))
}

// ---------------------------------------------------------------------------
// Sentence-translation flow

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

/** Dịch tab opened before any translation ran (single-word selections). */
function handleRequestTranslation(): void {
  const model = currentResult()
  if (model === null || model.translation !== null) return
  startTranslation()
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
  const model = currentModel
  const started = model !== null && model.kind !== 'status' && model.translation !== null
  if (started) startTranslation() // re-translate (cached = instant)
}

// ---------------------------------------------------------------------------
// Selection wiring

watchSelection(containsJapanese, {
  onSelect: (text, range) => {
    lastText = text
    requestSeq += 1
    if (text.length > LOOKUP_MAX_LENGTH) {
      // Tier 2: paragraph-sized selection — translation only. Tokenizing
      // hundreds of chips and dictionary work would be slow and useless.
      showCurrent({ kind: 'translation', translation: { status: 'translating' } }, range)
      startTranslation()
      return
    }
    const seq = requestSeq
    // Extract the containing sentence NOW — the live Range mutates with the
    // next selection, so it can't be trusted inside the async callback.
    const sentence = extractSentence(range) ?? { sentence: text, selStart: 0, selEnd: text.length }
    void requestLookup(text).then((response) => {
      if (seq !== requestSeq || response === null) return
      const transient = transientModel(response)
      if (transient !== null) {
        showCurrent(transient, range)
        return
      }
      if (response.status !== 'ready') return
      const translate = shouldTranslate(response.tokens)
      const model: PopupModel = {
        kind: 'result',
        entries: response.matches,
        tokens: response.tokens,
        grammar: response.grammar,
        deinflectionAvailable: response.deinflectionAvailable,
        kanjiChars: uniqueKanji(text).slice(0, KANJI_TAB_CAP),
        kanjiTab: 'idle',
        sentence,
        grammarTab: 'idle',
        translation: translate ? { status: 'translating' } : null,
      }
      showCurrent(model, range)
      if (translate) startTranslation()
    })
  },
  onClear: () => {
    // Interactions inside the popup (token clicks, tab switches, toggles)
    // collapse the page selection; don't let that dismiss the popup.
    if (controller.hasRecentInnerInteraction()) return
    requestSeq += 1
    lastText = null
    currentModel = null
    controller.hide()
  },
})
