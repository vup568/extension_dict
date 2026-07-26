import { useState } from 'preact/hooks'
import type { DictionaryEntry, TargetLang, TokenInfo, TranslationState } from '../shared/types'

/** Senses shown before the "show more" toggle kicks in. */
const SENSE_CAP = 4

/** Everything the popup can display. */
export type PopupModel =
  | {
      readonly kind: 'entries'
      readonly entries: readonly DictionaryEntry[]
      readonly tokens: readonly TokenInfo[] | null
      /** null → sentence translation not offered for this selection. */
      readonly translation: TranslationState | null
    }
  | {
      readonly kind: 'no-match'
      readonly tokens: readonly TokenInfo[] | null
      readonly deinflectionAvailable: boolean
      readonly translation: TranslationState | null
    }
  /** Transient states: dictionary still importing, or unavailable. */
  | { readonly kind: 'status'; readonly text: string }

/** Environment + callbacks for the sentence-translation controls. */
export interface TranslationControls {
  readonly targetLang: TargetLang
  readonly viAvailable: boolean
  readonly onTranslate: (target: TargetLang) => void
  readonly onTargetLangChange: (target: TargetLang) => void
}

export interface PopupProps {
  readonly model: PopupModel
  readonly onTokenClick?: (lookupTerm: string) => void
  readonly translation?: TranslationControls
}

export function Popup({ model, onTokenClick, translation }: PopupProps) {
  switch (model.kind) {
    case 'status':
      return (
        <div class="panel" role="dialog" aria-label="Dictionary status">
          <div class="status">{model.text}</div>
        </div>
      )
    case 'no-match':
      return (
        <div class="panel" role="dialog" aria-label="Dictionary result">
          <div class="status">
            {model.deinflectionAvailable ? 'No match found' : 'No match found (tokenizer unavailable)'}
          </div>
          {model.tokens !== null && model.tokens.length > 0 && (
            <TokenStrip tokens={model.tokens} onTokenClick={onTokenClick} />
          )}
          {model.translation !== null && <TranslateSection state={model.translation} controls={translation} />}
        </div>
      )
    case 'entries':
      return (
        <EntriesView
          entries={model.entries}
          tokens={model.tokens}
          onTokenClick={onTokenClick}
          translationState={model.translation}
          translationControls={translation}
        />
      )
  }
}

/**
 * The clickable token list for phrase selections. Each chip shows the
 * surface text and looks up the token's dictionary form when clicked.
 */
function TokenStrip({
  tokens,
  onTokenClick,
}: {
  readonly tokens: readonly TokenInfo[]
  readonly onTokenClick?: (lookupTerm: string) => void
}) {
  return (
    <div class="tokens">
      {tokens.map((token, i) => (
        <button
          key={`${i}-${token.surface}`}
          class="token"
          lang="ja"
          title={token.lookupTerm === token.surface ? undefined : token.lookupTerm}
          onClick={() => onTokenClick?.(token.lookupTerm)}
        >
          {token.surface}
        </button>
      ))}
    </div>
  )
}

/** Sentence-translation row: Translate button, VI|EN toggle, state below. */
function TranslateSection({
  state,
  controls,
}: {
  readonly state: TranslationState
  readonly controls?: TranslationControls
}) {
  if (controls === undefined) return null
  const busy = state.status === 'downloading' || state.status === 'translating'
  const langBtn = (lang: TargetLang, label: string) => (
    <button
      class={`lang-btn${controls.targetLang === lang ? ' active' : ''}`}
      disabled={busy || (lang === 'vi' && !controls.viAvailable)}
      title={lang === 'vi' && !controls.viAvailable ? 'Vietnamese pack unavailable on this device' : undefined}
      onClick={() => controls.onTargetLangChange(lang)}
    >
      {label}
    </button>
  )
  return (
    <div class="translate">
      <div class="translate-row">
        <button class="translate-btn" disabled={busy} onClick={() => controls.onTranslate(controls.targetLang)}>
          {state.status === 'done' ? 'Translate again' : 'Translate sentence'}
        </button>
        <span class="lang-toggle">
          {langBtn('vi', 'VI')}
          {langBtn('en', 'EN')}
        </span>
      </div>
      {state.status === 'downloading' && (
        <div class="translate-note">Downloading language pack (one-time)… {state.pct}%</div>
      )}
      {state.status === 'translating' && <div class="translate-note">Translating…</div>}
      {state.status === 'error' && <div class="translate-note error">{state.message}</div>}
      {state.status === 'done' && <div class="translate-result">{state.text}</div>}
    </div>
  )
}

/**
 * Dictionary entries. When several match (homophones, multiple readings),
 * ‹ › cycles through them; long sense lists are capped behind "show more".
 */
function EntriesView({
  entries,
  tokens,
  onTokenClick,
  translationState,
  translationControls,
}: {
  readonly entries: readonly DictionaryEntry[]
  readonly tokens: readonly TokenInfo[] | null
  readonly onTokenClick?: (lookupTerm: string) => void
  readonly translationState: TranslationState | null
  readonly translationControls?: TranslationControls
}) {
  const [entryIndex, setEntryIndex] = useState(0)
  const [showAllSenses, setShowAllSenses] = useState(false)

  const entry = entries[entryIndex]
  if (entry === undefined) return null

  const senses = showAllSenses ? entry.senses : entry.senses.slice(0, SENSE_CAP)
  const hiddenCount = entry.senses.length - senses.length

  const cycle = (delta: number): void => {
    setEntryIndex((entryIndex + delta + entries.length) % entries.length)
    setShowAllSenses(false)
  }

  return (
    <div class="panel" role="dialog" aria-label="Dictionary entry">
      {tokens !== null && tokens.length > 0 && <TokenStrip tokens={tokens} onTokenClick={onTokenClick} />}
      {entries.length > 1 && (
        <div class="entry-nav">
          <button class="nav-btn" onClick={() => cycle(-1)} aria-label="Previous entry">
            ‹
          </button>
          <span>
            {entryIndex + 1} / {entries.length}
          </span>
          <button class="nav-btn" onClick={() => cycle(1)} aria-label="Next entry">
            ›
          </button>
        </div>
      )}
      <div class="headword">
        <span class="expression" lang="ja">
          {entry.expression}
        </span>
        <span class="reading" lang="ja">
          {entry.reading}
        </span>
      </div>
      <ol class="senses">
        {senses.map((sense, i) => (
          <li class="sense" key={i}>
            <span class="pos">{sense.partsOfSpeech.join(', ')}</span>
            <span>{sense.glosses.join('; ')}</span>
          </li>
        ))}
      </ol>
      {hiddenCount > 0 && (
        <button class="more-btn" onClick={() => setShowAllSenses(true)}>
          show {hiddenCount} more {hiddenCount > 1 ? 'senses' : 'sense'}
        </button>
      )}
      {translationState !== null && <TranslateSection state={translationState} controls={translationControls} />}
    </div>
  )
}
