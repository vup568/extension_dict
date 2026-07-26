import { useState } from 'preact/hooks'
import type { DictionaryEntry, GrammarPart, TargetLang, TokenInfo, TranslationState } from '../shared/types'

/** Senses shown before the "show more" toggle kicks in. */
const SENSE_CAP = 4

/** Everything the popup can display. */
export type PopupModel =
  | {
      readonly kind: 'entries'
      readonly entries: readonly DictionaryEntry[]
      readonly tokens: readonly TokenInfo[] | null
      /** Breakdown of the conjugated unit being viewed, when there is one. */
      readonly grammar: readonly GrammarPart[] | null
      /** null → sentence translation not offered for this selection. */
      readonly translation: TranslationState | null
    }
  | {
      readonly kind: 'no-match'
      readonly tokens: readonly TokenInfo[] | null
      readonly grammar: readonly GrammarPart[] | null
      readonly deinflectionAvailable: boolean
      readonly translation: TranslationState | null
    }
  /** Long selections: translation only, no dictionary content. */
  | { readonly kind: 'translation'; readonly translation: TranslationState }
  /** Transient states: dictionary still importing, or unavailable. */
  | { readonly kind: 'status'; readonly text: string }

/** Environment + callbacks for the sentence-translation controls. */
export interface TranslationControls {
  readonly targetLang: TargetLang
  readonly onTargetLangChange: (target: TargetLang) => void
  readonly onRetry: () => void
  /** True when the on-device pack could be downloaded but isn't yet. */
  readonly offlinePackAvailable: boolean
  readonly onDownloadPack: () => void
}

export interface PopupProps {
  readonly model: PopupModel
  readonly onTokenClick?: (token: TokenInfo) => void
  readonly translation?: TranslationControls
}

export function Popup({ model, onTokenClick, translation }: PopupProps) {
  switch (model.kind) {
    case 'status':
      return (
        <div class="panel" role="dialog" aria-label="Dictionary status">
          <DragHandle />
          <div class="status">{model.text}</div>
        </div>
      )
    case 'no-match':
      return (
        <div class="panel" role="dialog" aria-label="Dictionary result">
          <DragHandle />
          <div class="status">
            {model.deinflectionAvailable ? 'No match found' : 'No match found (tokenizer unavailable)'}
          </div>
          {model.tokens !== null && model.tokens.length > 0 && (
            <TokenStrip tokens={model.tokens} onTokenClick={onTokenClick} />
          )}
          {model.grammar !== null && <GrammarSection parts={model.grammar} />}
          {model.translation !== null && <TranslateSection state={model.translation} controls={translation} />}
        </div>
      )
    case 'translation':
      return (
        <div class="panel" role="dialog" aria-label="Translation">
          <DragHandle />
          <TranslateSection state={model.translation} controls={translation} />
        </div>
      )
    case 'entries':
      return (
        <EntriesView
          entries={model.entries}
          tokens={model.tokens}
          grammar={model.grammar}
          onTokenClick={onTokenClick}
          translationState={model.translation}
          translationControls={translation}
        />
      )
  }
}

/** Grip strip: the popup can be dragged by this (or any empty panel area). */
function DragHandle() {
  return <div class="drag-handle" aria-hidden="true" />
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
  readonly onTokenClick?: (token: TokenInfo) => void
}) {
  return (
    <div class="tokens">
      {tokens.map((token, i) => (
        <button
          key={`${i}-${token.surface}`}
          class="token"
          lang="ja"
          title={token.lookupTerm === token.surface ? undefined : token.lookupTerm}
          onClick={() => onTokenClick?.(token)}
        >
          {token.surface}
        </button>
      ))}
    </div>
  )
}

/** Per-part breakdown of a conjugated grammar unit (なりました etc.). */
function GrammarSection({ parts }: { readonly parts: readonly GrammarPart[] }) {
  return (
    <div class="grammar">
      <div class="section-label">Grammar</div>
      <ul class="grammar-list">
        {parts.map((part, i) => (
          <li class="grammar-row" key={`${i}-${part.surface}`}>
            <span class="grammar-surface" lang="ja">
              {part.surface}
            </span>
            <span class="grammar-desc">{part.description}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}

/**
 * Auto-translation area: header labels it "Translation" so it can't be
 * mistaken for the dictionary senses above; VI|EN retranslates on switch.
 */
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
      disabled={busy}
      onClick={() => controls.onTargetLangChange(lang)}
    >
      {label}
    </button>
  )
  return (
    <div class="translate">
      <div class="translate-row">
        <span class="section-label">Translation</span>
        <span class="lang-toggle">
          {langBtn('vi', 'VI')}
          {langBtn('en', 'EN')}
        </span>
      </div>
      {state.status === 'translating' && <div class="translate-note">Translating…</div>}
      {state.status === 'downloading' && (
        <div class="translate-note">Downloading offline pack (one-time)… {state.pct}%</div>
      )}
      {state.status === 'error' && (
        <div class="translate-note error">
          {state.message}{' '}
          <button class="link-btn" onClick={controls.onRetry}>
            Retry
          </button>
        </div>
      )}
      {state.status === 'done' && (
        <div
          class="translate-result"
          title={state.engine === 'device' ? 'Translated on-device (offline, private)' : 'Translated online (Google)'}
        >
          {state.text}
        </div>
      )}
      {state.status === 'done' && state.engine === 'cloud' && controls.offlinePackAvailable && (
        <button
          class="link-btn"
          title="Chrome downloads a language pack once; afterwards translation is private and works offline"
          onClick={controls.onDownloadPack}
        >
          ⬇ Download offline pack
        </button>
      )}
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
  grammar,
  onTokenClick,
  translationState,
  translationControls,
}: {
  readonly entries: readonly DictionaryEntry[]
  readonly tokens: readonly TokenInfo[] | null
  readonly grammar: readonly GrammarPart[] | null
  readonly onTokenClick?: (token: TokenInfo) => void
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
      <DragHandle />
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
      {grammar !== null && <GrammarSection parts={grammar} />}
      {translationState !== null && <TranslateSection state={translationState} controls={translationControls} />}
    </div>
  )
}
